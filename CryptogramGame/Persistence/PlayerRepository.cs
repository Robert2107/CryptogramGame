using CryptogramGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Persistence
{
    public class PlayerRepository
    {
        private readonly string _directoryPath;

        public PlayerRepository(string directoryPath)
        {
            _directoryPath = directoryPath;

            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        private string GetPlayerFilePath(string playerName)
        {
            return Path.Combine(_directoryPath, $"{playerName.ToLower()}.txt");
        }

        /// <summary>
        /// Saves the player's details to a text file.
        /// </summary>
        /// 
        public bool SavePlayer(Player player)
        {
            try
            {
                var filePath = GetPlayerFilePath(player.Name);

                var lines = new[]
                {
                $"Name={player.Name}",
                $"CryptogramsPlayed={player.CryptogramsPlayed}",
                $"CryptogramsCompleted={player.CryptogramsCompleted}",
                $"NumGuesses={player.NumGuesses}",
                $"NumCorrectGuesses={player.NumCorrectGuesses}"
            };

                File.WriteAllLines(filePath, lines);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads a player from their text file.
        /// Returns null if the file doesn't exist or is corrupted.
        /// </summary>
        /// 
        public Player? LoadPlayer(string playerName)
        {
            var filePath = GetPlayerFilePath(playerName);

            if (!File.Exists(filePath))
            {
                return null;
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

                if (!data.ContainsKey("Name"))
                {
                    return null;
                }

                var player = new Player(data["Name"])
                {
                    CryptogramsPlayed = ParseInt(data, "CryptogramsPlayed"),
                    CryptogramsCompleted = ParseInt(data, "CryptogramsCompleted"),
                    NumGuesses = ParseInt(data, "NumGuesses"),
                    NumCorrectGuesses = ParseInt(data, "NumCorrectGuesses")
                };

                return player;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether a player file exists.
        /// </summary>
        /// 
        public bool PlayerExists(string playerName)
        {
            return File.Exists(GetPlayerFilePath(playerName));
        }

        /// <summary>
        /// Returns all saved players. Used for the scoreboard.
        /// </summary>
        public List<Player> LoadAllPlayers()
        {
            var players = new List<Player>();

            if (!Directory.Exists(_directoryPath))
            {
                return players;
            }

            foreach (var file in Directory.GetFiles(_directoryPath, "*.txt"))
            {
                var playerName = Path.GetFileNameWithoutExtension(file);

                var player = LoadPlayer(playerName);

                if (player != null)
                {
                    players.Add(player);
                }
            }

            return players;
        }

        private static int ParseInt(Dictionary<string, string> data, string key)
        {
            if (data.TryGetValue(key, out var value) && int.TryParse(value, out int result))
            {
                return result;
            }

            return 0;
        }
    }
}
