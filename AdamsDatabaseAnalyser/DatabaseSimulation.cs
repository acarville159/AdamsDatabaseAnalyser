using System;
using System.Collections.Generic;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    public class DatabaseSimulation
    {

        static List<Table> tables = new List<Table>();
        static List<UserDefinedTableType> userDefinedTableTypes = new List<UserDefinedTableType>();
        static List<UserDefinedFunction> functions = new List<UserDefinedFunction>();
        static List<StoredProcedure> storedProcedures = new List<StoredProcedure>();
        static List<View> views = new List<View>();

        public static void AddTable(Table t)
        {
            tables.Add(t);
        }

        public static void AddUserDefinedTable(UserDefinedTableType userDefinedTableType)
        {
            userDefinedTableTypes.Add(userDefinedTableType);
        }

        public static void AddFunction(UserDefinedFunction func)
        {
            functions.Add(func);
        }

        internal static UserDefinedTableType TryGetUserDefinedTableType(string type)
        {
            foreach(UserDefinedTableType tab in userDefinedTableTypes)
            {
                if(tab.TableName.ToLower() == type.ToLower())
                {
                    return tab;
                }
            }
            return null;
        }

        internal static int GetNumberTables()
        {
            return tables.Count;
        }

        internal static int GetNumberUserDefinedTables()
        {
            return userDefinedTableTypes.Count;
        }

        internal static int GetNumberUserDefinedFunctions()
        {
            return functions.Count;
        }

        internal static int GetNumbeViews()
        {
            return views.Count;
        }

        internal static void AddStoredProcedure(StoredProcedure sp)
        {
            storedProcedures.Add(sp);
        }

        internal static int GetNumberStoredProcedures()
        {
            return storedProcedures.Count;
        }

        public static List<StoredProcedure> GetStoredProcedures()
        {
            return storedProcedures;
        }

        public static List<Table> GetTables()
        {
            return tables;
        }

        public static List<UserDefinedFunction> GetUserDefinedFunctions()
        {
            return functions;
        }

        public static List<View> GetViews()
        {
            return views;
        }

        internal static UserDefinedFunction TryGetUserDefinedFunction(string funcName)
        {
            foreach (UserDefinedFunction func in functions)
            {
                if (func.FunctionName == funcName)
                {
                    return func;
                }
            }
            return null;
        }

        internal static void Reset()
        {
            tables.Clear();
            storedProcedures.Clear();
            functions.Clear();
            userDefinedTableTypes.Clear();
            views.Clear();
        }

        internal static Table TryGetTable(string tableName)
        {
            foreach (Table table in tables)
            {
                if (table.TableName == tableName)
                {
                    return table;
                }
            }

            foreach (Table table in tables)
            {
                if (table.TableName.ToLower() == tableName.ToLower())
                {
                    return table;
                }
            }

            return null;
        }

        internal static void AddView(View view)
        {
            views.Add(view);
        }

        internal static View TryGetView(string viewName)
        {
            foreach (View view in views)
            {
                if (view.ViewName == viewName)
                {
                    return view;
                }
            }

            return null;
        }

        internal static StoredProcedure TryGetStoredProcedure(string execName)
        {
            foreach(StoredProcedure sp in storedProcedures)
            {
                if(sp.ProcedureName.ToLower() == execName.ToLower())
                {
                    return sp;
                }
            }
            return null;
        }
    }
}
