using CryptogramGame.Models;
using CryptogramGame.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.Services
{
    public class PlayerService
    {
        private readonly PlayerRepository _playerRepository;

        public Player? CurrentPlayer { get; private set; }

        public PlayerService(PlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        /// <summary>
        /// Attempts to load an existing player by name.
        /// Returns true if loaded successfully, false if not found or corrupted.
        /// </summary>
        /// 
        public (bool Success, string Message) LoadPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return (false, "Player name cannot be empty.");
            }

            var player = _playerRepository.LoadPlayer(playerName);

            if (player == null && _playerRepository.PlayerExists(playerName))
            {
                // File exists but couldn't be loaded — corrupt file (US12 scenario 2)
                return (false, $"Error loading player details for '{playerName}'. The file may be corrupted.");
            }

            if (player == null)
            {
                // Player doesn't exist (US12 scenario 3) — create new player
                CurrentPlayer = new Player(playerName);

                return (true, $"Player '{playerName}' not found. A new player has been created.");
            }

            CurrentPlayer = player;

            return (true, $"Welcome back, {player.Name}!");
        }

        /// <summary>
        /// Creates a brand new player.
        /// </summary>
        /// 
        public Player CreatePlayer(string playerName)
        {
            var player = new Player(playerName);

            CurrentPlayer = player;

            return player;
        }

        /// <summary>
        /// Saves the current player's details to file (US8 — on exit).
        /// </summary>
        /// 
        public (bool Success, string Message) SaveCurrentPlayer()
        {
            if (CurrentPlayer == null)
            {
                return (false, "No player is currently loaded.");
            }

            var saved = _playerRepository.SavePlayer(CurrentPlayer);

            if (!saved)
            {
                return (false, "Error saving player details.");
            }

            return (true, $"Player '{CurrentPlayer.Name}' details saved.");
        }

        /// <summary>
        /// Increments CryptogramsPlayed for the current player (US10 — new game only).
        /// </summary>
        /// 
        public void IncrementCryptogramsPlayed()
        {
            if (CurrentPlayer != null)
            {
                CurrentPlayer.CryptogramsPlayed++;
            }
        }

        /// <summary>
        /// Increments CryptogramsCompleted for the current player (US9).
        /// </summary>
        /// 
        public void IncrementCryptogramsCompleted()
        {
            if (CurrentPlayer != null)
            {
                CurrentPlayer.CryptogramsCompleted++;
            }
        }

        /// <summary>
        /// Records a guess result (US11).
        /// </summary>
        /// 
        public void RecordGuess(bool isCorrect)
        {
            if (CurrentPlayer == null)
            {
                return;
            }

            CurrentPlayer.NumGuesses++;

            if (isCorrect)
            {
                CurrentPlayer.NumCorrectGuesses++;
            }
        }
    }
}
