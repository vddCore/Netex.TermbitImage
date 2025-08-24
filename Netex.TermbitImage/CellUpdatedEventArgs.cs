namespace Netex.TermbitImage;

public sealed class CellUpdatedEventArgs
{
    public TermbitImageCell Cell { get; }

    public CellUpdatedEventArgs(TermbitImageCell cell)
        => Cell = cell;
}