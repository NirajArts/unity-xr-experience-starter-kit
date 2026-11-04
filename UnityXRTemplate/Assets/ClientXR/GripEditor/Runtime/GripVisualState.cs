using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
namespace ClientXR.Grips
{
    /// <summary>Decorates only the SDK visual source. Never changes raw hand data or object transformers.</summary>
    [DisallowMultipleComponent]
    public sealed class GripVisualState : MonoBehaviour,IHandGrabState
    {
        public HandGrabInteractor source;
        public HandGrabStateVisual visual;
        HandGrabInteractable previous;
        GripTarget owner;
        IGripWristConstraint constraint;
        GripDefinition definition;
        HandPose composed;
        float acquiredAngle;
        public float AcquiredAngle=>acquiredAngle;
        readonly HandGrabTarget target=new HandGrabTarget();
        readonly HandGrabResult result=new HandGrabResult();
        int resolvedFrame=-1;
        public GripTarget ActiveOwner { get {Resolve();return definition!=null?owner:null;} }
        void OnEnable(){if(visual)visual.InjectHandGrabState(this);}
        void OnDisable(){Clear();if(visual && source)visual.InjectHandGrabState(source);}
        void Clear(){previous=null;owner=null;constraint=null;definition=null;composed=null;acquiredAngle=0;resolvedFrame=-1;}
        void Resolve()
        {
            if(resolvedFrame==Time.frameCount)return;resolvedFrame=Time.frameCount;
            var selected=source && source.IsGrabbing?source.SelectedInteractable:null;
            if(!selected || source.Hand==null || !source.Hand.IsTrackedDataValid){Clear();return;}
            if(previous!=selected)
            {
                previous=selected;owner=selected.GetComponent<GripTarget>();constraint=selected.GetComponent<IGripWristConstraint>();definition=null;composed=null;
                string validation=owner?owner.Validate():null;
                if(owner && validation!=null)Debug.LogWarning("Grip override unavailable on "+owner.name+": "+validation,owner);
                if(owner && source.Hand!=null && validation==null)
                {
                    var candidate=owner.Resolve(source.Hand.Handedness);
                    if(candidate!=null && candidate.policy!=GripContactPolicy.Existing)
                    {string error=candidate.Validate();if(error==null){definition=candidate.Clone();composed=definition.Compose();acquiredAngle=0;
                        if(definition.policy==GripContactPolicy.FixedVisualWrist && source.Hand.GetRootPose(out var wrist))acquiredAngle=GripPoseResolver.AcquireAngle(definition,Quaternion.Inverse(owner.transform.rotation)*wrist.rotation);}else Debug.LogWarning("Grip override unavailable on "+owner.name+": "+error,owner);}
                }
            }
            if(!owner || !owner.isActiveAndEnabled || !selected.isActiveAndEnabled){definition=null;composed=null;}
        }
        public bool IsGrabbing=>source && source.IsGrabbing;
        public float FingersStrength {get{Resolve();return definition!=null?1:source?source.FingersStrength:0;}}
        bool ConstrainedPose(out Pose pose)
        {
            pose=Pose.identity;
            if(definition==null || constraint==null || !owner || !owner.isActiveAndEnabled || source.Hand==null || !source.Hand.IsTrackedDataValid || !source.Hand.GetRootPose(out var raw))return false;
            float scale=source.Hand.Scale;if(!GripMath.Finite(scale)||scale<=0)scale=1;
            return constraint.TryResolveGripWrist(definition,acquiredAngle,scale,raw,out pose);
        }
        public float WristStrength {get{Resolve();return definition!=null?(ConstrainedPose(out _) || definition.policy==GripContactPolicy.FixedVisualWrist && !owner.ObjectFollowsTrackedWrist?1:0):source?source.WristStrength:0;}}
        public Pose WristToGrabPoseOffset=>source?source.WristToGrabPoseOffset:Pose.identity;
        public HandFingerFlags GrabbingFingers()=>source?source.GrabbingFingers():0;
        public HandGrabTarget HandGrabTarget
        {
            get
            {
                Resolve();
                if(definition==null)return source?source.HandGrabTarget:null;
                result.HasHandPose=true;result.HandPose.CopyFrom(composed);
                float scale=source.Hand!=null?source.Hand.Scale:1;
                if(!GripMath.Finite(scale) || scale<=0)scale=1;
                result.RelativePose=ConstrainedPose(out var constrained)?constrained:definition.policy==GripContactPolicy.FixedVisualWrist?owner.ResolvePose(definition,acquiredAngle,scale):new Pose(definition.positionMetres*owner.GripPositionScale,definition.rotation);
                // SDK GlobalPose uses the same rigid metre conversion as GripMath.ToWorld.
                target.Set(owner.transform,HandAlignType.AlignOnGrab,source.HandGrabTarget.Anchor,result);
                return target;
            }
        }
    }
}
