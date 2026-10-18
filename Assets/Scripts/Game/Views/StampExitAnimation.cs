using System;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>Shared scale-and-alpha exit for a live stamp or its departing visual copy.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class StampExitAnimation : MonoBehaviour
    {
        private CanvasGroup group;
        private Vector3 startScale;
        private float startAlpha, elapsed, duration;
        private Action completed;
        public bool IsPlaying { get; private set; }

        public void Play(float seconds, Action onCompleted)
        {
            Cancel();
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            startScale = transform.localScale;
            startAlpha = group.alpha;
            duration = Mathf.Max(.01f, seconds);
            elapsed = 0;
            completed = onCompleted;
            IsPlaying = true;
        }

        private void Update()
        {
            if (!IsPlaying) return;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0, 1, t);
            transform.localScale = startScale * Mathf.Lerp(1f, .1f, eased);
            group.alpha = startAlpha * (1f - eased);
            if (t < 1) return;
            Action callback = completed;
            Cancel();
            if (callback != null) callback();
            else gameObject.SetActive(false);
        }

        public void Cancel()
        {
            IsPlaying = false;
            completed = null;
        }

        private void OnDisable() => Cancel();
    }
}
