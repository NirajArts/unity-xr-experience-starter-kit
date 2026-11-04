using ClientXR.Grips;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace XRStarter
{
    /// <summary>Optional authored-wrist fit. Raw tracked hands drive objects; synthetic visuals never feed mechanics.</summary>
    [RequireComponent(typeof(GrabFreeTransformer))]
    public sealed class AuthoredGripTransformer : MonoBehaviour, ITransformer, IGripObjectMotion
    {
        public GripTarget target;
        public HandGrabInteractable handGrab;
        GrabFreeTransformer free;
        IGrabbable owner;
        readonly Pose[] wrists = new Pose[2], grips = new Pose[2];
        readonly int[] identifiers = { -1, -1 };
        readonly float[] angles = new float[2];
        Pose acquisition;
        float blend;
        bool usingFit;

        public void Initialize(IGrabbable grabbable)
        {
            owner = grabbable;
            free = GetComponent<GrabFreeTransformer>(); free.Initialize(grabbable);
        }
        public void BeginTransform()
        {
            free.BeginTransform(); acquisition = new Pose(owner.Transform.position, owner.Transform.rotation);
            blend = 0; identifiers[0] = identifiers[1] = -1; usingFit = false;
        }
        public void UpdateTransform()
        {
            int count = 0;
            bool fit = target && handGrab;
            if (fit)
            {
                foreach (var side in new[] { Handedness.Left, Handedness.Right })
                foreach (var selector in handGrab.SelectingInteractorViews)
                {
                    if (!(selector is IHandGrabInteractor h) || h.Hand == null || h.Hand.Handedness != side) continue;
                    var definition = target.Resolve(side);
                    if (definition == null || definition.policy != GripContactPolicy.FixedVisualWrist || definition.Validate() != null) { fit = false; break; }
                    if (!h.Hand.IsTrackedDataValid || !h.Hand.GetRootPose(out var wrist)) return; // Hold last safe pose on transient tracking loss.
                    if (count == 2) break;
                    if (identifiers[count] != selector.Identifier)
                    {
                        identifiers[count] = selector.Identifier;
                        angles[count] = GripPoseResolver.AcquireAngle(definition, Quaternion.Inverse(target.transform.rotation) * wrist.rotation);
                    }
                    wrists[count] = wrist; grips[count] = target.ResolvePose(definition, angles[count], h.Hand.Scale); count++;
                }
            }
            fit &= count > 0 && count == owner.GrabPoints.Count;
            if (!fit)
            {
                if (usingFit) { free.EndTransform(); free.BeginTransform(); usingFit = false; }
                if (target) target.ObjectFollowsTrackedWrist = false;
                free.UpdateTransform(); return;
            }
            if (count == 2 && Vector3.Distance(wrists[0].position, wrists[1].position) < .02f) return;
            usingFit = true; target.ObjectFollowsTrackedWrist = true;
            var desired = count == 1 ? Fit(wrists[0], grips[0]) : FitPair(wrists[0], grips[0], wrists[1], grips[1]);
            blend = Mathf.Min(1, blend + Mathf.Min(Time.unscaledDeltaTime, .05f) / .12f);
            float t = Mathf.SmoothStep(0, 1, blend);
            owner.Transform.SetPositionAndRotation(Vector3.Lerp(acquisition.position, desired.position, t), Quaternion.Slerp(acquisition.rotation, desired.rotation, t));
        }
        public void EndTransform() { free.EndTransform(); if (target) target.ObjectFollowsTrackedWrist = false; usingFit = false; }

        // Reused and generalized from the owner's production fit algorithm; no procedure-specific dependencies.
        public static Pose Fit(Pose wrist, Pose localGrip)
        {
            Quaternion q = wrist.rotation * Quaternion.Inverse(localGrip.rotation);
            return new Pose(wrist.position - q * localGrip.position, q);
        }
        public static Pose FitPair(Pose left, Pose leftGrip, Pose right, Pose rightGrip)
        {
            Quaternion q = Quaternion.Slerp(Fit(left, leftGrip).rotation, Fit(right, rightGrip).rotation, .5f);
            Vector3 from = q * (rightGrip.position - leftGrip.position), to = right.position - left.position;
            if (from.sqrMagnitude > .0004f && to.sqrMagnitude > .0004f) q = Quaternion.FromToRotation(from, to) * q;
            return new Pose((left.position + right.position) * .5f - q * ((leftGrip.position + rightGrip.position) * .5f), q);
        }
    }
}
