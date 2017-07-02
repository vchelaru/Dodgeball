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
