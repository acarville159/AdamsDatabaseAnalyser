using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace AdamsDatabaseAnalyser
{
    public  class ParsedToken
    {
        public int Start;
        public int End;
        public bool IsPairMatch;
        public bool IsExecAutoParamHelp;
        public string Sql;
        public Tokens Token;

        public override string ToString()
        {
            return ("[TOKEN]" + Sql);
        }
    }
}