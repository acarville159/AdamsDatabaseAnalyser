using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class InsertCall
    {

        public string TableName { get; set; }
        public List<ParsedTokenSection> Columns;
        public List<ParsedTokenSection> Values;

        public InsertCall(string tableName, List<ParsedTokenSection> columns,List<ParsedTokenSection> values)
        {
            TableName = Utils.StripStringOfSquareBrackets(tableName);
            Columns = columns;
            Values = values;
        }

    }
}