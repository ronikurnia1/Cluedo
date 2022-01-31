namespace Cluedo.Domain.Events;

public class AccusedEventArgs : EventArgs
{
    public AccusedEventArgs(IEnumerable<int> cardNumbers)
    {
        CardNumbers = cardNumbers.ToList().AsReadOnly();
    }
    public ReadOnlyCollection<int> CardNumbers { get; private set; }
}
