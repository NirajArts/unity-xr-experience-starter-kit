using System;
using UnityEngine;
using UnityEngine.UI;

namespace XRStarter
{
    [Serializable] public struct CaptionCue { public float start, end; [TextArea] public string text; }
    public sealed class NarrationPlayer : MonoBehaviour
    {
        public AudioSource voice;
        public Text caption;
        public bool guided = true;
        public CaptionCue[] cues;
        void Update()
        {
            if (!caption) return;
            string text = "";
            if (guided && voice && voice.isPlaying && cues != null)
                foreach (var cue in cues) if (voice.time >= cue.start && voice.time < cue.end) { text = cue.text; break; }
            caption.text = text;
        }
        public void Play(AudioClip clip, CaptionCue[] subtitles)
        {
            Cancel(); if (!guided || !clip || !voice) return;
            cues = subtitles; voice.clip = clip; voice.Play();
        }
        public void SetGuided(bool value) { guided = value; if (!value) Cancel(); }
        public void Cancel() { if (voice) voice.Stop(); if (caption) caption.text = ""; }
        void OnDisable() => Cancel();
        void OnApplicationPause(bool pause) { if (pause) Cancel(); }
    }
}
