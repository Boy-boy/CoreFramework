using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        where T : class
        {
            var columns = ExcelColumnExtensions.GetExportColumns<T>();
            return GetExcelMemoryStreams(sources, columns, sheetName);
        }

        /// <summary>
        /// 传入数据，返回Excel流文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">数据源</param>
        /// <param name="columns"></param>
        /// <param name="sheetName">sheet名字</param>
        /// <returns></returns>
        public static MemoryStream GetExcelMemoryStreams<T>(this IList<T> sources, IList<ExcelColumn> columns, string sheetName = "sheet1")
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
    }
}
