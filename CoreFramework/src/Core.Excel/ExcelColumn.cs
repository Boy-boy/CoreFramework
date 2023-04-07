namespace Core.Excel
{
    public struct ExcelColumn
    {
        public ExcelColumn(string name, string displayName, int order, ExcelCellStyle cellStyle)
        {
            Name = name;
            DisplayName = displayName;
            Order = order;
            CellStyle = cellStyle;
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public int Order { get; set; }

        public ExcelCellStyle CellStyle { get; set; }
    }
}
