using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging
{
    public class ActionNameAttribute : Attribute, IFilterMetadata
    {
        public ActionNameAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; }
    }
}
