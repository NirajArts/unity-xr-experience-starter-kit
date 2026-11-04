using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace XRStarter
{
    public enum PartState { Free, Held, Seated, Animating }

    /// <summary>A single mechanical owner; assembly motion never competes with a selected SDK transformer.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable))]
    public sealed class ExperiencePart : MonoBehaviour
    {
        public string partId;
        public string displayName;
        [TextArea] public string inspectionText;
        public Transform workspace, tableHome;
        public Rigidbody body;
        public Grabbable grabbable;
        public GrabInteractable controllerGrab;
        public HandGrabInteractable handGrab;
        public InteractionFeedback feedback;
        public Renderer[] surfaces;
        public AssemblySlot[] slots;
        public float recoveryFloor = .18f;
        public float recoveryRadius = 3.5f;
        public PartState State { get; private set; }
        public AssemblySlot Slot { get; private set; }
        public bool Held => grabbable && grabbable.SelectingPointsCount > 0;
        public event Action<ExperiencePart> Selected = delegate { };
        public event Action<ExperiencePart> Changed = delegate { };
        bool pendingRelease, suppressEvents;
        float droppedTime;
        MaterialPropertyBlock properties;
        RigidbodyInterpolation freeInterpolation;

        void Awake()
        {
            if (!body) body = GetComponent<Rigidbody>();
            if (!grabbable) grabbable = GetComponent<Grabbable>();
            properties = new MaterialPropertyBlock();
            freeInterpolation = body.interpolation;
        }
        void OnEnable() { if (grabbable) grabbable.WhenPointerEventRaised += OnPointer; }
        void OnDisable() { if (grabbable) grabbable.WhenPointerEventRaised -= OnPointer; }
        void OnPointer(PointerEvent evt)
        {
            if (suppressEvents) return;
            if (evt.Type == PointerEventType.Select)
            {
                Vacate(); State = PartState.Held; pendingRelease = false; droppedTime = 0;
                body.interpolation = RigidbodyInterpolation.None;
                feedback?.Play(FeedbackCue.Pickup, transform.position);
                Selected(this); Changed(this);
            }
            else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel) pendingRelease = true;
        }
        void Update()
        {
            if (State == PartState.Animating) return;
            if (pendingRelease && !Held)
            {
                pendingRelease = false;
                State = PartState.Free;
                bool seated = false;
                if (slots != null) foreach (var slot in slots) if (slot && slot.WithinTolerance(this) && Seat(slot)) { seated = true; break; }
                if (!seated) { SetMechanicalPose(transform.position,transform.rotation); body.isKinematic = false; body.useGravity = true; body.interpolation=freeInterpolation; feedback?.Play(FeedbackCue.Release, transform.position); Changed(this); }
            }
            if (slots != null) foreach (var slot in slots) if (slot) slot.ShowGuide(Held && slot.CanAccept(this) && Vector3.Distance(transform.position, slot.matingPose.position) < .35f);
            if (!Held && State == PartState.Free && workspace)
            {
                Vector3 p = workspace.InverseTransformPoint(transform.position);
                bool dropped = p.y < recoveryFloor || new Vector2(p.x,p.z).magnitude > recoveryRadius;
                droppedTime = dropped ? droppedTime + Time.unscaledDeltaTime : 0;
                if (droppedTime > 1.2f) ReturnHome();
            }
        }
        void Vacate() { if (Slot) Slot.Release(this); Slot = null; }
        public void ReleaseSelection()
        {
            suppressEvents = true;
            if (controllerGrab) controllerGrab.enabled = false;
            if (handGrab) handGrab.enabled = false;
            if (grabbable) grabbable.enabled = false; // SDK cancels all pointer IDs on disable.
            pendingRelease = false;
            suppressEvents = false;
        }
        public void EnableSelection()
        {
            if (grabbable) grabbable.enabled = true;
            if (controllerGrab) controllerGrab.enabled = true;
            if (handGrab) handGrab.enabled = true;
        }
        public void BeginScriptedMotion()
        {
            ReleaseSelection(); Vacate(); State = PartState.Animating;
            body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            if (slots != null) foreach (var slot in slots) if (slot) slot.ShowGuide(false);
        }
        public void FinishFreeMotion(bool floating = false)
        {
            SetMechanicalPose(transform.position,transform.rotation);
            State = PartState.Free; body.isKinematic = floating; body.useGravity = !floating;
            body.interpolation = floating ? RigidbodyInterpolation.None : freeInterpolation;
            droppedTime = 0; EnableSelection(); Changed(this);
        }
        public void SetMechanicalPose(Vector3 position, Quaternion rotation)
        {
            // Teleports/animation must update the physics pose as well as the visible Transform.
            body.position=position; body.rotation=rotation;
            transform.SetPositionAndRotation(position,rotation);
        }
        public bool Seat(AssemblySlot slot)
        {
            if (!slot || !slot.CanAccept(this)) return false;
            BeginScriptedMotion();
            if (!slot.Reserve(this)) { FinishFreeMotion(); return false; }
            Slot = slot;
            SetMechanicalPose(slot.matingPose.position, slot.matingPose.rotation);
            State = PartState.Seated; EnableSelection();
            feedback?.Play(FeedbackCue.Seat, transform.position); Changed(this);
            return true;
        }
        public void ReturnHome()
        {
            if (!tableHome) return;
            BeginScriptedMotion(); SetMechanicalPose(tableHome.position, tableHome.rotation); Physics.SyncTransforms();
            FinishFreeMotion(); feedback?.Play(FeedbackCue.Reset, transform.position);
        }
        public void Highlight(bool selected)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetColor("_EmissionColor", selected ? new Color(.02f,.35f,.45f) : Color.black);
            if (surfaces != null) foreach (var surface in surfaces) if (surface) surface.SetPropertyBlock(properties);
        }
        void OnApplicationPause(bool pause) { if (pause && Held) ReturnHome(); }
    }
}
