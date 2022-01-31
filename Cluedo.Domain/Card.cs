namespace Cluedo.Domain;

public class Card
{
    internal Card(int cardNo, string name, CardTypes cardType)
    {
        No = cardNo;
        Name = name;
        CardType = cardType;
    }

    public int No { get; private set; }
    public string Name { get; private set; }
    public CardTypes CardType { get; private set; }
}
