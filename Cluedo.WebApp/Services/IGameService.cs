using Cluedo.Domain;
using System.Collections.ObjectModel;

namespace Cluedo.WebApp.Services;

public interface IGameService
{
    public Game? GetGame(string gameId);
    public Game CreateNewGame();
    public ReadOnlyCollection<Game> Games { get; }
    public void DemolishGame(string gameId);
}
