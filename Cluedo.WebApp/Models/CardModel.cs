using Cluedo.Domain;
using Cluedo.Domain.Enums;

namespace Cluedo.WebApp.Models;

public class CardModel
{
    private readonly PlayingCard card;

    public CardModel(PlayingCard playingCard)
    {
        card = playingCard;
        Reposition();
    }

    public string Image => $"img/{card.No}.svg";
    public CardLocations Location { get; private set; }
    public void Undrop()
    {
        Reposition();
    }

    public PlayingCard State => card;

    public void Drop()
    {
        Location = CardLocations.DroppedZone;
    }

    private void Reposition()
    {
        switch (card.PlayingType)
        {
            case PlayingTypes.OnHand:
                Location = CardLocations.OnHandZone;
                break;
            case PlayingTypes.Suspect:
                switch (card.CardType)
                {
                    case CardTypes.Actor:
                        Location = CardLocations.ActorZone;
                        break;
                    case CardTypes.Weapon:
                        Location = CardLocations.WeaponZone;
                        break;
                    case CardTypes.Place:
                        Location = CardLocations.PlaceZone;
                        break;
                    default:
                        break;
                }
                break;
            default:
                Location = CardLocations.Undefine;
                break;
        }
    }
}

public enum CardLocations
{
    Undefine,
    OnHandZone,
    DroppedZone,
    ActorZone,
    WeaponZone,
    PlaceZone
}