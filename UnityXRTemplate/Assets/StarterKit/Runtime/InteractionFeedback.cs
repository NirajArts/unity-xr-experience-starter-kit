using UnityEngine;

namespace XRStarter
{
    public enum FeedbackCue { Pickup, Release, Seat, Reset, Button, Complete }

    /// <summary>Feedback observes interactions; it never owns selection or procedure progress.</summary>
    public sealed class InteractionFeedback : MonoBehaviour
    {
        public AudioClip[] clips;
        [Range(0, 1)] public float volume = .55f;
        public bool muted;
        AudioSource[] voices;
        int nextVoice;
        public int PlayedCount { get; private set; }

        void Awake()
        {
            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Spatial feedback " + i);
                child.transform.SetParent(transform, false);
                var voice = child.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 1;
                voice.minDistance = .4f;
                voice.maxDistance = 5;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.dopplerLevel = 0;
                voices[i] = voice;
            }
        }

        public void Play(FeedbackCue cue, Vector3 position)
        {
            if (!isActiveAndEnabled || muted || voices == null || clips == null || (int)cue >= clips.Length || !clips[(int)cue]) return;
            var voice = voices[nextVoice++ % voices.Length];
            voice.Stop();
            voice.transform.position = position;
            voice.clip = clips[(int)cue];
            voice.volume = volume;
            voice.pitch = 1;
            voice.Play();
            PlayedCount++;
        }

        public void StopAll() { if (voices != null) foreach (var voice in voices) if (voice) voice.Stop(); }
        public void SetMuted(bool value) { muted = value; if (value) StopAll(); }
        void OnDisable() => StopAll();
        void OnApplicationPause(bool paused) { if (paused) StopAll(); }
        void OnApplicationFocus(bool focused) { if (!focused) StopAll(); }
    }
}
