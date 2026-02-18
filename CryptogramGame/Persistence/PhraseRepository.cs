using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Persistence
{
    public class PhraseRepository
    {
        private readonly string _filePath;

        public PhraseRepository(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Loads all phrases from the file. Returns null if the file doesn't exist or is empty.
        /// </summary>
        /// 
        public List<string>? LoadPhrases()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var phrases = File.ReadAllLines(_filePath)
                .Select(line => line.Trim().ToLower())
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => line.All(c => char.IsLetter(c) || c == ' '))
                .ToList();

            if (phrases.Count == 0)
            {
                return null;
            }

            return phrases;
        }

        /// <summary>
        /// Returns a random phrase from the file, or null if no phrases are available.
        /// </summary>
        /// 
        public string? GetRandomPhrase()
        {
            var phrases = LoadPhrases();

            if (phrases == null)
            {
                return null;
            }

            var random = new Random();

            return phrases[random.Next(phrases.Count)];
        }
    }
}
