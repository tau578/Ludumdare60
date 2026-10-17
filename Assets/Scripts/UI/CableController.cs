using Switchboard.Cables;
using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>One cable: left-click a free socket to insert, or its occupied socket to remove.</summary>
    [DisallowMultipleComponent]
    public sealed class CableController : MonoBehaviour
    {
        [Header("Connection rules")]
        [SerializeField, Tooltip("Reject connecting both ends of this cable to the same socket.")]
        private bool preventSameSocketConnection = true;

        [Header("UI")]
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic firstPlug;
        [SerializeField] private PlugGraphic secondPlug;
        [SerializeField] private StoredCableView trayCableView;
        [SerializeField] private Color cableColor = new Color32(178, 99, 51, 255);

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip insertSound;
        [SerializeField] private AudioClip removeSound;
        [SerializeField] private AudioClip blockedSound;

        private CableConnection<SocketView> connection;
        private SocketView hoveredSocket;
        private Vector2 pointer;
        private bool pointerInside;

        public CablePhase Phase => Connection.Phase;
        public SocketView FirstSocket => Connection.FirstSocket;
        public SocketView SecondSocket => Connection.SecondSocket;
        public bool IsInTray => Connection.IsInTray;
        public Color CableColor => cableColor;
        public StoredCableView TrayView => trayCableView;
        public bool IsPointerVisible => pointerInside && Phase != CablePhase.Connected;
        private CableConnection<SocketView> Connection => connection ??
            (connection = new CableConnection<SocketView>(preventSameSocketConnection));

        public void Configure(RectTransform area, CableGraphic cable,
            PlugGraphic plugA, PlugGraphic plugB, AudioSource sound,
            AudioClip insert, AudioClip remove, AudioClip blocked)
        {
            interactionArea = area;
            wire = cable;
            firstPlug = plugA;
            secondPlug = plugB;
            audioSource = sound;
            insertSound = insert;
            removeSound = remove;
            blockedSound = blocked;
            ApplyColor();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            connection = new CableConnection<SocketView>(preventSameSocketConnection);
            if (trayCableView != null) trayCableView.Bind(this);
            ApplyColor();
            RefreshVisuals();
        }

        public void SetCableColor(Color value)
        {
            cableColor = value;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (wire != null) wire.InsulationColor = cableColor;
            if (firstPlug != null) firstPlug.SleeveColor = cableColor;
            if (secondPlug != null) secondPlug.SleeveColor = cableColor;
            if (trayCableView != null) trayCableView.SetCableColor(cableColor);
        }

        public bool TryPickUp()
        {
            if (!Connection.TryPickUp()) return false;
            RefreshVisuals();
            return true;
        }

        public bool TryReturnToTray()
        {
            SocketView first = FirstSocket, second = SecondSocket;
            if (!Connection.TryReturnToTray()) return false;
            if (first != null) first.Release(this);
            if (second != null) second.Release(this);
            if (first != null || second != null) PlaySound(removeSound, 1f, 0.65f);
            pointerInside = false;
            RefreshVisuals();
            return true;
        }

        public void UpdatePointer(Vector2 screenPosition, bool selected = true)
        {
            if (interactionArea == null) return;
            Canvas canvas = interactionArea.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            pointerInside = selected && RectTransformUtility.RectangleContainsScreenPoint(interactionArea, screenPosition, camera);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, screenPosition, camera, out pointer);
            RefreshVisuals();
        }

        public void HidePointer()
        {
            pointerInside = false;
            RefreshVisuals();
        }

        public bool TryInsert(SocketView socket)
        {
            if (socket == null || !socket.isActiveAndEnabled || socket.PlugAnchor == null) return false;
            if (!Connection.CanInsert(socket)) return Reject();
            if (!socket.TryOccupy(this)) return Reject();
            if (!Connection.TryInsert(socket)) { socket.Release(this); return false; }
            PlaySound(insertSound, Phase == CablePhase.Connected ? 1.07f : 1f, 0.75f);
            RefreshVisuals();
            return true;
        }

        public bool TryRemove(SocketView socket)
        {
            if (!Connection.TryRemove(socket)) return false;
            if (!Connection.Contains(socket)) socket.Release(this);
            PlaySound(removeSound, 1f, 0.65f);
            RefreshVisuals();
            return true;
        }

        private bool Reject()
        {
            PlayBlockedFeedback();
            return false;
        }

        public void PlayBlockedFeedback() => PlaySound(blockedSound, 1f, 0.3f);

        public void SetHoveredSocket(SocketView socket) => hoveredSocket = socket;

        private void RefreshVisuals()
        {
            RefreshTrayImage();
            if (wire == null || firstPlug == null || secondPlug == null) return;
            bool firstAttached = FirstSocket != null;
            bool secondAttached = SecondSocket != null;
            bool showFirst = firstAttached || (pointerInside && (Phase == CablePhase.Held || secondAttached));
            bool showSecond = secondAttached || (firstAttached && !secondAttached && pointerInside);
            firstPlug.gameObject.SetActive(showFirst);
            secondPlug.gameObject.SetActive(showSecond);
            firstPlug.rectTransform.anchoredPosition = firstAttached ? LocalAnchor(FirstSocket) : pointer;
            secondPlug.rectTransform.anchoredPosition = secondAttached ? LocalAnchor(SecondSocket) : pointer;

            bool blocked = hoveredSocket != null && !Connection.Contains(hoveredSocket) && hoveredSocket.IsOccupied;
            Color freeColor = blocked ? new Color(1f, 0.48f, 0.38f, 0.85f) : Color.white;
            firstPlug.color = firstAttached ? Color.white : freeColor;
            secondPlug.color = secondAttached ? Color.white : freeColor;

            wire.enabled = firstAttached || secondAttached;
            if (wire.enabled)
                wire.SetEndpoints(firstPlug.rectTransform.anchoredPosition + firstPlug.TailOffset,
                    secondPlug.rectTransform.anchoredPosition + secondPlug.TailOffset);
        }

        private Vector2 LocalAnchor(SocketView socket) => interactionArea.InverseTransformPoint(socket.PlugAnchor.position);

        public void SetTrayCableView(StoredCableView view)
        {
            trayCableView = view;
            if (view != null) { view.Bind(this); view.SetCableColor(cableColor); }
            RefreshTrayImage();
        }

        private void RefreshTrayImage()
        {
            if (trayCableView != null) trayCableView.SetAvailable(Connection.IsInTray);
        }

        private void PlaySound(AudioClip clip, float pitch, float volume)
        {
            if (!Application.isPlaying || audioSource == null || clip == null) return;
            audioSource.pitch = pitch * Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(clip, volume);
        }

        private void OnDisable()
        {
            if (connection != null)
            {
                if (connection.FirstSocket != null) connection.FirstSocket.Release(this);
                if (connection.SecondSocket != null) connection.SecondSocket.Release(this);
                connection.Reset();
            }
            RefreshTrayImage();
            if (wire != null) wire.enabled = false;
            if (firstPlug != null) firstPlug.gameObject.SetActive(false);
            if (secondPlug != null) secondPlug.gameObject.SetActive(false);
            pointerInside = false;
            hoveredSocket = null;
        }
    }
}
