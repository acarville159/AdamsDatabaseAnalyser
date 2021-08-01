using System;
using System.Collections.Generic;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    public class ViewJoin : Join
    {
        public View View { get; set; }
        public string TableAlias { get; set; }


        public ViewJoin(View view, string alias)
        {
            View = view;
            TableAlias = alias;
        }



    }
}
