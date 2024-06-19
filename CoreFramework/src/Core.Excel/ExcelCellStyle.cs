using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Core.Excel
{
    public struct ExcelCellStyle
    {
        /// <summary>
        /// 水平对齐
        /// </summary>
        public ExcelHorizontalAlignment? HorizontalAlignment { get; set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public int? FontSize { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public System.Drawing.Color? BackgroundColor { get; set; }

        /// <summary>
        /// 数字格式
        /// </summary>
        public string NumberFormat { get; set; }

        public void ConfigCellStyle(ExcelRange cell)
        {
            if (HorizontalAlignment.HasValue)
            {
                cell.Style.HorizontalAlignment = HorizontalAlignment.Value;
            }

            if (FontSize.HasValue)
            {
                cell.Style.Font.Size = FontSize.Value;
            }

            if (BackgroundColor.HasValue)
            {
                // 设置背景色
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(BackgroundColor.Value);
            }

            if (!string.IsNullOrEmpty(NumberFormat))
            {
                cell.Style.Numberformat.Format = NumberFormat;
            }
        }
    }
}
