using System;
using System.Collections.Generic;
using System.IO;

namespace AdamsDatabaseAnalyser
{
    public class UserDefinedTableType : Table
    {


        public UserDefinedTableType(FileInfo file):base()
        {
            string sql = File.ReadAllText(file.FullName);
            List<ParsedToken> tokens = Utils.ParseSql(sql);

            bool awaitingColumns = false;
            bool awaitingColumnsNextTime = false;
            int currentDepth = 0;

            //token parsing
            int tokenSkips = 0;
            for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex += tokenSkips + 1)
            {
                tokenSkips = 0;

                string thisTokenValue = Utils.GetTokenValue(tokenIndex, tokens);
                //Console.WriteLine(thisTokenValue);

                if (awaitingColumnsNextTime)
                {
                    awaitingColumns = true;
                    awaitingColumnsNextTime = false;
                }

                if (thisTokenValue == "CREATE" && Utils.GetTokenValue(tokenIndex + 1, tokens) == "TYPE")
                {
                    //the next few tokens should hold the table name
                    TableName = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 4, tokens));
                    tokenSkips = 6;
                    awaitingColumnsNextTime = true;
                }

                if (awaitingColumns)
                {
                    if (thisTokenValue == "(")
                    {
                        currentDepth++;
                    }
                    if (thisTokenValue == ")")
                    {
                        currentDepth--;
                    }
                    //possibly extract this to a method
                    if (thisTokenValue != "CONSTRAINT" && thisTokenValue != "contraint" && thisTokenValue != "," && thisTokenValue != "NULL" && thisTokenValue != "null" && thisTokenValue != "(" && thisTokenValue != ")" && thisTokenValue != "NOT" && thisTokenValue != "not" && thisTokenValue != "IDENTITY" && thisTokenValue != "identity")
                    {
                        //read this column
                        //read the name
                        string columnName = Utils.StripStringOfSquareBrackets(thisTokenValue);
                        //read the data type
                        string dataType = Utils.StripStringOfSquareBrackets(Utils.GetTokenValue(tokenIndex + 1, tokens));
                        tokenSkips++;
                        //read the length if it exists
                        int length = -1;
                        if (Utils.GetTokenValue(tokenIndex + 2, tokens) == "(" && Utils.GetTokenValue(tokenIndex + 4, tokens) == ")")
                        {
                            //this is a length
                            string lengthValue = Utils.GetTokenValue(tokenIndex + 3, tokens);
                            tokenSkips += 3;
                            if (lengthValue == "MAX" || lengthValue == "max")
                            {
                                length = 4000;
                            }
                            else
                            {
                                int.TryParse(lengthValue, out length);
                            }
                        }

                        Field col = new Field(columnName, dataType, length);
                        AddColumn(col);

                    }
                    else
                    {
                        if (thisTokenValue == "IDENTITY" || thisTokenValue == "identity")
                        {
                            tokenSkips = 5;
                        }
                        if (thisTokenValue == "CONSTRAINT" || thisTokenValue == "contraint")
                        {
                            awaitingColumns = false;
                            currentDepth = 0;
                        }
                    }

                    if (currentDepth == 0)
                    {
                        awaitingColumns = false;
                    }

                }

            }
        }
    }
}