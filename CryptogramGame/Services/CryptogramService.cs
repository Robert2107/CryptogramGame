using CryptogramGame.Models;
using CryptogramGame.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Services
{
    public class CryptogramService
    {
        private readonly PhraseRepository _phraseRepository;
        private readonly Random _random = new();

        public CryptogramService(PhraseRepository phraseRepository)
        {
            _phraseRepository = phraseRepository;
        }

        /// <summary>
        /// Generates a cryptogram of the specified type.
        /// Returns null with an error message if no phrases are available (US1 scenario 3).
        /// </summary>
        public (Cryptogram? Cryptogram, string Message) GenerateCryptogram(CryptogramType type)
        {
            var phrase = _phraseRepository.GetRandomPhrase();

            if (phrase == null)
            {
                return (null, "Error: No phrases available. Please ensure the phrases file exists and contains valid phrases.");
            }

            var cryptogram = new Cryptogram
            {
                Phrase = phrase,
                Type = type
            };

            if (type == CryptogramType.Letters)
            {
                cryptogram.EncryptionMap = GenerateLetterMapping(phrase);
            }
            else
            {
                cryptogram.EncryptionMap = GenerateNumberMapping(phrase);
            }

            cryptogram.BuildDerivedData();

            return (cryptogram, "Cryptogram generated successfully.");
        }

        /// <summary>
        /// Generates a bijective letter-to-letter mapping (shuffled alphabet).
        /// Ensures no letter maps to itself.
        /// </summary>
        private Dictionary<char, string> GenerateLetterMapping(string phrase)
        {
            var distinctLetters = phrase
                .Where(char.IsLetter)
                .Distinct()
                .ToList();

            var alphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray().ToList();

            // Shuffle the full alphabet to create a substitution cipher
            var shuffled = alphabet.OrderBy(_ => _random.Next()).ToList();

            // Ensure no letter maps to itself (derangement)
            for (int i = 0; i < 26; i++)
            {
                if (shuffled[i] == alphabet[i])
                {
                    // Swap with the next different one
                    int swapIndex = (i + 1) % 26;

                    while (shuffled[swapIndex] == alphabet[swapIndex] && swapIndex != i)
                    {
                        swapIndex = (swapIndex + 1) % 26;
                    }

                    (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
                }
            }

            var mapping = new Dictionary<char, string>();

            for (int i = 0; i < 26; i++)
            {
                if (distinctLetters.Contains(alphabet[i]))
                {
                    // Store as uppercase to visually distinguish cipher from plaintext
                    mapping[alphabet[i]] = shuffled[i].ToString().ToUpper();
                }
            }

            return mapping;
        }

        /// <summary>
        /// Generates a bijective letter-to-number (1-26) mapping.
        /// </summary>
        private Dictionary<char, string> GenerateNumberMapping(string phrase)
        {
            var distinctLetters = phrase
                .Where(char.IsLetter)
                .Distinct()
                .ToList();

            // Create numbers 1-26 and shuffle
            var numbers = Enumerable.Range(1, 26).OrderBy(_ => _random.Next()).ToList();

            var alphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

            var mapping = new Dictionary<char, string>();

            for (int i = 0; i < 26; i++)
            {
                if (distinctLetters.Contains(alphabet[i]))
                {
                    mapping[alphabet[i]] = numbers[i].ToString();
                }
            }

            return mapping;
        }
    }
}
