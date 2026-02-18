using CryptogramGame;
using CryptogramGame.Persistence;
using CryptogramGame.Services;
using CryptogramGame.UI;

// File paths
var phrasesFile = "Phrases.txt";
var playersDir = "players";
var savesDir = "saves";

// Repositories
var phraseRepository = new PhraseRepository(phrasesFile);
var playerRepository = new PlayerRepository(playersDir);
var gameStateRepository = new GameStateRepository(savesDir);

// Services
var playerService = new PlayerService(playerRepository);
var cryptogramService = new CryptogramService(phraseRepository);
var gameService = new GameService(playerService);
var scoreboardService = new ScoreboardService(playerRepository);

// UI
var ui = new ConsoleUI(
    playerService,
    cryptogramService,
    gameService,
    scoreboardService,
    gameStateRepository);

ui.Run();