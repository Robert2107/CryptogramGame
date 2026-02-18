using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Models
{
    public class GameState
    {
        /// <summary>
        /// The cryptogram being played.
        /// </summary>
        /// 
        public Cryptogram Cryptogram { get; set; } = null!;

        /// <summary>
        /// The player's current guesses: cryptogram value -> guessed plain letter.
        /// E.g. for a letter cryptogram: "X" -> 'e', "M" -> 't'
        /// For a number cryptogram: "7" -> 'e', "22" -> 't'
        /// </summary>
        /// 
        public Dictionary<string, char> PlayerMapping { get; set; } = new();

        /// <summary>
        /// Checks whether all cryptogram values used in the phrase have been mapped by the player.
        /// </summary>
        /// 
        public bool IsFullyMapped
        {
            get
            {
                return Cryptogram.UsedCryptogramValues.All(v => PlayerMapping.ContainsKey(v));
            }
        }

        /// <summary>
        /// Checks whether the player's complete mapping matches the correct decryption.
        /// Only meaningful when IsFullyMapped is true.
        /// </summary>
        /// 
        public bool IsCorrect
        {
            get
            {
                foreach (var cryptoValue in Cryptogram.UsedCryptogramValues)
                {
                    if (!PlayerMapping.TryGetValue(cryptoValue, out char guessed))
                    {
                        return false;
                    }

                    if (!Cryptogram.DecryptionMap.TryGetValue(cryptoValue, out char correct))
                    {
                        return false;
                    }

                    if (guessed != correct)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Returns the set of plain letters the player has already used in their mapping.
        /// </summary>
        /// 
        public HashSet<char> UsedPlainLetters
        {
            get
            {
                return PlayerMapping.Values.ToHashSet();
            }
        }

        /// <summary>
        /// Returns the set of cryptogram values that have not yet been mapped by the player.
        /// </summary>
        /// 
        public HashSet<string> UnmappedValues
        {
            get
            {
                return Cryptogram.UsedCryptogramValues
                    .Where(v => !PlayerMapping.ContainsKey(v))
                    .ToHashSet();
            }
        }
    }
}
