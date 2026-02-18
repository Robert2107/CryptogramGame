using CryptogramGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Services
{
    public class GameService
    {
        private readonly PlayerService _playerService;

        public GameState? CurrentGame { get; private set; }

        public bool GameInProgress => CurrentGame != null;

        public GameService(PlayerService playerService)
        {
            _playerService = playerService;
        }

        /// <summary>
        /// Starts a new game with the given cryptogram.
        /// Increments CryptogramsPlayed (US10 — only for new games, not loads).
        /// </summary>
        /// 
        public void StartNewGame(Cryptogram cryptogram)
        {
            CurrentGame = new GameState
            {
                Cryptogram = cryptogram,
                PlayerMapping = new Dictionary<string, char>()
            };

            _playerService.IncrementCryptogramsPlayed();
        }

        /// <summary>
        /// Loads an existing game state (no stats change per US10).
        /// </summary>
        /// 
        public void LoadGame(GameState gameState)
        {
            CurrentGame = gameState;
        }

        /// <summary>
        /// Ends the current game.
        /// </summary>
        /// 
        public void EndGame()
        {
            CurrentGame = null;
        }

        // =========================================================================
        // US2 — Enter a letter
        // =========================================================================

        /// <summary>
        /// Validates whether a cryptogram value exists in the current cryptogram.
        /// </summary>
        /// 
        public (bool Valid, string Message) ValidateCryptogramValue(string cryptoValue)
        {
            if (CurrentGame == null)
            {
                return (false, "No game is currently in progress.");
            }

            if (!CurrentGame.Cryptogram.UsedCryptogramValues.Contains(cryptoValue))
            {
                return (false, $"The value '{cryptoValue}' is not used in this cryptogram.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Checks if a cryptogram value has already been mapped by the player.
        /// </summary>
        /// 
        public bool IsValueAlreadyMapped(string cryptoValue)
        {
            if (CurrentGame == null)
            {
                return false;
            }

            return CurrentGame.PlayerMapping.ContainsKey(cryptoValue);
        }

        /// <summary>
        /// Checks if a plain letter has already been used in the player's mapping.
        /// </summary>
        /// 
        public bool IsPlainLetterAlreadyUsed(char letter)
        {
            if (CurrentGame == null)
            {
                return false;
            }

            return CurrentGame.UsedPlainLetters.Contains(letter);
        }

        /// <summary>
        /// Enters a letter mapping: maps a cryptogram value to a plain letter.
        /// Handles all US2 scenarios and updates stats (US11).
        /// Returns a result indicating what happened.
        /// </summary>
        /// 
        public EnterLetterResult EnterLetter(string cryptoValue, char plainLetter, bool overwriteConfirmed = false)
        {
            if (CurrentGame == null)
            {
                return new EnterLetterResult
                {
                    Success = false,
                    Message = "No game is currently in progress."
                };
            }

            // Validate the cryptogram value exists
            var validation = ValidateCryptogramValue(cryptoValue);

            if (!validation.Valid)
            {
                return new EnterLetterResult
                {
                    Success = false,
                    Message = validation.Message
                };
            }

            // Check if plain letter is already used on a DIFFERENT cryptogram value
            if (IsPlainLetterAlreadyUsed(plainLetter))
            {
                var existingEntry = CurrentGame.PlayerMapping
                    .FirstOrDefault(kv => kv.Value == plainLetter);

                if (existingEntry.Key != cryptoValue)
                {
                    return new EnterLetterResult
                    {
                        Success = false,
                        Message = $"The letter '{plainLetter}' is already mapped to cryptogram value '{existingEntry.Key}'. Please try a different letter.",
                        DuplicatePlainLetter = true
                    };
                }
            }

            // Check if the cryptogram value is already mapped
            if (IsValueAlreadyMapped(cryptoValue) && !overwriteConfirmed)
            {
                char currentMapping = CurrentGame.PlayerMapping[cryptoValue];

                return new EnterLetterResult
                {
                    Success = false,
                    Message = $"The value '{cryptoValue}' is already mapped to '{currentMapping}'. Do you want to overwrite?",
                    RequiresOverwriteConfirmation = true,
                    ExistingMapping = currentMapping
                };
            }

            // Apply the mapping
            CurrentGame.PlayerMapping[cryptoValue] = plainLetter;

            // Check if the guess is correct and update stats (US11)
            bool isCorrectGuess = CurrentGame.Cryptogram.DecryptionMap.TryGetValue(cryptoValue, out char correctLetter)
                && correctLetter == plainLetter;

            _playerService.RecordGuess(isCorrectGuess);

            // Check if the cryptogram is now fully mapped
            if (CurrentGame.IsFullyMapped)
            {
                if (CurrentGame.IsCorrect)
                {
                    // US2 scenario 4 / US9 — successful completion
                    _playerService.IncrementCryptogramsCompleted();

                    return new EnterLetterResult
                    {
                        Success = true,
                        Message = "Congratulations! You have successfully solved the cryptogram!",
                        GameCompleted = true,
                        GameWon = true
                    };
                }
                else
                {
                    // US2 scenario 5 — all mapped but incorrect
                    return new EnterLetterResult
                    {
                        Success = true,
                        Message = "All values are mapped, but the solution is incorrect. Keep trying!",
                        GameCompleted = false,
                        GameWon = false,
                        AllMappedButIncorrect = true
                    };
                }
            }

            return new EnterLetterResult
            {
                Success = true,
                Message = isCorrectGuess
                    ? $"Letter '{plainLetter}' mapped to '{cryptoValue}'."
                    : $"Letter '{plainLetter}' mapped to '{cryptoValue}'.",
                IsCorrectGuess = isCorrectGuess
            };
        }

        // =========================================================================
        // US3 — Undo a letter
        // =========================================================================

        /// <summary>
        /// Removes a mapping for the given cryptogram value.
        /// </summary>
        /// 
        public (bool Success, string Message) UndoLetter(string cryptoValue)
        {
            if (CurrentGame == null)
            {
                return (false, "No game is currently in progress.");
            }

            if (!CurrentGame.Cryptogram.UsedCryptogramValues.Contains(cryptoValue))
            {
                return (false, $"The value '{cryptoValue}' is not used in this cryptogram.");
            }

            if (!CurrentGame.PlayerMapping.ContainsKey(cryptoValue))
            {
                return (false, $"The value '{cryptoValue}' has not been mapped yet.");
            }

            char removed = CurrentGame.PlayerMapping[cryptoValue];

            CurrentGame.PlayerMapping.Remove(cryptoValue);

            return (true, $"Mapping for '{cryptoValue}' (was '{removed}') has been removed.");
        }

        // =========================================================================
        // US6 — Show solution
        // =========================================================================

        /// <summary>
        /// Returns the full solution mapping and the decoded phrase.
        /// </summary>
        /// 
        public (Dictionary<string, char> SolutionMap, string DecodedPhrase) ShowSolution()
        {
            if (CurrentGame == null)
            {
                return (new Dictionary<string, char>(), string.Empty);
            }

            return (CurrentGame.Cryptogram.DecryptionMap, CurrentGame.Cryptogram.Phrase);
        }

        // =========================================================================
        // US7 — Letter frequencies
        // =========================================================================

        /// <summary>
        /// Calculates the frequency proportions of each letter in the cryptogram phrase,
        /// alongside the standard English language frequencies.
        /// </summary>
        /// 
        public List<FrequencyEntry> GetFrequencies()
        {
            if (CurrentGame == null)
            {
                return new List<FrequencyEntry>();
            }

            var phrase = CurrentGame.Cryptogram.Phrase;

            var letterCount = phrase.Count(char.IsLetter);

            var phraseCounts = phrase
                .Where(char.IsLetter)
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            var entries = new List<FrequencyEntry>();

            foreach (char c in "abcdefghijklmnopqrstuvwxyz")
            {
                double cryptogramProportion = 0.0;

                if (phraseCounts.TryGetValue(c, out int count))
                {
                    cryptogramProportion = (double)count / letterCount;
                }

                entries.Add(new FrequencyEntry
                {
                    Letter = c,
                    CryptogramProportion = cryptogramProportion,
                    EnglishProportion = EnglishFrequencies.ContainsKey(c) ? EnglishFrequencies[c] : 0.0
                });
            }

            return entries
                .OrderByDescending(e => e.CryptogramProportion)
                .ToList();
        }

        // =========================================================================
        // US14 — Hints
        // =========================================================================

        /// <summary>
        /// Provides a hint by correctly mapping one unmapped cryptogram value.
        /// If the correct letter was already used on a wrong value, that wrong mapping is removed first.
        /// </summary>
        /// 
        public (bool Success, string Message) GetHint()
        {
            if (CurrentGame == null)
            {
                return (false, "No game is currently in progress.");
            }

            var unmapped = CurrentGame.UnmappedValues.ToList();

            if (unmapped.Count == 0)
            {
                return (false, "All values have already been mapped.");
            }

            // Pick a random unmapped value
            var random = new Random();

            var hintValue = unmapped[random.Next(unmapped.Count)];

            var correctLetter = CurrentGame.Cryptogram.DecryptionMap[hintValue];

            // Check if the correct letter is already used on a different value (US14 scenario 2)
            var conflictEntry = CurrentGame.PlayerMapping
                .FirstOrDefault(kv => kv.Value == correctLetter && kv.Key != hintValue);

            string message;

            if (conflictEntry.Key != null)
            {
                // Remove the incorrect mapping
                CurrentGame.PlayerMapping.Remove(conflictEntry.Key);

                message = $"Hint: '{hintValue}' = '{correctLetter}'. Your previous mapping of '{correctLetter}' to '{conflictEntry.Key}' was incorrect and has been removed.";
            }
            else
            {
                message = $"Hint: '{hintValue}' = '{correctLetter}'.";
            }

            // Apply the correct mapping
            CurrentGame.PlayerMapping[hintValue] = correctLetter;

            return (true, message);
        }

        // =========================================================================
        // Display helpers
        // =========================================================================

        /// <summary>
        /// Builds the current display string showing the cryptogram with player's guesses filled in.
        /// Unmapped values show as underscores.
        /// </summary>
        /// 
        public string GetDisplayString()
        {
            if (CurrentGame == null)
            {
                return string.Empty;
            }

            var cryptogram = CurrentGame.Cryptogram;
            var parts = new List<string>();

            foreach (char c in cryptogram.Phrase)
            {
                if (c == ' ')
                {
                    parts.Add(" ");
                    continue;
                }

                if (!cryptogram.EncryptionMap.TryGetValue(c, out string? cryptoValue))
                {
                    continue;
                }

                if (CurrentGame.PlayerMapping.TryGetValue(cryptoValue, out char guessed))
                {
                    parts.Add(guessed.ToString());
                }
                else
                {
                    parts.Add("_");
                }
            }

            return string.Concat(parts);
        }

        // =========================================================================
        // Standard English letter frequencies
        // =========================================================================

        private static readonly Dictionary<char, double> EnglishFrequencies = new()
    {
        { 'e', 0.127 }, { 't', 0.091 }, { 'a', 0.082 }, { 'o', 0.075 },
        { 'i', 0.070 }, { 'n', 0.067 }, { 's', 0.063 }, { 'h', 0.061 },
        { 'r', 0.060 }, { 'd', 0.043 }, { 'l', 0.040 }, { 'c', 0.028 },
        { 'u', 0.028 }, { 'm', 0.024 }, { 'w', 0.024 }, { 'f', 0.022 },
        { 'g', 0.020 }, { 'y', 0.020 }, { 'p', 0.019 }, { 'b', 0.015 },
        { 'v', 0.010 }, { 'k', 0.008 }, { 'j', 0.002 }, { 'x', 0.002 },
        { 'q', 0.001 }, { 'z', 0.001 }
    };
    }

    // =========================================================================
    // Result / DTO types
    // =========================================================================

    public class EnterLetterResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool RequiresOverwriteConfirmation { get; set; }

        public char? ExistingMapping { get; set; }

        public bool DuplicatePlainLetter { get; set; }

        public bool GameCompleted { get; set; }

        public bool GameWon { get; set; }

        public bool AllMappedButIncorrect { get; set; }

        public bool IsCorrectGuess { get; set; }
    }

    public class FrequencyEntry
    {
        public char Letter { get; set; }

        public double CryptogramProportion { get; set; }

        public double EnglishProportion { get; set; }
    }
}
