using System;
using System.Collections.Generic;

namespace Core.Configuration.Dashboard
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            Attributes = new List<Attribute>();
        }

        public List<Attribute> Attributes { get; }

        public DashboardOptions AddAttribute(params Attribute[] attributes)
        {
            if (attributes != null && attributes.Length > 0)
                Attributes.AddRange(attributes);
            return this;
        }
    }
}
