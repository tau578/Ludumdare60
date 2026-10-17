using Switchboard.Cables;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField] private RectTransform socketContainer;
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic firstPlug;
        [SerializeField] private PlugGraphic secondPlug;
        [SerializeField] private Image trayCableImage;

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip insertSound;
        [SerializeField] private AudioClip removeSound;
        [SerializeField] private AudioClip blockedSound;

        private CableConnection<SocketView> connection;
        private SocketView[] sockets;
        private SocketView hoveredSocket;
        private Vector2 pointer;
        private bool pointerInside;
        private bool cursorWasVisible;
        private bool ownsCursor;

        public CablePhase Phase => Connection.Phase;
        public SocketView FirstSocket => Connection.FirstSocket;
        public SocketView SecondSocket => Connection.SecondSocket;
        public bool IsInTray => Connection.IsInTray;
        private CableConnection<SocketView> Connection => connection ??
            (connection = new CableConnection<SocketView>(preventSameSocketConnection));

        public void Configure(RectTransform board, RectTransform area, CableGraphic cable,
            PlugGraphic plugA, PlugGraphic plugB, AudioSource sound,
            AudioClip insert, AudioClip remove, AudioClip blocked)
        {
            socketContainer = board;
            interactionArea = area;
            wire = cable;
            firstPlug = plugA;
            secondPlug = plugB;
            audioSource = sound;
            insertSound = insert;
            removeSound = remove;
            blockedSound = blocked;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            connection = new CableConnection<SocketView>(preventSameSocketConnection);
            RefreshTrayImage();
            if (socketContainer == null) return;
            sockets = socketContainer.GetComponentsInChildren<SocketView>(true);
            foreach (SocketView socket in sockets)
            {
                socket.Clicked += HandleSocketClick;
                socket.HoverChanged += HandleHover;
            }
            cursorWasVisible = Cursor.visible;
            ownsCursor = true;
        }

        private void Update()
        {
            if (interactionArea == null) return;
            if (Mouse.current != null && Application.isFocused)
                UpdatePointer(Mouse.current.position.ReadValue());
            else { pointerInside = false; RefreshVisuals(); }
            if (ownsCursor) Cursor.visible = !pointerInside || Phase == CablePhase.Connected;
        }

        public void UpdatePointer(Vector2 screenPosition)
        {
            if (interactionArea == null) return;
            Canvas canvas = interactionArea.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            pointerInside = RectTransformUtility.RectangleContainsScreenPoint(interactionArea, screenPosition, camera);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, screenPosition, camera, out pointer);
            RefreshVisuals();
        }

        public void HandleSocketClick(SocketView socket)
        {
            if (Connection.Contains(socket)) TryRemove(socket);
            else TryInsert(socket);
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
            PlaySound(blockedSound, 1f, 0.3f);
            return false;
        }

        private void HandleHover(SocketView socket, bool entered)
        {
            if (entered) hoveredSocket = socket;
            else if (hoveredSocket == socket) hoveredSocket = null;
        }

        private void RefreshVisuals()
        {
            RefreshTrayImage();
            if (wire == null || firstPlug == null || secondPlug == null) return;
            bool firstAttached = FirstSocket != null;
            bool secondAttached = SecondSocket != null;
            bool showFirst = firstAttached || (pointerInside && (Phase == CablePhase.Ready || secondAttached));
            bool showSecond = secondAttached || (firstAttached && !secondAttached && pointerInside);
            firstPlug.gameObject.SetActive(showFirst);
            secondPlug.gameObject.SetActive(showSecond);
            firstPlug.rectTransform.anchoredPosition = firstAttached ? LocalAnchor(FirstSocket) : pointer;
            secondPlug.rectTransform.anchoredPosition = secondAttached ? LocalAnchor(SecondSocket) : pointer;

            bool blocked = hoveredSocket != null && !Connection.Contains(hoveredSocket) && hoveredSocket.IsOccupied;
            Color freeColor = blocked ? new Color(1f, 0.48f, 0.38f, 0.85f) : Color.white;
            firstPlug.color = firstAttached ? Color.white : freeColor;
            secondPlug.color = secondAttached ? Color.white : freeColor;

            wire.enabled = Phase != CablePhase.Ready;
            if (wire.enabled)
                wire.SetEndpoints(firstPlug.rectTransform.anchoredPosition + firstPlug.TailOffset,
                    secondPlug.rectTransform.anchoredPosition + secondPlug.TailOffset);
        }

        private Vector2 LocalAnchor(SocketView socket) => interactionArea.InverseTransformPoint(socket.PlugAnchor.position);

        public void SetTrayCableImage(Image image)
        {
            trayCableImage = image;
            RefreshTrayImage();
        }

        private void RefreshTrayImage()
        {
            if (trayCableImage != null) trayCableImage.enabled = Connection.IsInTray;
        }

        private void PlaySound(AudioClip clip, float pitch, float volume)
        {
            if (!Application.isPlaying || audioSource == null || clip == null) return;
            audioSource.pitch = pitch * Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(clip, volume);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && ownsCursor) Cursor.visible = cursorWasVisible;
        }

        private void OnDisable()
        {
            if (sockets != null)
                foreach (SocketView socket in sockets)
                {
                    if (socket == null) continue;
                    socket.Clicked -= HandleSocketClick;
                    socket.HoverChanged -= HandleHover;
                }
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
            if (ownsCursor) Cursor.visible = cursorWasVisible;
            ownsCursor = false;
            hoveredSocket = null;
            sockets = null;
        }
    }
}
