using CryptogramGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Persistence
{
    public class GameStateRepository
    {
        private readonly string _directoryPath;

        public GameStateRepository(string directoryPath)
        {
            _directoryPath = directoryPath;

            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        private string GetSaveFilePath(string playerName)
        {
            return Path.Combine(_directoryPath, $"{playerName.ToLower()}_save.txt");
        }

        /// <summary>
        /// Checks whether a saved game exists for the player.
        /// </summary>
        /// 
        public bool SaveExists(string playerName)
        {
            return File.Exists(GetSaveFilePath(playerName));
        }

        /// <summary>
        /// Saves the current game state to a text file (US4).
        /// </summary>
        /// 
        public bool SaveGameState(string playerName, GameState gameState)
        {
            try
            {
                var filePath = GetSaveFilePath(playerName);
                var lines = new List<string>();

                // Phrase
                lines.Add($"Phrase={gameState.Cryptogram.Phrase}");

                // Type
                lines.Add($"Type={gameState.Cryptogram.Type}");

                // Encryption map: a=X,b=Q,c=R,...
                var encryptionEntries = gameState.Cryptogram.EncryptionMap
                    .Select(kv => $"{kv.Key}={kv.Value}");

                lines.Add($"EncryptionMap={string.Join(",", encryptionEntries)}");

                // Player mapping: X=e,Q=t,...
                var playerEntries = gameState.PlayerMapping
                    .Select(kv => $"{kv.Key}={kv.Value}");

                lines.Add($"PlayerMapping={string.Join(",", playerEntries)}");

                File.WriteAllLines(filePath, lines);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads a saved game state from file (US5).
        /// Returns null if file doesn't exist, is corrupted, or can't be parsed.
        /// </summary>
        /// 
        public (GameState? State, LoadResult Result) LoadGameState(string playerName)
        {
            var filePath = GetSaveFilePath(playerName);

            if (!File.Exists(filePath))
            {
                return (null, LoadResult.NotFound);
            }

            try
            {
                var data = new Dictionary<string, string>();

                foreach (var line in File.ReadAllLines(filePath))
                {
                    var parts = line.Split('=', 2);

                    if (parts.Length == 2)
                    {
                        data[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                // Validate required fields
                if (!data.ContainsKey("Phrase") || !data.ContainsKey("Type") || !data.ContainsKey("EncryptionMap"))
                {
                    return (null, LoadResult.Corrupted);
                }

                // Parse type
                if (!Enum.TryParse<CryptogramType>(data["Type"], out var cryptoType))
                {
                    return (null, LoadResult.Corrupted);
                }

                // Parse encryption map
                var encryptionMap = ParseEncryptionMapping(data["EncryptionMap"]);

                if (encryptionMap == null)
                {
                    return (null, LoadResult.Corrupted);
                }

                // Build cryptogram
                var cryptogram = new Cryptogram
                {
                    Phrase = data["Phrase"],
                    Type = cryptoType,
                    EncryptionMap = encryptionMap
                };

                cryptogram.BuildDerivedData();

                // Parse player mapping (may be empty if they hadn't guessed yet)
                var playerMapping = new Dictionary<string, char>();

                if (data.TryGetValue("PlayerMapping", out var playerMapStr) && !string.IsNullOrEmpty(playerMapStr))
                {
                    var parsed = ParsePlayerMapping(playerMapStr);

                    if (parsed == null)
                    {
                        return (null, LoadResult.Corrupted);
                    }

                    playerMapping = parsed;
                }

                var gameState = new GameState
                {
                    Cryptogram = cryptogram,
                    PlayerMapping = playerMapping
                };

                return (gameState, LoadResult.Success);
            }
            catch
            {
                return (null, LoadResult.Corrupted);
            }
        }

        /// <summary>
        /// Deletes a saved game for the player.
        /// </summary>
        /// 
        public void DeleteSave(string playerName)
        {
            var filePath = GetSaveFilePath(playerName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Parses encryption map string: "a=X,b=Q,c=R" -> Dictionary char->string
        /// </summary>
        /// 
        private Dictionary<char, string>? ParseEncryptionMapping(string mapStr)
        {
            try
            {
                var map = new Dictionary<char, string>();

                foreach (var entry in mapStr.Split(','))
                {
                    var parts = entry.Split('=', 2);

                    if (parts.Length != 2 || parts[0].Length != 1)
                    {
                        return null;
                    }

                    map[parts[0][0]] = parts[1];
                }

                return map;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses player mapping string: "X=e,Q=t" -> Dictionary string->char
        /// </summary>
        private Dictionary<string, char>? ParsePlayerMapping(string mapStr)
        {
            try
            {
                var map = new Dictionary<string, char>();

                foreach (var entry in mapStr.Split(','))
                {
                    var parts = entry.Split('=', 2);

                    if (parts.Length != 2 || parts[1].Length != 1)
                    {
                        return null;
                    }

                    map[parts[0]] = parts[1][0];
                }

                return map;
            }
            catch
            {
                return null;
            }
        }
    }

    public enum LoadResult
    {
        Success,
        NotFound,
        Corrupted
    }
}
