A very bad program designed to analyse T-SQL Scripts for implicit conversions

How to use:
1. Export the database schema to sql files - https://codebots.com/docs/export-schema-using-sql-server-management-studio-ssms
2. In the Advanced settings make sure "Schema Only" and that Triggers are exported
3. Export "Single File per object"
4. Make a copy of the exported folder and add suffix "_changed" to name of folder
5. Open Program.cs - change filePath to the path to first folder, change filePath2 to path of copied folder
6. Run program should generate bunch of excel documents with found issues
7. Make changes to the "_changed" folder and run program again - will export any "fixed" issues and "new" issues as a result of the changes made
8. Should catch about 90% (hopefully) of issues that exist (some edge cases not parsed correctly and have no had time to fix them - maybe will one day)
9. During execution should see message "During Execution there were {0} Errors :(" number shown is how many times we didn't parse something correctly so possibly missing issues here (will maybe fix these cases one day if there's ever time)

Please be kind this was written over a number of sleepless nights :(
