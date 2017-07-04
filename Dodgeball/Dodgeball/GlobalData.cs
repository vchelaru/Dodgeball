using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodgeball
{
    

    public enum JoinStatus { Team1, Undecided, Team2 };

    public static class GlobalData
    {
        public static JoinStatus[] JoinStatuses;

        public static Color Team1HighlightColor = Color.CadetBlue;
        public static Color Team2HighlightColor = Color.OrangeRed;
        
        public static Color Team1ShirtColor = Color.Red;
        public static Color Team2ShirtColor = Color.Blue;

        public static Color Team1ShortsColor = Color.AntiqueWhite;
        public static Color Team2ShortsColor = Color.BlanchedAlmond;


        static GlobalData()
        {
            JoinStatuses = new JoinStatus[]
            {
                JoinStatus.Undecided,
                JoinStatus.Undecided,
                JoinStatus.Undecided,
                JoinStatus.Undecided
            };
        }
    }
}
