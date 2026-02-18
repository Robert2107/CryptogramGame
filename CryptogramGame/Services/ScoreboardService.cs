using CryptogramGame.Models;
using CryptogramGame.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Services
{
    public class ScoreboardService
    {
        private readonly PlayerRepository _playerRepository;

        public ScoreboardService(PlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        /// <summary>
        /// Returns the top 10 players ordered by proportion of successfully completed cryptograms.
        /// Only includes players who have completed at least one cryptogram (US13 scenario 1).
        /// Returns null if no player stats exist (US13 scenario 2).
        /// </summary>
        /// 
        public (List<ScoreboardEntry>? Entries, string Message) GetTopTen()
        {
            var players = _playerRepository.LoadAllPlayers();

            if (players.Count == 0)
            {
                return (null, "No player stats have been stored yet.");
            }

            var eligible = players
                .Where(p => p.CryptogramsCompleted > 0)
                .ToList();

            if (eligible.Count == 0)
            {
                return (null, "No players have successfully completed a cryptogram yet.");
            }

            var entries = eligible
                .OrderByDescending(p => p.CompletionProportion)
                .ThenByDescending(p => p.CryptogramsCompleted)
                .Take(10)
                .Select(p => new ScoreboardEntry
                {
                    PlayerName = p.Name,
                    CryptogramsCompleted = p.CryptogramsCompleted,
                    CryptogramsPlayed = p.CryptogramsPlayed
                })
                .ToList();

            return (entries, "Top 10 players by completion proportion:");
        }
    }
}
