using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Adapters
{
    /// <summary>
    /// 适配器管理器
    /// 管理和协调多个图数据适配器
    /// </summary>
    public class AdapterManager
    {
        private readonly Dictionary<string, IGraphAdapter> _adapters = new Dictionary<string, IGraphAdapter>(StringComparer.OrdinalIgnoreCase);
        private static AdapterManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static AdapterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AdapterManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private AdapterManager()
        {
            // 注册内置适配器
            RegisterBuiltInAdapters();
        }

        /// <summary>
        /// 注册内置适配器
        /// </summary>
        private void RegisterBuiltInAdapters()
        {
            Register(new JsonGraphAdapter());
        }

        /// <summary>
        /// 注册适配器
        /// </summary>
        public void Register(IGraphAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            _adapters[adapter.Name] = adapter;
        }

        /// <summary>
        /// 注销适配器
        /// </summary>
        public bool Unregister(string adapterName)
        {
            return _adapters.Remove(adapterName);
        }

        /// <summary>
        /// 获取适配器
        /// </summary>
        public IGraphAdapter GetAdapter(string name)
        {
            _adapters.TryGetValue(name, out var adapter);
            return adapter;
        }

        /// <summary>
        /// 根据文件扩展名获取适配器
        /// </summary>
        public IGraphAdapter GetAdapterByExtension(string extension)
        {
            return _adapters.Values.FirstOrDefault(a => a.CanHandle(extension));
        }

        /// <summary>
        /// 根据MIME类型获取适配器
        /// </summary>
        public IGraphAdapter GetAdapterByMimeType(string mimeType)
        {
            return _adapters.Values.FirstOrDefault(a => 
                string.Equals(a.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取所有注册的适配器
        /// </summary>
        public IEnumerable<IGraphAdapter> GetAllAdapters()
        {
            return _adapters.Values;
        }

        /// <summary>
        /// 从文件导入图数据
        /// </summary>
        public AdapterResult<GraphData> ImportFromFile(string filePath, AdapterOptions options = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return AdapterResult<GraphData>.Fail("File path is empty");

            if (!File.Exists(filePath))
                return AdapterResult<GraphData>.Fail($"File not found: {filePath}");

            var extension = Path.GetExtension(filePath);
            var adapter = GetAdapterByExtension(extension);

            if (adapter == null)
                return AdapterResult<GraphData>.Fail($"No adapter found for extension: {extension}");

            try
            {
                var content = File.ReadAllText(filePath);
                return adapter.AdapterIn(content, options ?? AdapterOptions.Default);
            }
            catch (Exception ex)
            {
                return AdapterResult<GraphData>.Fail($"Error reading file: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出图数据到文件
        /// </summary>
        public AdapterResult<string> ExportToFile(GraphData data, string filePath, AdapterOptions options = null)
        {
            if (data == null)
                return AdapterResult<string>.Fail("Data is null");

            if (string.IsNullOrEmpty(filePath))
                return AdapterResult<string>.Fail("File path is empty");

            var extension = Path.GetExtension(filePath);
            var adapter = GetAdapterByExtension(extension);

            if (adapter == null)
                return AdapterResult<string>.Fail($"No adapter found for extension: {extension}");

            try
            {
                var result = adapter.AdapterOut(data, options ?? AdapterOptions.Default);
                
                if (result.Success)
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllText(filePath, result.Data);
                }

                return result;
            }
            catch (Exception ex)
            {
                return AdapterResult<string>.Fail($"Error writing file: {ex.Message}");
            }
        }

        /// <summary>
        /// 从字符串导入图数据（自动检测格式）
        /// </summary>
        public AdapterResult<GraphData> Import(string content, string formatHint = null, AdapterOptions options = null)
        {
            if (string.IsNullOrEmpty(content))
                return AdapterResult<GraphData>.Fail("Content is empty");

            IGraphAdapter adapter = null;

            // 如果提供了格式提示，优先使用
            if (!string.IsNullOrEmpty(formatHint))
            {
                adapter = GetAdapterByExtension(formatHint) ?? GetAdapter(formatHint);
            }

            // 自动检测格式
            if (adapter == null)
            {
                adapter = DetectFormat(content);
            }

            if (adapter == null)
                return AdapterResult<GraphData>.Fail("Unable to detect format or no suitable adapter found");

            return adapter.AdapterIn(content, options ?? AdapterOptions.Default);
        }

        /// <summary>
        /// 导出图数据到字符串
        /// </summary>
        public AdapterResult<string> Export(GraphData data, string adapterName, AdapterOptions options = null)
        {
            if (data == null)
                return AdapterResult<string>.Fail("Data is null");

            var adapter = GetAdapter(adapterName);
            if (adapter == null)
                return AdapterResult<string>.Fail($"Adapter not found: {adapterName}");

            return adapter.AdapterOut(data, options ?? AdapterOptions.Default);
        }

        /// <summary>
        /// 转换格式
        /// </summary>
        public AdapterResult<string> Convert(string content, string sourceFormat, string targetFormat, AdapterOptions options = null)
        {
            // 导入
            var sourceAdapter = GetAdapterByExtension(sourceFormat) ?? GetAdapter(sourceFormat);
            if (sourceAdapter == null)
                return AdapterResult<string>.Fail($"Source adapter not found: {sourceFormat}");

            var importResult = sourceAdapter.AdapterIn(content, options ?? AdapterOptions.Default);
            if (!importResult.Success)
                return AdapterResult<string>.Fail($"Import failed: {importResult.ErrorMessage}");

            // 导出
            var targetAdapter = GetAdapterByExtension(targetFormat) ?? GetAdapter(targetFormat);
            if (targetAdapter == null)
                return AdapterResult<string>.Fail($"Target adapter not found: {targetFormat}");

            return targetAdapter.AdapterOut(importResult.Data, options ?? AdapterOptions.Default);
        }

        /// <summary>
        /// 验证内容格式
        /// </summary>
        public ValidationResult Validate(string content, string format = null)
        {
            if (string.IsNullOrEmpty(content))
                return ValidationResult.Invalid("Content is empty");

            IGraphAdapter adapter = null;

            if (!string.IsNullOrEmpty(format))
            {
                adapter = GetAdapterByExtension(format) ?? GetAdapter(format);
            }
            else
            {
                adapter = DetectFormat(content);
            }

            if (adapter == null)
                return ValidationResult.Invalid("Unable to detect format or no suitable adapter found");

            return adapter.Validate(content);
        }

        /// <summary>
        /// 检测内容格式
        /// </summary>
        private IGraphAdapter DetectFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var trimmed = content.TrimStart();

            // 检测JSON
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                return GetAdapter("JSON");
            }

            // 检测XML
            if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<"))
            {
                // 可以在这里返回XML适配器（如果有的话）
                return null;
            }

            return null;
        }

        /// <summary>
        /// 获取支持的文件过滤器（用于文件对话框）
        /// </summary>
        public string GetFileFilter()
        {
            var filters = new List<string>();
            
            foreach (var adapter in _adapters.Values)
            {
                var extensions = string.Join(";", adapter.SupportedExtensions.Select(e => $"*{e}"));
                filters.Add($"{adapter.Name} Files ({extensions})|{extensions}");
            }

            // 添加所有支持的格式
            var allExtensions = string.Join(";", _adapters.Values.SelectMany(a => a.SupportedExtensions.Select(e => $"*{e}")));
            filters.Insert(0, $"All Supported Formats|{allExtensions}");

            // 添加所有文件选项
            filters.Add("All Files (*.*)|*.*");

            return string.Join("|", filters);
        }
    }
}
