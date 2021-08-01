namespace AdamsDatabaseAnalyser
{
    public class ComparisonCall
    {

        public string FirstField { get; set; } = "";
        public string SecondField { get; set; } = "";


        public ComparisonCall(string first,string second)
        {
            FirstField = first;
            SecondField = second;
        }
    }
}