using System;
using System.Collections.Generic;
using System.IO;

namespace AdamsDatabaseAnalyser
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Definitions
            //Define Input/Output locations
            string filePath = "XXX";
            string filePath2 = "XXX";
            string exportFileLoaction = "XXX";

            //Define a filter for exports
            string filter = "";
            #endregion


            #region Doing Stuff
            //Generate Issues from folders
            IssueSet issues1 = new IssueSet(filePath);
            Console.WriteLine(string.Format("There were {0} issues in {1}", issues1.issues.Count, "Issue Set 1"));
            IssueSet issues2 = new IssueSet(filePath2);
            Console.WriteLine(string.Format("There were {0} issues in {1}", issues2.issues.Count, "Issue Set 2"));

            //Errors that exist in issues1 but not issues2 are fixed
            List<Issue> fixedIssues = Utils.GetUnmatchedIssues(issues1, issues2);

            //Errors that exist in issues2 but not in issues1 are new
            List<Issue> newIssues = Utils.GetUnmatchedIssues(issues2, issues1);


            //Generate filtered issues, issue set 1, issue set 2, new issues, fixed issues
            List<Issue> filteredIssues1 = Utils.FilteredIssues(issues1.issues, filter);
            List<Issue> filteredIssues2 = Utils.FilteredIssues(issues2.issues, filter);
            List<Issue> filteredNewIssues = Utils.FilteredIssues(newIssues, filter);
            List<Issue> filteredFixedIssues = Utils.FilteredIssues(fixedIssues, filter);

            Console.WriteLine(String.Format("There are {0} fixed issues", fixedIssues.Count));
            Console.WriteLine(String.Format("There are {0} new issues", newIssues.Count));
            Console.WriteLine(String.Format("There are {0} issues in Issue Set 1(filtered)", filteredIssues1.Count));
            Console.WriteLine(String.Format("There are {0} issues in Issue Set 2(filtered)", filteredIssues2.Count));
            Console.WriteLine(String.Format("There are {0} new issues (filtered)", filteredNewIssues.Count));
            Console.WriteLine(String.Format("There are {0} fixed issues (filtered)", filteredFixedIssues.Count));

            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_1.xlsx", issues1.issues);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_2.xlsx", issues2.issues);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_fixed.xlsx", fixedIssues);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_new.xlsx", newIssues);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues1_filtered.xlsx", filteredIssues1);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues2_filtered.xlsx", filteredIssues2);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_new_filtered.xlsx", filteredNewIssues);
            ExportBuilder.CreateIssueListDoc(exportFileLoaction + "\\issues_fixed_filtered.xlsx", filteredFixedIssues);
            #endregion

        }
    }
}
