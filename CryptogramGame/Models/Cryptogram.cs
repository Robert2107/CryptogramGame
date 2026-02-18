using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Models
{
    public class Cryptogram
    {
        /// <summary>
        /// The original plaintext phrase (lowercase letters and spaces only).
        /// </summary>
        /// 
        public string Phrase { get; set; } = string.Empty;

        public CryptogramType Type { get; set; }

        /// <summary>
        /// Maps each plain letter (a-z) used in the phrase to its cryptogram value.
        /// For letter cryptograms the value is a single uppercase letter (e.g. "X").
        /// For number cryptograms the value is a number string from "1" to "26".
        /// </summary>
        /// 
        public Dictionary<char, string> EncryptionMap { get; set; } = new();

        /// <summary>
        /// Reverse lookup: cryptogram value back to the plain letter.
        /// Built from EncryptionMap for convenience.
        /// </summary>
        /// 
        public Dictionary<string, char> DecryptionMap { get; set; } = new();

        /// <summary>
        /// The encoded display string produced by applying the encryption map to the phrase.
        /// For letter cryptograms: "XMQR PLZZ" etc.
        /// For number cryptograms: "7-22-3 15-12-12" etc.
        /// </summary>
        /// 
        public string EncodedPhrase { get; set; } = string.Empty;

        /// <summary>
        /// All distinct cryptogram values that appear in the encoded phrase.
        /// Used to validate player input.
        /// </summary>
        /// 
        public HashSet<string> UsedCryptogramValues { get; set; } = new();

        public void BuildDerivedData()
        {
            // Build decryption map (reverse of encryption)
            DecryptionMap = EncryptionMap.ToDictionary(kv => kv.Value, kv => kv.Key);

            // Build encoded phrase and collect used values
            UsedCryptogramValues.Clear();
            var parts = new List<string>();

            foreach (char c in Phrase)
            {
                if (c == ' ')
                {
                    parts.Add(" ");
                }
                else if (EncryptionMap.TryGetValue(c, out string? value))
                {
                    parts.Add(value);
                    UsedCryptogramValues.Add(value);
                }
            }

            if (Type == CryptogramType.Letters)
            {
                EncodedPhrase = string.Concat(parts);
            }
            else
            {
                var words = new List<string>();
                var currentWord = new List<string>();

                foreach (string part in parts)
                {
                    if (part == " ")
                    {
                        if (currentWord.Count > 0)
                        {
                            words.Add(string.Join("-", currentWord));
                            currentWord.Clear();
                        }
                    }
                    else
                    {
                        currentWord.Add(part);
                    }
                }

                if (currentWord.Count > 0)
                {
                    words.Add(string.Join("-", currentWord));
                }

                EncodedPhrase = string.Join("  ", words);
            }
        }
    }
}
