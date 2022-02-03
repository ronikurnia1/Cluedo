using Microsoft.Extensions.Logging;
using System.Timers;

namespace Cluedo.Domain;

public class Game : IDisposable
{
    private readonly object registrationLock = new();
    private readonly ILogger<Game> logger;
    private readonly string id;
    private readonly List<Card> distributedCards;

    private readonly System.Timers.Timer timer;
    private Player? accusingPlayer;
    private List<Card> accusingCards = new();

    public Game(ILogger<Game> logger)
    {
        id = Guid.NewGuid().ToString();
        this.logger = logger;
        this.logger.LogInformation("Game initialized");
        distributedCards = GetCards();
        CurrentQuestionCards = new List<Card>().AsReadOnly();
        secretEnvelope = GenerateSecretEnvelope(distributedCards);
        ClueMode = ClueModes.WaitForClue;
        timer = new System.Timers.Timer(5000);
    }

    public string Id => id;
    public string RegisterPlayer(string playerName)
    {
        Player player;
        lock (registrationLock)
        {
            player = CreateAndJoinPlayer(playerName, players.Count);
            logger.LogInformation("{playerName} has joined the game.", playerName);
        }
        List<Player> except = new() { player };
        BroadcastMessage(except, $"{player.Name} has joined the game");

        if (players.Count == 4) StartGame();
        return player.Id;
    }
    public ReadOnlyCollection<Card> SecretEnvelope => secretEnvelope.AsReadOnly();
    public ReadOnlyCollection<Card> CurrentQuestionCards { get; private set; }
    public Player? CurrentAskingClue { get; private set; }
    public Player? CurrentGivingClue { get; private set; }
    public Player? Winner { get; private set; }
    public ClueModes ClueMode { get; private set; }
    public ReadOnlyCollection<Player> Players => players.AsReadOnly();


    public event EventHandler<EventArgs>? StateChangedEvent;

    public void RunWithMachine()
    {
        int numberOfMachine = 4 - players.Count;
        for (int i = 1; i <= numberOfMachine; i++)
        {
            var playerId = RegisterPlayer($"Machine-{i}");
            machines.Add(new(players.First(p => p.Id == playerId)));
        }
    }



    private readonly List<Player> players = new();
    private readonly List<MachinePlayer> machines = new();

    private Player CreateAndJoinPlayer(string playerName, int seqNo)
    {
        List<Card> cardsToPlay = new();

        var actor = distributedCards.ToList()
            .Where(c => c.CardType == CardTypes.Actor)
            .OrderBy(c => Guid.NewGuid()).FirstOrDefault();
        if (actor != null)
        {
            cardsToPlay.Add(actor);
            distributedCards.Remove(actor);
        }

        var weapon = distributedCards.ToList()
            .Where(c => c.CardType == CardTypes.Weapon)
            .OrderBy(c => Guid.NewGuid()).FirstOrDefault();
        if (weapon != null)
        {
            cardsToPlay.Add(weapon);
            distributedCards.Remove(weapon);
        }

        var place = distributedCards.ToList()
            .Where(c => c.CardType == CardTypes.Place)
            .OrderBy(c => Guid.NewGuid()).FirstOrDefault();
        if (place != null)
        {
            cardsToPlay.Add(place);
            distributedCards.Remove(place);
        }

        var additionalCard = distributedCards
            .OrderBy(i => Guid.NewGuid()).Take(4 - cardsToPlay.Count).ToList();
        if (additionalCard.Any())
        {
            cardsToPlay.AddRange(additionalCard);
            distributedCards.RemoveAll(c => additionalCard.Contains(c));
        }

        List<PlayingCard> playingCards = cardsToPlay
            .Select(c => new PlayingCard(c, PlayingTypes.OnHand)).ToList();

        List<PlayingCard> suspectCards = GetCards().Where(c => cardsToPlay.All(o => o.No != c.No))
            .Select(c => new PlayingCard(c, PlayingTypes.Suspect)).ToList();

        playingCards.AddRange(suspectCards);

        Player player = new(playerName, seqNo, playingCards);
        players.Add(player);
        return player;
    }

