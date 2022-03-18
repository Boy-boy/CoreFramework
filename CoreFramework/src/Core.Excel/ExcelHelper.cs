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
        public static MemoryStream GetExcelMemoryStreams<T>(this IList<T> sources, string sheetName = "sheet1")
        {
            var ms = new MemoryStream();
            if (sources.Any())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var sheet = package.Workbook.Worksheets.Add(sheetName);

                //获取传入的数据类型
                var propertiesList = typeof(T).GetProperties();
                for (var row = 1; row <= sources.Count + 1; row++)
                {
                    var index = 1;
                    for (var cl = 1; cl <= propertiesList.Length; cl++)
                    {
                        //获取备注名字
                        var hasDisplayName = propertiesList[cl - 1].TryGetExportExcelDisplayName(out var displayName);
                        //判断字段是否有自定义属性
                        if (!hasDisplayName) continue;
                        if (row == 1) //设置表头
                            sheet.Cells[row, index].Value = displayName;
                        else
                        {
                            //获取字段名字
                            var name = propertiesList[cl - 1].Name;
                            //获取对应的值
                            var value = sources[row - 2]?.GetType().GetProperty(name)?.GetValue(sources[row - 2])?.ToString();
                            sheet.Cells[row, index].Value = value;
                        }
                        index++;
                    }
                }
                //设置Excel列宽
                sheet.Cells.AutoFitColumns(1.5);
                package.SaveAs(ms);
                ms.Position = 0;
            }
            else
                throw new Exception($"{sheetName}暂无数据！");

            return ms;
        }

    }
}
