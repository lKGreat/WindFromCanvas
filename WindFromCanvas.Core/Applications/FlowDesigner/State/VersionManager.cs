using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Models;
using WindFromCanvas.Core.Applications.FlowDesigner.Core.Enums;
using WindFromCanvas.Core.Applications.FlowDesigner.Serialization;

namespace WindFromCanvas.Core.Applications.FlowDesigner.State
{
    /// <summary>
    /// 版本管理器（匹配 Activepieces 版本管理功能）
    /// </summary>
    public class VersionManager
    {
        private static VersionManager _instance;
        public static VersionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new VersionManager();
                return _instance;
            }
        }

        private Dictionary<string, List<FlowVersion>> _flowVersions;
        private FlowSerializer _serializer;

        private VersionManager()
        {
            _flowVersions = new Dictionary<string, List<FlowVersion>>();
            _serializer = new FlowSerializer();
        }

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

            // 检查是否已存在相同ID的版本
            var existingIndex = _flowVersions[version.FlowId].FindIndex(v => v.Id == version.Id);
            if (existingIndex >= 0)
            {
                // 更新现有版本
                version.UpdatedAt = DateTime.Now;
                _flowVersions[version.FlowId][existingIndex] = version;
            }
            else
            {
                // 添加新版本
                version.CreatedAt = DateTime.Now;
                version.UpdatedAt = DateTime.Now;
                _flowVersions[version.FlowId].Add(version);
            }
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

            // 深拷贝版本
            var json = _serializer.Serialize(sourceVersion);
            var newVersion = _serializer.Deserialize(json);
            
            // 设置新版本属性
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
                    return _flowVersions[flowId].Remove(version);
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
                // 锁定版本
                version.State = FlowVersionState.LOCKED;
                version.UpdatedAt = DateTime.Now;
                SaveVersion(version);
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

            // 创建草稿版本
            return CreateVersionFrom(flowId, versionId, sourceVersion.DisplayName);
        }
    }
}
