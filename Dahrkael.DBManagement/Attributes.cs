using System;

namespace Dahrkael.DBManagement
{
    [AttributeUsage(AttributeTargets.Class)]
    class TableAttribute : Attribute
    {
        private string table;
        public TableAttribute(string table)
        {
            this.table = table;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    class IndexAttribute : Attribute
    {
        private string index;
        public IndexAttribute(string index)
        {
            this.index = index;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class RequiredAttribute : Attribute
    {
    }

    class FKAttribute : Attribute
    {
    }
}
