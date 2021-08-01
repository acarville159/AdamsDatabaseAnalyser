using System;
using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class SelectCall
    {
        public List<ParsedTokenSection> selects = new List<ParsedTokenSection>();

        internal void AddArgument(ParsedTokenSection arg)
        {
            selects.Add(arg);
        }

        public string GetFieldName(string fieldName)
        {
            foreach(ParsedTokenSection select in selects)
            {
                for (int i = 0; i < select.Tokens.Count; i++)
                {
                    string lastToken = Utils.GetTokenValue(i-1, select.Tokens);
                    string thisToken = Utils.GetTokenValue(i, select.Tokens);
                    string nextToken = Utils.GetTokenValue(i+1, select.Tokens);

                    if(thisToken.ToLower() == "as")
                    {
                        if(nextToken == fieldName)
                        {
                            return lastToken;
                        }
                    }
                }
            }

            return null;
        }
    }
}