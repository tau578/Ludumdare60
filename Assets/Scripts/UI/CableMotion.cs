using System;
using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>
    /// One short timed animation on a cord (D60): the connection wiggle of its wire now, and later moves such as a new
    /// cord bouncing into the tray. The step gets the progress from 0 to 1 and always ends with exactly 1, so it can put
    /// everything back. The component is enabled only while it plays, so a still cord costs nothing per frame. It runs
    /// on unscaled time: one already started finishes even if the game pauses.
    /// </summary>
    public sealed class CableMotion : MonoBehaviour
    {
        private float age = -1;
        private float seconds;
        private Action<float> step;

        public bool IsPlaying => age >= 0;

        /// <summary>Starts <paramref name="onStep"/> over <paramref name="duration"/> seconds; one still running ends first.</summary>
        public void Play(float duration, Action<float> onStep)
        {
            Finish();
            if (onStep == null || duration <= 0) return;
            seconds = duration;
            step = onStep;
            age = 0;
            enabled = true;
            onStep(0);
        }

        /// <summary>Moves the animation on; Update drives it, tests call it directly.</summary>
        public void Advance(float dt)
        {
            if (age < 0) return;
            age += Mathf.Max(0, dt);
            if (age >= seconds) Finish();
            else step(age / seconds);
        }

        /// <summary>Jumps to the end: the step gets 1 and the component goes idle.</summary>
        public void Finish()
        {
            if (age < 0) return;
            Action<float> last = step;
            age = -1;
            step = null;
            enabled = false;
            last(1);
        }

        // A cord copied from another (a bought cord) copies this component too: it starts idle.
        private void Awake() => enabled = false;

        private void Update() => Advance(Mathf.Min(Time.unscaledDeltaTime, .05f));

        private void OnDisable() => Finish();

        /// <summary>A new, idle motion on <paramref name="host"/>; it is never saved with the scene.</summary>
        public static CableMotion AddTo(GameObject host)
        {
            var motion = host.AddComponent<CableMotion>();
            motion.hideFlags = HideFlags.DontSave;
            motion.enabled = false;
            return motion;
        }
    }
}
