using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Dahrkael.DBManagement.Backends
{
    class SQLiteWrapper
    {
        private String dbConnection = "";

        /// <summary>
        ///     Constructor por defecto.
        /// </summary>
        public SQLiteWrapper()
            : this("database", "") // FIXME this should be something, not empty
        {
            
        }

        /// <summary>
        ///     Constructor de un sólo parámetro para indicar el fichero de base de datos.
        /// </summary>
        /// <param name="ficheroEntrada">El archivo de base de datos.</param>
        public SQLiteWrapper(string dataSource)
            : this(dataSource, "masterkey")
        {

        }

        /// <summary>
        /// Constructor para indicar opciones avanzadas de conexión.
        /// </summary>
        public SQLiteWrapper(string dataSource, string userPass)
        {
            SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder();
            connectionStringBuilder.DataSource = "";
            connectionStringBuilder.Password = userPass;
            connectionStringBuilder.Version = 3;
            connectionStringBuilder.FailIfMissing = false;
            connectionStringBuilder.Pooling = false;
            dbConnection = connectionStringBuilder.ToString();
        }

        /// <summary>
        ///     Permite realizar una consulta sobre la base de datos.
        /// </summary>
        /// <param name="sql">SQL a ejecutar.</param>
        /// <returns>DataTable con los resultados de la consulta.</returns>
        public DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            SQLiteDataReader dataReader = null;
            try
            {
                connection = new SQLiteConnection(dbConnection);
                connection.Open();
                command = new SQLiteCommand(sql, connection);
                dataReader = command.ExecuteReader();
                dt.Load(dataReader);
                CloseConnection(dataReader, connection);
            }
            catch (Exception e)
            {
                CloseConnection(dataReader, connection);
                throw new Exception(e.Message);
                //new MensajeExcepcion(e, "Error de consulta Firebird");
            }
            return dt;
        }

        /// <summary>
        ///     Permite ejecutar acciones que no son consultas.
        /// </summary>
        /// <param name="sql">SQL a ejecutar.</param>
        /// <returns>Entero con el número de filas afectadas.</returns>
        public int ExecuteNonQuery(string sql)
        {
            int result = 0;
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            try
            {
                connection = new SQLiteConnection(dbConnection);
                connection.Open();
                command = new SQLiteCommand(sql, connection);
                result = command.ExecuteNonQuery();
                CloseConnection(connection);
            }
            catch (Exception e)
            {
                CloseConnection(connection);
                throw new Exception(e.Message);
                //new MensajeExcepcion(e, "Error de consulta Firebird");
            }
            return result;
        }

        /// <summary>
        ///     Permite ejecutar acciones que no son consultas.
        /// </summary>
        /// <param name="sql">Lista de SQLs a ejecutar.</param>
        /// <returns>Entero con el número de filas afectadas.</returns>
        public List<int> ExecuteNonQuery(List<string> sql)
        {
            int result = 0;
            List<int> results = new List<int>();
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            SQLiteCommand command = null;
            try
            {
                connection = new SQLiteConnection(dbConnection);
                connection.Open();
                transaction = connection.BeginTransaction();
                command = new SQLiteCommand();
                command.Connection = connection;
                command.Transaction = transaction;
                foreach (string s in sql)
                {
                    result = command.ExecuteNonQuery();
                    results.Add(result);
                }
                transaction.Commit();
                CloseConnection(connection);
            }
            catch (Exception e)
            {
                results.Clear();
                results.Add(-1);
                transaction.Rollback();
                CloseConnection(connection);
                throw new Exception(e.Message);
                //new MensajeExcepcion(e, "Error de consulta Firebird");
            }
            return results;
        }

        /// <summary>
        ///     Permite ejecutar acciones que no son consultas.
        /// </summary>
        /// <param name="sql">SQL a ejecutar.</param>
        /// <param name="datos">Datos a insertar.</param>
        /// <returns>Entero con el número de filas afectadas.</returns>
        public int ExecuteNonQueryBlob(string sql, byte[] data)
        {
            int result = 0;
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            try
            {
                connection = new SQLiteConnection(dbConnection);
                connection.Open();
                command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue(@"?", data);
                result = command.ExecuteNonQuery();
                CloseConnection(connection);
            }
            catch (Exception e)
            {
                CloseConnection(connection);
                throw new Exception(e.Message);
                //new MensajeExcepcion(e, "Error de consulta Firebird");
            }
            return result;
        }

        /// <summary>
        ///     Permite obtener elementos simples de la Base de Datos.
        /// </summary>
        /// <param name="sql">SQL a ejecutar.</param>
        /// <returns>Cadena.</returns>
        public string ExecuteScalar(string sql)
        {
            Object value = null;
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            SQLiteDataReader dataReader = null;
            try
            {
                connection = new SQLiteConnection(dbConnection);
                connection.Open();
                command = new SQLiteCommand(sql, connection);
                value = command.ExecuteScalar();
                CloseConnection(dataReader, connection);
            }
            catch (Exception e)
            {
                CloseConnection(dataReader, connection);
                throw new Exception(e.Message);
                //new MensajeExcepcion(e, "Error de consulta Firebird");
            }
            if (value != null)
            {
                return value.ToString();
            }
            return "0";
        }

        /// <summary>
        ///     Permite actualizar filas.
        /// </summary>
        /// <param name="nombreTabla">Tabla a actualizar.</param>
        /// <param name="datos">Diccionario con nombres de columna y sus valores.</param>
        /// <param name="where">Clausula where de la sentencia de actualización.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool Update(String table, Dictionary<String, String> data, String where)
        {
            String vals = "";

            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> values in data)
                {
                    vals += String.Format(" {0} = '{1}',", values.Key.ToString(), values.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", table, vals, where));
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Permite actualizar filas.
        /// </summary>
        /// <param name="nombreTabla">Tabla a actualizar.</param>
        /// <param name="datos">Diccionario con nombres de columna y sus valores.</param>
        /// <param name="where">Clausula where de la sentencia de actualización.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool Update(String table, Dictionary<String, object> data, String where)
        {
            String vals = "";

            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, object> values in data)
                {
                    if (values.Value != null)
                    {
                        if (values.Value.GetType() == typeof(String) || values.Value.GetType() == typeof(DateTime))
                        {
                            vals += String.Format(" {0} = '{1}',", values.Key.ToString(), values.Value.ToString());
                        }
                        else
                        {
                            if (values.Value.GetType() == typeof(double))
                            {
                                vals += String.Format(" {0} = {1},", values.Key.ToString(),
                                                      values.Value.ToString().Replace(',', '.'));
                            }
                            else
                            {
                                vals += String.Format(" {0} = {1},", values.Key.ToString(), values.Value.ToString());
                            }
                        }
                    }
                    else
                    {
                        vals += String.Format(" {0} = {1},", values.Key.ToString(), "''");
                    }

                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                int affected = affected = this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", table, vals, where));
            }
            catch
            {
                return false;
            }
            return false;
        }

        /// <summary>
        ///     Permite borrar filas de la Base de Datos.
        /// </summary>
        /// <param name="nombreTabla">Tabla a borrar.</param>
        /// <param name="where">Clausula where de la sentencia de borrado.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool Delete(String table, String where)
        {
            try
            {
                if (this.ExecuteNonQuery(String.Format("delete from {0} where {1};", table, where)) == 0)
                {
                    return false;
                }
            }
            catch (Exception fail)
            {
                throw new Exception(fail.Message);
                //MessageBox.Show(fail.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Permite insertar filas en la Base de Datos
        /// </summary>
        /// <param name="nombreTabla">Tabla en la que se insertaran los datos.</param>
        /// <param name="datos">Diccionario con nombres de columna y sus valores.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool Insert(String table, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";

            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", table, columns, values));
            }
            catch (Exception fail)
            {
                throw new Exception(fail.Message);
                //MessageBox.Show(fail.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Permite insertar filas en la Base de Datos
        /// </summary>
        /// <param name="nombreTabla">Tabla en la que se insertaran los datos.</param>
        /// <param name="datos">Diccionario con nombres de columna y sus valores.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool Insert(String table, Dictionary<String, object> datos)
        {
            String columns = "";
            String values = "";

            foreach (KeyValuePair<String, object> val in datos)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                if (val.Value != null)
                {
                    if (val.Value.GetType() == typeof(String) || val.Value.GetType() == typeof(DateTime))
                    {
                        values += String.Format(" '{0}',", val.Value.ToString());
                    }
                    else
                    {
                        if (val.Value.GetType() == typeof(double))
                        {
                            values += String.Format(" {0},", val.Value.ToString().Replace(',', '.'));
                        }
                        else
                        {
                            //Cambiar esto para aceptar BLOB
                            values += String.Format(" {0},", val.Value.ToString());
                        }
                    }
                }
                else
                {
                    values += String.Format(" {0},", "''"); ;
                }

            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                // FIXME Cambiar esto para aceptar BLOB
                int afectadas = this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", table, columns, values));
            }
            catch (Exception fail)
            {
                throw new Exception(fail.Message);
                //MessageBox.Show(fail.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Permite limpiar la base de datos.
        /// </summary>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool ClearDB()
        {
            DataTable tables;
            try
            {
                tables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");
                foreach (DataRow table in tables.Rows)
                {
                    this.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Permite limpiar tablas.
        /// </summary>
        /// <param name="table">Nombre de la tabla a limpiar.</param>
        /// <returns>Booleano que indica si la operación es correcta.</returns>
        public bool ClearTable(String table)
        {
            try
            {
                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Verifica si la conexión está establecida.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool CheckConnection(SQLiteConnection connection)
        {
            try
            {
                if (connection.State == ConnectionState.Open)
                { return true; }
                else
                { return false; }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Permite optimizar el espacio en disco.
        /// </summary>
        public bool OptimizeDB()
        {
            try
            {
                this.ExecuteNonQuery("vacuum;");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Cierra la conexión.
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="connection"></param>
        public void CloseConnection(SQLiteDataReader dataReader, SQLiteConnection connection)
        {
            try
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                }
                if (connection != null)
                {
                    connection.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Cierra la conexión.
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="connection"></param>
        public void CloseConnection(SQLiteConnection connection)
        {
            try
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        public bool Renumerate(String table, String where)
        {
            try
            {
                if (this.ExecuteNonQuery(String.Format("update {0} set {1} = {1}-1 where {2};", table, Lookup.defaultIndex, where)) == 0)
                {
                    return false;
                }
            }
            catch (Exception fail)
            {
                throw new Exception(fail.Message);
                //MessageBox.Show(fail.Message);

                return false;
            }
            return true;
        }
    }
}
