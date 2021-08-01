using System;
using System.Collections.Generic;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    public class Issue
    {
        public string FileName { get; set; } = "";
        public IssueType Type = IssueType.FUNC_ARG_IMP;
        public string Cause { get; set; } = "";

        public Issue(string fileName,IssueType type,string cause)
        {
            FileName = fileName;
            Type = type;
            Cause = cause;
        }

        public override string ToString()
        {
            return String.Format("Issue in {0} - {1} - {2} ", FileName, Type, Cause);
        }

        public enum IssueType
        {
            FUNC_ARG_IMP,
            SP_ARG_IMP,
            COMPARISON_IMP,
            INSERT_IMP,

        }
    }
}
