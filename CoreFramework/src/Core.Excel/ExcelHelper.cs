using OfficeOpenXml;

namespace Core.Excel
{
    public static class ExcelHelper
    {
        /// <summary>
        /// 传入数据，返回Excel流文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">数据源</param>
        /// <param name="sheetName">sheet名字</param>
        /// <returns></returns>
        public static MemoryStream GetExcelStream<T>(this IList<T> sources, string sheetName = "sheet1")
        where T : class
        {
            var columns = ExcelColumnExtensions.GetExportColumns<T>();
            return GetExcelStream(sources, columns, sheetName);
        }

        /// <summary>
        /// 传入数据，返回Excel流文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">数据源</param>
        /// <param name="columns"></param>
        /// <param name="sheetName">sheet名字</param>
        /// <returns></returns>
        public static MemoryStream GetExcelStream<T>(this IList<T> sources, IList<ExcelColumn> columns, string sheetName = "sheet1")
        {
            if (sources == null || !sources.Any())
            {
                throw new Exception($"{sheetName}暂无数据！");
            }

            if (columns == null || !columns.Any())
            {
                throw new Exception($"{sheetName}暂无数据！");
            }

            columns = columns
                .OrderBy(p => p.Order)
                .ToList();

            var ms = new MemoryStream();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add(sheetName);

            for (var row = 1; row <= sources.Count + 1; row++)
            {
                var index = 1;
                for (var cl = 1; cl <= columns.Count; cl++)
                {
                    if (row == 1) //设置表头
                    {
                        sheet.Cells[row, index].Value = columns[cl - 1].DisplayName;
                    }
                    else
                    {
                        var excelCellStyle = columns[cl - 1].CellStyle;
                        sheet.Cells[row, index].Style.HorizontalAlignment = excelCellStyle.HorizontalAlignment;
                        sheet.Cells[row, index].Style.Numberformat.Format = excelCellStyle.NumberFormat;

                        //获取字段名字
                        var name = columns[cl - 1].Name;
                        //获取对应的值
                        var value = sources[row - 2]?.GetType().GetProperty(name)?.GetValue(sources[row - 2]);
                        sheet.Cells[row, index].Value = value;
                    }
                    index++;
                }
            }
            //设置Excel列宽
            sheet.Cells.AutoFitColumns(1.5);
            package.SaveAs(ms);
            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// 传入excel流,返回IList集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="excelStream"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public static IList<T> GetExcelData<T>(this Stream excelStream, string sheetName = "sheet1")
            where T : class, new()
        {
            var columns = ExcelColumnExtensions.GetExportColumns<T>();
            return GetExcelData<T>(excelStream, columns, sheetName);
        }

        /// <summary>
        ///  传入excel流,返回IList集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="excelStream"></param>
        /// <param name="columns"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IList<T> GetExcelData<T>(this Stream excelStream, IList<ExcelColumn> columns, string sheetName = "sheet1")
            where T : class, new()
        {
            if (excelStream == null)
            {
                throw new Exception($"{sheetName}暂无数据！");
            }

            if (columns == null || !columns.Any())
            {
                throw new Exception($"{sheetName}暂无数据！");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[sheetName];
            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            var result = new List<T>();

            var excelColumns = new Dictionary<int, string>();

            for (var col = 1; col <= colCount; col++)
            {
                var cellValue = worksheet.Cells[1, col].Value?.ToString() ?? "";
                var excelColumn = columns.FirstOrDefault(c => string.Equals(c.DisplayName, cellValue, StringComparison.OrdinalIgnoreCase));
                if (excelColumn != null)
                {
                    excelColumns.Add(col, excelColumn.Name);
                }
            }

            if (!excelColumns.Any())
                return result;

            for (var row = 2; row <= rowCount; row++)
            {
                var data = new T();
                for (var col = 1; col <= colCount; col++)
                {
                    if (!excelColumns.TryGetValue(col, out var propertyName))
                        continue;

                    var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    typeof(T).GetProperty(propertyName)?.SetValue(data, cellValue);
                }
                result.Add(data);
            }
            return result;
        }
    }
}
