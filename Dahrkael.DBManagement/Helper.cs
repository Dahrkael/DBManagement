using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Data;
using System.IO;
using System.Drawing;
using System.Collections;

namespace Dahrkael.DBManagement
{
    class Helper
    {
        // this doesnt look like its used
        /*
        public static Dictionary<int, string> IDName<T>()
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();

            List<int> ids = DBManager.I.GetListIDs<T>();
            foreach (int id in ids)
            {
                Dictionary<string, object> classDict = Helper.DT2Dict(DBManager.I.LoadDT<T>(id));
                dict.Add((int)classDict[Lookup.Class2Index<T>().ToLower()], (string)classDict["nombre"]); // FIXME y si no tiene nombre?
            }
            return dict;
        }
        */

        /// <summary>
        /// Carga las properties de un obj que sean listas a partir de relaciones en la base de datos
        /// Carga indistintamente relaciones 1 a N (field referenciando al padre) y N a N (tabla intermedia)
        /// posible ToDo: conseguir usar Entidad.Cargar() para que se cargue recursivamente si hay listas en las listas
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void LoadLists<T>(T obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                {
                    Type type = property.PropertyType.GetGenericArguments()[0];
                    List<PropertyInfo> propertiesFather = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => IsFK(x, typeof(T))).ToList();
                    if (propertiesFather.Count() > 0)
                    {
                        // 1 a N
                        int id = (int)typeof(T).GetProperty(Lookup.Class2Index<T>()).GetValue(obj, null);
                        var lista = (IList)typeof(List<>).MakeGenericType(type).GetConstructor(Type.EmptyTypes).Invoke(null);
                        List<int> ids = DBManager.I.GetListIDs(type, propertiesFather.First().Name + "=" + id);
                        foreach(int i in ids)
                        {
                            lista.Add(Dict2Object(type, DT2Dict(DBManager.I.LoadDT(type, i))));
                        }
                        property.SetValue(obj, lista, null);
                    }
                    else
                    {
                        // N a N
                        int id = (int)typeof(T).GetProperty(Lookup.Class2Index<T>()).GetValue(obj, null);
                        List<int> ids = DBManager.I.GetListRelationIDs(typeof(T), type, id);
                        var lista = (IList)typeof(List<>).MakeGenericType(type).GetConstructor(Type.EmptyTypes).Invoke(null);
                        foreach (int i in ids)
                        {
                            lista.Add(Dict2Object(type, DT2Dict(DBManager.I.LoadDT(type, i))));
                        }
                        property.SetValue(obj, lista, null);
                    }
                }
            }
        }

        /// <summary>
        /// Guarda las properties de un obj que sean listas en la base de datos.
        /// Guarda la relacion de IDs (Codigos) en una tabla para tal fin.
        /// </summary>
        /// <typeparam name="T">Tipo del obj que tiene las listas</typeparam>
        /// <param name="obj">El obj que tiene las listas</param>
        public static void SaveLists<T>(T obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (PropertyInfo property in properties)
            {
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                {
                    // type de la lista
                    Type type = property.PropertyType.GetGenericArguments()[0];
                    // lista con las properties del type que son del type del padre
                    List<PropertyInfo> propertiesFather = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => IsFK(x, typeof(T))).ToList();
                    if (propertiesFather.Count() > 0)
                    {
                        // id del obj con la lista
                        int id1 = (int)typeof(T).GetProperty(Lookup.Class2Index<T>()).GetValue(obj, null);
                        // lista en si
                        IList lista = (IList)property.GetValue(obj, null);
                        // tratar los borrados
                        List<int> idsEnBD = DBManager.I.GetListIDs(type, propertiesFather.First().Name + "=" + id1);
                        foreach(int id in idsEnBD)
                        {
                            bool existe = false;
                            foreach(object i in lista)
                            {
                                // id del obj a tratar actualmente dentro de la lista
                                int id2 = (int)type.GetProperty(Lookup.Class2Index(type)).GetValue(i, null);
                                if (id == id2)
                                {
                                    existe = true;
                                    break;
                                }
                            }
                            if (!existe)
                            {
                                // el obj se ha borrado de la lista, borrar de la base de datos
                                DBManager.I.Delete(type, Lookup.Class2Index(type) + "=" + id);
                            }
                        }

                        // FIXME comprobar que todo este foreach funciona
                        foreach (object i in lista)
                        {
                            // modificar los datos del obj que ya existe en la base de datos
                            if (!Helper.ModifyDBObject(type, i))
                            {
                                // insertarlos en caso de que no existan
                                // FIXME deberian existir ya? aunque salgan de un datagridview?
                            }
                        }
                    }
                    else
                    {
                        // id del obj con la lista
                        int id1 = (int)typeof(T).GetProperty(Lookup.Class2Index<T>()).GetValue(obj, null);
                        // lista en si
                        IList lista = (IList)property.GetValue(obj, null);

                        // FIXME borrar todas las relaciones existentes primero?
                        // muy overkill a lo mejor
                        List<int> idsExistentes = DBManager.I.GetListRelationIDs(typeof(T), type, id1);
                        foreach (int i in idsExistentes)
                        {
                            DBManager.I.DeleteRelation(typeof(T), type, i);
                        }
                        foreach (object i in lista)
                        {
                            int id2 = (int)type.GetProperty(Lookup.Class2Index(type)).GetValue(i, null);
                            DBManager.I.InsertRelation(typeof(T), type, id1, id2);
                        }
                        property.SetValue(obj, lista, null);
                    }
                }
            }
        }

        
        /// <summary>
        /// No se si esto va aqui realmente, es como Entidad.Modificar() pero especificando el type
        /// FIXME
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool ModifyDBObject(Type type, object obj)
        {
            PropertyInfo property = type.GetProperty(Lookup.Class2Index(type));
            string index = property.Name;
            int value = (int)property.GetValue(obj, null);
            return DBManager.I.Modify(type, Helper.Object2Dict(type, obj, true), index + "=" + value);
        }
        
        /// <summary>
        /// Convierte las properties publicas de un obj en un dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Objeto a convertir</param>
        /// <returns></returns>
        public static Dictionary<string, object> Object2Dict<T>(T obj)
        {
            return Object2Dict<T>(obj, true);
        }

        /// <summary>
        /// Convierte las properties publicas de un obj en un dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Objeto a convertir</param>
        /// <param name="lowcase">pasar nombres a lowcase o no</param>
        /// <returns></returns>
        public static Dictionary<string, object> Object2Dict<T>(T obj, bool lowcase)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string field;
            object value;

            foreach (PropertyInfo property in properties)
            {
                // no guardar listas
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                {
                    continue;
                }
                field = property.Name;
                value = property.GetValue(obj, null);

                if (lowcase)
                {
                    values.Add(field.ToLower(), value);
                }
                else
                {
                    values.Add(field, value);
                }
            }
            values = Object2FK<T>(values); // FIXME test
            return values;
        }

        /// <summary>
        /// Convierte las properties publicas de un obj en un dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Objeto a convertir</param>
        /// <param name="lowcase">pasar nombres a lowcase o no</param>
        /// <returns></returns>
        public static Dictionary<string, object> Object2Dict(Type type, object obj, bool lowcase)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string field;
            object value;

            foreach (PropertyInfo property in properties)
            {
                // no guardar listas
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                {
                    continue;
                }
                field = property.Name;
                value = property.GetValue(obj, null);

                if (lowcase)
                {
                    values.Add(field.ToLower(), value);
                }
                else
                {
                    values.Add(field, value);
                }
            }
            values = Object2FK(type, values); // FIXME test
            return values;
        }

        /// <summary>
        /// Convierte un dictionary en un obj de la clase especificada, 
        /// asignando los values de las claves a las properties del obj con el mismo nombre
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary">Diccionario con los datos del obj</param>
        /// <returns></returns>
        public static T Dict2Object<T>(Dictionary<string, object> dictionary)
        {
            dictionary = FK2Object<T>(dictionary); // FIXME test
            T obj = (T)Activator.CreateInstance(typeof(T), new object[] { });

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    if (pair.Key.ToLower() == property.Name.ToLower())
                    {
                        property.SetValue(obj, Convert.ChangeType(pair.Value, property.PropertyType), null);
                        break;
                    }
                }
            }
            return obj;
        }

        public static object Dict2Object(Type type, Dictionary<string, object> dictionary)
        {
            dictionary = FK2Object(type, dictionary); // FIXME test
            var obj = Activator.CreateInstance(type, new object[] { });

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    if (pair.Key.ToLower() == property.Name.ToLower())
                    {
                        property.SetValue(obj, Convert.ChangeType(pair.Value, property.PropertyType), null);
                        break;
                    }
                }
            }
            return obj;
        }

        public static T DT2Object<T>(DataTable dt)
        {
            return Dict2Object<T>(DT2Dict(dt));
        }

        /// <summary>
        /// Devuelve el nombre del type en formato texto
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string TypeString<T>()
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// Convierte el primer registro de un DataTable en un dictionary
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static Dictionary<string, object> DT2Dict(DataTable dt)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            DataRow row = dt.Rows[0];

            foreach (DataColumn column in dt.Columns)
            {
                dict.Add(column.ColumnName.ToLower(), row[column]);
            }
            return dict;
        }

        /// <summary>
        /// Convierte los booleanos de un dictionary en smallint ("1" o "0") para su insercion en una base de datos sin booleanos
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Dictionary<string, object> Bool2Smallint(Dictionary<string, object> dictionary)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                object value = pair.Value;

                if (pair.Value is bool)
                {
                    value = true ? "1" : "0";
                }
                dic.Add(pair.Key, value);
            }
            return dic;
        }

        public static string BDType(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }
            if (type == typeof(string))
            {
                return "varchar(1024)";
            }
            if (type == typeof(double))
            {
                return "double precision";
            }
            if (type == typeof(float))
            {
                return "float";
            }
            if (type == typeof(DateTime))
            {
                return "timestamp";
            }
            if (type == typeof(bool))
            {
                if (DBManager.dbBackendType == DBManager.DBType.FireBird)
                {
                    return "smallint";
                }
                return "boolean";
            }
            if (type == typeof(byte[]))
            {
                return "blob";
            }
            // los que no sean types estandar los consideramos int, para que guarden el id de la clase correspondiente
            return "int";
        }

        /// <summary>
        /// Convierte los fields enteros que tengan el atributo FK al obj correspondiente a ese index
        /// FIXME untested
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Dictionary<string, object> FK2Object<T>(Dictionary<string, object> dictionary)
        {
            List<PropertyInfo> FKs = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => IsFK(x)).ToList();
            foreach (PropertyInfo fk in FKs)
            {
                if (dictionary.Keys.Contains(fk.Name.ToLower()))
                {
                    // FIXME comprobar que el index es valido
                    object obj = Helper.Dict2Object(fk.PropertyType, Helper.DT2Dict(DBManager.I.LoadDT(fk.PropertyType, (int)dictionary[fk.Name.ToLower()])));
                    dictionary[fk.Name.ToLower()] = obj;
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Convierte los fields enteros que tengan el atributo FK al obj correspondiente a ese index
        /// FIXME untested
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Dictionary<string, object> FK2Object(Type type, Dictionary<string, object> dictionary)
        {
            List<PropertyInfo> FKs = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => IsFK(x)).ToList();
            foreach (PropertyInfo fk in FKs)
            {
                if (dictionary.Keys.Contains(fk.Name.ToLower()))
                {
                    // FIXME comprobar que el index es valido
                    object obj = Helper.Dict2Object(fk.PropertyType, Helper.DT2Dict(DBManager.I.LoadDT(fk.PropertyType, (int)dictionary[fk.Name.ToLower()])));
                    dictionary[fk.Name.ToLower()] = obj;
                }
            }
            return dictionary;
        }

        public static Dictionary<string, object> Object2FK<T>(Dictionary<string, object> dictionary)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                dict[pair.Key] = pair.Value;
                PropertyInfo[] properties = typeof(T).GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (property.Name.ToLower() == pair.Key)
                    {
                        if (IsFK(property))
                        {
                            if (pair.Value != null)
                            {
                                dict[pair.Key] = property.PropertyType.GetProperty(Lookup.Class2Index(property.PropertyType)).GetValue(pair.Value, null);
                            }
                            else
                            {
                                dict[pair.Key] = 0;
                            }
                        }
                        break;
                    }
                }
            }
            return dict;
        }

        public static Dictionary<string, object> Object2FK(Type type, Dictionary<string, object> dictionary)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                dict[pair.Key] = pair.Value;
                PropertyInfo[] properties = type.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (property.Name.ToLower() == pair.Key)
                    {
                        if (IsFK(property))
                        {
                            if (pair.Value != null)
                            {
                                dict[pair.Key] = property.PropertyType.GetProperty(Lookup.Class2Index(property.PropertyType)).GetValue(pair.Value, null);
                            }
                            else
                            {
                                dict[pair.Key] = 0;
                            }
                        }
                        break;
                    }
                }
            }
            return dict;
        }

        public static bool IsFK(PropertyInfo property)
        {
            object[] attributes = property.GetCustomAttributes(true);
            foreach (Attribute attribute in attributes)
            {
                if (attribute is FKAttribute)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsFK(PropertyInfo property, Type type)
        {
            object[] attributes = property.GetCustomAttributes(true);
            foreach (Attribute attribute in attributes)
            {
                if (attribute is FKAttribute)
                {
                    if (property.PropertyType == type)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool ISRequired(PropertyInfo property)
        {
            object[] attributes = property.GetCustomAttributes(true);
            foreach (Attribute attribute in attributes)
             {
                 if (attribute is RequiredAttribute)
                 {
                     return true;
                 }
             }
            return false;
        }

        public static string Capitalize(string text)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(char.ToUpper(text[0]));
            builder.Append(text.Substring(1));
            return builder.ToString();
        }
    }
}
