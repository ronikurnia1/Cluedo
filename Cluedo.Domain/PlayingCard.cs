namespace Cluedo.Domain;

public class PlayingCard : Card
{
    private const string CARD_ELIMINATED_CALSS = "card-eliminated";
    private const string CLUE_CARD_CALSS = "clue-card";

    public PlayingCard(Card card, PlayingTypes playingType) :
        base(card.No, card.Name, card.CardType)
    {
        IsEliminated = false;
        IsClueCard = false;
        PlayingType = playingType;
    }

    public PlayingCard(PlayingCard card) :
        base(card.No, card.Name, card.CardType)
    {
        IsEliminated = card.IsEliminated;
        IsClueCard = card.IsClueCard;
        PlayingType = card.PlayingType;
    }

    public PlayingTypes PlayingType { get; private set; }
    public bool IsEliminated { get; private set; }
    public string EliminatedClass => IsEliminated ? CARD_ELIMINATED_CALSS : "";

    public bool IsClueCard { get; private set; }
    public string ClueCardClass => IsClueCard ? CLUE_CARD_CALSS : "";

    public void Eliminate(bool isEliminated)
    {
        if (PlayingType == PlayingTypes.Suspect)
        {
            IsEliminated = isEliminated;
        }
    }
    public void MarkAsCardInQuestion(IEnumerable<int> cardNumbers)
    {
        IsClueCard = cardNumbers.Contains(No);
    }
}
