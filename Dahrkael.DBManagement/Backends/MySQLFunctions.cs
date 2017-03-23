using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using IniParser;
using IniParser.Model;

namespace Dahrkael.DBManagement.Backends
{
    class MySQLFunctions : IDBBackend
    {
        private MySQLWrapper manager;

        public MySQLFunctions()
        {
            FileIniDataParser parser = new FileIniDataParser();
            try
            {
                IniData data = parser.ReadFile("BBDD.ini");
                manager = new MySQLWrapper(data["MySQL"]["UserID"],
                                           data["MySQL"]["Password"],
                                           data["MySQL"]["Server"],
                                           Int32.Parse(data["MySQL"]["Port"]),
                                           data["MySQL"]["Database"]);
            }
            catch (Exception)
            {
                //MessageBox.Show("Seccion MySQL no encontrada en archivo de configuración. Cargando datos por defecto.");
                manager = new MySQLWrapper();
            }
        }

        #region utils

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            return text.Substring(0, Math.Min(text.Length, maxLength));
        }

        public static bool ToBoolean(object field)
        {
            return ((field.ToString()) == "0" ? false : true);
        }

        #endregion

        #region varios

        public bool CreateTable(string table, Dictionary<string, string> fields)
        {
            string sentence = "";
            sentence += "create table " + table + " (";
            foreach (KeyValuePair<string, string> field in fields)
            {
                sentence += field.Key + " " + field.Value + ", ";
            }
            sentence = sentence.Remove(sentence.Length - 2); // FIXME borramos la coma y el espacio extras
            sentence += ");";
            manager.ExecuteNonQuery(sentence);
            return true;
        }

