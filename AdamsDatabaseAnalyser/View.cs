using System;
using System.Collections.Generic;
using System.IO;

namespace AdamsDatabaseAnalyser
{
    public class View
    {
        public string ViewName { get; set; } = "";

        public List<Table> exposedTables = new List<Table>();
        public List<SelectCall> selectCalls = new List<SelectCall>();


        //Used to read in a stored procedure from a file
        public View(FileInfo file)
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

                #region Create View
                if (thisTokenValue == "CREATE" && nextTokenValue == "VIEW")
                {
                    //the next few tokens should hold the table name
                    ViewName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));
                }
                #endregion

                #region Defining All Select Calls
                if (ViewName != "" && thisTokenValue.ToLower() == "select")
                {
                    List<ParsedToken> selectTokens = Utils.CreateTokenBlockUntilString(tokens, tokenIndex + 1, "from");

                    List<ParsedTokenSection> selectArguments = Utils.SplitTokensIntoSections(selectTokens, ",");
                    SelectCall selectCall = new SelectCall();

                    foreach (ParsedTokenSection argument in selectArguments)
                    {
                        selectCall.AddArgument(argument);
                    }
                    selectCalls.Add(selectCall);



                }



                #region Defining Froms
                if (ViewName != "" && thisTokenValue.ToLower() == "from")
                {
                    string fromTarget = nextTokenValue;
                    if(fromTarget == "dbo")
                    {
                        fromTarget = Utils.GetTokenValue(tokenIndex + 3, tokens);
                    }
                    Table table = DatabaseSimulation.TryGetTable(fromTarget);
                    if (table != null)
                    {
                        if (!exposedTables.Contains(table))
                        {
                            exposedTables.Add(table);
                        }
                    }
                    else
                    {
                        //Not a table could be a view
                        View targetView = DatabaseSimulation.TryGetView(fromTarget);
                        if (targetView != null)
                        {
                            foreach (Table t in targetView.exposedTables)
                            {
                                if (!exposedTables.Contains(t))
                                {
                                    exposedTables.Add(t);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Defining Joins
                if (ViewName != "" && (thisTokenValue.ToLower() == "inner" || thisTokenValue.ToLower() == "outer" || thisTokenValue.ToLower() == "left"))
                {
                    if (nextTokenValue.ToLower() == "join")
                    {
                        string joinTarget = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 2, tokens));
                        if (joinTarget == "dbo")
                        {
                            joinTarget = Utils.GetTokenValue(tokenIndex + 4, tokens);
                        }
                        Table targetTable = DatabaseSimulation.TryGetTable(joinTarget);
                        if (targetTable != null)
                        {
                            if (!exposedTables.Contains(targetTable))
                            {
                                exposedTables.Add(targetTable);
                            }
                        }
                        else
                        {
                            //Not a tabl
                        }
                    }
                }
                #endregion


                #endregion
            }

        }


        public List<Field> GetViewFields()
        {
            List<Field> fields = new List<Field>();
            foreach (SelectCall call in selectCalls)
            {
                foreach (ParsedTokenSection select in call.selects)
                {
                    if(select.Tokens.Count > 0)
                    {
                        string lastToken = Utils.GetTokenValue(select.Tokens.Count - 1, select.Tokens);
                        foreach(Table t in exposedTables)
                        {
                            foreach(Field column in t.Columns)
                            {
                                fields.Add(column);
                            }
                        }
                    }
                }
            }

            return fields;
        }
    }
}