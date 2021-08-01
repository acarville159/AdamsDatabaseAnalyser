using Microsoft.SqlServer.Management.SqlParser.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AdamsDatabaseAnalyser
{
    public class Utils
    {
        public static string GetCustomExtension(FileInfo file)
        {
            //files that we care about are named in the format dbo.[name].[type].sql
            //we want to return the type
            try
            {
                string[] parts = file.Name.Split('.');
                return parts[2];
            }
            catch (Exception)
            {
                return "null";
            }
        }

        public static string StripStringOfSquareBrackets(string s)
        {
            s = s.Replace("[", "");
            s = s.Replace("]", "");
            return s;
        }

        public static string StripWhiteSpace(string s)
        {
            s = s.Replace(" ", "");
            return s;
        }

        internal static List<Issue> GetUnmatchedIssues(IssueSet issues1, IssueSet issues2)
        {
            List<Issue> returnIssues = new List<Issue>();
            foreach(Issue i in issues1.issues)
            {
                if (!ContainsIssue(issues2, i.FileName, i.Type, i.Cause))
                {
                    returnIssues.Add(i);
                }
            }
            return returnIssues;
        }

        internal static List<Issue> FilteredIssues(List<Issue> issues, string v)
        {
            List<Issue> returnIssues = new List<Issue>();
            foreach(Issue i in issues)
            {
                if (i.Cause.Contains(v))
                {
                    returnIssues.Add(i);
                }
            }
            return returnIssues;
        }

        internal static bool ContainsIssue(IssueSet issueSet,string fileName,Issue.IssueType type,string cause)
        {
            foreach (Issue i in issueSet.issues)
            {
                if(i.FileName == fileName && i.Type == type && i.Cause == cause)
                {
                    return true;
                }
            }
            return false;
        }

        public static string MakeSingleSpaces(string s)
        {
            return string.Join(" ", s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string RemoveIntegers(string input)
        {
            return Regex.Replace(input, @"[\d-]", string.Empty);
        }

        public static bool CausesImplicitConversion(DataBaseDataType type1, DataBaseDataType type2)
        {
            if(type1 == DataBaseDataType.integer && type2 == DataBaseDataType.bit)
            {
                return false;
                return false;
            }

            if (type1 == DataBaseDataType.bit && type2 == DataBaseDataType.integer)
            {
                return false;
            }

            if (type1 == DataBaseDataType.none)
            {
                //null compares are normal
                return false;
            }

            if (type2 == DataBaseDataType.none)
            {
                //null compares are normal
                return false;
            }


            return type1 != type2;
        }

        public static bool CausesImplicitConversion(DataBaseDataType type1,string value1, DataBaseDataType type2)
        {
            if(type1 == DataBaseDataType.none)
            {
                //null compares are normal
                return false;
            }

            if(type1 == DataBaseDataType.varchar && value1.Replace("\'","").Length == 1 && type2 == DataBaseDataType.charOne)
            {
                return false;
            }

            if(type1 == DataBaseDataType.integer && value1.Length==1 && type2 == DataBaseDataType.bit)
            {
                return false;
            }

            return CausesImplicitConversion(type1, type2);
        }

        internal static string GetIssueString(Issue.IssueType type)
        {
            switch (type)
            {
                case Issue.IssueType.FUNC_ARG_IMP:
                    return "Function Argument Implicit Conversion";
                case Issue.IssueType.SP_ARG_IMP:
                    return "SP Argument Implicit Conversion";
                case Issue.IssueType.COMPARISON_IMP:
                    return "Comparison Implicit Conversion";
                case Issue.IssueType.INSERT_IMP:
                    return "Insertion Implicit Conversion";
            }
            return "No Type";
        }

        internal static Field CovertTokenSectionToField(ParsedTokenSection section)
        {
            Field f = new Field("NewField", DataBaseDataType.none, -1);

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
            finisherList.Add("identity");
            finisherList.Add("if");
            List<ParsedToken> tokens = Utils.CreateTokenBlockUntilStrings(section.Tokens, 0 , finisherList);




            //Case 1 @param AS <type>
            if(tokens.Count == 3  && tokens[1].Sql.ToLower() == "as")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[2].Sql);
                f.DataTypeString = tokens[2].Sql;


                return f;
            }

            //Case 2 @param <type>
            if(tokens.Count == 2)
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;
                return f;
            }

            //Case 3 @param <type>(len)
            if(tokens.Count == 3 && (tokens[2].Sql.ToLower()=="max" || Utils.isAlpha(tokens[2].Sql.ToLower())))
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;
                f.Length = convertAlpha(tokens[2].Sql, -1);
                return f;
            }

            //Case 4 @param <type> = <val>
            if(tokens.Count == 4 && tokens[2].Sql == "=")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;
                return f;
            }

            //Case 5 @param <type>(len) = <al
            if(tokens.Count == 5 && (tokens[2].Sql.ToLower() == "max" || Utils.isAlpha(tokens[2].Sql.ToLower())) && tokens[3].Sql == "=")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;
                f.Length = convertAlpha(tokens[2].Sql, -1);
                return f;
            }

            //Case 6 @param as <type> READONLY
            if (tokens.Count == 4 && tokens[1].Sql.ToLower() == "as")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[2].Sql);
                f.DataTypeString = tokens[2].Sql;

                return f;
            }

            //Case 7 @param <type> EXTRA
            if (tokens.Count == 3)
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;

                return f;
            }

            //Case 8 @param <type> ( <length> ) = null
            if(tokens.Count == 7 && tokens[2].Sql == "(" && tokens[4].Sql == ")")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;

                f.Length = convertAlpha(tokens[3].Sql, -1);
                return f;
            }

            //Case 8 @param <type> ( <length> )
            if (tokens.Count == 5 && tokens[2].Sql == "(" && tokens[4].Sql == ")")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;

                f.Length = convertAlpha(tokens[3].Sql, -1);
                return f;
            }

            if (tokens.Count == 5 && tokens[2].Sql.ToLower() == "output" && tokens[3].Sql.ToLower()=="with" && tokens[4].Sql.ToLower() == "recompile")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;

                return f;
            }

            if (tokens.Count == 7 && (tokens[2].Sql.ToLower() == "max" || Utils.isAlpha(tokens[2].Sql.ToLower())) && tokens[3].Sql == "=")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.DataTypeString = tokens[1].Sql;

                f.Length = convertAlpha(tokens[2].Sql, -1);
                return f;
            }

            if(tokens.Count == 4 && tokens[2].Sql.ToLower() == "not" && tokens[3].Sql.ToLower() == "null")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                return f;
            }

            if (tokens.Count == 5 && (tokens[2].Sql.ToLower() == "max" || Utils.isAlpha(tokens[2].Sql.ToLower())) && tokens[3].Sql.ToLower() == "not" && tokens[4].Sql.ToLower() == "null")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.Length = convertAlpha(tokens[2].Sql, -1);
                return f;
            }

            if (tokens.Count == 4 && (tokens[2].Sql.ToLower() == "max" || Utils.isAlpha(tokens[2].Sql.ToLower())) && tokens[3].Sql.ToLower() == "null")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                f.Length = convertAlpha(tokens[2].Sql, -1);
                return f;
            }

            if (tokens.Count == 4 && tokens[2].Sql.ToLower() == "primary" && tokens[3].Sql.ToLower() == "key")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                return f;
            }

            if (tokens.Count == 5 && tokens[2].Sql.ToLower() == "primary" && tokens[3].Sql.ToLower() == "key" && tokens[4].Sql.ToLower() == "identity")
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = GetDataType(tokens[1].Sql);
                return f;
            }

            if(tokens.Count == 1)
            {
                f.FieldName = tokens[0].Sql;
                f.DataType = DataBaseDataType.none;
                return f;
            }

            if(tokens.Count == 4 && tokens[1].Sql == "=" && tokens[2].Sql.ToLower() == "object_id")
            {
                f.FieldName = tokens[3].Sql;
                if(tokens[3].Sql[0]=='N' && tokens[3].Sql[0] == '\n')
                {
                    f.DataType = DataBaseDataType.nvarchar;
                }
                else
                {
                    f.DataType = DataBaseDataType.varchar;
                }

                return f;

            }

            //THE AS KEYWORD IS EFFECTING THIS HERE

            return f;
            
        }

        public static int convertAlpha(string s,int def)
        {
            try
            {
                int i = int.Parse(s);
                return i;
            }
            catch (Exception)
            {

                return def;
            }
        }

        public static bool isAlpha(string s)
        {
            try
            {
                int.Parse(s);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Used to automatically determine how to split the tokens into sections based on their assumed datatype
        internal static List<ParsedTokenSection> SplitTokensIntoSections(List<ParsedToken> block)
        {

            //by default we will split by comma
            return SplitTokensIntoSections(block, ",");
        }

        public static List<ParsedToken> CleanupNumberFormatting(List<ParsedToken> block)
        {
            //clean up decimal formatting
            List<ParsedToken> tokens = new List<ParsedToken>();
            while(block.Count > 0)
            {
                ParsedToken token = block[0];
                if(token.Sql.ToLower() == "decimal")
                {
                    tokens.Add(token);
                    block.RemoveAt(0);
                    if (block.Count > 1)
                    {
                        ParsedToken nextToken = block[0];
                        if(Utils.isAlpha(nextToken.Sql))
                        {
                            //we want to get rid of the rest of the number bit's we dont need them for this purpose and they're annoying
                            //skip adding the next three tokens (delete them)                         
                            block.RemoveAt(0);
                            block.RemoveAt(0);
                            block.RemoveAt(0);
                        }
                    }

                }
                else
                {
                    tokens.Add(token);
                    block.RemoveAt(0);
                }
            }
            return tokens;
        }

        internal static List<ParsedTokenSection> SplitTokensIntoSections(List<ParsedToken> block,string splitter)
        {
            block = CleanupNumberFormatting(block);
            List<ParsedTokenSection> parsedTokenSections = new List<ParsedTokenSection>();
            ParsedTokenSection currentSection = new ParsedTokenSection();
            foreach(ParsedToken token in block)
            {
                if(token.Sql == splitter)
                {
                    parsedTokenSections.Add(currentSection);
                    currentSection = new ParsedTokenSection();
                }
                else
                {
                    currentSection.AddTOken(token);
                }
            }
            if(currentSection.Tokens.Count > 0)
            {
                parsedTokenSections.Add(currentSection);
            }
            return parsedTokenSections;
        }

        internal static List<ParsedToken> CleanupParsedTokenList(List<ParsedToken> tokens,string removeVal)
        {
            List<ParsedToken> returnTokens = new List<ParsedToken>();
            foreach(ParsedToken token in tokens)
            {
                if(token.Sql != removeVal)
                {
                    returnTokens.Add(token);
                }
            }
            return returnTokens;
        }

        public static List<ParsedToken> CleanupParsedTokenListPlus(List<ParsedToken> tokens, string first, int plus)
        {
            List<ParsedToken> returnTokens = new List<ParsedToken>();
            for (int i = 0; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (i + 1 < tokens.Count)
                {
                    ParsedToken nextToken = tokens[i + 1];
                    if (token.Sql.ToLower() == first)
                    {
                        //do not add
                        //skip over the next one
                        i += plus;
                    }
                    else
                    {
                        returnTokens.Add(token);
                    }
                }
                else
                {
                    returnTokens.Add(token);

                }
            }
            return returnTokens;
        }


        public static List<ParsedToken> CleanupParsedTokenListSequence(List<ParsedToken> tokens,string seq1,string seq2)
        {
            List<ParsedToken> returnTokens = new List<ParsedToken>();
            for (int i = 0; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (i + 1 < tokens.Count)
                {
                    ParsedToken nextToken = tokens[i + 1];
                    if (token.Sql.ToLower() == seq1 && nextToken.Sql.ToLower() == seq2)
                    {
                        //do not add
                        //skip over the next one
                        i++;
                    }
                    else
                    {
                        returnTokens.Add(token);
                    }
                }
                else
                {
                    returnTokens.Add(token);

                }
            }
            return returnTokens;
        }

        public static void CleanUpParsedTokenSquareBrackets(List<ParsedToken> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                token.Sql = Utils.StripStringOfSquareBrackets(token.Sql);
            }
        }

        public static List<ParsedToken> CleanUpParsedTokenIdentity(List<ParsedToken> tokens)
        {
            List<ParsedToken> returnTokens = new List<ParsedToken>();
            for (int i = 0; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (i + 1 < tokens.Count)
                {
                    ParsedToken nextToken = tokens[i + 1];
                    if (token.Sql.ToLower() == "identity" && Utils.isAlpha(nextToken.Sql))
                    {
                        //do not add
                        //skip over the next two
                        i += 3;
                    }
                    else
                    {
                        returnTokens.Add(token);
                    }
                }
                else
                {
                    returnTokens.Add(token);

                }
            }
            return returnTokens;
        }

        public static List<ParsedToken> CleanupParsedTokenListNegativeNumbers(List<ParsedToken> tokens)
        {
            List<ParsedToken> returnTokens = new List<ParsedToken>();
            for (int i = 0; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (i + 1 < tokens.Count)
                {
                    ParsedToken nextToken = tokens[i + 1];
                    if (token.Sql == "-" && Utils.isAlpha(nextToken.Sql))
                    {
                        //combine the tokens
                        token.Sql = token.Sql + nextToken.Sql;
                        //skip over the next one
                        i++;
                    }
                    else
                    {
                        returnTokens.Add(token);
                    }
                }
                else
                {
                    returnTokens.Add(token);

                }
            }
            return returnTokens;
        }

        internal static List<ParsedToken> CleanupParsedTokenList(List<ParsedToken> tokens, int removeAt)
        {
            tokens.RemoveAt(removeAt);
            return tokens;
        }

        internal static List<ParsedToken> CreateTokenBlockUntilString(List<ParsedToken> tokens, int startIndex, List<string> finishers, List<string> nextLineFinishers)
        {
            List<ParsedToken> tokenSection = new List<ParsedToken>();

            for (int i = startIndex; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (finishers.Contains(token.Sql.ToLower()) && nextLineFinishers.Contains(tokens[i+1].Sql.ToLower()))
                {
                    break;
                }
                else
                {
                    tokenSection.Add(token);
                }
            }
            return tokenSection;

        }

        internal static List<ParsedToken> CreateTokenBlockUntilString(List<ParsedToken> tokens, int startIndex, string finisher)
        {
            List<ParsedToken> tokenSection = new List<ParsedToken>();

            for (int i = startIndex; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if(token.Sql.ToLower() == finisher)
                {
                    break;
                }
                else
                {
                    tokenSection.Add(token);
                }
            }
            return tokenSection;

        }

        internal static List<ParsedToken> CreateTokenBlockUntilStringsWithDepth(List<ParsedToken> tokens, int startIndex, List<string> finishers,string opener,string closer)
        {
            int depth = 0;
            List<ParsedToken> tokenSection = new List<ParsedToken>();

            for (int i = startIndex; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if(token.Sql.ToLower() == opener)
                {
                    depth++;
                }
                if(token.Sql.ToLower() == closer)
                {
                    depth--;
                }
                if (finishers.Contains(token.Sql.ToLower()) && depth == 0)
                {
                    break;
                }
                else
                {
                    tokenSection.Add(token);
                }
            }
            return tokenSection;
        }

        internal static List<ParsedToken> CreateTokenBlockUntilStrings(List<ParsedToken> tokens, int startIndex, List<string> finishers)
        {
            List<ParsedToken> tokenSection = new List<ParsedToken>();

            for (int i = startIndex; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (finishers.Contains(token.Sql.ToLower()))
                {
                    break;
                }
                else
                {
                    tokenSection.Add(token);
                }
            }
            return tokenSection;

        }

        internal static Table CreateTableFromTokens(List<ParsedToken> tableContent, string tableName)
        {
            #region Cleanup Values
            tableContent = Utils.CleanupParsedTokenList(tableContent, "(");
            tableContent = Utils.CleanupParsedTokenList(tableContent, ")");
            tableContent = Utils.CleanupParsedTokenList(tableContent, "\n");
            tableContent = Utils.CleanupParsedTokenList(tableContent, "\t");
            tableContent = Utils.CleanupParsedTokenList(tableContent, " ");
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
            t.TableName = tableName;

            foreach (Field f in tableColumns)
            {
                t.AddColumn(f);
            }

            return t;
        }

        /// <summary>
        /// This method creates a sub list of ParsedTokens using a a bigger list, given a startIndex and two strings to use for openeings and closings
        /// </summary>
        internal static List<ParsedToken> CreateTokenBlockWithinStrings(List<ParsedToken> tokens, int startIndex, string opener, string closer,bool addDividers = false)
        {
            List<ParsedToken> tokenSection = new List<ParsedToken>();
            bool hasOpened = false;
            int currentDepth = 0;
            for (int i = startIndex; i < tokens.Count; i++)
            {
                ParsedToken token = tokens[i];
                if (hasOpened)
                {
                    if(token.Sql.ToLower() == opener)
                    {
                        currentDepth++;
                    }

                    if(token.Sql.ToLower() == closer)
                    {
                        currentDepth--;
                    }

                    if(token.Sql.ToLower() == opener && addDividers)
                    {
                        tokenSection.Add(token);

                    }

                    if (token.Sql.ToLower() == closer && addDividers)
                    {
                        tokenSection.Add(token);
                    }

                    if (token.Sql.ToLower() != opener && token.Sql.ToLower() != closer)
                    {
                        //Add to the section
                        tokenSection.Add(token);
                    }

                    if(currentDepth <= 0)
                    {
                        //Return the section
                        return tokenSection;
                    }
                }
                else
                {
                    if (token.Sql.ToLower() == opener)
                    {
                        if (addDividers)
                        {
                            tokenSection.Add(token);
                        }
                        hasOpened = true;
                        currentDepth = 1;
                    }
                }
            }
            return tokenSection;
        }

        public static DataBaseDataType GetDataType(string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "varchar":
                    return DataBaseDataType.varchar;
                case "nvarchar":
                    return DataBaseDataType.nvarchar;
                case "int":
                    return DataBaseDataType.integer;
                case "integer":
                    return DataBaseDataType.integer;
                case "datetime":
                    return DataBaseDataType.datetime;
                case "bit":
                    return DataBaseDataType.bit;
                case "real":
                    return DataBaseDataType.real;
                case "text":
                    return DataBaseDataType.text;
                case "sysname":
                    return DataBaseDataType.sysname;
                case "varbinary":
                    return DataBaseDataType.varbinary;
                case "time":
                    return DataBaseDataType.time;
                case "date":
                    return DataBaseDataType.date;
                case "table":
                    return DataBaseDataType.table;
                case "asc":
                    //this is wrong change me
                    return DataBaseDataType.asc;
                case "money":
                    return DataBaseDataType.money;
                case "char":
                    return DataBaseDataType.charOne;
                case "uniqueidentifier":
                    return DataBaseDataType.uniqueidentifier;
                case "decimal":
                    return DataBaseDataType.dec;
                case "cursor":
                    return DataBaseDataType.cursor;
                case "float":
                    return DataBaseDataType.flt;
                case "bigint":
                    return DataBaseDataType.bigint;
                default:
                    //If not any of the above there is a chance it could be user defined table
                    if(DatabaseSimulation.TryGetUserDefinedTableType(type) != null)
                    {
                        return DataBaseDataType.table;
                    }
                    else
                    {
                        return DataBaseDataType.none;

                    }
            }
        }

        internal static int CountChars(string s, char v)
        {
            int i = 0;
            foreach (Char c in s)
            {
                if (c == v) { i++; }
            }
            return i;
        }

        public static List<ParsedToken> ParseSql(string sql)
        {
            ParseOptions parseOptions = new ParseOptions();
            Scanner scanner = new Scanner(parseOptions);

            int state = 0,
                start,
                end,
                lastTokenEnd = -1,
                token;

            bool isPairMatch, isExecAutoParamHelp;

            List<ParsedToken> tokens = new List<ParsedToken>();

            scanner.SetSource(sql, 0);

            while ((token = scanner.GetNext(ref state, out start, out end, out isPairMatch, out isExecAutoParamHelp)) != (int)Tokens.EOF)
            {
                Tokens toke = (Tokens)token;
                ParsedToken parsedToken =
                    new ParsedToken()
                    {
                        Start = start,
                        End = end,
                        IsPairMatch = isPairMatch,
                        IsExecAutoParamHelp = isExecAutoParamHelp,
                        Sql = sql.Substring(start, end - start + 1),
                        Token = toke,
                    };

                if (toke != Tokens.LEX_END_OF_LINE_COMMENT && toke != Tokens.LEX_MULTILINE_COMMENT)
                {
                    tokens.Add(parsedToken);
                }

                lastTokenEnd = end;
            }

            return tokens;
        }

        public static string GetTokenValue(int index, List<ParsedToken> tokens)
        {
            if(index < 0)
            {
                return "";
            }
            if (tokens.Count > index)
            {
                ParsedToken token = tokens[index];
                if (token != null)
                {
                    return token.Sql;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }
    }
}
