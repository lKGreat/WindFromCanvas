using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    #region 版本管理事件参数

    /// <summary>
    /// 快照创建事件参数
    /// </summary>
    public class SnapshotCreatedEventArgs : EventArgs
    {
        public FlowSnapshot Snapshot { get; set; }
    }

    /// <summary>
    /// 版本变更事件参数
    /// </summary>
    public class VersionChangedEventArgs : EventArgs
    {
        public string FlowId { get; set; }
        public FlowVersion Version { get; set; }
        public VersionChangeType ChangeType { get; set; }
    }

    /// <summary>
    /// 版本变更类型
    /// </summary>
    public enum VersionChangeType
    {
        Created,
        Updated,
        Deleted,
        Published,
        Rollback
    }

    #endregion

    #region 快照模型

    /// <summary>
    /// 流程快照
    /// </summary>
    public class FlowSnapshot
    {
        /// <summary>
        /// 快照ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 流程ID
        /// </summary>
        public string FlowId { get; set; }

        /// <summary>
        /// 快照名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 快照描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 快照数据（JSON序列化的FlowDocument）
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 是否为自动保存
        /// </summary>
        public bool IsAutoSave { get; set; }

        /// <summary>
        /// 节点数量
        /// </summary>
        public int NodeCount { get; set; }

        /// <summary>
        /// 连接数量
        /// </summary>
        public int ConnectionCount { get; set; }
    }

    #endregion

    #region 版本对比结果

    /// <summary>
    /// 版本对比结果
    /// </summary>
    public class VersionCompareResult
    {
        /// <summary>
        /// 源版本ID
        /// </summary>
        public string SourceVersionId { get; set; }

        /// <summary>
        /// 目标版本ID
        /// </summary>
        public string TargetVersionId { get; set; }

        /// <summary>
        /// 新增的节点
        /// </summary>
        public List<FlowNodeData> AddedNodes { get; set; } = new List<FlowNodeData>();

        /// <summary>
        /// 删除的节点
        /// </summary>
        public List<FlowNodeData> RemovedNodes { get; set; } = new List<FlowNodeData>();

        /// <summary>
        /// 修改的节点
        /// </summary>
        public List<NodeChange> ModifiedNodes { get; set; } = new List<NodeChange>();

        /// <summary>
        /// 新增的连接
        /// </summary>
        public List<FlowConnectionData> AddedConnections { get; set; } = new List<FlowConnectionData>();

        /// <summary>
        /// 删除的连接
        /// </summary>
        public List<FlowConnectionData> RemovedConnections { get; set; } = new List<FlowConnectionData>();

        /// <summary>
        /// 是否有变更
        /// </summary>
        public bool HasChanges => AddedNodes.Count > 0 || RemovedNodes.Count > 0 || 
                                  ModifiedNodes.Count > 0 || AddedConnections.Count > 0 || 
                                  RemovedConnections.Count > 0;

        /// <summary>
        /// 变更摘要
        /// </summary>
        public string Summary => $"新增节点: {AddedNodes.Count}, 删除节点: {RemovedNodes.Count}, " +
                                $"修改节点: {ModifiedNodes.Count}, 新增连接: {AddedConnections.Count}, " +
                                $"删除连接: {RemovedConnections.Count}";
    }

    /// <summary>
    /// 节点变更
    /// </summary>
    public class NodeChange
    {
        public string NodeName { get; set; }
        public List<PropertyChange> PropertyChanges { get; set; } = new List<PropertyChange>();
    }

    /// <summary>
    /// 属性变更
    /// </summary>
    public class PropertyChange
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    #endregion

    /// <summary>
    /// 版本管理器（匹配 Activepieces 版本管理功能）
    /// 支持快照、回退、对比功能
    /// </summary>
    public class VersionManager
    {
        private static VersionManager _instance;
        private static readonly object _instanceLock = new object();

        public static VersionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new VersionManager();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, List<FlowVersion>> _flowVersions;
        private readonly Dictionary<string, List<FlowSnapshot>> _snapshots;
        private readonly FlowSerializer _serializer;
        private readonly FlowDocumentConverter _documentConverter;

        /// <summary>
        /// 最大快照数量（每个流程）
        /// </summary>
        public int MaxSnapshotsPerFlow { get; set; } = 50;

        /// <summary>
        /// 自动保存间隔（分钟）
        /// </summary>
        public int AutoSaveIntervalMinutes { get; set; } = 5;

        #region 事件

        /// <summary>
        /// 快照创建事件
        /// </summary>
        public event EventHandler<SnapshotCreatedEventArgs> SnapshotCreated;

        /// <summary>
        /// 版本变更事件
        /// </summary>
        public event EventHandler<VersionChangedEventArgs> VersionChanged;

        #endregion

        private VersionManager()
        {
            _flowVersions = new Dictionary<string, List<FlowVersion>>();
            _snapshots = new Dictionary<string, List<FlowSnapshot>>();
            _serializer = new FlowSerializer();
            _documentConverter = new FlowDocumentConverter();
        }

        #region 版本管理

        /// <summary>
        /// 保存版本
        /// </summary>
        public void SaveVersion(FlowVersion version)
        {
            if (string.IsNullOrEmpty(version.FlowId))
            {
                version.FlowId = Guid.NewGuid().ToString();
            }

            if (!_flowVersions.ContainsKey(version.FlowId))
            {
                _flowVersions[version.FlowId] = new List<FlowVersion>();
            }

            var existingIndex = _flowVersions[version.FlowId].FindIndex(v => v.Id == version.Id);
            var changeType = existingIndex >= 0 ? VersionChangeType.Updated : VersionChangeType.Created;

            if (existingIndex >= 0)
            {
                version.UpdatedAt = DateTime.Now;
                _flowVersions[version.FlowId][existingIndex] = version;
            }
            else
            {
                version.CreatedAt = DateTime.Now;
                version.UpdatedAt = DateTime.Now;
                _flowVersions[version.FlowId].Add(version);
            }

            VersionChanged?.Invoke(this, new VersionChangedEventArgs
            {
                FlowId = version.FlowId,
                Version = version,
                ChangeType = changeType
            });
        }

        /// <summary>
        /// 获取流程的所有版本
        /// </summary>
        public List<FlowVersion> GetVersions(string flowId)
        {
            if (_flowVersions.ContainsKey(flowId))
            {
                return _flowVersions[flowId].OrderByDescending(v => v.CreatedAt).ToList();
            }
            return new List<FlowVersion>();
        }

        /// <summary>
        /// 获取指定版本
        /// </summary>
        public FlowVersion GetVersion(string flowId, string versionId)
        {
            if (_flowVersions.ContainsKey(flowId))
            {
                return _flowVersions[flowId].FirstOrDefault(v => v.Id == versionId);
            }
            return null;
        }

        /// <summary>
        /// 创建新版本（从现有版本复制）
        /// </summary>
        public FlowVersion CreateVersionFrom(string flowId, string sourceVersionId, string newDisplayName = null)
        {
            var sourceVersion = GetVersion(flowId, sourceVersionId);
            if (sourceVersion == null)
            {
                throw new ArgumentException($"Source version '{sourceVersionId}' not found");
            }

            var json = _serializer.Serialize(sourceVersion);
            var newVersion = _serializer.Deserialize(json);
            
            newVersion.Id = Guid.NewGuid().ToString();
            newVersion.DisplayName = newDisplayName ?? $"{sourceVersion.DisplayName} (副本)";
            newVersion.State = FlowVersionState.DRAFT;
            newVersion.CreatedAt = DateTime.Now;
            newVersion.UpdatedAt = DateTime.Now;

            SaveVersion(newVersion);
            return newVersion;
        }

        /// <summary>
        /// 删除版本
        /// </summary>
        public bool DeleteVersion(string flowId, string versionId)
        {
            if (_flowVersions.ContainsKey(flowId))
            {
                var version = _flowVersions[flowId].FirstOrDefault(v => v.Id == versionId);
                if (version != null)
                {
                    var result = _flowVersions[flowId].Remove(version);
                    if (result)
                    {
                        VersionChanged?.Invoke(this, new VersionChangedEventArgs
                        {
                            FlowId = flowId,
                            Version = version,
                            ChangeType = VersionChangeType.Deleted
                        });
                    }
                    return result;
                }
            }
            return false;
        }

        /// <summary>
        /// 发布版本
        /// </summary>
        public void PublishVersion(string flowId, string versionId)
        {
            var version = GetVersion(flowId, versionId);
            if (version != null)
            {
                version.State = FlowVersionState.LOCKED;
                version.UpdatedAt = DateTime.Now;
                SaveVersion(version);

                VersionChanged?.Invoke(this, new VersionChangedEventArgs
                {
                    FlowId = flowId,
                    Version = version,
                    ChangeType = VersionChangeType.Published
                });
            }
        }

        /// <summary>
        /// 使用版本作为草稿
        /// </summary>
        public FlowVersion UseAsDraft(string flowId, string versionId)
        {
            var sourceVersion = GetVersion(flowId, versionId);
            if (sourceVersion == null)
            {
                throw new ArgumentException($"Version '{versionId}' not found");
            }

            return CreateVersionFrom(flowId, versionId, sourceVersion.DisplayName);
        }

        #endregion

        #region 快照管理

        /// <summary>
        /// 创建快照
        /// </summary>
        public FlowSnapshot CreateSnapshot(FlowDocument document, string name = null, string description = null, bool isAutoSave = false)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var flowId = document.Id;

            if (!_snapshots.ContainsKey(flowId))
            {
                _snapshots[flowId] = new List<FlowSnapshot>();
            }

            // 序列化文档
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(document, Newtonsoft.Json.Formatting.None);

            var snapshot = new FlowSnapshot
            {
                Id = Guid.NewGuid().ToString(),
                FlowId = flowId,
                Name = name ?? $"快照 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Description = description,
                CreatedAt = DateTime.Now,
                Data = data,
                IsAutoSave = isAutoSave,
                NodeCount = document.Nodes?.Count ?? 0,
                ConnectionCount = document.Connections?.Count ?? 0
            };

            _snapshots[flowId].Add(snapshot);

            // 清理旧快照
            CleanupOldSnapshots(flowId);

            SnapshotCreated?.Invoke(this, new SnapshotCreatedEventArgs { Snapshot = snapshot });

            return snapshot;
        }

        /// <summary>
        /// 获取快照列表
        /// </summary>
        public List<FlowSnapshot> GetSnapshots(string flowId)
        {
            if (_snapshots.ContainsKey(flowId))
            {
                return _snapshots[flowId].OrderByDescending(s => s.CreatedAt).ToList();
            }
            return new List<FlowSnapshot>();
        }

        /// <summary>
        /// 获取指定快照
        /// </summary>
        public FlowSnapshot GetSnapshot(string flowId, string snapshotId)
        {
            if (_snapshots.ContainsKey(flowId))
            {
                return _snapshots[flowId].FirstOrDefault(s => s.Id == snapshotId);
            }
            return null;
        }

        /// <summary>
        /// 删除快照
        /// </summary>
        public bool DeleteSnapshot(string flowId, string snapshotId)
        {
            if (_snapshots.ContainsKey(flowId))
            {
                var snapshot = _snapshots[flowId].FirstOrDefault(s => s.Id == snapshotId);
                if (snapshot != null)
                {
                    return _snapshots[flowId].Remove(snapshot);
                }
            }
            return false;
        }

        /// <summary>
        /// 从快照恢复文档
        /// </summary>
        public FlowDocument RestoreFromSnapshot(string flowId, string snapshotId)
        {
            var snapshot = GetSnapshot(flowId, snapshotId);
            if (snapshot == null)
            {
                throw new ArgumentException($"Snapshot '{snapshotId}' not found");
            }

            try
            {
                var document = Newtonsoft.Json.JsonConvert.DeserializeObject<FlowDocument>(snapshot.Data);
                return document;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore from snapshot: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理旧快照
        /// </summary>
        private void CleanupOldSnapshots(string flowId)
        {
            if (!_snapshots.ContainsKey(flowId))
                return;

            var snapshots = _snapshots[flowId];
            if (snapshots.Count <= MaxSnapshotsPerFlow)
                return;

            // 保留最新的快照，删除自动保存的旧快照优先
            var autoSaveSnapshots = snapshots.Where(s => s.IsAutoSave).OrderBy(s => s.CreatedAt).ToList();
            var manualSnapshots = snapshots.Where(s => !s.IsAutoSave).ToList();

            int toRemove = snapshots.Count - MaxSnapshotsPerFlow;
            foreach (var snapshot in autoSaveSnapshots.Take(toRemove))
            {
                snapshots.Remove(snapshot);
                toRemove--;
                if (toRemove <= 0) break;
            }
        }

        #endregion

        #region 版本回退

        /// <summary>
        /// 回退到指定版本
        /// </summary>
        public FlowVersion RollbackToVersion(string flowId, string versionId)
        {
            var targetVersion = GetVersion(flowId, versionId);
            if (targetVersion == null)
            {
                throw new ArgumentException($"Version '{versionId}' not found");
            }

            // 创建新的草稿版本
            var newVersion = CreateVersionFrom(flowId, versionId, $"{targetVersion.DisplayName} (回退)");
            newVersion.State = FlowVersionState.DRAFT;

            VersionChanged?.Invoke(this, new VersionChangedEventArgs
            {
                FlowId = flowId,
                Version = newVersion,
                ChangeType = VersionChangeType.Rollback
            });

            return newVersion;
        }

        /// <summary>
        /// 回退到指定快照
        /// </summary>
        public FlowDocument RollbackToSnapshot(string flowId, string snapshotId)
        {
            var document = RestoreFromSnapshot(flowId, snapshotId);
            
            // 创建回退快照记录
            CreateSnapshot(document, "回退前状态", $"回退到快照 {snapshotId}", false);

            return document;
        }

        #endregion

        #region 版本对比

        /// <summary>
        /// 对比两个版本
        /// </summary>
        public VersionCompareResult CompareVersions(string flowId, string sourceVersionId, string targetVersionId)
        {
            var sourceVersion = GetVersion(flowId, sourceVersionId);
            var targetVersion = GetVersion(flowId, targetVersionId);

            if (sourceVersion == null || targetVersion == null)
            {
                throw new ArgumentException("One or both versions not found");
            }

            // 转换为FlowDocument进行对比
            var sourceDoc = _documentConverter.ConvertToDocument(sourceVersion);
            var targetDoc = _documentConverter.ConvertToDocument(targetVersion);

            return CompareDocuments(sourceDoc, targetDoc, sourceVersionId, targetVersionId);
        }

        /// <summary>
        /// 对比两个文档
        /// </summary>
        public VersionCompareResult CompareDocuments(FlowDocument sourceDoc, FlowDocument targetDoc, 
            string sourceId = null, string targetId = null)
        {
            var result = new VersionCompareResult
            {
                SourceVersionId = sourceId ?? sourceDoc.Id,
                TargetVersionId = targetId ?? targetDoc.Id
            };

            var sourceNodes = sourceDoc.Nodes?.ToDictionary(n => n.Name) ?? new Dictionary<string, FlowNodeData>();
            var targetNodes = targetDoc.Nodes?.ToDictionary(n => n.Name) ?? new Dictionary<string, FlowNodeData>();

            // 查找新增的节点
            foreach (var kvp in targetNodes)
            {
                if (!sourceNodes.ContainsKey(kvp.Key))
                {
                    result.AddedNodes.Add(kvp.Value);
                }
            }

            // 查找删除的节点
            foreach (var kvp in sourceNodes)
            {
                if (!targetNodes.ContainsKey(kvp.Key))
                {
                    result.RemovedNodes.Add(kvp.Value);
                }
            }

            // 查找修改的节点
            foreach (var kvp in sourceNodes)
            {
                if (targetNodes.TryGetValue(kvp.Key, out var targetNode))
                {
                    var changes = CompareNodes(kvp.Value, targetNode);
                    if (changes.PropertyChanges.Count > 0)
                    {
                        result.ModifiedNodes.Add(changes);
                    }
                }
            }

            // 对比连接
            var sourceConns = sourceDoc.Connections ?? new List<FlowConnectionData>();
            var targetConns = targetDoc.Connections ?? new List<FlowConnectionData>();

            var sourceConnKeys = new HashSet<string>(sourceConns.Select(c => $"{c.SourceNode}->{c.TargetNode}"));
            var targetConnKeys = new HashSet<string>(targetConns.Select(c => $"{c.SourceNode}->{c.TargetNode}"));

            foreach (var conn in targetConns)
            {
                var key = $"{conn.SourceNode}->{conn.TargetNode}";
                if (!sourceConnKeys.Contains(key))
                {
                    result.AddedConnections.Add(conn);
                }
            }

            foreach (var conn in sourceConns)
            {
                var key = $"{conn.SourceNode}->{conn.TargetNode}";
                if (!targetConnKeys.Contains(key))
                {
                    result.RemovedConnections.Add(conn);
                }
            }

            return result;
        }

        /// <summary>
        /// 对比两个节点
        /// </summary>
        private NodeChange CompareNodes(FlowNodeData source, FlowNodeData target)
        {
            var changes = new NodeChange { NodeName = source.Name };

            // 对比基本属性
            if (source.DisplayName != target.DisplayName)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = "DisplayName",
                    OldValue = source.DisplayName,
                    NewValue = target.DisplayName
                });
            }

            if (source.Type != target.Type)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = "Type",
                    OldValue = source.Type,
                    NewValue = target.Type
                });
            }

            if (Math.Abs(source.PositionX - target.PositionX) > 0.1f || 
                Math.Abs(source.PositionY - target.PositionY) > 0.1f)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = "Position",
                    OldValue = $"({source.PositionX}, {source.PositionY})",
                    NewValue = $"({target.PositionX}, {target.PositionY})"
                });
            }

            if (source.Skip != target.Skip)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = "Skip",
                    OldValue = source.Skip,
                    NewValue = target.Skip
                });
            }

            // 对比自定义属性
            var sourceProps = source.Properties ?? new Dictionary<string, object>();
            var targetProps = target.Properties ?? new Dictionary<string, object>();

            foreach (var kvp in targetProps)
            {
                object sourceValue = null;
                bool hasSourceValue = sourceProps.TryGetValue(kvp.Key, out sourceValue);
                if (!hasSourceValue || !Equals(sourceValue, kvp.Value))
                {
                    changes.PropertyChanges.Add(new PropertyChange
                    {
                        PropertyName = $"Properties.{kvp.Key}",
                        OldValue = sourceValue,
                        NewValue = kvp.Value
                    });
                }
            }

            foreach (var kvp in sourceProps)
            {
                if (!targetProps.ContainsKey(kvp.Key))
                {
                    changes.PropertyChanges.Add(new PropertyChange
                    {
                        PropertyName = $"Properties.{kvp.Key}",
                        OldValue = kvp.Value,
                        NewValue = null
                    });
                }
            }

            return changes;
        }

        /// <summary>
        /// 对比两个快照
        /// </summary>
        public VersionCompareResult CompareSnapshots(string flowId, string sourceSnapshotId, string targetSnapshotId)
        {
            var sourceDoc = RestoreFromSnapshot(flowId, sourceSnapshotId);
            var targetDoc = RestoreFromSnapshot(flowId, targetSnapshotId);

            return CompareDocuments(sourceDoc, targetDoc, sourceSnapshotId, targetSnapshotId);
        }

        #endregion
    }
}
