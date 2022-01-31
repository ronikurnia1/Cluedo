namespace Cluedo.Domain.Events;

public class GivenClueEventArgs : EventArgs
{
    public GivenClueEventArgs(Card? card = null)
    {
        Card = card;
    }
    public Card? Card { get; private set; }
}
