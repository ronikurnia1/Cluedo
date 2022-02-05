using System.Timers;

namespace Cluedo.Domain;

public class MachinePlayer : IDisposable
{
    private readonly Player player;
    private readonly System.Timers.Timer timer;

    internal MachinePlayer(Player player)
    {
        this.player = player;
        this.player.MachineToResponseEvent += MachineRespose;
        timer = new System.Timers.Timer(4000);
    }

    public void MachineRespose(object? sender, EventArgs e)
    {
        timer.Elapsed += MachineAction;
        timer.Start();
    }


    private void MachineAction(object? sender, ElapsedEventArgs e)
    {
        timer.Stop();
        timer.Elapsed -= MachineAction;

        switch (player.State)
        {
            case PlayingStates.AskingForClue:
                // If only 3 cards uneliminated then Accuse
                var cards = player.Cards.Where(c => !c.IsEliminated
                && c.PlayingType == PlayingTypes.Suspect).ToList();
                if (cards.Count == 3)
                {
                    player.Accuse(cards);
                }
                else
                {
                    // asking for clue
                    // take 3 uneliminated random cards
                    player.AskClue(cards.OrderBy(c => Guid.NewGuid()).Take(3));
                }
                break;
            case PlayingStates.ConfirmingClue:
                player.ConfirmClue();
                break;
            case PlayingStates.GivingClue:
                var card = player.Cards.Where(c => c.PlayingType == PlayingTypes.OnHand
                && c.IsClueCard).FirstOrDefault();
                if (card != null)
                {
                    player.GiveClue(card);
                }
                else
                {
                    player.GiveNoClue();
                }
                break;
            case PlayingStates.Waiting:
                break;
            case PlayingStates.Lose:
                break;
            case PlayingStates.Win:
                break;
        }
    }

    public void Dispose()
    {
        if (player != null)
        {
            player.MachineToResponseEvent -= MachineRespose;
        }
        GC.SuppressFinalize(this);
    }
}
