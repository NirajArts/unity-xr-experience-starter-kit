using System.Collections.Generic;
using Oculus.Interaction.HandGrab.Editor;
using Oculus.Interaction.HandGrab.Visuals;
using UnityEngine;
namespace ClientXR.Grips.Editor
{
    public sealed class GripPreview
    {
        HandGhost ghost;
        GameObject geometry;
        Material material;
        Transform geometryTarget;
        readonly List<MeshFilter> sources=new List<MeshFilter>();
        readonly List<MeshFilter> proxies=new List<MeshFilter>();
        Oculus.Interaction.Input.Handedness ghostSide;
        public string Error {get;private set;}
        public HandGhost Ghost=>ghost;
        public GameObject Geometry=>geometry;
        public static Bounds GeometryBounds(Transform target)
        {
            var result=new Bounds(target.position,Vector3.zero);bool found=false;
            foreach(var f in target.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f.sharedMesh)continue;var b=f.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);
                }
            }
            return result;
        }
        public void Show(Transform target,GripDefinition grip,bool includeGeometry=true,float angle=0,float handSize=1)
        {
            if(!target || grip==null || grip.Validate()!=null || GripMath.ValidateFrame(target)!=null){Clear();return;}
            Error=null;
            if(ghost && ghostSide!=grip.side)Clear();
            if(geometryTarget!=target){ClearGeometry();geometryTarget=target;}
            if(!target.gameObject.activeInHierarchy && includeGeometry)
            {
                var filters=target.GetComponentsInChildren<MeshFilter>(true);
                bool rebuild=!geometry || filters.Length!=sources.Count;
                if(!rebuild)for(int i=0;i<filters.Length;i++)if(filters[i]!=sources[i]){rebuild=true;break;}
                if(rebuild)
                {
                    ClearGeometry();geometryTarget=target;
                    geometry=new GameObject("Grip Editor Preview inactive target geometry"){hideFlags=HideFlags.HideAndDontSave};
                    foreach(var source in filters)
                    {
                        var proxy=new GameObject(source.name){hideFlags=HideFlags.HideAndDontSave};proxy.transform.SetParent(geometry.transform);
                        sources.Add(source);proxies.Add(proxy.AddComponent<MeshFilter>());proxy.AddComponent<MeshRenderer>();
                    }
                }
                for(int i=0;i<sources.Count;i++)
                {
                    var source=sources[i];var proxy=proxies[i];proxy.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);proxy.transform.localScale=source.transform.lossyScale;
                    if(proxy.sharedMesh!=source.sharedMesh)proxy.sharedMesh=source.sharedMesh;
                    var r=source.GetComponent<MeshRenderer>();var pr=proxy.GetComponent<MeshRenderer>();pr.enabled=r && r.enabled;
                    if(r)pr.sharedMaterials=r.sharedMaterials;
                }
            }
            else ClearGeometry();
            if(!ghost)
            {
                if(!HandGhostProviderUtils.TryGetDefaultProvider(out var provider) || !provider.GetHand(grip.side)){Error="Installed SDK ghost resource missing. Restore the configured Meta hand ghost provider.";Clear();return;}
                ghostSide=grip.side;
                ghost=Object.Instantiate(provider.GetHand(grip.side));ghost.name="Grip Editor Preview "+grip.side;
                foreach(var t in ghost.GetComponentsInChildren<Transform>(true))t.gameObject.hideFlags=HideFlags.HideAndDontSave;
                foreach(var c in ghost.GetComponentsInChildren<Collider>(true))c.enabled=false;
                foreach(var body in ghost.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                foreach(var b in ghost.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
                material=new Material(Shader.Find("Standard")){hideFlags=HideFlags.HideAndDontSave,color=grip.side==Oculus.Interaction.Input.Handedness.Left?new Color(.3f,.68f,.9f):new Color(.92f,.65f,.36f)};
                foreach(var r in ghost.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
            }
            ghost.transform.localScale=Vector3.one*handSize;
            ghost.SetPose(grip.Compose(),GripPoseResolver.World(target,GripPoseResolver.Resolve(grip,angle,handSize,GripMath.PositionScale(target))));
            foreach(var r in ghost.GetComponentsInChildren<Renderer>(true))r.enabled=true;
        }
        void ClearGeometry(){if(geometry)Object.DestroyImmediate(geometry);geometry=null;geometryTarget=null;sources.Clear();proxies.Clear();}
        public void Clear(){if(ghost)Object.DestroyImmediate(ghost.gameObject);ghost=null;ClearGeometry();if(material)Object.DestroyImmediate(material);material=null;}
    }
}
