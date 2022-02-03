namespace Cluedo.Domain.Events;

public class AccusedEventArgs : EventArgs
{
    public AccusedEventArgs(IEnumerable<Card> cards)
    {
        Cards = cards.ToList().AsReadOnly();
    }
    public ReadOnlyCollection<Card> Cards { get; private set; }
}
