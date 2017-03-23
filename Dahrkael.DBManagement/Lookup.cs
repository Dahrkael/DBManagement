using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dahrkael.DBManagement
{
    class Lookup
    {
        private const string prefix = "fa_";
        public const string defaultIndex = "id";
        // this should be on the backend
        public const string defaultPrimaryKey = "int not null primary key";

        private static Dictionary<string, string> ClassTables = new Dictionary<string, string>();
        private static Dictionary<string, string> ClassIndices = new Dictionary<string, string>();
        
        public static string Class2Table<T>()
        {
            return Class2Table(typeof(T));
        }

        public static string Class2Table(Type type)
        {
            string className = type.Name;

            if (!ClassTables.ContainsKey(className))
            {
                string tableName = className + "s";
                ClassTables.Add(className, tableName);
            }

            return prefix + ClassTables[className];
        }
        
        public static string Class2Index<T>()
        {
            return "id";
            //return ClassIndices[ClassTables[typeof(T).Name]];
        }

        public static string Class2Index(Type type)
        {
            return "id";
            //return Tabla2IndiceClassIndices[ClassTables[type.Name]];
        }
    }
}
