using System;

namespace AdamsDatabaseAnalyser
{
    public class Field
    {
        public string FieldName { get; set; }
        public DataBaseDataType DataType { get; set; }

        public string DataTypeString { get; set; }

        public UserDefinedTableType UserDefinedTableType;
        public bool usesUserDefinedTableType = false;

        public bool definedField = false;

        public int Length { get; set; }

        public Field(string paramName, DataBaseDataType type, int length)
        {
            FieldName = paramName;
            DataType = type;
            Length = length;
        }

        public Field(string paramName, DataBaseDataType type, int length,bool defined)
        {
            FieldName = paramName;
            DataType = type;
            Length = length;
            definedField = defined;
        }

        public Field(string paramName, string type, int length, bool defined):this(paramName, type, length)
        {
            definedField = defined;
        }

        public Field(string paramName, string type, int length)
        {
            FieldName = paramName;
            DataType = Utils.GetDataType(type);
            if(DataType == DataBaseDataType.none)
            {
                //try use a temp table data type instead
                UserDefinedTableType = DatabaseSimulation.TryGetUserDefinedTableType(type);
                if(UserDefinedTableType != null)
                {
                    usesUserDefinedTableType = true;
                }
                else
                {
                    //Console.WriteLine("X-> Error with setting up param: Invalid datatype- " + type);
                }
            }
            Length = length;
        }
    }
}