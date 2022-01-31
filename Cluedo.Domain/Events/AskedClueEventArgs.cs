namespace Cluedo.Domain.Events;

public class AskedClueEventArgs : EventArgs
{
    public AskedClueEventArgs(IEnumerable<Card> cards)
    {
        Cards = new ReadOnlyCollection<Card>(cards.ToList());
    }
    public ReadOnlyCollection<Card> Cards { get; private set; }
}
