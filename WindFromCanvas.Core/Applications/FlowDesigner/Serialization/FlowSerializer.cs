using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Serialization
{
    /// <summary>
    /// 8.1 流程序列化器
    /// 支持版本兼容、增量保存、自动保存
    /// </summary>
    public class FlowSerializer
    {
        #region 字段

        private static readonly JsonSerializerSettings _serializerSettings;
        private readonly AutoSaveManager _autoSaveManager;
        private readonly VersionMigrator _versionMigrator;

        // 当前序列化版本
        public const string CurrentVersion = "2.0.0";

        #endregion

        #region 事件

        public event EventHandler<AutoSaveEventArgs> AutoSaved;
        public event EventHandler<SerializationErrorEventArgs> SerializationError;

        #endregion

        #region 构造

        static FlowSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Auto,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
            };
        }

        public FlowSerializer()
        {
            _autoSaveManager = new AutoSaveManager(this);
            _versionMigrator = new VersionMigrator();
        }

        #endregion

        #region 8.1.1 序列化

        /// <summary>
        /// 序列化流程版本
        /// </summary>
        public string Serialize(FlowVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            try
            {
                // 创建包装对象以包含版本信息
                var wrapper = new SerializedFlowDocument
                {
                    SchemaVersion = CurrentVersion,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    FlowVersion = version
                };

                return JsonConvert.SerializeObject(wrapper, _serializerSettings);
            }
            catch (Exception ex)
            {
                OnSerializationError(new SerializationErrorEventArgs
                {
                    Operation = "Serialize",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// 序列化为紧凑格式（不带缩进）
        /// </summary>
        public string SerializeCompact(FlowVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            var compactSettings = new JsonSerializerSettings(_serializerSettings)
            {
                Formatting = Formatting.None
            };

            var wrapper = new SerializedFlowDocument
            {
                SchemaVersion = CurrentVersion,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                FlowVersion = version
            };

            return JsonConvert.SerializeObject(wrapper, compactSettings);
        }

        #endregion

        #region 8.1.2 反序列化

        /// <summary>
        /// 反序列化流程版本
        /// </summary>
        public FlowVersion Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            try
            {
                // 首先解析为JObject以检查版本
                var jObject = JObject.Parse(json);

                // 检查是否为新格式（包含schemaVersion）
                var schemaVersion = jObject["schemaVersion"]?.Value<string>();
                
                if (schemaVersion != null)
                {
                    // 新格式：需要版本迁移检查
                    var document = JsonConvert.DeserializeObject<SerializedFlowDocument>(json, _serializerSettings);
                    
                    // 8.1.3 版本兼容处理
                    if (document.SchemaVersion != CurrentVersion)
                    {
                        document = _versionMigrator.Migrate(document, CurrentVersion);
                    }

                    return document.FlowVersion;
                }
                else
                {
                    // 旧格式：直接反序列化为FlowVersion
                    return JsonConvert.DeserializeObject<FlowVersion>(json, _serializerSettings);
                }
            }
            catch (JsonException ex)
            {
                OnSerializationError(new SerializationErrorEventArgs
                {
                    Operation = "Deserialize",
                    Exception = ex
                });
                throw new InvalidOperationException("Failed to deserialize flow: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 尝试反序列化（不抛出异常）
        /// </summary>
        public bool TryDeserialize(string json, out FlowVersion version, out string error)
        {
            version = null;
            error = null;

            try
            {
                version = Deserialize(json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 保存到文件
        /// </summary>
        public void SaveToFile(FlowVersion version, string filePath)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var json = Serialize(version);
            
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 异步保存到文件
        /// </summary>
        public async Task SaveToFileAsync(FlowVersion version, string filePath, CancellationToken cancellationToken = default)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var json = Serialize(version);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(filePath, false))
            {
                await writer.WriteAsync(json);
            }
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        public FlowVersion LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Flow file not found", filePath);

            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }

        /// <summary>
        /// 异步从文件加载
        /// </summary>
        public async Task<FlowVersion> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Flow file not found", filePath);

            using (var reader = new StreamReader(filePath))
            {
                var json = await reader.ReadToEndAsync();
                return Deserialize(json);
            }
        }

        #endregion

        #region 8.1.4 增量保存

        /// <summary>
        /// 保存差异（增量保存）
        /// </summary>
        public string SerializeDiff(FlowVersion original, FlowVersion modified)
        {
            if (original == null || modified == null)
                throw new ArgumentNullException("Original and modified versions are required");

            var diff = new FlowDiff
            {
                OriginalId = original.Id,
                ModifiedAt = DateTime.UtcNow,
                Changes = CalculateChanges(original, modified)
            };

            return JsonConvert.SerializeObject(diff, _serializerSettings);
        }

        private List<FlowChange> CalculateChanges(FlowVersion original, FlowVersion modified)
        {
            var changes = new List<FlowChange>();

            // 比较基本属性
            if (original.DisplayName != modified.DisplayName)
            {
                changes.Add(new FlowChange
                {
                    Type = ChangeType.PropertyChanged,
                    Path = "displayName",
                    OldValue = original.DisplayName,
                    NewValue = modified.DisplayName
                });
            }

            // 比较状态
            if (original.State != modified.State)
            {
                changes.Add(new FlowChange
                {
                    Type = ChangeType.PropertyChanged,
                    Path = "state",
                    OldValue = original.State.ToString(),
                    NewValue = modified.State.ToString()
                });
            }

            // TODO: 深度比较trigger和steps

            return changes;
        }

        #endregion

        #region 8.1.5 自动保存

        /// <summary>
        /// 启动自动保存
        /// </summary>
        public void StartAutoSave(FlowVersion version, string filePath, int intervalSeconds = 30)
        {
            _autoSaveManager.Start(version, filePath, intervalSeconds);
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void StopAutoSave()
        {
            _autoSaveManager.Stop();
        }

        /// <summary>
        /// 更新自动保存的数据
        /// </summary>
        public void UpdateAutoSaveData(FlowVersion version)
        {
            _autoSaveManager.UpdateData(version);
        }

        internal void OnAutoSaved(string filePath, bool success, string error = null)
        {
            AutoSaved?.Invoke(this, new AutoSaveEventArgs
            {
                FilePath = filePath,
                Success = success,
                Error = error,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region 泛型方法

        /// <summary>
        /// 泛型序列化方法
        /// </summary>
        public string Serialize<T>(T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return JsonConvert.SerializeObject(obj, _serializerSettings);
        }

        /// <summary>
        /// 泛型反序列化方法
        /// </summary>
        public T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }

        #endregion

        #region 辅助方法

        private void OnSerializationError(SerializationErrorEventArgs e)
        {
            SerializationError?.Invoke(this, e);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 序列化的流程文档
    /// </summary>
    public class SerializedFlowDocument
    {
        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        [JsonProperty("flowVersion")]
        public FlowVersion FlowVersion { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 流程差异
    /// </summary>
    public class FlowDiff
    {
        [JsonProperty("originalId")]
        public string OriginalId { get; set; }

        [JsonProperty("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        [JsonProperty("changes")]
        public List<FlowChange> Changes { get; set; } = new List<FlowChange>();
    }

    /// <summary>
    /// 流程变更
    /// </summary>
    public class FlowChange
    {
        [JsonProperty("type")]
        public ChangeType Type { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("oldValue")]
        public object OldValue { get; set; }

        [JsonProperty("newValue")]
        public object NewValue { get; set; }
    }

    /// <summary>
    /// 变更类型
    /// </summary>
    public enum ChangeType
    {
        PropertyChanged,
        NodeAdded,
        NodeRemoved,
        NodeMoved,
        ConnectionAdded,
        ConnectionRemoved
    }

    #endregion

    #region 事件参数

    public class AutoSaveEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SerializationErrorEventArgs : EventArgs
    {
        public string Operation { get; set; }
        public Exception Exception { get; set; }
    }

    #endregion

    #region 8.1.3 版本迁移器

    /// <summary>
    /// 版本迁移器 - 处理不同版本间的数据兼容
    /// </summary>
    public class VersionMigrator
    {
        private readonly Dictionary<string, Func<SerializedFlowDocument, SerializedFlowDocument>> _migrators;

        public VersionMigrator()
        {
            _migrators = new Dictionary<string, Func<SerializedFlowDocument, SerializedFlowDocument>>
            {
                { "1.0.0->1.1.0", MigrateFrom100To110 },
                { "1.1.0->2.0.0", MigrateFrom110To200 }
            };
        }

        /// <summary>
        /// 迁移文档到目标版本
        /// </summary>
        public SerializedFlowDocument Migrate(SerializedFlowDocument document, string targetVersion)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var currentVersion = document.SchemaVersion ?? "1.0.0";
            
            while (currentVersion != targetVersion)
            {
                var nextVersion = GetNextVersion(currentVersion, targetVersion);
                if (nextVersion == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot migrate from version {0} to {1}", currentVersion, targetVersion));
                }

                var migrationKey = string.Format("{0}->{1}", currentVersion, nextVersion);
                if (_migrators.TryGetValue(migrationKey, out var migrator))
                {
                    document = migrator(document);
                    document.SchemaVersion = nextVersion;
                }
                else
                {
                    // 如果没有迁移器，假设兼容
                    document.SchemaVersion = nextVersion;
                }

                currentVersion = nextVersion;
            }

            return document;
        }

        private string GetNextVersion(string current, string target)
        {
            var versions = new[] { "1.0.0", "1.1.0", "2.0.0" };
            var currentIndex = Array.IndexOf(versions, current);
            var targetIndex = Array.IndexOf(versions, target);

            if (currentIndex < 0 || targetIndex < 0)
                return null;

            if (currentIndex < targetIndex && currentIndex + 1 < versions.Length)
                return versions[currentIndex + 1];

            return null;
        }

        private SerializedFlowDocument MigrateFrom100To110(SerializedFlowDocument document)
        {
            // 1.0.0 -> 1.1.0 迁移逻辑
            // 例如：添加新字段默认值
            if (document.Metadata == null)
            {
                document.Metadata = new Dictionary<string, object>();
            }
            return document;
        }

        private SerializedFlowDocument MigrateFrom110To200(SerializedFlowDocument document)
        {
            // 1.1.0 -> 2.0.0 迁移逻辑
            // 例如：重构数据结构
            return document;
        }
    }

    #endregion

    #region 8.1.5 自动保存管理器

    /// <summary>
    /// 自动保存管理器
    /// </summary>
    public class AutoSaveManager
    {
        private readonly FlowSerializer _serializer;
        private Timer _timer;
        private FlowVersion _currentVersion;
        private string _filePath;
        private bool _isDirty;
        private readonly object _lock = new object();

        public AutoSaveManager(FlowSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// 启动自动保存
        /// </summary>
        public void Start(FlowVersion version, string filePath, int intervalSeconds)
        {
            Stop();

            _currentVersion = version;
            _filePath = filePath;
            _isDirty = false;

            _timer = new Timer(OnTimerElapsed, null, 
                TimeSpan.FromSeconds(intervalSeconds), 
                TimeSpan.FromSeconds(intervalSeconds));
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// 更新数据并标记为脏
        /// </summary>
        public void UpdateData(FlowVersion version)
        {
            lock (_lock)
            {
                _currentVersion = version;
                _isDirty = true;
            }
        }

        private void OnTimerElapsed(object state)
        {
            lock (_lock)
            {
                if (!_isDirty || _currentVersion == null || string.IsNullOrEmpty(_filePath))
                    return;

                try
                {
                    // 保存到临时文件，然后替换
                    var tempPath = _filePath + ".tmp";
                    _serializer.SaveToFile(_currentVersion, tempPath);

                    // 备份现有文件
                    if (File.Exists(_filePath))
                    {
                        var backupPath = _filePath + ".bak";
                        File.Copy(_filePath, backupPath, true);
                    }

                    // 替换原文件
                    if (File.Exists(_filePath))
                    {
                        File.Delete(_filePath);
                    }
                    File.Move(tempPath, _filePath);

                    _isDirty = false;
                    _serializer.OnAutoSaved(_filePath, true);
                }
                catch (Exception ex)
                {
                    _serializer.OnAutoSaved(_filePath, false, ex.Message);
                }
            }
        }
    }

    #endregion
}
