namespace Cluedo.Domain.Events;

public class ConfirmedClueEventArgs : EventArgs
{
    public ConfirmedClueEventArgs(int round, bool needNextClue)
    {
        Round = round;
        NeedNextClue = needNextClue;
    }
    public int Round { get; private set; }
    public bool NeedNextClue { get; private set; }
}
