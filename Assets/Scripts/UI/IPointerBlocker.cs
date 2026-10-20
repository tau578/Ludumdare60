namespace Switchboard.UI
{
    /// <summary>
    /// A raycast target that swallows clicks (the tutorial's dark layer, D57): when it is the top hit, the cord input
    /// neither picks, plugs nor drops, and a held cord stays in hand.
    /// </summary>
    public interface IPointerBlocker
    {
    }
}
