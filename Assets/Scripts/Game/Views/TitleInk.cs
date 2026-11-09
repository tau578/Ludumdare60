using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// A big title's colour from the paint box (D75): Title gold, on screens that don't show the money. It sits on menu
    /// titles outside the game installer's reach, so it holds its own reference, and reads the colour each time the title shows.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TitleInk : MonoBehaviour
    {
        [SerializeField] private GameStyle style;

        private void OnEnable()
        {
            if (style != null) GetComponent<TMP_Text>().color = style.TitleGold;
        }
    }
}
