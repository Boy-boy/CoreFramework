namespace Core.Excel
{
    public class ExcelColumn
    {
        public ExcelColumn(string name,
            string displayName,
            int order,
            ExcelCellStyle? headCellStyle = null,
            ExcelCellStyle? contentCellStyle = null)
        {
            Name = name;
            DisplayName = displayName;
            Order = order;
            HeadCellStyle = headCellStyle;
            ContentCellStyle = contentCellStyle;
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public int Order { get; set; }

        public ExcelCellStyle? HeadCellStyle { get; set; }

        public ExcelCellStyle? ContentCellStyle { get; set; }
    }
}
