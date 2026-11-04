using System;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace ClientXR.Grips
{
    public enum GripContactPolicy { Existing, FingersOnly, FixedVisualWrist }
    [Serializable]
    public sealed class GripDefinition
    {
        public string id = "primary";
        public Handedness side;
        public string mapping = GripMath.Mapping;
        public GripContactPolicy policy;
        public HandGrabPose basePose;
        public Vector3 positionMetres;
        public Quaternion rotation = Quaternion.identity;
        public float[] curl = new float[5];
        public float[] spread = new float[5];
        public float thumbOpposition;
        // Additive opt-in schema: absent legacy fields leave wrist behavior unchanged.
        public int formatVersion = 1;
        public bool axisEnabled;
        public Vector3 axisPivot;
        public Vector3 axisDirection = Vector3.up;
        public bool axisLimited;
        public float axisMinimum = -180, axisMaximum = 180;
        public bool contactEnabled;
        public Vector3 contactWristLocal;
        public bool[] overrideFreedom = new bool[5];
        public JointFreedom[] freedom = new JointFreedom[5];
        public Vector3[] jointDegrees = new Vector3[FingersMetadata.HAND_JOINT_IDS.Length];
        public GripDefinition Clone() => JsonUtility.FromJson<GripDefinition>(JsonUtility.ToJson(this));
        public string Validate()
        {
            if(!Enum.IsDefined(typeof(Handedness),side) || !Enum.IsDefined(typeof(GripContactPolicy),policy))return "Invalid hand or contact policy.";
            if(formatVersion < 0 || formatVersion > 1)return "Unsupported grip format version.";
            if(axisEnabled && (!GripMath.Finite(axisPivot) || !GripMath.Finite(axisDirection) || Mathf.Abs(axisDirection.magnitude-1)>.001f))return "Axis needs a finite pivot and normalized nonzero direction.";
            if(axisEnabled && axisLimited && (!GripMath.Finite(axisMinimum) || !GripMath.Finite(axisMaximum) || axisMinimum < -180 || axisMaximum > 180 || axisMinimum > axisMaximum))return "Axis limits must be ordered within -180 to +180.";
            if(contactEnabled && !GripMath.Finite(contactWristLocal))return "Contact must be finite wrist-local metres.";
            if(string.IsNullOrWhiteSpace(id))return "A stable grip id is required.";
            if (mapping != GripMath.Mapping) return "Incompatible skeleton mapping. Rebind using the installed SDK mapping.";
            if (!basePose || basePose.HandPose == null) return "Choose an installed Meta HandGrabPose base reference with finger data.";
            if (basePose.HandPose.JointRotations.Length != FingersMetadata.HAND_JOINT_IDS.Length) return "Base pose joint count does not match the installed skeleton.";
            if (curl == null || curl.Length != 5 || spread == null || spread.Length != 5 || freedom == null || freedom.Length != 5 || overrideFreedom == null || overrideFreedom.Length != 5 || jointDegrees == null || jointDegrees.Length != FingersMetadata.HAND_JOINT_IDS.Length) return "Invalid finger arrays; restore a compatible draft.";
            if (!GripMath.Finite(positionMetres) || !GripMath.Finite(rotation) || Quaternion.Dot(rotation,rotation) < .99f || Quaternion.Dot(rotation,rotation) > 1.01f) return "Invalid placement or rotation.";
            if(float.IsNaN(thumbOpposition) || float.IsInfinity(thumbOpposition))return "Invalid thumb adjustment.";
            foreach(var v in jointDegrees) if(!GripMath.Finite(v))return "Invalid joint adjustment.";
            for(int i=0;i<5;i++)if(overrideFreedom[i] && !Enum.IsDefined(typeof(JointFreedom),freedom[i]))return "Invalid finger freedom.";
            for(int i=0;i<5;i++)if(float.IsNaN(curl[i]) || float.IsInfinity(curl[i]) || float.IsNaN(spread[i]) || float.IsInfinity(spread[i]))return "Invalid finger value.";
            return null;
        }
        public HandPose Compose()
        {
            string error = Validate(); if(error != null) throw new InvalidOperationException(error);
            var result = new HandPose(basePose.HandPose); result.Handedness = side;
            bool mirrored = basePose.HandPose.Handedness != side;
            for(int i=0;i<result.JointRotations.Length;i++)
            {
                var joint=FingersMetadata.HAND_JOINT_IDS[i];
                int finger=(int)HandJointUtils.JointToFingerList[(int)joint];
                Quaternion q=result.JointRotations[i];
                if(mirrored)q=HandMirroring.Mirror(q);
                Vector3 adjust=jointDegrees[i];
                if(finger>=0 && finger<5 && FingersMetadata.HAND_JOINT_CAN_MOVE[i])
                {
                    adjust.z += curl[finger];
                    if(FingersMetadata.HAND_JOINT_CAN_SPREAD[i])adjust.y += spread[finger];
                    if(finger==0 && i==0)adjust.x += thumbOpposition;
                }
                // Controls are expressed in left-hand skeleton space, then mapped by SDK.
                Quaternion delta=Quaternion.Euler(adjust);
                if(side==Handedness.Right)delta=HandMirroring.Mirror(delta);
                result.JointRotations[i]=q*delta;
            }
            for(int f=0;f<5;f++)if(overrideFreedom[f])result.FingersFreedom[f]=freedom[f];
            return result;
        }
        public GripDefinition Mirrored(int plane = 0)
        {
            var copy=Clone(); copy.side=side==Handedness.Left?Handedness.Right:Handedness.Left;
            var normal=plane==1?Vector3.up:plane==2?Vector3.forward:Vector3.right;
            copy.positionMetres=Vector3.Reflect(positionMetres,normal);
            var from=side==Handedness.Left?HandMirroring.LeftHandSpace:HandMirroring.RightHandSpace;
            var to=copy.side==Handedness.Left?HandMirroring.LeftHandSpace:HandMirroring.RightHandSpace;
            copy.rotation=Quaternion.LookRotation(Vector3.Reflect(rotation*from.distal,normal),Vector3.Reflect(rotation*from.dorsal,normal))*Quaternion.Inverse(to.rotation);
            copy.axisPivot=Vector3.Reflect(axisPivot,normal);
            copy.axisDirection=Vector3.Reflect(axisDirection,normal);
            copy.axisMinimum=-axisMaximum;copy.axisMaximum=-axisMinimum;
            copy.contactWristLocal=Quaternion.Inverse(copy.rotation)*Vector3.Reflect(rotation*contactWristLocal,normal);
            return copy;
        }
    }
    public static class GripMath
    {
#if ISDK_OPENXR_HAND
        public const string Mapping="Meta77.OpenXR";
#else
        public const string Mapping="Meta77.OVR";
#endif
        public static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public static bool Finite(Vector3 v)=> !(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
        public static bool Finite(Quaternion q)=>Finite(new Vector3(q.x,q.y,q.z))&&!float.IsNaN(q.w)&&!float.IsInfinity(q.w);
        // Rigid, target-oriented metres. Import scale never scales hand size or offsets twice.
        public static float PositionScale(Transform target)=>target && target.TryGetComponent<GripTarget>(out var owner)?owner.GripPositionScale:1;
        public static Pose ToWorld(Transform target,GripDefinition grip)=>new Pose(target.position+target.rotation*(grip.positionMetres*PositionScale(target)),target.rotation*grip.rotation);
        public static Pose ToLocal(Transform target,Pose world)=>new Pose(Quaternion.Inverse(target.rotation)*(world.position-target.position)/PositionScale(target),Quaternion.Inverse(target.rotation)*world.rotation);
        public static string ValidateFrame(Transform t)
        {
            for(var p=t;p;p=p.parent)if(p.localScale.x<=0||p.localScale.y<=0||p.localScale.z<=0)return "Mirrored/zero scale is unsupported. Use a positive-scale editable wrapper.";
            var s=t.lossyScale;
            if(Mathf.Abs(s.x-s.y)>.0001f*Mathf.Max(1,s.x)||Mathf.Abs(s.y-s.z)>.0001f*Mathf.Max(1,s.y))return "Non-uniform inherited scale is unsupported. Choose a uniform-scale wrapper without changing the model.";
            return null;
        }
    }
}
