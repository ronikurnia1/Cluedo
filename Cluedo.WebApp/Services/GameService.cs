using Cluedo.Domain;
using System.Collections.ObjectModel;

namespace Cluedo.WebApp.Services;

public class GameService : IGameService
{
    private readonly List<Game> games;
    private readonly ILoggerFactory loggerFactory;

    public GameService(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        games = new();
    }

    public Game CreateNewGame()
    {
        var newGame = new Game(loggerFactory.CreateLogger<Game>());
        games.Add(newGame);
        return newGame;
    }

    public void DemolishGame(string gameId)
    {
        var game = games.First(g => g.Id == gameId);
        games.Remove(game);
        game.Dispose();
        game = null;
    }

    public ReadOnlyCollection<Game> Games => games.AsReadOnly();

    public Game? GetGame(string gameId)
    {
        return games.FirstOrDefault(g => g.Id == gameId);
    }

}
