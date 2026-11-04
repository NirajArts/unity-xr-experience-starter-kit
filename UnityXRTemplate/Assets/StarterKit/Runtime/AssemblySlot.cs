using UnityEngine;

namespace XRStarter
{
    public sealed class AssemblySlot : MonoBehaviour
    {
        public string compatibleId;
        public Transform matingPose;
        public Renderer guide;
        [Min(.01f)] public float snapDistance = .12f;
        [Range(0,180)] public float snapAngle = 75;
        public ExperiencePart Occupant { get; private set; }
        public bool CanAccept(ExperiencePart part) => part && part.partId == compatibleId && (!Occupant || Occupant == part);
        public bool WithinTolerance(ExperiencePart part) => CanAccept(part) && matingPose &&
            Vector3.Distance(part.transform.position, matingPose.position) <= snapDistance &&
            Quaternion.Angle(part.transform.rotation, matingPose.rotation) <= snapAngle;
        public bool Reserve(ExperiencePart part)
        {
            if (!CanAccept(part)) return false;
            Occupant = part;
            if (guide) guide.enabled = false;
            return true;
        }
        public void Release(ExperiencePart part) { if (Occupant == part) Occupant = null; }
        public void ShowGuide(bool visible) { if (guide) guide.enabled = visible && !Occupant; }
    }
}
