using OfficeOpenXml.Style;
using System.Drawing;
using System.Reflection;

namespace Core.Excel
{
    /// <summary>
    /// Excel导出单元格配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelCellStyleAttribute : Attribute
    {
        /// <summary>
        /// 水平对齐
        /// </summary>
        public ExcelHorizontalAlignment HorizontalAlignment { get; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public KnownColor BackgroundColor { get; }

        /// <summary>
        /// 数字格式
        /// </summary>
        public string NumberFormat { get; }


        public ExcelCellStyleAttribute(ExcelHorizontalAlignment horizontalAlignment,
            int fontSize,
            KnownColor backgroundColor,
            string numberFormat = null)
        {
            HorizontalAlignment = horizontalAlignment;
            FontSize = fontSize;
            BackgroundColor = backgroundColor;
            NumberFormat = numberFormat;
        }

    }

    public static class ExcelCellStyleExtensions
    {
        public static ExcelCellStyleAttribute TryGetExcelCellStyleAttribute(this Type type)
        {
            if (!type.IsDefined(typeof(ExcelCellStyleAttribute), true))
            {
                return null;
            }
            var attribute = (ExcelCellStyleAttribute)type.GetCustomAttribute(typeof(ExcelCellStyleAttribute), true);
            return attribute;
        }

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
