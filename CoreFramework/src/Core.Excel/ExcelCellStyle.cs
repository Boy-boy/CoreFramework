using OfficeOpenXml.Style;

namespace Core.Excel
{
    public struct ExcelCellStyle
    {
        public ExcelCellStyle(string numberFormat)
        : this(numberFormat, ExcelHorizontalAlignment.Left)
        {
        }

        public ExcelCellStyle(string numberFormat, ExcelHorizontalAlignment horizontalAlignment)
        {
            NumberFormat = numberFormat;
            HorizontalAlignment = horizontalAlignment;
        }

        public ExcelHorizontalAlignment HorizontalAlignment { get; }

        public string NumberFormat { get; set; }
    }
}