    private void StartGame()
    {
        // wire up event handler
        SetupEventHandlers();
        Winner = null;
        // asign first player to ask 
        CurrentAskingClue = players.OrderBy(p => p.SequenceNo).First();
        CurrentAskingClue.RequestToAction(PlayingStates.AskingForClue, "Your turn to ask a clue or accuse");
        ClueMode = ClueModes.WaitForClue;
        logger.LogInformation("Game started.");
        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] to ask for clue",
            CurrentAskingClue?.SequenceNo, CurrentAskingClue?.Name);

        // tell the rest
        string info = $"Please wait, {CurrentAskingClue?.Name} turn to ask a clue";
        List<Player> except = new();
        if (CurrentAskingClue != null) except.Add(CurrentAskingClue);
        BroadcastMessage(except, info);

        StateChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private static List<Card> GetCards()
    {
        return new List<Card>
        {
            new Card(0, "Mrs. Peacock", CardTypes.Actor),
            new Card(1, "Miss. Scarlet", CardTypes.Actor),
            new Card(2, "Mrs. White", CardTypes.Actor),
            new Card(3, "Col. Mustard", CardTypes.Actor),
            new Card(4, "Prof. Plum", CardTypes.Actor),
            new Card(5, "Mr. Green", CardTypes.Actor),
            new Card(6, "Knife", CardTypes.Weapon),
            new Card(7, "Candlestick", CardTypes.Weapon),
            new Card(8, "Revolver", CardTypes.Weapon),
            new Card(9, "Rope", CardTypes.Weapon),
            new Card(10, "Lead Pipe", CardTypes.Weapon),
            new Card(11, "Wrench", CardTypes.Weapon),
            new Card(12, "Main Hall", CardTypes.Place),
            new Card(13, "Dining Room", CardTypes.Place),
            new Card(14, "Kitchen", CardTypes.Place),
            new Card(15, "Ballroom", CardTypes.Place),
            new Card(16, "Lounge", CardTypes.Place),
            new Card(17, "Bedroom", CardTypes.Place),
            new Card(18, "Library", CardTypes.Place)
        };
    }

    private readonly List<Card> secretEnvelope;

    private List<Card> GenerateSecretEnvelope(List<Card> cards)
    {
        Card actor = cards.Where(c => c.CardType == CardTypes.Actor)
            .OrderBy(c => Guid.NewGuid()).First();
        cards.Remove(actor);

        Card place = cards.Where(c => c.CardType == CardTypes.Place)
            .OrderBy(c => Guid.NewGuid()).First();
        cards.Remove(place);

        Card weapon = cards.Where(c => c.CardType == CardTypes.Weapon)
            .OrderBy(c => Guid.NewGuid()).First();
        cards.Remove(weapon);

        this.logger.LogInformation("Secret envelop: [{cardNo}]-[{actor}], [{cardNo}]-[{weapon}], [{cardNo}]-[{place}]",
            actor.No, actor.Name, weapon.No, weapon.Name, place.No, place.Name);

        return new List<Card> { actor, place, weapon };
    }

    private void SetupEventHandlers()
    {
        foreach (Player player in players)
        {
            player.AskClueEvent += AskClueEventHandler;
            player.GiveClueEvent += GiveClueEventHandler;
            player.ConfirmClueEvent += ConfirmClueEventHandler;
            player.AccuseEvent += AccuseEventHandler;
        }
    }

    private void TearDownEventHandlers()
    {
        foreach (Player player in players)
        {
            player.AskClueEvent -= AskClueEventHandler;
            player.GiveClueEvent -= GiveClueEventHandler;
            player.ConfirmClueEvent -= ConfirmClueEventHandler;
            player.AccuseEvent -= AccuseEventHandler;
        }
    }

    private void AskClueEventHandler(object? sender, AskedClueEventArgs e)
    {
        Player? player = sender as Player;

        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] ask clue for: {cards}",
            player?.SequenceNo, player?.Name, string.Join(", ", e.Cards.Select(c => c.Name)));

        CurrentQuestionCards = e.Cards.ToList().AsReadOnly();
        GetRightPlayerToGiveClue(0);
        StateChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void GetRightPlayerToGiveClue(int round)
    {
        int playerIndex = 0;
        if (CurrentAskingClue != null)
        {
            playerIndex = CurrentAskingClue.SequenceNo + 1 + round;
        }

        if (playerIndex > 3)
        {
            playerIndex -= 4;
        }

        CurrentGivingClue = players[playerIndex];
        MarkingCardInQuestion(CurrentQuestionCards);

        CurrentAskingClue?.RequestToAction(PlayingStates.Waiting,
            $"Please wait if {CurrentGivingClue?.Name} has any clue");

        CurrentGivingClue?.RequestToAction(PlayingStates.GivingClue,
            $"Your turn, please give {CurrentAskingClue?.Name} a clue you have");

        // tell the rest
        string info = $"Watch out if {CurrentGivingClue?.Name}" +
            $" has a clue for {CurrentAskingClue?.Name} or not";

        List<Player> except = new();
        if (CurrentAskingClue != null) except.Add(CurrentAskingClue);
        if (CurrentGivingClue != null) except.Add(CurrentGivingClue);
        BroadcastMessage(except, info);

        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] to provide clue",
            CurrentGivingClue?.SequenceNo, CurrentGivingClue?.Name);
    }

    private void GiveClueEventHandler(object? sender, GivenClueEventArgs e)
    {
        Player? player = sender != null ? sender as Player : null;

        player?.RequestToAction(PlayingStates.Waiting, $"You've given a clue to {CurrentAskingClue?.Name}");

        // show if any card given to CurrentAskingClue (player)
        ClueMode = e.Card != null ? ClueModes.HasClue : ClueModes.NoClue;
        string clue = ClueMode == ClueModes.HasClue ?
            $"HAS CARD: {e.Card?.Name} ({e.Card?.CardType.ToString()})" :
            "HAS NO CARD";

        string clueInfo = $"{CurrentGivingClue?.Name} {clue} - please confirm";

        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] give clue: {info}",
            CurrentGivingClue?.SequenceNo, CurrentGivingClue?.Name, clue);

        CurrentAskingClue?.GotClue(clueInfo, e.Card);

        // tell the rest
        string info = ClueMode == ClueModes.HasClue
            ? $"{CurrentGivingClue?.Name} HAS ONE of the cards: {string.Join(", ", CurrentQuestionCards.Select(c => c.Name))}"
            : $"{CurrentGivingClue?.Name} HAS NONE of the cards: {string.Join(", ", CurrentQuestionCards.Select(c => c.Name))}";
        List<Player> except = new();
        if (CurrentGivingClue != null) except.Add(CurrentGivingClue);
        if (CurrentAskingClue != null) except.Add(CurrentAskingClue);
        BroadcastMessage(except, info);
        StateChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void MarkingCardInQuestion(IEnumerable<Card> cards)
    {
        List<int> cardsNo = cards.Select(c => c.No).ToList();
        CurrentGivingClue?.Cards.Where(c => c.PlayingType == PlayingTypes.OnHand)
            .ToList().ForEach(c => c.MarkAsCardInQuestion(cardsNo));
    }

    private void ConfirmClueEventHandler(object? sender, ConfirmedClueEventArgs e)
    {
        if (e.NeedNextClue && e.Round < 3)
        {
            GetRightPlayerToGiveClue(e.Round);
        }
        else
        {
            // move for next player
            NextPlayerTurnToAsk();
        }
        StateChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void NextPlayerTurnToAsk()
    {
        // clear current question cards
        CurrentQuestionCards = new List<Card>().AsReadOnly();
        ClueMode = ClueModes.WaitForClue;
        CurrentGivingClue = null;

        var nextPlayers = players.Where(p => p.Id
        != CurrentAskingClue?.Id && p.State != PlayingStates.Lose).ToList();

        // Get next player if any
        CurrentAskingClue = nextPlayers.Where(p => p.SequenceNo > CurrentAskingClue?.SequenceNo)
            .OrderBy(o => o.SequenceNo).FirstOrDefault();

        if (CurrentAskingClue == null)
        {
            // Take the very first available
            CurrentAskingClue = nextPlayers.OrderBy(o => o.SequenceNo).FirstOrDefault();
        }

        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] to ask for clue",
            CurrentAskingClue?.SequenceNo, CurrentAskingClue?.Name);

        CurrentAskingClue?.RequestToAction(PlayingStates.AskingForClue, "Your turn to ask a clue or accuse");

        List<Player> except = new();
        string info = $"Please wait, {CurrentAskingClue?.Name} turn to ask a clue";
        if (CurrentAskingClue != null) except.Add(CurrentAskingClue);
        BroadcastMessage(except, info);
    }

    private void AccuseEventHandler(object? sender, AccusedEventArgs e)
    {
        accusingPlayer = sender != null ? sender as Player : null;
        accusingCards = e.Cards.ToList();

        logger.LogInformation("Player [{sequenceNo}]-[{playerName}] to accuse with card numbers: {cardNumbers}",
            accusingPlayer?.SequenceNo, accusingPlayer?.Name, string.Join(", ", e.Cards.OrderBy(i => i.No).Select(c => c.No)));
        logger.LogInformation("Secret envelope card numbers: {cardNumbers}",
            string.Join(", ", secretEnvelope.OrderBy(i => i.No).Select(c => c.No)));

        CurrentQuestionCards = e.Cards;

        List<Player> except = new();
        if (accusingPlayer != null) except.Add(accusingPlayer);
        BroadcastMessage(except, $"{accusingPlayer?.Name} try to accuse with: {string.Join(", ", e.Cards.Select(c => c.Name))}");

        StateChangedEvent?.Invoke(this, EventArgs.Empty);

        timer.Elapsed += DecideWinner;
        timer.Start();
    }

    private void DecideWinner(object? sender, ElapsedEventArgs e)
    {
        timer.Stop();
        timer.Elapsed -= DecideWinner;

        if (accusingCards.OrderBy(i => i.No).Select(c => c.No)
            .SequenceEqual(secretEnvelope.Select(c => c.No).OrderBy(i => i)))
        {
            Winner = accusingPlayer;
            Winner?.RequestToAction(PlayingStates.Win, "Congratulation you win the game!");
            logger.LogInformation("Player [{sequenceNo}]-[{playerName}] WIN the game",
                Winner?.SequenceNo, Winner?.Name);

            Parallel.ForEach(players.Where(p => p.Id != Winner?.Id).ToList(),
                i => i.RequestToAction(PlayingStates.Lose,
                $"Sorry! but {Winner?.Name} has win the game!"));

            logger.LogInformation("Game has ended");
            StateChangedEvent?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            logger.LogInformation("Player [{sequenceNo}]-[{playerName}] LOSE the game",
                accusingPlayer?.SequenceNo, accusingPlayer?.Name);

            accusingPlayer?.RequestToAction(PlayingStates.Lose,
                $"Sorry you lose, but the correct answer are: {string.Join(", ", secretEnvelope.Select(p => p.Name))}");

            Parallel.ForEach(players.Where(p => p.Id != accusingPlayer?.Id).ToList(),
                i => i.GiveGameInfo($"player {accusingPlayer?.Name} LOSES the game!"));

            StateChangedEvent?.Invoke(this, EventArgs.Empty);

            timer.Elapsed += ProceedTheGame;
            timer.Start();
        }
    }


    private void ProceedTheGame(object? sender, ElapsedEventArgs e)
    {
        timer.Stop();
        timer.Elapsed -= ProceedTheGame;

        NextPlayerTurnToAsk();
        StateChangedEvent?.Invoke(this, EventArgs.Empty);
    }


    private void BroadcastMessage(IEnumerable<Player> except, string info)
    {
        var tasks = new List<Task>();
        players.Except(except).ToList().ForEach
            (i => tasks.Add(Task.Run(() => i.GiveGameInfo(info))));
        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        TearDownEventHandlers();
        GC.SuppressFinalize(this);
    }
}
