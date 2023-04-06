using System;
using System.Reflection;

namespace Core.Excel
{
    /// <summary>
    /// Excel导出列配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelColumnAttribute : Attribute
    {
        private readonly string _name;

        private readonly int _order;

        /// <summary>
        /// 构造涵数传入值
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="order">顺序</param>
        public ExcelColumnAttribute(string name, int order = 0)
        {
            _name = name;
            _order = order;
        }

        public string GetDisplayName()
        {
            return _name;
        }

        public int GetOrder()
        {
            return _order;
        }
    }


    public static class ExportExcelColumnExtensions
    {
        public static bool HasExcelColumnAttribute(this PropertyInfo prop)
        {
            return prop.IsDefined(typeof(ExcelColumnAttribute), true);
        }

        /// <summary>
        /// 获取属性设置的导出备注
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static bool TryGetExportExcelColumnDisplayName(this PropertyInfo prop, out string displayName)
        {
            if (!prop.IsDefined(typeof(ExcelColumnAttribute), true))
            {
                displayName = null;
                return false;
            }
            var attribute = (ExcelColumnAttribute)prop.GetCustomAttribute(typeof(ExcelColumnAttribute), true);
            displayName = attribute.GetDisplayName();
            return true;
        }

        /// <summary>
        /// 获取属性特性
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static bool TryGetExportExcelColumnOrder(this PropertyInfo prop, out int order)
        {
            if (!prop.IsDefined(typeof(ExcelColumnAttribute), true))
            {
                order = 0;
                return false;
            }
            var attribute = (ExcelColumnAttribute)prop.GetCustomAttribute(typeof(ExcelColumnAttribute), true);
            order = attribute.GetOrder();
            return true;
        }
    }
}
