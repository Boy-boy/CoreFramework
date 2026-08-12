# Excel 指南

> EPPlus 导入导出。`ExcelHelper`（`Core.Excel`）提供静态扩展方法，一个 `List<T>` 即可导出，一个 `Stream` 即可读回。

## 1. 定义模型

```csharp
// using Core.Excel;

public class Row
{
    [ExcelColumn("Name", 1)]
    public string Name { get; set; }

    [ExcelColumn("Age", 2)]
    public string Age { get; set; }
}
```

- `[ExcelColumn("列名", order)]` 标注在属性上，`order` 控制列顺序。
- `[ExcelCellStyle(...)]` 可选，控制表头 / 单元格样式（水平对齐、字号、背景色、数字格式）。

## 2. 导出 / 导入

```csharp
// using Core.Excel;
// using System.Collections.Generic;
// using System.IO;

var rows = new List<Row>
{
    new Row { Name = "a", Age = "1" },
    new Row { Name = "b", Age = "2" },
};

// 导出 → MemoryStream
MemoryStream ms = rows.GetExcelStream("sheet1");

// 导入 ← Stream
IList<Row> back = ms.GetExcelData<Row>("sheet1");
```

`ExcelHelper` 扩展方法一览：

| 方法 | 说明 |
|---|---|
| `IList<T>.GetExcelStream<T>(string sheetName = "sheet1")` | 按 `[ExcelColumn]` 导出单个工作表 |
| `IList<T>.GetExcelStream<T>(IList<ExcelColumn> columns, string sheetName = "sheet1")` | 用显式列定义导出 |
| `IList<T>.GetExcelStream<T>(IDictionary<string, IList<ExcelColumn>> sheets)` | 多工作表导出 |
| `Stream.GetExcelData<T>(string sheetName = "sheet1")` | 读回单个工作表（`T : class, new()`） |
| `Stream.GetExcelData<T>(IList<ExcelColumn> columns, string sheetName = "sheet1")` | 按显式列定义读回 |

需要精细控制列（列名、显示名、顺序、表头/内容样式）时，用 `ExcelColumn(name, displayName, order, headCellStyle, contentCellStyle)` 手动构造列定义。
