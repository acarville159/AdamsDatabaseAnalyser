using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class TableJoin : Join
    {
        public Table Table { get; set; }
        public string TableAlias { get; set; }


        public TableJoin(Table table, string alias)
        {
            Table = table;
            TableAlias = alias;
        }



    }
}