using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class FunctionCall
    {
        public string FunctionName;
        public List<string> Arguments = new List<string>();

        public FunctionCall(string functionName)
        {
            FunctionName = functionName;
        }

        public void AddArgument(string arg)
        {
            Arguments.Add(arg);
        }
    }
}