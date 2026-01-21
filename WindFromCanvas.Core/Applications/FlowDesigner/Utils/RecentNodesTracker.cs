using System;
using System.Collections.Generic;
using System.Linq;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Utils
{
    /// <summary>
    /// 最近使用节点跟踪器（MRU - Most Recently Used）
    /// </summary>
    public class RecentNodesTracker
    {
        private static RecentNodesTracker _instance;
        private List<FlowNodeType> _recentNodes = new List<FlowNodeType>();
        private const int MaxRecentCount = 10;

        public static RecentNodesTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RecentNodesTracker();
                }
                return _instance;
            }
        }

        private RecentNodesTracker()
        {
        }

        /// <summary>
        /// 记录节点使用
        /// </summary>
        public void RecordNodeUsage(FlowNodeType nodeType)
        {
            // 移除已存在的相同类型
            _recentNodes.Remove(nodeType);
            
            // 添加到顶部
            _recentNodes.Insert(0, nodeType);
            
            // 限制数量
            if (_recentNodes.Count > MaxRecentCount)
            {
                _recentNodes.RemoveAt(_recentNodes.Count - 1);
            }
        }

        /// <summary>
        /// 获取最近使用的节点列表
        /// </summary>
        public List<FlowNodeType> GetRecentNodes(int count = 5)
        {
            return _recentNodes.Take(count).ToList();
        }

        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void Clear()
        {
            _recentNodes.Clear();
        }
    }
}
