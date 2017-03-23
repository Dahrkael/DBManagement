using System;
using System.Collections.Generic;
using System.Data;

namespace Dahrkael.DBManagement
{
    interface IDBBackend
    {
        bool      CreateTable(string table, Dictionary<string, string> fields);
        bool      TableExists(string table);

        int       NextIndex(string index, string table);
        int       Exists(string field, string table, string where);
        bool      Insert(Dictionary<string, object> values, string table);
        bool      Modify(Dictionary<string, object> values, string table, string where);
        bool      Delete(string table, string where);
        string    GetScalar(string field, string table, string where);
        DataTable GetDataTable(string table, string where);
        //int       OperateBlob(string field, string table, string where, object obj);
        
        int GetFirstID(string table);
        int GetFirstID(string table, string index);
        int GetLastID(string table);
        int GetLastID(string table, string index);
        int GetIDsCount(string table);
        List<int> GetListIDs(string table, string index);
        List<int> GetListIDs(string table, string index, string where);
        List<int> GetListRelationIDs(string table, string index1, string index2, int id);
        bool InsertRelation(string table, string index1, string index2);
    }
}
