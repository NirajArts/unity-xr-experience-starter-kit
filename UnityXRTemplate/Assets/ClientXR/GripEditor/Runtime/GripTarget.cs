using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
namespace ClientXR.Grips
{
    public interface IGripObjectMotion {}
    // Optional visual-only constraint supplied by the current mechanical owner.
    // Its output is a rigid target-relative metre pose, never a replacement for tracked input.
    public interface IGripWristConstraint
    {
        bool TryResolveGripWrist(GripDefinition definition,float acquiredAngle,float handScale,Pose trackedWrist,out Pose relativePose);
    }
    [DisallowMultipleComponent]
    public sealed class GripTarget : MonoBehaviour
    {
        public HandGrabInteractable interactable;
        // Runtime motion owner opts in; authored definitions remain untouched.
        public bool ObjectFollowsTrackedWrist { get; set; }
        // Opt-in for resizable interaction wrappers; saved definitions stay at their original metre reference.
        public bool scaleGripPositions;
        [HideInInspector] public float gripReferenceScale=1;
        public float GripPositionScale=>scaleGripPositions && gripReferenceScale>0?transform.lossyScale.x/gripReferenceScale:1;
        public Pose ResolvePose(GripDefinition grip,float angle=0,float handScale=1)=>GripPoseResolver.Resolve(grip,angle,handScale,GripPositionScale);
        public string activeGripId="primary";
        public GripProfile sharedProfile;
        public List<GripDefinition> grips=new List<GripDefinition>();
        public IReadOnlyList<GripDefinition> Definitions=>sharedProfile?sharedProfile.grips:grips;
        public GripDefinition Resolve(Handedness side)
        {
            if(!isActiveAndEnabled)return null;
            foreach(var grip in Definitions)if(grip!=null && grip.id==activeGripId && grip.side==side)return grip;
            return null;
        }
        public string Validate()
        {
            if(!interactable || interactable.gameObject!=gameObject)return "Bind a Meta HandGrabInteractable on this editable target.";
            var frame=GripMath.ValidateFrame(transform);if(frame!=null)return frame;
            var ids=new HashSet<string>();
            foreach(var grip in Definitions)
            {
                if(grip==null)return "Null grip definition.";
                if(!ids.Add(grip.id+":"+grip.side))return "Duplicate grip id/hand; preserve unique definitions.";
                if(grip.policy!=GripContactPolicy.Existing && grip.Validate()!=null)return grip.Validate();
            }
            return null;
        }
    }
}
