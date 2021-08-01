using System;
using System.Collections.Generic;
using System.IO;

namespace AdamsDatabaseAnalyser
{
    public class UserDefinedFunction
    {

        public string FunctionName { get; set; } = "";
        public List<Field> Parameters = new List<Field>();
        public DataBaseDataType ReturnType = DataBaseDataType.table;
        public bool returnsSelectCalls = false;
        public Table ReturnTable = null;

        public List<Field> DeclaredFields = new List<Field>();

        public List<Table> exposedTables = new List<Table>();

        public List<FunctionCall> functionCalls = new List<FunctionCall>();
        public List<ComparisonCall> comparisonCalls = new List<ComparisonCall>();
        public List<SelectCall> selectCalls = new List<SelectCall>();
        public List<InsertCall> insertCalls = new List<InsertCall>();
        public List<ExecCall> execCalls = new List<ExecCall>();
        public Dictionary<string, Table> aliasTables = new Dictionary<string, Table>();



        public UserDefinedFunction(FileInfo file)
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

                #region Create Function
                if (thisTokenValue == "CREATE" && nextTokenValue == "FUNCTION")
                {
                    //the next few tokens should hold the table name
                    FunctionName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));
                }
                #endregion

                #region Defining All Temp tables   
                if (FunctionName != "" && thisTokenValue.ToLower() == "create" && nextTokenValue.ToLower() == "table")
                {
                    string tablename = Utils.GetTokenValue(tokenIndex + 2, tokens);
                    //read the table
                    List<ParsedToken> tableContent = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 3, "(", ")");

                    Table t = Utils.CreateTableFromTokens(tableContent, tablename);

                    exposedTables.Add(t);
                }

                #endregion

                #region Defining All Select Calls
                if (FunctionName != "" && thisTokenValue.ToLower() == "select")
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
                #endregion

                #region Define Parameters

                if (FunctionName != "" && thisTokenValue == FunctionName)
                {
                    //This is where a function should start defining it's parameters
                    List<ParsedToken> parameterTokens = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex, "(", ")");

                    List<ParsedTokenSection> parsedTokenSections = Utils.SplitTokensIntoSections(parameterTokens, ",");

                    List<Field> parameters = new List<Field>();

                    foreach (ParsedTokenSection tokenSection in parsedTokenSections)
                    {
                        Field f = Utils.CovertTokenSectionToField(tokenSection);
                        if (f.DataType == DataBaseDataType.table)
                        {
                            //search ud tables
                            UserDefinedTableType t = DatabaseSimulation.TryGetUserDefinedTableType(f.DataTypeString);
                            if (!exposedTables.Contains(t))
                            {
                                exposedTables.Add(t);
                            }
                        }
                        parameters.Add(f);
                    }

                    Parameters = parameters;


                }
                #endregion

                #region Define Return Type
                if (FunctionName != "" && thisTokenValue.ToLower() == "returns")
                {
                    if(Utils.GetTokenValue(tokenIndex + 1, tokens).ToLower() == "table" && Utils.GetTokenValue(tokenIndex + 1, tokens).ToLower() == "as")
                    {
                        //will return all the select calls
                        //but we don't have all the select calls stored yet????
                        returnsSelectCalls = true;

                    }
                    {

                    }
                    if (Utils.GetTokenValue(tokenIndex + 2, tokens).ToLower() == "table")
                    {
                        //returns a table
                        ReturnType = DataBaseDataType.table;
                        string tableName = nextTokenValue;
                        List<ParsedToken> tableContent = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 3, "(", ")");

                        Table t = Utils.CreateTableFromTokens(tableContent, tableName);

                        ReturnTable = t;

                    }
                    else
                    {
                        //returns just a bog standard data type
                        DataBaseDataType dataType = Utils.GetDataType(nextTokenValue);
                        if (dataType != DataBaseDataType.none)
                        {
                            ReturnType = dataType;
                        }
                        else
                        {

                        }
                    }
                }
                #endregion

                #region Defining Delcares
                if (FunctionName != "" && thisTokenValue.ToLower() == "declare")
                {
                    List<string> declareFinishers = new List<string>();
                    declareFinishers.Add("declare");
                    declareFinishers.Add("if");
                    declareFinishers.Add("for");
                    declareFinishers.Add("declare");
                    declareFinishers.Add("select");
                    declareFinishers.Add("update");
                    declareFinishers.Add("from");
                    declareFinishers.Add("for");
                    declareFinishers.Add("insert");
                    declareFinishers.Add("dbo");
                    List<ParsedToken> declareContent = Utils.CreateTokenBlockUntilStringsWithDepth(tokens, tokenIndex + 1, declareFinishers,"(",")");

                    List<ParsedTokenSection> tokenSections = Utils.SplitTokensIntoSections(declareContent, ",");

                    foreach (ParsedTokenSection section in tokenSections)
                    {
                        List<string> untilStrings = new List<string>();
                        untilStrings.Add("=");
                        List<ParsedToken> sectionTokens = Utils.CreateTokenBlockUntilStrings(section.Tokens, 0, untilStrings);
                        string delcaredName = Utils.GetTokenValue(0, sectionTokens);
                        string type = Utils.GetTokenValue(1, sectionTokens);
                        if (type.ToLower() == "as")
                        {
                            type = Utils.GetTokenValue(2, sectionTokens);
                        }
                        DataBaseDataType dataType = Utils.GetDataType(type);
                        if (dataType == DataBaseDataType.table)
                        {
                            if (DatabaseSimulation.TryGetUserDefinedTableType(type) != null)
                            {
                                UserDefinedTableType userDefinedTableType = DatabaseSimulation.TryGetUserDefinedTableType(type);
                                if (!exposedTables.Contains(userDefinedTableType))
                                {
                                    exposedTables.Add(userDefinedTableType);
                                }
                                DeclaredFields.Add(new Field(delcaredName, DataBaseDataType.table, -1));
                                aliasTables.Add(delcaredName, userDefinedTableType);
                            }
                            else
                            {
                                List<ParsedToken> tableContent = Utils.CreateTokenBlockWithinStrings(sectionTokens, 2, "(", ")");

                                #region Cleanup Values
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "(");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, ")");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "\n");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "\t");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "clustered");
                                tableContent = Utils.CleanupParsedTokenList(tableContent, "out");

                                tableContent = Utils.CleanupParsedTokenListSequence(tableContent, "with", "recompile");
                                tableContent = Utils.CleanupParsedTokenListPlus(tableContent, "index", 2);
                                tableContent = Utils.CleanupParsedTokenListPlus(tableContent, "collate", 1);

                                tableContent = Utils.CleanupParsedTokenListNegativeNumbers(tableContent);

                                tableContent = Utils.CleanUpParsedTokenIdentity(tableContent);
                                #endregion

                                List<ParsedTokenSection> parsedTokenSections = Utils.SplitTokensIntoSections(tableContent, ",");

                                List<Field> tableColumns = new List<Field>();

                                foreach (ParsedTokenSection tokenSection in parsedTokenSections)
                                {
                                    tableColumns.Add(Utils.CovertTokenSectionToField(tokenSection));
                                }

                                Table t = new Table();
                                t.TableName = delcaredName;

                                foreach (Field f in tableColumns)
                                {
                                    t.AddColumn(f);
                                }

                                exposedTables.Add(t);
                            }



                        }
                        else
                        {
                            DeclaredFields.Add(new Field(delcaredName, dataType, -1));
                        }
                    }


                }
                #endregion

                #region Defining Froms
                if (FunctionName != "" && thisTokenValue.ToLower() == "from")
                {
                    string fromTarget = nextTokenValue;
                    if (fromTarget == "dbo")
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
                if (FunctionName != "" && (thisTokenValue.ToLower() == "inner" || thisTokenValue.ToLower() == "outer" || thisTokenValue.ToLower() == "left" || thisTokenValue.ToLower() == "cross"))
                {
                    if (nextTokenValue.ToLower() == "join")
                    {
                        string joinTarget = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 2, tokens));
                        if (joinTarget == "dbo")
                        {
                            joinTarget = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));
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
                            //Not a table could be a view
                            View targetView = DatabaseSimulation.TryGetView(joinTarget);
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
                }
                #endregion

                #region Defining All Function Calls
                if (FunctionName != "" && thisTokenValue != FunctionName && thisTokenValue.ToLower().Contains("func"))
                {
                    string functionName = thisTokenValue;


                    FunctionCall functionCall = new FunctionCall(functionName);
                    List<ParsedToken> functionContent = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex, "(", ")");

                    List<ParsedTokenSection> functionArguments = Utils.SplitTokensIntoSections(functionContent, ",");

                    foreach (ParsedTokenSection argument in functionArguments)
                    {
                        string arg = "";
                        foreach (ParsedToken token in argument.Tokens)
                        {
                            arg += token.Sql;
                        }
                        functionCall.AddArgument(arg);
                    }
                    functionCalls.Add(functionCall);
                }
                #endregion

                #region Defining All Comparison Calls
                if (FunctionName != "" && thisTokenValue.ToLower() == "=")
                {
                    string justBefore = lastTokenValue;
                    string justAfter = nextTokenValue;
                    if (justBefore != "" && justAfter != "")
                    {
                        if (justBefore != ")" && justBefore != "!" && justBefore != ">" && justBefore != "<" && justBefore != ") " && justBefore.ToLower() != "int" && justBefore.ToLower() != "bit" && justBefore.ToLower() != "datetime" && justBefore.ToLower() != "date")
                        {
                            //we dont want to do this for befre (think about it)
                            ////detect to see if these are aliased values
                            ////get the value before justBefore
                            //string beforeJustBefore = Utils.GetTokenValue(tokenIndex - 2, tokens);
                            //if(beforeJustBefore == ".")
                            //{
                            //    //before value is aliased
                            //    justBefore = Utils.GetTokenValue(tokenIndex - 3, tokens);
                            //}

                            //get the value after justAfter
                            string afterJustAfter = Utils.GetTokenValue(tokenIndex + 2, tokens);
                            if (afterJustAfter == ".")
                            {
                                //after value is aliased
                                justAfter = Utils.GetTokenValue(tokenIndex + 3, tokens);
                            }
                            else
                            {
                                if (justAfter.ToLower() == "replace")
                                {
                                    //add on everything inside the brackets
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "max")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "min")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "concat")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "substring")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "convert")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "datediff")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "cast")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "left")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }


                                if (justAfter.ToLower() == "right")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "charindex")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }

                                if (justAfter.ToLower() == "isnull")
                                {
                                    List<ParsedToken> content = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")", true);
                                    for (int i = 0; i < content.Count; i++)
                                    {
                                        ParsedToken t = content[i];
                                        justAfter += t.Sql;

                                        if (i < content.Count - 1)
                                        {
                                            justAfter += " ";
                                        }
                                    }
                                }
                            }




                            ComparisonCall comparisonCall = new ComparisonCall(justBefore, justAfter);
                            comparisonCalls.Add(comparisonCall);
                        }

                    }
                }
                #endregion

                #region Defining All Updates
                if (FunctionName != "" && thisTokenValue.ToLower() == "update")
                {
                    string updateName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 1, tokens));
                    if (updateName == "dbo")
                    {
                        updateName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 3, tokens));
                    }
                    //try get table
                    Table tab = DatabaseSimulation.TryGetTable(updateName);
                    if (tab != null)
                    {
                        if (!exposedTables.Contains(tab))
                        {
                            exposedTables.Add(tab);
                        }
                    }

                }
                #endregion

                #region Define All Inserts
                if (FunctionName != "" && thisTokenValue.ToLower() == "insert" && nextTokenValue.ToLower() == "into")
                {
                    string insertTableName = Utils.GetTokenValue(tokenIndex + 2, tokens);

                    //get the columns
                    List<ParsedToken> columnTokens = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2, "(", ")");

                    //get the values
                    List<ParsedToken> valueTokens = Utils.CreateTokenBlockWithinStrings(tokens, tokenIndex + 2 + columnTokens.Count + 2 + 1, "(", ")");

                    List<ParsedTokenSection> columns = Utils.SplitTokensIntoSections(columnTokens, ",");
                    List<ParsedTokenSection> values = Utils.SplitTokensIntoSections(valueTokens, ",");


                    InsertCall insertCall = new InsertCall(insertTableName, columns, values);
                    insertCalls.Add(insertCall);

                }
                #endregion

                #region Define All Execs
                if (FunctionName != "" && thisTokenValue.ToLower() == "exec")
                {
                    string execName = nextTokenValue;
                    if (execName != "" && execName != " ")
                    {
                        List<string> finishers = new List<string>();
                        finishers.Add("set");
                        finishers.Add("from");
                        finishers.Add("insert");
                        finishers.Add("delcare");
                        finishers.Add("print");
                        finishers.Add("update");
                        finishers.Add("delete");
                        finishers.Add("end");
                        finishers.Add(";");
                        List<ParsedToken> execTokens = Utils.CreateTokenBlockUntilStrings(tokens, tokenIndex + 2, finishers);
                        execTokens = Utils.CleanupParsedTokenList(execTokens, " ");
                        execTokens = Utils.CleanupParsedTokenList(execTokens, "\t");
                        execTokens = Utils.CleanupParsedTokenList(execTokens, "\n");
                        execTokens = Utils.CleanupParsedTokenList(execTokens, "");

                        List<ParsedTokenSection> execParameters = Utils.SplitTokensIntoSections(execTokens, ",");

                        ExecCall execCall = new ExecCall(execName, execParameters);
                        execCalls.Add(execCall);
                    }

                }

                #endregion





            }
        }


        public Field GetField(string fieldName)
        {


            if (fieldName.Contains("."))
            {
                string[] parts = fieldName.Split('.');
                fieldName = parts[1];
            }

            fieldName = fieldName.Replace("\t", "");

            fieldName = Utils.StripStringOfSquareBrackets(fieldName);
            fieldName = fieldName.Trim();

            if (fieldName == "" || fieldName.Length == 0)
            {
                return null;
            }

            //First check parameters
            foreach (Field f in Parameters)
            {
                if (f.FieldName.ToLower() == fieldName.ToLower())
                {
                    return f;
                }
            }


            //Check declared types
            foreach (Field f in DeclaredFields)
            {
                if (f.FieldName.ToLower() == fieldName.ToLower())
                {
                    return f;
                }
            }

            //Check exposed tables
            foreach (Table t in exposedTables)
            {
                foreach (Field f in t.Columns)
                {
                    if (f.FieldName.ToLower() == fieldName.ToLower())
                    {
                        return f;
                    }
                }
            }

            //Could be a field taken from select under an alias
            foreach (SelectCall selectCall in selectCalls)
            {
                if (selectCall.GetFieldName(fieldName) != null)
                {
                    string fieldNameString = selectCall.GetFieldName(fieldName);
                    return GetField(fieldNameString);

                }
            }

            //Could be a function return field
            if (fieldName.Contains("func"))
            {
                UserDefinedFunction function = DatabaseSimulation.TryGetUserDefinedFunction(fieldName);
                if (function != null)
                {
                    return new Field(fieldName, function.ReturnType, -1);
                }
            }

            //Could be a field that is from the return table of one of the called functions
            foreach (FunctionCall functionCall in functionCalls)
            {
                UserDefinedFunction function = DatabaseSimulation.TryGetUserDefinedFunction(functionCall.FunctionName);
                if (function != null)
                {
                    if (function.ReturnTable != null)
                    {
                        if (!exposedTables.Contains(function.ReturnTable))
                        {
                            exposedTables.Add(function.ReturnTable);
                            foreach (Field f in function.ReturnTable.Columns)
                            {
                                if (f.FieldName.ToLower() == fieldName.ToLower())
                                {
                                    return f;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (function.returnsSelectCalls)
                        {
                            foreach (SelectCall select in function.selectCalls)
                            {
                                if (select.GetFieldName(fieldName) != null)
                                {
                                    string fieldNameString = select.GetFieldName(fieldName);

                                }
                            }
                        }
                    }
                }
            }

            //Could be a defined field i.e 'VALUE' or N'VALUE'

            if (fieldName.ToLower().Contains("selectcount"))
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "getdate")
            {
                return new Field(fieldName, DataBaseDataType.datetime, -1, true);
            }


            if ((fieldName.Length > 1) && fieldName[0] == 'N' && fieldName[1] == '\'')
            {
                //defined nvarchar
                return new Field(fieldName, DataBaseDataType.nvarchar, -1, true);
            }

            if (fieldName.ToLower() == "null")
            {
                //defined null
                return new Field(fieldName, DataBaseDataType.none, -1, true);
            }

            if (fieldName[0] == '\'' && fieldName[fieldName.Length - 1] == '\'')
            {
                //defined varchar
                return new Field(fieldName, DataBaseDataType.varchar, -1, true);
            }

            if (Utils.isAlpha(fieldName))
            {
                //defined int field
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "default")
            {
                return new Field(fieldName, DataBaseDataType.none, -1, true);
            }


            if (fieldName.ToLower() == "count")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "dateadd")
            {
                return new Field(fieldName, DataBaseDataType.date, -1, true);
            }

            if (fieldName.ToLower() == "@@fetch_status")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "@@identity")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "scope_identity")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "sysname")
            {
                return new Field(fieldName, DataBaseDataType.nvarchar, -1, true);
            }

            if (fieldName.ToLower() == "is_member")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "database_principal_id")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "object_id")
            {
                return new Field(fieldName, DataBaseDataType.integer, -1, true);
            }

            if (fieldName.ToLower() == "row_number")
            {
                return new Field(fieldName, DataBaseDataType.bigint, -1, true);
            }

            string debugFieldName = fieldName;

            if(fieldName.Length > 3 && fieldName.ToLower().Substring(0,3) == "max")
            {
                string workingPart = fieldName.Remove(0, 4);
                if(workingPart[workingPart.Length-1] == ')')
                {
                    workingPart = workingPart.Remove(workingPart.Length - 1, 1);
                }

                workingPart = workingPart.Trim();

                Field paramField = GetField(workingPart);
                if(paramField != null)
                {
                    return new Field("MAX-" + workingPart, paramField.DataType, -1, true);
                }

            }

            if (fieldName.Length > 3 && fieldName.ToLower().Substring(0, 3) == "min")
            {
                string workingPart = fieldName.Remove(0, 4);
                if (workingPart[workingPart.Length - 1] == ')')
                {
                    workingPart = workingPart.Remove(workingPart.Length - 1, 1);
                }

                workingPart = workingPart.Trim();

                Field paramField = GetField(workingPart);
                if (paramField != null)
                {
                    return new Field("MIN-" + workingPart, paramField.DataType, -1, true);
                }

            }

            if(fieldName.Length > 7 && fieldName.ToLower().Substring(0,7) == "convert")
            {
                string workingPart = fieldName.Remove(0, 8);
                if (workingPart[workingPart.Length - 1] == ')')
                {
                    workingPart = workingPart.Remove(workingPart.Length - 1, 1);
                }

                workingPart = workingPart.Trim();
                string newWorkingPart = "";
                //read until comma
                foreach(Char c in workingPart)
                {
                    if(c != ',')
                    {
                        newWorkingPart += c;
                    }
                    else
                    {
                        break;
                    }
                }


                workingPart = newWorkingPart.Replace(" ","");
                newWorkingPart = "";
                //read until bracket
                foreach (Char c in workingPart)
                {
                    if (c != '(')
                    {
                        newWorkingPart += c;
                    }
                    else
                    {
                        break;
                    }
                }
                workingPart = newWorkingPart;

                DataBaseDataType type = Utils.GetDataType(workingPart);
                return new Field("CONVERT-"+type,type,-1);

            }

            ErrorManager.Log("could not find field " + fieldName + " in " + FunctionName);
            return null;
        }

        public List<Issue> GetIssues()
        {
            List<Issue> issues = new List<Issue>();

            //Check function calls
            foreach (FunctionCall functionCall in functionCalls)
            {
                //Check for errors
                //we're looking to see if that data types of the parameters match the arguments
                string functionName = functionCall.FunctionName;
                UserDefinedFunction function = DatabaseSimulation.TryGetUserDefinedFunction(functionName);
                if (function != null)
                {
                    if (function.Parameters.Count == functionCall.Arguments.Count)
                    {
                        //check each
                        for (int i = 0; i < function.Parameters.Count; i++)
                        {
                            Field parameter = function.Parameters[i];
                            string argString = functionCall.Arguments[i];
                            //Get the field of this string
                            Field argument = GetField(argString);
                            if (argument != null)
                            {
                                if (argument.definedField)
                                {
                                    if (Utils.CausesImplicitConversion(argument.DataType, argument.FieldName, parameter.DataType))
                                    {
                                        //not good
                                        issues.Add(new Issue(FunctionName, Issue.IssueType.FUNC_ARG_IMP, String.Format("Argument {0} ({1}) does not match datatype of parameter {2} ({3}) when calling {4}", argument.FieldName, argument.DataType, parameter.FieldName, parameter.DataType, function.FunctionName)));
                                    }
                                }
                                else
                                {
                                    if (Utils.CausesImplicitConversion(argument.DataType, parameter.DataType))
                                    {
                                        //not good
                                        issues.Add(new Issue(FunctionName, Issue.IssueType.FUNC_ARG_IMP, String.Format("Argument {0} ({1}) does not match datatype of parameter {2} ({3}) when calling {4}", argument.FieldName, argument.DataType, parameter.FieldName, parameter.DataType, function.FunctionName)));
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    //ErrorManager.Log("Could not find function: " + functionName + " in " + FunctionName);
                }

            }

            //Check comparison calls
            foreach (ComparisonCall comparisonCall in comparisonCalls)
            {
                string field1Name = comparisonCall.FirstField;
                string field2Name = comparisonCall.SecondField;

                if (field1Name.Contains("."))
                {
                    string[] parts = field1Name.Split('.');
                    field1Name = parts[1];
                }

                if (field2Name.Contains("."))
                {
                    string[] parts = field2Name.Split('.');
                    field2Name = parts[0];
                }

                Field field1 = GetField(field1Name);
                Field field2 = GetField(field2Name);

                if (field1 != null && field2 != null)
                {
                    if (Utils.CausesImplicitConversion(field1.DataType, field2.DataType))
                    {
                        issues.Add(new Issue(FunctionName, Issue.IssueType.COMPARISON_IMP, String.Format("{0} ({1}) does not match datatype in comparison with {2} ({3}) in {4}", field1.FieldName, field1.DataType, field2.FieldName, field2.DataType, String.Format("{0} = {1}", field1.FieldName, field2.FieldName))));

                    }
                }
                else
                {

                }

            }

            //Cehck insert calls
            foreach (InsertCall insertCall in insertCalls)
            {
                string tableName = insertCall.TableName;
                Table table = null;
                foreach (Table t in exposedTables)
                {
                    if (t.TableName == tableName)
                    {
                        table = t;
                        break;
                    }
                }
                if (table == null)
                {
                    //maybe the return table
                    if (ReturnTable != null)
                    {
                        if (ReturnTable.TableName.ToLower() == tableName.ToLower())
                        {
                            table = ReturnTable;
                        }
                    }
                }
                if (table == null)
                {
                    //maybe a table that was added as an alias
                    if (aliasTables.ContainsKey(tableName))
                    {
                        table = aliasTables[tableName];
                    }

                }
                if (table != null)
                {
                    List<ParsedTokenSection> columns = insertCall.Columns;
                    List<ParsedTokenSection> values = insertCall.Values;
                    if (columns.Count == values.Count)
                    {
                        int i = 0;
                        foreach (ParsedTokenSection colToken in columns)
                        {
                            Field column = table.TryGetColumn(colToken.Tokens[0].Sql);
                            if (column != null)
                            {
                                //get the value
                                Field value = GetField(values[i].Tokens[0].Sql);
                                if (value != null)
                                {
                                    //comapre the data type
                                    if (Utils.CausesImplicitConversion(column.DataType, value.DataType))
                                    {
                                        issues.Add(new Issue(FunctionName, Issue.IssueType.INSERT_IMP, String.Format("{0} ({1}) is inserted into column {2} ({3}) in {4} causing implicit conversion", value.FieldName, value.DataType, column.FieldName, column.DataType, insertCall.TableName)));
                                    }
                                }
                            }
                            else
                            {

                            }
                            i++;
                        }
                    }
                    else
                    {
                        //ErrorManager.Log("Error Insert call has unmatching lengths in: " + FunctionName);
                    }
                }
                else
                {
                    ErrorManager.Log("Error Insert call has unknown table " + tableName + " in: " + FunctionName);

                }
            }

            //Check exec calls
            foreach (ExecCall execCall in execCalls)
            {
                string execName = execCall.ProcedureName;
                StoredProcedure execProcedure = DatabaseSimulation.TryGetStoredProcedure(execName);
                if (execProcedure != null)
                {
                    //we have the procedure
                    //compare the args for the procedure to what we've got
                    if (execProcedure.Parameters.Count == execCall.ArgumentSections.Count)
                    {
                        int i = 0;
                        foreach (Field parameter in execProcedure.Parameters)
                        {
                            DataBaseDataType paramType = parameter.DataType;

                            Field argField = GetField(execCall.ArgumentSections[i].Tokens[0].Sql);

                            if (argField != null)
                            {
                                DataBaseDataType argType = argField.DataType;
                                if (Utils.CausesImplicitConversion(argType, paramType))
                                {
                                    issues.Add(new Issue(FunctionName, Issue.IssueType.SP_ARG_IMP, String.Format("{0} ({1}) argument datatype does not match parameter {2} ({3}) when calling {4}", argField.FieldName, argField.DataType, parameter.FieldName, paramType, execProcedure.ProcedureName)));
                                }

                            }
                            else
                            {
                                ErrorManager.Log("Exec cannot find field: " + execCall.ArgumentSections[i].Tokens[0].Sql + " in: " + FunctionName);
                            }


                            i++;
                        }
                    }
                    else
                    {
                        //ErrorManager.Log("Exec call has wrong argument length " + " in: " + FunctionName);
                    }
                }
            }
            return issues;
        }

        public void DebugPrint()
        {
            Console.WriteLine(FunctionName);
            Console.WriteLine("  Parameters");
            foreach (Field f in Parameters)
            {
                Console.WriteLine("    " + f.FieldName + " " + f.DataType);
            }
            Console.WriteLine("  Declared Feilds");
            foreach (Field f in DeclaredFields)
            {
                Console.WriteLine("    " + f.FieldName + " " + f.DataType);
            }
            Console.WriteLine("  Tables");
            foreach (Table t in exposedTables)
            {
                Console.WriteLine("    " + t.TableName);
            }
        }

    }
}
