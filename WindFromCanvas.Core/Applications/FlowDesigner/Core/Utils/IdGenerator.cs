using System;
using System.Linq;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Utils
{
    /// <summary>
    /// ID生成器（匹配 Activepieces 命名规范）
    /// </summary>
    public static class IdGenerator
    {
        /// <summary>
        /// 生成步骤名称（step_1, step_2, ...）
        /// </summary>
        public static string GenerateStepName(string[] existingNames)
        {
            int index = 1;
            string name = $"step_{index}";
            
            while (existingNames.Contains(name))
            {
                index++;
                name = $"step_{index}";
            }
            
            return name;
        }

        /// <summary>
        /// 生成唯一ID（GUID格式）
        /// </summary>
        public static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
