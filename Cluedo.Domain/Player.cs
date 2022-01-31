namespace Cluedo.Domain;

public class Player
{
    private int clueRound;
    private readonly IEnumerable<PlayingCard> cards;
    private StringBuilder infoBuffer;

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

    //public void MarkingCard(int cardNo, bool isNotSuspect)
    //{
    //    PlayingCard playingCard = suspectCards.First(c => c.No == cardNo);
    //    if (playingCard != null)
    //    {
    //        playingCard.Eliminate(isNotSuspect);
    //    }
    //}

    public void RequestToAction(PlayingStates state, string actionNote)
    {
        if (State == PlayingStates.Lose)
        {
            if (state == PlayingStates.GivingClue)
            {
                // Auto giving clue
                var clueCard = Cards.FirstOrDefault(c => c.IsClueCard);

                GivenClueEventArgs eventArgs = new(clueCard);
                GiveClueEvent?.Invoke(this, eventArgs);
                ClearCardInQuestionMarking();
            }
            if (state == PlayingStates.Waiting)
            {
                infoBuffer.AppendLine(actionNote);
            }
        }
        else
        {
            State = state;
            infoBuffer.AppendLine(actionNote);
            if (state == PlayingStates.AskingForClue)
            {
                clueRound = 0;
            }
        }
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

    public void Accuse(IEnumerable<int> cardsNo)
    {
        if (State == PlayingStates.AskingForClue)
        {
            AccusedEventArgs eventArgs = new(cardsNo);
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
