using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XRStarter
{
    public sealed class AssemblyExperience : MonoBehaviour
    {
        public ExperiencePart[] parts;
        public AssemblySlot[] slots;
        public Transform[] explodedPoses;
        public Transform inspectionPose;
        public InteractionFeedback feedback;
        public NarrationPlayer narration;
        public Text statusText, inspectionTitle, inspectionBody;
        public Image progressFill;
        [Min(.1f)] public float motionSeconds = 1.5f;
        public bool Busy { get; private set; }
        public ExperiencePart SelectedPart { get; private set; }
        public int SeatedCount { get { int n = 0; foreach (var p in parts) if (p.State == PartState.Seated) n++; return n; } }
        Coroutine motion;
        bool completed;

        void OnEnable()
        {
            foreach (var part in parts) { part.Selected += Select; part.Changed += UpdateProgress; }
        }
        void Start() { Select(parts[0]); Refresh("Pick up a part. Release near its matching outline to assemble."); }
        void OnDisable()
        {
            foreach (var part in parts) if (part) { part.Selected -= Select; part.Changed -= UpdateProgress; }
            CancelMotion();
        }
        public void Select(ExperiencePart part)
        {
            if (SelectedPart) SelectedPart.Highlight(false);
            SelectedPart = part;
            if (!part) return;
            part.Highlight(true);
            if (inspectionTitle) inspectionTitle.text = part.displayName.ToUpperInvariant();
            if (inspectionBody) inspectionBody.text = part.inspectionText;
        }
        void UpdateProgress(ExperiencePart _) { if (!Busy) Refresh(null); }
        public void Refresh(string message)
        {
            int count = SeatedCount;
            if (progressFill) progressFill.fillAmount = (float)count / parts.Length;
            if (message != null && statusText) statusText.text = message;
            if (count == parts.Length && !completed)
            {
                completed = true;
                if (statusText) statusText.text = "ASSEMBLY COMPLETE  /  All parts are seated.";
                feedback?.Play(FeedbackCue.Complete, transform.position + Vector3.up);
            }
            if (count != parts.Length) completed = false;
        }
        public void Explode() { if (!Busy) motion = StartCoroutine(MoveParts(false)); }
        public void Reassemble() { if (!Busy) motion = StartCoroutine(MoveParts(true)); }
        IEnumerator MoveParts(bool assemble)
        {
            Busy = true; narration?.Cancel(); feedback?.Play(FeedbackCue.Button, transform.position + Vector3.up);
            Refresh(assemble ? "Reassembling..." : "Exploding the assembly...");
            var starts = new Pose[parts.Length]; var targets = new Pose[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].BeginScriptedMotion(); starts[i] = new Pose(parts[i].transform.position, parts[i].transform.rotation);
                var target = assemble ? slots[i].matingPose : explodedPoses[i];
                targets[i] = new Pose(target.position, target.rotation);
            }
            float time = 0;
            while (time < motionSeconds)
            {
                time += Time.deltaTime; float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / motionSeconds));
                for (int i = 0; i < parts.Length; i++) parts[i].SetMechanicalPose(Vector3.Lerp(starts[i].position, targets[i].position, t), Quaternion.Slerp(starts[i].rotation, targets[i].rotation, t));
                yield return null;
            }
            for (int i = 0; i < parts.Length; i++)
            {
                if (assemble) parts[i].Seat(slots[i]);
                else parts[i].FinishFreeMotion(true);
            }
            Busy = false; motion = null;
            Refresh(assemble ? "ASSEMBLY COMPLETE  /  All parts are seated." : "Exploded view. Pick up any part to inspect it.");
        }
        void CancelMotion()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null; Busy = false;
            foreach (var part in parts) if (part && part.State == PartState.Animating) part.FinishFreeMotion();
        }
        public void ResetExperience()
        {
            CancelMotion(); narration?.Cancel(); feedback?.StopAll(); completed = false;
            foreach (var part in parts) part.ReturnHome();
            Select(parts[0]); Refresh("Ready. Pick up a part or explore the assembly controls.");
        }
        public void InspectSelected()
        {
            if (Busy || !SelectedPart || !inspectionPose) return;
            SelectedPart.BeginScriptedMotion();
            SelectedPart.SetMechanicalPose(inspectionPose.position, inspectionPose.rotation);
            SelectedPart.FinishFreeMotion(true);
            Refresh("Inspection position. Grab and rotate the highlighted part.");
        }
        public void ReturnSelected() { if (!Busy && SelectedPart) { SelectedPart.ReturnHome(); Refresh("Part returned to its table home."); } }
        void OnApplicationPause(bool paused) { if (paused) ResetExperience(); }
    }
}
