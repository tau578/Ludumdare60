using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>
    /// The colours the board's own graphics take from the game's paint box (<c>GameStyle</c>, D75). An interface, so
    /// the board code never depends on the Game layer.
    /// </summary>
    public interface IBoardPalette
    {
        /// <summary>Crimson light on dark: a wrong number's flash, the red ring over a live call's jack.</summary>
        Color ErrorGlow { get; }
        /// <summary>The wrong-number ring's outer edge, as a share of the jack's size.</summary>
        float WrongNumberRingOuter { get; }
        /// <summary>The wrong-number ring's strength, 0 to 1.</summary>
        float WrongNumberStrength { get; }
        /// <summary>The red ring's strength when the pointer is over a live call's jack.</summary>
        float BlockedGlowIntensity { get; }
    }
}
