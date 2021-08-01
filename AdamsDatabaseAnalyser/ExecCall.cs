using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class ExecCall
    {
        public string ProcedureName { get; set; }
        public List<ParsedTokenSection> ArgumentSections;

        public ExecCall(string procedureName,List<ParsedTokenSection> args)
        {
            ProcedureName = procedureName;
            ArgumentSections = args;
        }
    }
}