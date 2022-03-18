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

        /// <summary>
        /// 构造涵数传入值
        /// </summary>
        /// <param name="name"></param>
        public ExportExcelColumnAttribute(string name)
        {
            _name = name;
        }

        public string GetDisplayName()
        {
            return _name;
        }
    }


    public static class ExportExcelColumnExtensions
    {
        /// <summary>
        /// 获取属性设置的导出备注
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static bool TryGetExportExcelDisplayName(this PropertyInfo prop, out string displayName)
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
    }
}
