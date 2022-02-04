using Cluedo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Cluedo.Domain.UnitTest;

[Collection("game")]
public class GameUnitTest : IAsyncLifetime
{
    [Fact]
    public async Task Game_started_when_4_players_join_with_correct_states()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());

        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        // act
        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));

        await Task.WhenAll(tasks);

        // assert
        Assert.True(game.CurrentAskingClue?.Name == game.Players.First(p => p.SequenceNo == 0).Name);
        Assert.True(game.CurrentAskingClue?.State == PlayingStates.AskingForClue);

        Assert.True(game.Players.All(p => p.Cards.Count(c => c.PlayingType == PlayingTypes.OnHand) == 4));

        Assert.True(game.Players.All(p => p.Cards.Count(c => c.PlayingType == PlayingTypes.Suspect) == 15));

        Assert.True(game.SecretEnvelope.AsEnumerable().Count(c => c.CardType == CardTypes.Place) == 1);
        Assert.True(game.SecretEnvelope.AsEnumerable().Count(c => c.CardType == CardTypes.Weapon) == 1);
        Assert.True(game.SecretEnvelope.AsEnumerable().Count(c => c.CardType == CardTypes.Actor) == 1);
    }

    [Fact]
    public async Task GivingClue_IsNextPlayer_AskingClue()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));

        await Task.WhenAll(tasks);

        // act
        game.CurrentAskingClue?.AskClue(game.CurrentAskingClue
            .Cards.ToList());

        // assert
        Assert.True(game.CurrentGivingClue?.Name == game.Players.First(p => p.SequenceNo == 1).Name);
    }

    [Fact]
    public async Task AcusseWithTheRightCards()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));

        await Task.WhenAll(tasks);

        // act
        game.CurrentAskingClue?.Accuse(game.SecretEnvelope);

        System.Threading.Thread.Sleep(5000);
        // assert
        Assert.True(game.CurrentAskingClue?.State == PlayingStates.Win);
    }

    [Fact]
    public async Task AcusseWithTheWrongCards()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));

        await Task.WhenAll(tasks);

        // act
        game.CurrentAskingClue?.Accuse(game.CurrentAskingClue
            .Cards.Take(3));
        System.Threading.Thread.Sleep(5000);

        // assert
        Assert.True(game.CurrentAskingClue?.State == PlayingStates.Lose);
    }


    [Fact]
    public async Task GivingClue_WithRightCard()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));
        await Task.WhenAll(tasks);

        var cardsToAsk = game.Players.First(p => p.SequenceNo == 1).Cards.Take(3);

        game.CurrentAskingClue?.AskClue(cardsToAsk);

        // act
        game.CurrentGivingClue?.GiveClue(game.CurrentGivingClue.Cards
            .First(c => c.IsClueCard));

        // assert
        Assert.True(game.CurrentAskingClue?.Cards.Count(c => c.IsEliminated) == 1);
    }

    [Fact]
    public async Task OnHandCardsMustBeDifferent()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        // act
        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));
        await Task.WhenAll(tasks);

        // assert
        List<int> cards = game.Players.SelectMany(p => p.Cards)
            .Where(i => i.PlayingType == PlayingTypes.OnHand).Select(c => c.No).ToList();

        cards.AddRange(game.SecretEnvelope.Select(c => c.No).ToList());

        int i = 0;
        foreach (int no in cards.OrderBy(n => n).ToList())
        {
            Assert.True(i == no);
            i++;
        }
    }

    [Fact]
    public async Task SecretEnvelopShouldContainActorPlaceAndWeapon()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };

        // act
        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));
        await Task.WhenAll(tasks);

        // assert
        Assert.True(game.SecretEnvelope.Count == 3);
        Assert.True(game.SecretEnvelope.Count(c => c.CardType == CardTypes.Actor) == 1);
        Assert.True(game.SecretEnvelope.Count(c => c.CardType == CardTypes.Weapon) == 1);
        Assert.True(game.SecretEnvelope.Count(c => c.CardType == CardTypes.Place) == 1);
    }


    [Fact]
    public async Task EliminatedCardShouldPresist()
    {
        // arrange
        using Game game = new(new NullLogger<Game>());
        List<string> players = new() { "Player1", "Player2", "Player3", "Player4" };
        var tasks = new List<Task>();
        players.ForEach(p => tasks.Add(Task.Run(() => game.RegisterPlayer(p))));
        await Task.WhenAll(tasks);

        // Act
        var card = game.Players[0].Cards.Where(c => c.PlayingType == PlayingTypes.Suspect).First();
        card.Eliminate(true);

        // assert
        var actualCard = game.Players[0].Cards.Where(c => c.No == card.No).First();

        Assert.True(actualCard.IsEliminated);
        Assert.True(actualCard.EliminatedClass == "card-eliminated");
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
