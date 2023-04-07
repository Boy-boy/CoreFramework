using OfficeOpenXml.Style;
using System.Reflection;

namespace Core.Excel
{
    /// <summary>
    /// Excel导出单元格配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelCellStyleAttribute : Attribute
    {
        public ExcelHorizontalAlignment HorizontalAlignment { get; }

        public string NumberFormat { get; }

        public ExcelCellStyleAttribute(string numberFormat)
        : this(numberFormat, ExcelHorizontalAlignment.Left)
        {
        }

        public ExcelCellStyleAttribute(string numberFormat, ExcelHorizontalAlignment horizontalAlignment)
        {
            HorizontalAlignment = horizontalAlignment;
            NumberFormat = numberFormat;
        }

    }

    public static class ExcelCellStyleExtensions
    {
        /// <summary>
        /// 获取属性设置的导出备注
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static ExcelCellStyleAttribute TryGetExcelCellStyleAttribute(this PropertyInfo prop)
        {
            if (!prop.IsDefined(typeof(ExcelCellStyleAttribute), true))
            {
                return null;
            }
            var attribute = (ExcelCellStyleAttribute)prop.GetCustomAttribute(typeof(ExcelCellStyleAttribute), true);
            return attribute;
        }

    }
}
