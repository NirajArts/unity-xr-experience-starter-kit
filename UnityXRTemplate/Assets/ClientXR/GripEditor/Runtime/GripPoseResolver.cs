using UnityEngine;
namespace ClientXR.Grips
{
    /// <summary>Rigid target-relative metres, orbit first, then explicit wrist-local contact compensation.</summary>
    public static class GripPoseResolver
    {
        public static Pose Resolve(GripDefinition grip,float angle=0,float handScale=1,float positionScale=1)
        {
            var p=grip.positionMetres;var q=grip.rotation;
            if(grip.axisEnabled){var r=Quaternion.AngleAxis(Clamp(grip,angle),grip.axisDirection);p=grip.axisPivot+r*(p-grip.axisPivot);q=r*q;}
            p*=positionScale; // Scale object-relative placement, never the hand skeleton or hand-size compensation.
            if(grip.contactEnabled)p+=q*grip.contactWristLocal*(1-handScale);
            return new Pose(p,q);
        }
        public static float Clamp(GripDefinition grip,float angle)
        {
            angle=Mathf.DeltaAngle(0,angle);
            if(!grip.axisLimited || (angle>=grip.axisMinimum && angle<=grip.axisMaximum))return angle;
            return Mathf.Abs(Mathf.DeltaAngle(angle,grip.axisMinimum))<=Mathf.Abs(Mathf.DeltaAngle(angle,grip.axisMaximum))?grip.axisMinimum:grip.axisMaximum;
        }
        public static float AcquireAngle(GripDefinition grip,Quaternion targetRotation)
        {
            if(!grip.axisEnabled)return 0;
            var d=targetRotation*Quaternion.Inverse(grip.rotation);
            float projection=Vector3.Dot(new Vector3(d.x,d.y,d.z),grip.axisDirection);
            if(projection*projection+d.w*d.w<1e-10f)return Clamp(grip,0);
            return Clamp(grip,2*Mathf.Atan2(projection,d.w)*Mathf.Rad2Deg);
        }
        public static Pose World(Transform target,Pose local)=>new Pose(target.position+target.rotation*local.position,target.rotation*local.rotation);
    }
}
