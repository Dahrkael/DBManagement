using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System;

namespace Dahrkael.DBManagement
{
    public class DBManager
    {
        public enum DBType { Default, SQLite, MySQL }

        public static DBType dbBackendType { get; set; }

        private static DBManager instance;
        private IDBBackend dbBackend;

        public static DBManager I
        {
            get
            {
                if (instance == null)
                {
                    instance = new DBManager();
                }
                return instance;
            }
        }

        public static bool Restart()
        {
            instance = new DBManager();
            return true;
        }

        private DBManager()
            : this(DBType.Default)
        {
        }

        private DBManager(DBType mode)
        {
            if (mode == DBType.Default)
            {
                mode = DBType.SQLite;
            }
            
            switch (mode)
            {
                case DBType.MySQL:
                    dbBackend = new Backends.MySQLFunctions();
                    break;
                case DBType.SQLite:
                    dbBackend = new Backends.SQLiteFunctions();
                    break;
            }
        }

        public bool CreateTable(string table, Dictionary<string, string> fields)
        {
            return dbBackend.CreateTable(table, fields);
        }

        public bool TableExists(string table)
        {
            return dbBackend.TableExists(table);
        }
        
        // Actual functions

        public bool CreateTableRelation<T1, T2>()
        {
            string table = Lookup.Class2Table<T1>() + "_" + Lookup.Class2Table<T2>();
            string index1 = Lookup.Class2Index<T1>() + "_" + Lookup.Class2Table<T1>();
            string index2 = Lookup.Class2Index<T2>() + "_" + Lookup.Class2Table<T2>();

            if (TableExists(table))
            {
                return false;
            }
            // FIXME esto deberia ser abstracto, no vale poner SQL a pelo
            Dictionary<string, string> fields = new Dictionary<string,string>();
            fields.Add(Lookup.defaultIndex, Lookup.defaultPrimaryKey);
            fields.Add(index1, "int not null");
            fields.Add(index2, "int not null");
            return dbBackend.CreateTable(table, fields);
        }

        public int NextIndex<T>()
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();

            return dbBackend.NextIndex(index, table);
        }

        public bool Insert<T>(Dictionary<string, object> valores)
        {
            string table = Lookup.Class2Table<T>();
            return dbBackend.Insert(valores, table);
        }

        public bool Insert<T>(T obj)
        {
            return Insert<T>(Helper.Object2Dict<T>(obj));
        }

        public DataTable LoadDT<T>(int id)
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();
            return dbBackend.GetDataTable(table, index + "=" + id + ";");
        }

        public DataTable LoadDT(Type type, int id)
        {
            string table = Lookup.Class2Table(type);
            string index = Lookup.Class2Index(type);
            return dbBackend.GetDataTable(table, index + "=" + id + ";");
        }

        public bool Modify(Type type, Dictionary<string, object> values, string where)
        {
            string table = Lookup.Class2Table(type);
            return dbBackend.Modify(values, table, where);

        }
        public bool Modify<T>(Dictionary<string, object> values, string where)
        {
            string table = Lookup.Class2Table<T>();
            return dbBackend.Modify(values, table, where);
        }

        public bool Modify<T>(T obj, string where)
        {
            return Modify<T>(Helper.Object2Dict<T>(obj), where);
        }

        public bool Delete(Type type, string where)
        {
            string table = Lookup.Class2Table(type);
            return dbBackend.Delete(table, where);
        }

        public bool Delete<T>(string where)
        {
            string table = Lookup.Class2Table<T>();
            return dbBackend.Delete(table, where);
        }

        public int Exists<T>(string where)
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();
            return dbBackend.Exists(index, table, where);
        }

        public int GetFirstID<T>()
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();
            return dbBackend.GetFirstID(table, index);
        }

        public int GetLastID<T>()
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();
            return dbBackend.GetLastID(table, index);
        }

        public int GetIDsCount<T>()
        {
            string table = Lookup.Class2Table<T>();
            return dbBackend.GetIDsCount(table);
        }

        public List<int> GetListIDs<T>()
        {
            string table = Lookup.Class2Table<T>();
            string index = Lookup.Class2Index<T>();
            return dbBackend.GetListIDs(table, index);
        }

        public List<int> GetListIDs(Type type)
        {
            string table = Lookup.Class2Table(type);
            string index = Lookup.Class2Index(type);
            return dbBackend.GetListIDs(table, index);
        }

        public List<int> GetListIDs(Type type, string where)
        {
            string table = Lookup.Class2Table(type);
            string index = Lookup.Class2Index(type);
            return dbBackend.GetListIDs(table, index, where);
        }

        public List<int> GetListRelationIDs<T1, T2>(int id)
        {
            string table = Lookup.Class2Table<T1>() + "_" + Lookup.Class2Table<T2>();
            string index1 = Lookup.Class2Index<T1>() + "_" + Lookup.Class2Table<T1>();
            string index2 = Lookup.Class2Index<T2>() + "_" + Lookup.Class2Table<T2>();

            return dbBackend.GetListRelationIDs(table, index1, index2, id);
        }

        public List<int> GetListRelationIDs(Type type1, Type type2, int id)
        {
            string table = Lookup.Class2Table(type1) + "_" + Lookup.Class2Table(type2);
            string index1 = Lookup.Class2Index(type1) + "_" + Lookup.Class2Table(type1);
            string index2 = Lookup.Class2Index(type2) + "_" + Lookup.Class2Table(type2);

            return dbBackend.GetListRelationIDs(table, index1, index2, id);
        }

        public bool InsertRelation(Type type1, Type type2, int id1, int id2)
        {
            string table = Lookup.Class2Table(type1) + "_" + Lookup.Class2Table(type2);
            string index1 = Lookup.Class2Index(type1) + "_" + Lookup.Class2Table(type1);
            string index2 = Lookup.Class2Index(type2) + "_" + Lookup.Class2Table(type2);
            if (dbBackend.Exists(Lookup.defaultIndex, table, index1 + "=" + id1 + " AND " + index2 + "=" + id2 + ";") == -1)
            {
                return dbBackend.InsertRelation(table, id1.ToString(), id2.ToString());
            }
            return false;
        }

        public bool DeleteRelation(Type type1, Type type2, int id)
        {
            string table = Lookup.Class2Table(type1) + "_" + Lookup.Class2Table(type2);
            return dbBackend.Delete(table, Lookup.defaultIndex + "=" + id);
        }
    }
}