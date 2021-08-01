using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    class IssueSet
    {
        public List<Issue> issues = new List<Issue>();

        public IssueSet(string filePath)
        {

            #region Building Simulation

            Console.WriteLine("Starting Analyser...");

            DirectoryInfo di = new DirectoryInfo(filePath);
            FileInfo[] files = di.GetFiles();

            Console.WriteLine("Found " + files.Length + " Files To Parse...");

            List<FileInfo> tableFiles = new List<FileInfo>();
            List<FileInfo> storedProcedureFiles = new List<FileInfo>();
            List<FileInfo> userDefinedTablesFiles = new List<FileInfo>();
            List<FileInfo> userDefinedFunctionFiles = new List<FileInfo>();
            List<FileInfo> viewFiles = new List<FileInfo>();


            foreach (FileInfo file in files)
            {
                switch (Utils.GetCustomExtension(file))
                {
                    case "Table":
                        tableFiles.Add(file);
                        break;
                    case "StoredProcedure":
                        storedProcedureFiles.Add(file);
                        break;
                    case "UserDefinedTableType":
                        userDefinedTablesFiles.Add(file);
                        break;
                    case "UserDefinedFunction":
                        userDefinedFunctionFiles.Add(file);
                        break;
                    case "View":
                        viewFiles.Add(file);
                        break;
                    case "sql":
                        //we do not care
                        break;
                    case "User":
                        //we do not care
                        break;
                    default:
                        Console.WriteLine("Unhandled file-type: " + Utils.GetCustomExtension(file));
                        break;
                }
            }

            Console.WriteLine("Found {0} Tables", tableFiles.Count);
            Console.WriteLine("Found {0} StoredProcedures", storedProcedureFiles.Count);
            Console.WriteLine("Found {0} UserDefinedTableTypes", userDefinedTablesFiles.Count);
            Console.WriteLine("Found {0} UserDefinedFunctions", userDefinedFunctionFiles.Count);
            Console.WriteLine("Found {0} Views", viewFiles.Count);

            //Reset DataseSimulation
            DatabaseSimulation.Reset();

            //Build the tables the new and improved way
            foreach (FileInfo file in tableFiles)
            {
                Table t = new Table(file);
                DatabaseSimulation.AddTable(t);
                //Console.WriteLine("Built Table: " + t.TableName);

                //Console.WriteLine("Table{0} Added With {1} Columns", t.TableName, t.Columns.Count);
                //foreach (Field c in t.Columns)
                //{
                //    Console.WriteLine("  " + c.FieldName + " " + c.DataType);
                //}
            }

            Console.WriteLine("Built {0} Tables", DatabaseSimulation.GetNumberTables());

            foreach (FileInfo file in userDefinedTablesFiles)
            {
                UserDefinedTableType t = new UserDefinedTableType(file);
                DatabaseSimulation.AddUserDefinedTable(t);
            }


            Console.WriteLine("Built {0} User Defined Table Types", DatabaseSimulation.GetNumberUserDefinedTables());


            foreach (FileInfo file in viewFiles)
            {
                View view = new View(file);
                DatabaseSimulation.AddView(view);
            }


            Console.WriteLine("Built {0} Views", DatabaseSimulation.GetNumbeViews());


            //Build the functions the new and improved way
            foreach (FileInfo file in userDefinedFunctionFiles)
            {
                string filterValue = "";
                if (filterValue != "")
                {
                    if (file.Name.Contains(filterValue))
                    {
                        UserDefinedFunction func = new UserDefinedFunction(file);
                        DatabaseSimulation.AddFunction(func);
                        func.DebugPrint();
                    }
                }
                else
                {
                    UserDefinedFunction func = new UserDefinedFunction(file);
                    DatabaseSimulation.AddFunction(func);
                }

            }

            Console.WriteLine("Built {0} User Defined Functions", DatabaseSimulation.GetNumberUserDefinedFunctions());


            //Build the stored procedures
            foreach (FileInfo file in storedProcedureFiles)
            {
                string filterValue = "";
                if (filterValue != "")
                {
                    if (file.Name.Contains(filterValue))
                    {
                        StoredProcedure sp = new StoredProcedure(file);
                        DatabaseSimulation.AddStoredProcedure(sp);
                        sp.DebugPrint();
                    }
                }
                else
                {
                    StoredProcedure sp = new StoredProcedure(file);
                    DatabaseSimulation.AddStoredProcedure(sp);
                    //
                }



            }

            Console.WriteLine("Built {0} Stored Procedures", DatabaseSimulation.GetNumberStoredProcedures());

            #endregion

            #region Finding Issues


            foreach (Table table in DatabaseSimulation.GetTables())
            {
                foreach (Trigger trigger in table.Triggers)
                {
                    foreach (Issue triggerIssue in trigger.GetIssues())
                    {
                        issues.Add(triggerIssue);
                    }
                }
            }

            foreach (UserDefinedFunction function in DatabaseSimulation.GetUserDefinedFunctions())
            {
                foreach (Issue funcIssue in function.GetIssues())
                {
                    issues.Add(funcIssue);
                }
            }

            //Check the sps
            foreach (StoredProcedure sp in DatabaseSimulation.GetStoredProcedures())
            {
                foreach (Issue spIssue in sp.GetIssues())
                {
                    issues.Add(spIssue);
                }
            }

            Console.WriteLine(String.Format("During Execution there were {0} Errors :(", ErrorManager.ErrorCount));

            #endregion
        }
    }
}
