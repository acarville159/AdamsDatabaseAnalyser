using System.Collections.Generic;

namespace AdamsDatabaseAnalyser
{
    public class ParsedTokenSection
    {
        public List<ParsedToken> Tokens = new List<ParsedToken>();

        public void AddTOken(ParsedToken token)
        {
            Tokens.Add(token);
        }
    }
}