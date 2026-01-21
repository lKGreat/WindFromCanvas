using System;
using System.Drawing;
using WindFromCanvas.Core.Applications.FlowDesigner.Models;

namespace WindFromCanvas.Core.Applications.FlowDesigner.Core.Models
{
    /// <summary>
    /// 备注模型（匹配 Activepieces 结构）
    /// </summary>
    public class Note
    {
        /// <summary>
        /// 备注ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 备注内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        public PointF Position { get; set; }

        /// <summary>
        /// 大小
        /// </summary>
        public SizeF Size { get; set; }

        /// <summary>
        /// 颜色变体
        /// </summary>
        public NoteColorVariant Color { get; set; }

        /// <summary>
        /// 所有者ID
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        public Note()
        {
            Id = Guid.NewGuid().ToString();
            Content = "<br>";
            Position = PointF.Empty;
            Size = new SizeF(200, 150);
            Color = NoteColorVariant.Blue;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
