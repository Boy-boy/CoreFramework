using OfficeOpenXml.Style;
using System.Collections.Generic;

namespace Core.Excel
{
    public static class ExcelColumnExtensions
    {
        public static List<ExcelColumn> GetExportColumns<T>()
        where T : class
        {
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
                var cellStyle = new ExcelCellStyle(cellStyleAttribute?.NumberFormat, cellStyleAttribute?.HorizontalAlignment ?? ExcelHorizontalAlignment.Left);

                columns.Add(new ExcelColumn(propertyName, displayName, order, cellStyle));
            }
            return columns;
        }
    }
}
