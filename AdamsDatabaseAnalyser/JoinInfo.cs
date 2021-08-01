namespace AdamsDatabaseAnalyser
{
    public class JoinInfo
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }

        public string Column1Name { get; set; }
        public string Column2Name { get; set; }

        public string Column1Alias { get; set; }
        public string Column2Alias { get; set; }

        public bool IsValid = false;
    }
}