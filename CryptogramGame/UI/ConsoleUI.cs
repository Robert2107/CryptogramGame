using CryptogramGame.Models;
using CryptogramGame.Persistence;
using CryptogramGame.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptogramGame.UI
{
    public class ConsoleUI
    {
        private readonly PlayerService _playerService;
        private readonly CryptogramService _cryptogramService;
        private readonly GameService _gameService;
        private readonly ScoreboardService _scoreboardService;
        private readonly GameStateRepository _gameStateRepository;

        public ConsoleUI(
            PlayerService playerService,
            CryptogramService cryptogramService,
            GameService gameService,
            ScoreboardService scoreboardService,
            GameStateRepository gameStateRepository)
        {
            _playerService = playerService;
            _cryptogramService = cryptogramService;
            _gameService = gameService;
            _scoreboardService = scoreboardService;
            _gameStateRepository = gameStateRepository;
        }

        public void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("       CRYPTOGRAM GAME");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // Player identification (US8/US12)
            IdentifyPlayer();

            if (_playerService.CurrentPlayer == null)
            {
                Console.WriteLine("Could not load or create player. Exiting.");

                return;
            }

            // Main menu loop
            bool running = true;

            while (running)
            {
                running = ShowMainMenu();
            }

            // Save player on exit (US8)
            var saveResult = _playerService.SaveCurrentPlayer();

            Console.WriteLine(saveResult.Message);
            Console.WriteLine("Goodbye!");
        }

        // =========================================================================
        // Player identification
        // =========================================================================

        private void IdentifyPlayer()
        {
            Console.Write("Enter your player name: ");

            var name = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Player name cannot be empty.");

                IdentifyPlayer();

                return;
            }

            var result = _playerService.LoadPlayer(name);

            Console.WriteLine(result.Message);
            Console.WriteLine();
        }

        // =========================================================================
        // Main menu
        // =========================================================================

        private bool ShowMainMenu()
        {
            var player = _playerService.CurrentPlayer!;

            Console.WriteLine();
            Console.WriteLine("--- MAIN MENU ---");
            Console.WriteLine($"Player: {player.Name} | Played: {player.CryptogramsPlayed} | Completed: {player.CryptogramsCompleted} | Accuracy: {player.AccuracyPercentage:F1}%");
            Console.WriteLine();
            Console.WriteLine("1. New Game");
            Console.WriteLine("2. Load Saved Game");
            Console.WriteLine("3. Scoreboard");
            Console.WriteLine("4. Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    StartNewGame();

                    break;

                case "2":
                    LoadSavedGame();

                    break;

                case "3":
                    ShowScoreboard();

                    break;

                case "4":
                    return false;

                default:
                    Console.WriteLine("Invalid option. Please try again.");

                    break;
            }

            return true;
        }

        // =========================================================================
        // US1 — New game
        // =========================================================================

        private void StartNewGame()
        {
            Console.WriteLine();
            Console.WriteLine("Select cryptogram type:");
            Console.WriteLine("1. Letters");
            Console.WriteLine("2. Numbers");
            Console.Write("Choice: ");

            var typeInput = Console.ReadLine()?.Trim();

            CryptogramType type;

            switch (typeInput)
            {
                case "1":
                    type = CryptogramType.Letters;

                    break;

                case "2":
                    type = CryptogramType.Numbers;

                    break;

                default:
                    Console.WriteLine("Invalid choice.");

                    return;
            }

            var result = _cryptogramService.GenerateCryptogram(type);

            if (result.Cryptogram == null)
            {
                Console.WriteLine(result.Message);

                return;
            }

            _gameService.StartNewGame(result.Cryptogram);

            Console.WriteLine();
            Console.WriteLine("Cryptogram generated! Let's play.");

            RunGameLoop();
        }

        // =========================================================================
        // US5 — Load saved game
        // =========================================================================

        private void LoadSavedGame()
        {
            var playerName = _playerService.CurrentPlayer!.Name;

            var (state, loadResult) = _gameStateRepository.LoadGameState(playerName);

            switch (loadResult)
            {
                case LoadResult.NotFound:
                    Console.WriteLine("You don't have a saved game.");

                    return;

                case LoadResult.Corrupted:
                    Console.WriteLine("Error: Your save file appears to be corrupted.");

                    return;

                case LoadResult.Success:
                    _gameService.LoadGame(state!);

                    Console.WriteLine("Saved game loaded! Resuming play.");

                    RunGameLoop();

                    break;
            }
        }

        // =========================================================================
        // Game loop
        // =========================================================================

        private void RunGameLoop()
        {
            bool playing = true;

            while (playing && _gameService.GameInProgress)
            {
                DisplayCryptogram();

                Console.WriteLine();
                Console.WriteLine("Actions: [E]nter letter | [U]ndo | [H]int | [F]requencies | [S]ave | So[l]ution | [Q]uit to menu");
                Console.Write("Choice: ");

                var input = Console.ReadLine()?.Trim().ToLower();

                switch (input)
                {
                    case "e":
                        HandleEnterLetter();

                        break;

                    case "u":
                        HandleUndo();

                        break;

                    case "h":
                        HandleHint();

                        break;

                    case "f":
                        HandleFrequencies();

                        break;

                    case "s":
                        HandleSave();

                        break;

                    case "l":
                        HandleShowSolution();

                        playing = false;

                        break;

                    case "q":
                        playing = false;

                        _gameService.EndGame();

                        break;

                    default:
                        Console.WriteLine("Invalid action. Please try again.");

                        break;
                }
            }
        }

        // =========================================================================
        // Display
        // =========================================================================

        private void DisplayCryptogram()
        {
            if (_gameService.CurrentGame == null)
            {
                return;
            }

            var cryptogram = _gameService.CurrentGame.Cryptogram;

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine($"Cryptogram ({cryptogram.Type}):");
            Console.WriteLine(cryptogram.EncodedPhrase);
            Console.WriteLine();
            Console.WriteLine("Your progress:");
            Console.WriteLine(_gameService.GetDisplayString());
            Console.WriteLine("========================================");

            // Show current mappings
            if (_gameService.CurrentGame.PlayerMapping.Count > 0)
            {
                Console.WriteLine();
                Console.Write("Mappings: ");

                var mappings = _gameService.CurrentGame.PlayerMapping
                    .OrderBy(kv => kv.Value)
                    .Select(kv => $"{kv.Key}->{kv.Value}");

                Console.WriteLine(string.Join("  ", mappings));
            }
        }

        // =========================================================================
        // US2 — Enter letter
        // =========================================================================

        private void HandleEnterLetter()
        {
            Console.Write("Enter cryptogram value to replace: ");

            var cryptoValue = Console.ReadLine()?.Trim() ?? string.Empty;

            if (_gameService.CurrentGame?.Cryptogram.Type == CryptogramType.Letters)
            {
                cryptoValue = cryptoValue.ToUpper();
            }

            // Validate the cryptogram value
            var validation = _gameService.ValidateCryptogramValue(cryptoValue);

            if (!validation.Valid)
            {
                Console.WriteLine(validation.Message);

                return;
            }

            Console.Write("Enter the plain letter to map: ");

            var letterInput = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(letterInput) || letterInput.Length != 1 || !char.IsLetter(letterInput[0]))
            {
                Console.WriteLine("Please enter a single letter (a-z).");

                return;
            }

            char plainLetter = letterInput[0];

            // First attempt without overwrite
            var result = _gameService.EnterLetter(cryptoValue, plainLetter);

            // Handle overwrite confirmation
            if (result.RequiresOverwriteConfirmation)
            {
                Console.Write($"'{cryptoValue}' is already mapped to '{result.ExistingMapping}'. Overwrite? (y/n): ");

                var confirm = Console.ReadLine()?.Trim().ToLower();

                if (confirm == "y")
                {
                    result = _gameService.EnterLetter(cryptoValue, plainLetter, overwriteConfirmed: true);
                }
                else
                {
                    Console.WriteLine("Mapping not changed.");

                    return;
                }
            }

            Console.WriteLine(result.Message);

            // Handle game completion
            if (result.GameCompleted && result.GameWon)
            {
                DisplayCryptogram();

                _gameService.EndGame();

                // Delete save file on completion
                _gameStateRepository.DeleteSave(_playerService.CurrentPlayer!.Name);
            }
        }

        // =========================================================================
        // US3 — Undo
        // =========================================================================

        private void HandleUndo()
        {
            Console.Write("Enter cryptogram value to unmap: ");

            var cryptoValue = Console.ReadLine()?.Trim() ?? string.Empty;

            if (_gameService.CurrentGame?.Cryptogram.Type == CryptogramType.Letters)
            {
                cryptoValue = cryptoValue.ToUpper();
            }

            var result = _gameService.UndoLetter(cryptoValue);

            Console.WriteLine(result.Message);
        }

        // =========================================================================
        // US14 — Hint
        // =========================================================================

        private void HandleHint()
        {
            var result = _gameService.GetHint();

            Console.WriteLine(result.Message);
        }

        // =========================================================================
        // US7 — Frequencies
        // =========================================================================

        private void HandleFrequencies()
        {
            var frequencies = _gameService.GetFrequencies();

            Console.WriteLine();
            Console.WriteLine("Letter Frequencies:");
            Console.WriteLine($"{"Letter",-8} {"Cryptogram",-14} {"English",-14}");
            Console.WriteLine(new string('-', 36));

            foreach (var entry in frequencies)
            {
                if (entry.CryptogramProportion > 0 || entry.EnglishProportion > 0)
                {
                    Console.WriteLine($"  {entry.Letter,-6} {entry.CryptogramProportion,10:P1}    {entry.EnglishProportion,10:P1}");
                }
            }

            Console.WriteLine();
        }

        // =========================================================================
        // US4 — Save
        // =========================================================================

        private void HandleSave()
        {
            var playerName = _playerService.CurrentPlayer!.Name;

            // Check if save already exists (US4 scenario 2)
            if (_gameStateRepository.SaveExists(playerName))
            {
                Console.Write("You already have a saved game. Overwrite? (y/n): ");

                var confirm = Console.ReadLine()?.Trim().ToLower();

                if (confirm != "y")
                {
                    Console.WriteLine("Save cancelled. Original save kept.");

                    return;
                }
            }

            var saved = _gameStateRepository.SaveGameState(playerName, _gameService.CurrentGame!);

            if (saved)
            {
                Console.WriteLine("Game saved successfully.");
            }
            else
            {
                Console.WriteLine("Error saving game.");
            }
        }

        // =========================================================================
        // US6 — Show solution
        // =========================================================================

        private void HandleShowSolution()
        {
            var (solutionMap, decodedPhrase) = _gameService.ShowSolution();

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("SOLUTION:");
            Console.WriteLine(decodedPhrase);
            Console.WriteLine("========================================");
            Console.WriteLine();

            Console.Write("Correct mapping: ");

            var mappings = solutionMap
                .OrderBy(kv => kv.Value)
                .Select(kv => $"{kv.Key}->{kv.Value}");

            Console.WriteLine(string.Join("  ", mappings));

            Console.WriteLine();

            _gameService.EndGame();

            // Delete save file since game is over
            _gameStateRepository.DeleteSave(_playerService.CurrentPlayer!.Name);
        }

        // =========================================================================
        // US13 — Scoreboard
        // =========================================================================

        private void ShowScoreboard()
        {
            var (entries, message) = _scoreboardService.GetTopTen();

            Console.WriteLine();

            if (entries == null)
            {
                Console.WriteLine(message);

                return;
            }

            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine($"{"Rank",-6} {"Player",-20} {"Completed",-12} {"Played",-10} {"Proportion",-12}");
            Console.WriteLine(new string('-', 60));

            for (int i = 0; i < 10; i++)
            {
                if (i < entries.Count)
                {
                    var e = entries[i];

                    Console.WriteLine($"  {i + 1,-4} {e.PlayerName,-20} {e.CryptogramsCompleted,-12} {e.CryptogramsPlayed,-10} {e.CompletionProportion,8:P1}");
                }
                else
                {
                    Console.WriteLine($"  {i + 1,-4} {"---",-20}");
                }
            }

            Console.WriteLine();
        }
    }
}
