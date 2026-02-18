using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Models
{
    public class Player
    {
        public string Name { get; set; } = string.Empty;

        public int CryptogramsPlayed { get; set; }

        public int CryptogramsCompleted { get; set; }

        public int NumGuesses { get; set; }

        public int NumCorrectGuesses { get; set; }

        public double AccuracyPercentage
        {
            get
            {
                if (NumGuesses == 0)
                {
                    return 0.0;
                }

                return (double)NumCorrectGuesses / NumGuesses * 100.0;
            }
        }

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

        public Player(string name)
        {
            Name = name;
        }
    }
}
