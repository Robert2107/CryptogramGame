using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Models
{
    public class ScoreboardEntry
    {
        public string PlayerName { get; set; } = string.Empty;

        public int CryptogramsCompleted { get; set; }

        public int CryptogramsPlayed { get; set; }

        public double CompletionProportion
        {
            get
            {
                if (CryptogramsPlayed == 0)
                {
                    return 0.0;
                }

                return (double)CryptogramsCompleted / CryptogramsPlayed;
            }
        }
    }
}
