using System.Reflection;

namespace Core.Excel
{
    /// <summary>
    /// Excel导出用
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExportExcelColumnAttribute : Attribute
    {
        private readonly string _name;

        private readonly int _order;

        /// <summary>
        /// 构造涵数传入值
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="order">顺序</param>
        public ExportExcelColumnAttribute(string name, int order = 1)
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
        /// <summary>
        /// 获取属性设置的导出列备注
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static bool TryGetExportExcelColumnDisplayName(this PropertyInfo prop, out string displayName)
        {
            if (!prop.IsDefined(typeof(ExportExcelColumnAttribute), true))
            {
                displayName = null;
                return false;
            }
            var attribute = (ExportExcelColumnAttribute)prop.GetCustomAttribute(typeof(ExportExcelColumnAttribute), true);
            displayName = attribute.GetDisplayName();
            return true;
        }

        /// <summary>
        /// 获取属性设置的导出列顺序
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static bool TryGetExportExcelColumnOrder(this PropertyInfo prop, out int order)
        {
            if (!prop.IsDefined(typeof(ExportExcelColumnAttribute), true))
            {
                order = 0;
                return false;
            }
            var attribute = (ExportExcelColumnAttribute)prop.GetCustomAttribute(typeof(ExportExcelColumnAttribute), true);
            order = attribute.GetOrder();
            return true;
        }
    }
}
