using System.Timers;

namespace Cluedo.Domain;

public class Player
{
    private int clueRound;
    private readonly IEnumerable<PlayingCard> cards;
    private readonly StringBuilder infoBuffer;
    private readonly System.Timers.Timer timer;

    internal protected Player(string name, int seqNo,
        IEnumerable<PlayingCard> cards)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        SequenceNo = seqNo;
        this.cards = cards;
        State = PlayingStates.Waiting;
        clueRound = 0;
        ClueCard = null;
        infoBuffer = new StringBuilder();
        timer = new System.Timers.Timer(5000);
    }

    public ReadOnlyCollection<PlayingCard> Cards
        => cards.ToList().AsReadOnly();

    public string Id { get; private set; }
    public string Name { get; private set; }
    public int SequenceNo { get; private set; }
    public PlayingStates State { get; private set; }
    public Card? ClueCard { get; private set; }
    public IEnumerable<string> GameInfo => infoBuffer.ToString()
        .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

    public event EventHandler<AskedClueEventArgs>? AskClueEvent;
    public event EventHandler<GivenClueEventArgs>? GiveClueEvent;
    public event EventHandler<ConfirmedClueEventArgs>? ConfirmClueEvent;
    public event EventHandler<AccusedEventArgs>? AccuseEvent;
    public event EventHandler<EventArgs>? MachineToResponseEvent;

    public void RequestToAction(PlayingStates state, string actionNote)
    {
        infoBuffer.AppendLine(actionNote);
        if (State == PlayingStates.Lose)
        {
            if (state == PlayingStates.GivingClue)
            {
                // Auto giving clue
                timer.Elapsed += AutoGiveClue;
                timer.Start();
            }
        }
        else
        {
            State = state;
            if (state == PlayingStates.AskingForClue)
            {
                clueRound = 0;
            }
            if (MachineToResponseEvent != null)
            {
                // If machine, ask it to response
                MachineToResponseEvent.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void AutoGiveClue(object? sender, ElapsedEventArgs e)
    {
        timer.Stop();
        timer.Elapsed -= AutoGiveClue;

        var clueCard = Cards.FirstOrDefault(c => c.IsClueCard);
        GivenClueEventArgs eventArgs = new(clueCard);
        GiveClueEvent?.Invoke(this, eventArgs);
        ClearCardInQuestionMarking();
    }

    public void AskClue(IEnumerable<Card> cards)
    {
        if (State == PlayingStates.AskingForClue)
        {
            AskedClueEventArgs eventArgs = new(cards);
            AskClueEvent?.Invoke(this, eventArgs);
        }
        else
        {
            throw new InvalidOperationException("Please wait until your turn to ask.");
        }
    }

    public void GiveClue(Card card)
    {
        if (State == PlayingStates.GivingClue)
        {
            if (cards.Where(c => c.IsClueCard).All(c => c.No != card.No))
            {
                throw new InvalidOperationException("Please select any match card.");
            }
            GivenClueEventArgs eventArgs = new(card);
            GiveClueEvent?.Invoke(this, eventArgs);
            ClearCardInQuestionMarking();
        }
        else
        {
            throw new InvalidOperationException("Please wait until your turn to answer.");
        }
    }

    public void GiveNoClue()
    {
        GivenClueEventArgs eventArgs = new();
        State = PlayingStates.Waiting;
        GiveClueEvent?.Invoke(this, eventArgs);
    }

    public void GiveGameInfo(string message)
    {
        infoBuffer.AppendLine(message);
    }

    public void GotClue(string clueNote, Card? card)
    {
        clueRound += 1;
        if (card != null)
        {
            // eliminate suspect card
            var suspect = cards.FirstOrDefault(c => c.No == card.No);
            if (suspect != null) suspect.Eliminate(true);
        }
        infoBuffer.AppendLine(clueNote);
        ClueCard = card;
        State = PlayingStates.ConfirmingClue;

        if (MachineToResponseEvent != null)
        {
            // If machine as to response
            MachineToResponseEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ConfirmClue()
    {
        if (State == PlayingStates.ConfirmingClue)
        {
            State = PlayingStates.Waiting;
            ConfirmedClueEventArgs eventArgs = new(clueRound, ClueCard == null);
            ConfirmClueEvent?.Invoke(this, eventArgs);
            ClueCard = null;
        }
        else
        {
            throw new InvalidOperationException("Please wait until your turn to notice.");
        }
    }

    public void Accuse(IEnumerable<Card> cards)
    {
        if (State == PlayingStates.AskingForClue)
        {
            AccusedEventArgs eventArgs = new(cards);
            State = PlayingStates.Waiting;
            AccuseEvent?.Invoke(this, eventArgs);
        }
        else
        {
            throw new InvalidOperationException("Please wait until your turn.");
        }
    }

    private void ClearCardInQuestionMarking()
    {
        List<int> dummyNo = new() { 99 };
        cards.ToList().ForEach(c => c.MarkAsCardInQuestion(dummyNo));
    }
}
