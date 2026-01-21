# FlowDesigner 流程设计器使用文档

## 概述

FlowDesigner 是一个基于 WinForms 的可视化流程设计器，支持拖拽式创建流程图，类似于 Activepieces 的流程构建器。

## 功能特性

### 基础功能
- ✅ 节点类型：开始、处理、判断、循环、结束
- ✅ 节点拖拽移动
- ✅ 连接线创建和编辑
- ✅ 节点选择（单选、多选、框选）
- ✅ 节点属性编辑
- ✅ 复制/粘贴节点
- ✅ 撤销/重做（Ctrl+Z / Ctrl+Y）
- ✅ 删除节点（Delete键）

### 画布控制
- ✅ 缩放（Ctrl+滚轮，50%-150%）
- ✅ 平移（空格+拖拽 或 中键拖拽）
- ✅ 网格背景
- ✅ 画布边界限制

### 序列化
- ✅ 保存为JSON文件
- ✅ 从JSON文件加载
- ✅ 导出为PNG图片

## 快速开始

### 1. 创建流程设计器

```csharp
using WindFromCanvas.Core.Applications.FlowDesigner;
using WindFromCanvas.Core.Applications.FlowDesigner.Widgets;

// 创建画布
var canvas = new FlowDesignerCanvas();
canvas.Dock = DockStyle.Fill;

// 创建工具箱
var toolbox = new ToolboxPanel();
toolbox.Dock = DockStyle.Left;
toolbox.Width = 150;

// 创建属性面板
var propertiesPanel = new NodePropertiesPanel();
propertiesPanel.Dock = DockStyle.Right;
propertiesPanel.Width = 200;

// 关联面板
canvas.Toolbox = toolbox;
canvas.PropertiesPanel = propertiesPanel;

// 添加到窗体
this.Controls.Add(canvas);
this.Controls.Add(toolbox);
this.Controls.Add(propertiesPanel);
```

### 2. 添加节点

```csharp
// 方式1：通过工具箱拖拽添加（推荐）
// 用户从工具箱拖拽节点类型到画布

// 方式2：程序化添加
var nodeData = new FlowNodeData
{
    Name = "node1",
    DisplayName = "处理节点",
    Type = FlowNodeType.Process,
    Position = new PointF(100, 100)
};

var node = toolbox.CreateNodeFromData(nodeData);
canvas.AddNode(node);
```

### 3. 创建连接

```csharp
// 方式1：用户拖拽创建（推荐）
// 从源节点的输出端口拖拽到目标节点的输入端口

// 方式2：程序化创建
canvas.CreateConnection(sourceNode, targetNode);
```

### 4. 保存和加载

```csharp
// 保存流程
canvas.SaveToFile("flow.json");

// 加载流程
canvas.LoadFromFile("flow.json");

// 导出为图片
canvas.ExportToPng("flow.png");
```

## 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+Z | 撤销 |
| Ctrl+Y | 重做 |
| Ctrl+C | 复制选中节点 |
| Ctrl+V | 粘贴节点 |
| Delete | 删除选中节点 |
| Ctrl+滚轮 | 缩放画布 |
| 空格+拖拽 | 平移画布 |
| 中键拖拽 | 平移画布 |

## 节点类型

### StartNode（开始节点）
- 形状：圆形
- 颜色：绿色
- 端口：只有输出端口
- 特性：固定位置，不可删除

### ProcessNode（处理节点）
- 形状：圆角矩形
- 颜色：蓝色
- 端口：输入和输出端口

### DecisionNode（判断节点）
- 形状：菱形
- 颜色：黄色
- 端口：1个输入端口，2个输出端口（True/False）

### LoopNode（循环节点）
- 形状：圆角矩形（特殊边框）
- 颜色：紫色
- 端口：输入和输出端口
- 特性：可包含子节点

### EndNode（结束节点）
- 形状：圆形
- 颜色：红色
- 端口：只有输入端口

## 数据模型

### FlowDocument
流程文档，包含所有节点和连接数据。

### FlowNodeData
节点数据，包含：
- Name: 节点唯一标识
- DisplayName: 显示名称
- Type: 节点类型
- Position: 位置
- Settings: 配置字典
- Valid: 是否有效
- Skip: 是否跳过

### FlowConnectionData
连接数据，包含：
- SourceNode: 源节点名称
- TargetNode: 目标节点名称
- SourcePort: 源端口（可选）
- TargetPort: 目标端口（可选）
- Label: 连接标签
- Type: 连接类型

## 命令系统

FlowDesigner 使用命令模式实现撤销/重做功能：

```csharp
// 执行命令
var command = new AddNodeCommand(canvas, node);
canvas.CommandManager.Execute(command);

// 撤销
canvas.CommandManager.Undo();

// 重做
canvas.CommandManager.Redo();
```

## 事件处理

```csharp
// 节点属性变化事件
propertiesPanel.NodePropertyChanged += (sender, node) =>
{
    // 处理属性变化
    canvas.Invalidate();
};
```

## 扩展开发

### 创建自定义节点类型

1. 继承 `FlowNode` 基类
2. 重写 `Draw` 方法实现自定义渲染
3. 重写 `HitTest` 方法实现自定义碰撞检测
4. 在 `ToolboxPanel.CreateNodeFromData` 中添加创建逻辑

### 添加自定义命令

1. 实现 `ICommand` 接口
2. 在 `Execute` 中实现操作
3. 在 `Undo` 中实现撤销逻辑

## 注意事项

1. 节点名称必须唯一
2. 连接只能在兼容的端口之间创建
3. 缩放和平移会影响坐标转换，使用 `ScreenToWorld` 和 `WorldToScreen` 进行转换
4. 序列化使用 DataContractJsonSerializer，确保所有数据类都标记了 `[DataContract]` 和 `[DataMember]`

## 示例代码

完整示例请参考 `WindFromCanvas/Form1.cs` 中的 `SetupFlowDesigner` 方法。
