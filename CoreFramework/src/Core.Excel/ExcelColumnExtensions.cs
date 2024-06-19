using System.Drawing;

namespace Core.Excel
{
    public static class ExcelColumnExtensions
    {
        public static List<ExcelColumn> GetExportColumns<T>()
        where T : class
        {
            var headStyleAttribute = typeof(T).TryGetExcelCellStyleAttribute();
            var headCellStyle = new ExcelCellStyle
            {
                HorizontalAlignment = headStyleAttribute?.HorizontalAlignment,
                FontSize = headStyleAttribute?.FontSize,
                BackgroundColor = headStyleAttribute == null
                    ? null
                    : Color.FromKnownColor(headStyleAttribute.BackgroundColor),
                NumberFormat = headStyleAttribute?.NumberFormat
            };

            var properties = typeof(T).GetProperties();

            var columns = new List<ExcelColumn>();
            foreach (var propertyInfo in properties)
            {
                if (!propertyInfo.HasExcelColumnAttribute())
                {
                    continue;
                }

                var propertyName = propertyInfo.Name;

                propertyInfo.TryGetExportExcelColumnDisplayName(out var displayName);
                propertyInfo.TryGetExportExcelColumnOrder(out var order);

                var cellStyleAttribute = propertyInfo.TryGetExcelCellStyleAttribute();
                var contentCellStyle = new ExcelCellStyle
                {
                    HorizontalAlignment = cellStyleAttribute?.HorizontalAlignment,
                    FontSize = cellStyleAttribute?.FontSize,
                    BackgroundColor = cellStyleAttribute == null
                        ? null
                        : Color.FromKnownColor(cellStyleAttribute.BackgroundColor),
                    NumberFormat = cellStyleAttribute?.NumberFormat
                };

                columns.Add(new ExcelColumn(propertyName, displayName, order, headCellStyle, contentCellStyle));
            }
            return columns;
        }
    }
}
