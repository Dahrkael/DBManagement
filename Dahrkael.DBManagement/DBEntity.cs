using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Dahrkael.DBManagement
{
    class DBEntity<T>
    {
        public static T New()
        {
            T obj = (T)Activator.CreateInstance(typeof(T), new object[] { });

            string name = Lookup.Class2Index<T>();
            PropertyInfo property = obj.GetType().GetProperty(name);
            property.SetValue(obj, DBManager.I.NextIndex<T>(), null);

            if (DBManager.I.Insert<T>(obj))
            {
                return obj;
            }
            return default(T);
        }

        public static T Load(int id)
        {
            if (id == -1)
            {
                return default(T);
            }

            T obj = Helper.DT2Object<T>(DBManager.I.LoadDT<T>(id));
            if (obj == null)
            {
                throw new Exception("El " + Helper.TypeString<T>().ToLower() + " " + id + " no existe");
            }
            Helper.LoadLists<T>(obj); // FIXME esto va aqui?
            return obj;
        }

        public static bool Modify(T obj)
        {
            PropertyInfo property = obj.GetType().GetProperty(Lookup.Class2Index<T>());
            string index = property.Name;
            int value = (int)property.GetValue(obj, null);
            // FIXME guardo aqui las listas?
            Helper.SaveLists<T>(obj);
            return DBManager.I.Modify<T>(obj, index + "=" + value);
        }

        public static bool Delete(T obj)
        {
            PropertyInfo property = obj.GetType().GetProperty(Lookup.Class2Index<T>());
            string index = property.Name;
            int value = (int)property.GetValue(obj, null);

            return DBManager.I.Delete<T>(index + "=" + value);
        }

        /// <summary>
        /// Ultraexperimental, ya que no aun no se como definir la longitud de los varchar para los string
        /// </summary>
        /// <returns></returns>
        public static bool CreateTable()
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            // creo un objeto vacio por no escribir otra funcion para pasar las propiedades a diccionario
            T obj = (T)Activator.CreateInstance(typeof(T), new object[] { });
            Dictionary<string, object> propiedades = Helper.Object2Dict<T>(obj, false);

            // campos
            propiedades.ToList().ForEach(x =>
                fields[x.Key] = Helper.BDType(obj.GetType().GetProperty(x.Key).PropertyType)
            );

            // clave primaria
            fields[Lookup.Class2Index<T>()] = Lookup.defaultPrimaryKey;
            //campos.Add("nombre", "tipo caracteristicas");
            return DBManager.I.CreateTable(Lookup.Class2Table(typeof(T)), fields);
        }
    }
}