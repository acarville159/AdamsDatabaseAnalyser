using Babel;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    public class Table
    {

        public string TableName { get; set; } = "";
        public List<Field> Columns = new List<Field>();
        public List<Trigger> Triggers = new List<Trigger>();

        public void AddColumn(Field column)
        {
            Columns.Add(column);
        }

        internal Field TryGetColumn(string colName)
        {
            foreach (Field c in Columns)
            {
                if (c.FieldName == colName)
                {
                    return c;
                }
            }
            return null;
        }

        public Table()
        {

        }

        public Table(FileInfo file) : base()
        {
            string sql = File.ReadAllText(file.FullName);
            List<ParsedToken> tokens = Utils.ParseSql(sql);

            int currentDepth = 0;

            //token parsing
            for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                #region Token Values
                string lastTokenValue = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex - 1, tokens));
                string thisTokenValue = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex, tokens));
                string nextTokenValue = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 1, tokens));
                #endregion

                #region Create Table
                if (TableName == "" && thisTokenValue.ToLower() == "create" && nextTokenValue.ToLower() == "table")
                {
                    //the next few tokens should hold the table name
                    TableName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));
                }
                #endregion

                #region Define Columns

                if (TableName != "" && thisTokenValue == TableName && Columns.Count == 0)
                {
                    //This is where a function should start defining it's parameters
                    List<string> finisherList = new List<string>();
                    finisherList.Add("begin");
                    finisherList.Add("insert");
                    finisherList.Add("update");
                    finisherList.Add("execute");
                    finisherList.Add("select");
                    finisherList.Add("constraint");
                    finisherList.Add("delete");
                    finisherList.Add("set");
                    finisherList.Add("declare");
                    finisherList.Add("primary");
                    finisherList.Add("on");
                    finisherList.Add("if");
                    List<ParsedToken> parameterTokens = Utils.CreateTokenBlockUntilStrings(tokens, tokenIndex + 1, finisherList);

                    #region Cleanup Values
                    Utils.CleanUpParsedTokenSquareBrackets(parameterTokens);
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "(");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, ")");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "\n");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "\t");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, " ");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "clustered");
                    parameterTokens = Utils.CleanupParsedTokenList(parameterTokens, "out");

                    parameterTokens = Utils.CleanupParsedTokenListSequence(parameterTokens, "with", "recompile");
                    parameterTokens = Utils.CleanupParsedTokenListPlus(parameterTokens, "index", 2);
                    parameterTokens = Utils.CleanupParsedTokenListPlus(parameterTokens, "collate", 1);

                    parameterTokens = Utils.CleanupParsedTokenListNegativeNumbers(parameterTokens);

                    parameterTokens = Utils.CleanUpParsedTokenIdentity(parameterTokens);
                    #endregion

                    List<ParsedTokenSection> parsedTokenSections = Utils.SplitTokensIntoSections(parameterTokens, ",");

                    List<Field> columns = new List<Field>();

                    foreach (ParsedTokenSection tokenSection in parsedTokenSections)
                    {
                        Field f = Utils.CovertTokenSectionToField(tokenSection);
                        columns.Add(f);
                    }

                    Columns = columns;


                }
                #endregion

                #region Define Triggers
                if (TableName != "" && thisTokenValue.ToLower() == "create" && nextTokenValue.ToLower() == "trigger")
                {
                    //the next few tokens should hold the table name
                    string triggerName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));

                    //get the "on" section
                    List<ParsedToken> onSection = Utils.CreateTokenBlockUntilString(tokens, tokenIndex + 4, "as");

                    List<ParsedToken> triggerDefinition = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 4, "as", "end");

                    Trigger trigger = new Trigger(triggerName, onSection, triggerDefinition);
                    Triggers.Add(trigger);
                
                }

                #endregion
            }
        }

    }
}