        public bool TableExists(string table)
        {
            if(manager.GetDataTable("SHOW TABLES LIKE '" + table + "'").Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        public int NextIndex(string index, string table)
        {
            int id = -1;

            try
            {
                id = int.Parse(manager.ExecuteScalar("select coalesce(max(" + index + ")+1, 1) from " + table));
            }
            catch (Exception)
            {
                id = -1;
            }

            return id;
        }

        public bool Insert(Dictionary<string, object> values, string table)
        {
            bool result = false;
            //values = Helper.Bool2Smallint(values);
            try
            {
                result = manager.Insert(table, values);
            }
            catch (Exception e)
            {
                throw new Exception("Error inserting:\r\n" + e.Message);
            }
            return result;
        }
        /*
        public int OperarBlob(string campo, string table, string clausulaWhere, object objeto)
        {
            int resultado = -1;
            byte[] datos;
            try
            {
                using (Serializador serializador = new Serializador())
                {
                    datos = serializador.serializarABytes(objeto);
                }
                if (datos != null)
                {
                    resultado = manager.EjecutarNonQueryBlob(("update " + table + " set " + campo + "=? where " + clausulaWhere + ";"), datos);
                }
            }
            catch (Exception e)
            {
                new MensajeExcepcion(e, MensajeExcepcion.ErrorInsertarDatosBD);
            }
            return resultado;
        }
        */
        public string GetScalar(string field, string table, string where)
        {
            string scalar = string.Empty;
            try
            {
                scalar = manager.ExecuteScalar(("select " + field + " from " + table + " where " + where + ";"));
            }
            catch (Exception e)
            {
                throw new Exception("Error loading:\r\n" + e.Message);
            }
            return scalar;
        }

        public DataTable GetDataTable(string table, string where)
        {
            DataTable dataTable = null;
            try
            {
                dataTable = manager.GetDataTable(("select * from " + table + " where " + where + ";"));
            }
            catch (Exception e)
            {
                throw new Exception("Error loading:\r\n" + e.Message);
            }
            return dataTable;
        }

        public bool Modify(Dictionary<string, object> values, string table, string where)
        {
            bool result = false;
            //valores = Helper.Bool2Smallint(valores);
            try
            {
                result = manager.Update(table, values, where);
            }
            catch (Exception e)
            {
                throw new Exception("Error modifying:\r\n" + e.Message);
            }
            return result;
        }

        public bool Delete(string table, string where)
        {
            bool result = false;
            try
            {
                result = manager.Delete(table, where);
            }
            catch (Exception e)
            {
                throw new Exception("Error deleting:\r\n" + e.Message);
            }
            return result;
        }

        public bool Renumerate(string table, string where)
        {
            bool resultado = false;
            try
            {
                resultado = manager.Renumerate(table, where);
            }
            catch (Exception e)
            {
                throw new Exception("Error deleting:\r\n" + e.Message);
            }
            return resultado;
        }

        public int Exists(string field, string table, string where)
        {
            int id = -1;
            string temp;
            DataRow dataRow;
            DataTable dataTable;
            try
            {
                dataTable = manager.GetDataTable("SELECT " + field + " FROM " + table + " WHERE " + where + ";");
                if (dataTable.Rows.Count != 0)
                {
                    dataRow = dataTable.Rows[0];
                    temp = dataRow[field].ToString();
                    id = int.Parse(temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return id;
        }

        public int GetFirstID(string table, string index)
        {
            int id = -1;
            string temp;
            DataRow dataRow;
            DataTable dataTable;
            try
            {
                dataTable = manager.GetDataTable("select min(" + index + ") from " + table + ";");
                if (dataTable.Rows.Count != 0)
                {
                    dataRow = dataTable.Rows[0];
                    temp = dataRow[0].ToString();
                    id = (temp == "") ? -1 : int.Parse(temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return id;
        }

        public int GetFirstID(string table)
        {
            return GetFirstID(table, Lookup.defaultIndex);
        }

        public int GetLastID(string table, string index)
        {
            int id = -1;
            string temp;
            DataRow dataRow;
            DataTable dataTable;
            try
            {
                dataTable = manager.GetDataTable("select max(" + index + ") from " + table + ";");
                if (dataTable.Rows.Count != 0)
                {
                    dataRow = dataTable.Rows[0];
                    temp = dataRow[0].ToString();
                    id = (temp == "") ? -1 : int.Parse(temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return id;
        }

        public int GetLastID(string table)
        {
            return GetLastID(table, Lookup.defaultIndex);
        }

        public int GetIDsCount(string table)
        {
            int id = -1;
            string temp;
            DataRow dataRow;
            DataTable dataTable;
            try
            {
                dataTable = manager.GetDataTable("SELECT count(*) FROM " + table + ";");
                if (dataTable.Rows.Count != 0)
                {
                    dataRow = dataTable.Rows[0];
                    temp = dataRow["count"].ToString();
                    id = int.Parse(temp);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return id;
        }

        public List<int> GetListIDs(string table, string index)
        {
            DataTable dataTable;
            List<int> ids = new List<int>();
            try
            {
                dataTable = manager.GetDataTable("SELECT " + index + " FROM " + table + ";");
                if (dataTable.Rows.Count != 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ids.Add(int.Parse(row[0].ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return ids;
        }

        public List<int> GetListIDs(string table, string index, string where)
        {
            DataTable dataTable;
            List<int> ids = new List<int>();
            try
            {
                dataTable = manager.GetDataTable("SELECT " + index + " FROM " + table + " WHERE " + where + ";");
                if (dataTable.Rows.Count != 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ids.Add(int.Parse(row[0].ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return ids;
        }

        public List<int> GetListRelationIDs(string table, string index1, string index2, int id)
        {
            DataTable dataTable;
            List<int> ids = new List<int>();
            try
            {
                dataTable = manager.GetDataTable("SELECT " + index2 + " FROM " + table + " WHERE " + index1 + "=" + id + ";");
                if (dataTable.Rows.Count != 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ids.Add(int.Parse(row[0].ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error accessing:\r\n" + e.Message);
            }
            return ids;
        }

        public bool InsertRelation(string table, string index1, string index2)
        {
            int id = NextIndex(Lookup.defaultIndex, table);
            int ret = manager.ExecuteNonQuery("INSERT INTO " + table + " VALUES(" + id + "," + index1 + "," + index2 + ");");
            if (ret > 0)
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
