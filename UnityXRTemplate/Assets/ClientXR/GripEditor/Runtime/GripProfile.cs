using System.Collections.Generic;
using UnityEngine;
namespace ClientXR.Grips
{
    [CreateAssetMenu(menuName="XR/Grip Profile")]
    public sealed class GripProfile : ScriptableObject
    {
        public List<GripDefinition> grips = new List<GripDefinition>();
    }
}
