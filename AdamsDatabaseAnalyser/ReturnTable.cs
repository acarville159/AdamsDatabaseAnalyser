using System;

namespace AdamsDatabaseAnalyser
{
    public class ReturnTable : Table
    {
        public ReturnTableType returnTableType = ReturnTableType.DefinedColumns;
        public string ReturnTableName { get; set; }

        public ReturnTable(string tabName)
        {
            ReturnTableName = tabName;
        }

        public enum ReturnTableType
        {
            AllColumns,
            DefinedColumns,
        }
    }
}