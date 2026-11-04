using System;
using System.IO;
using System.Linq;
using ClientXR.Grips;
using ClientXR.Grips.Editor;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Grab;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object = UnityEngine.Object;

namespace XRStarter.Editor
{
    /// <summary>Explicit authoring command for the starter demo. Never runs on import.</summary>
    public static class DemoBuilder
    {
        public const string ScenePath = "Assets/StarterKit/Scenes/ExperienceDemo.unity";
        const string Root = "Assets/StarterKit";
        static Material graphite, metal, teal, gold, pale, floor;
        static Font font;
        static Transform workspace;

        [MenuItem("XR Starter/Create Demo In Empty Project")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit your Play session before authoring.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Demo already exists. Open it; this command never overwrites authored scenes.");
            if (Enumerable.Range(0, EditorSceneManager.sceneCount).Any(i => EditorSceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Unsaved scenes found. Save or discard them yourself before creating the demo.");
            foreach (string folder in new[] { "Scenes", "Materials", "Meshes", "Prefabs", "Settings", "Input" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            ConfigureProject();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.42f,.49f,.6f);
            RenderSettings.ambientEquatorColor = new Color(.21f,.27f,.34f);
            RenderSettings.ambientGroundColor = new Color(.08f,.1f,.14f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.035f,.05f,.075f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .035f;
            graphite = Mat("Graphite", new Color(.065f,.09f,.12f), .15f, .55f);
            metal = Mat("Brushed alloy", new Color(.62f,.69f,.74f), .65f, .38f);
            teal = Mat("Signal teal", new Color(.015f,.62f,.66f), .3f, .55f);
            gold = Mat("Signal amber", new Color(.98f,.53f,.12f), .35f, .5f);
            pale = Mat("Ceramic", new Color(.8f,.88f,.91f), .15f, .45f);
            floor = Mat("Studio floor", new Color(.045f,.06f,.085f), .05f, .25f);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            workspace = new GameObject("Workspace - move as one rigid frame").transform;
            var fixture = new GameObject("Static fixture").transform; fixture.SetParent(workspace,false);
            Primitive("Floor", PrimitiveType.Cube, fixture, new Vector3(0,-.06f,1), new Vector3(12,.1f,12), floor);
            Primitive("Workbench", PrimitiveType.Cube, fixture, new Vector3(0,.79f,.96f), new Vector3(2.05f,.1f,.92f), graphite);
            Primitive("Front accent", PrimitiveType.Cube, fixture, new Vector3(0,.805f,.495f), new Vector3(1.96f,.012f,.012f), teal, false);
            for (int i=0;i<4;i++) Primitive("Bench leg", PrimitiveType.Cube, fixture, new Vector3(i%2==0?-.85f:.85f,.38f,i<2?.61f:1.3f), new Vector3(.06f,.76f,.06f), graphite);
            Primitive("Assembly pedestal", PrimitiveType.Cylinder, fixture, new Vector3(0,.865f,1.02f), new Vector3(.4f,.025f,.4f), metal);
            Primitive("Pedestal top", PrimitiveType.Cylinder, fixture, new Vector3(0,.894f,1.02f), new Vector3(.34f,.005f,.34f), graphite);
            for(int i=0;i<2;i++) Primitive("Part tray",PrimitiveType.Cube,fixture,new Vector3(i==0?-.62f:.62f,.845f,.87f),new Vector3(.48f,.01f,.53f),graphite);
            WorldText("Station number", "01  /  EXPLORE", workspace, new Vector3(-.86f,.852f,.59f), Quaternion.Euler(90,0,0), 26, new Vector2(400,60), .001f, new Color(.45f,.62f,.72f));

            var services = new GameObject("Experience services");
            var feedback = services.AddComponent<InteractionFeedback>();
            feedback.clips = new[] { "pickup", "release", "seat", "reset", "button", "complete" }.Select(n=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+n+".wav")).ToArray();
            var experience = services.AddComponent<AssemblyExperience>(); experience.feedback = feedback;
            experience.narration = services.AddComponent<NarrationPlayer>();
            experience.narration.voice = services.AddComponent<AudioSource>(); experience.narration.voice.playOnAwake=false; experience.narration.voice.spatialBlend=0;
            var references = new GameObject("Pose references - mating / table / exploded").transform; references.SetParent(workspace,false);
            experience.parts = new ExperiencePart[3]; experience.slots = new AssemblySlot[3]; experience.explodedPoses = new Transform[3];
            string[] ids={"rotor","collar","cap"}, names={"Rotor core","Locating collar","Top cap"};
            string[] descriptions={"A solid core with a keyed top. Grab with either hand, inspect all sides, then release near its matching pedestal outline.","The collar sits over the rotor. Separate mating references keep the finished assembly independent from table staging.","The cap completes this generic module. Reset safely cancels grabs and animations before returning every part home."};
            Vector3[] homes={new Vector3(-.61f,.905f,.86f),new Vector3(.61f,.86f,.72f),new Vector3(.61f,.865f,1.05f)};
            Vector3[] mating={new Vector3(0,.96f,1.02f),new Vector3(0,1.04f,1.02f),new Vector3(0,1.08f,1.02f)};
            Material[] colors={teal,metal,gold};
            for (int i=0;i<3;i++)
            {
                var home = Empty("Table home - "+ids[i],references,homes[i]);
                var mate = Empty("Mating pose - "+ids[i],references,mating[i]);
                experience.explodedPoses[i] = Empty("Exploded pose - "+ids[i],references,mating[i]+Vector3.up*(.08f+i*.04f));
                var slot = mate.gameObject.AddComponent<AssemblySlot>(); slot.compatibleId=ids[i]; slot.matingPose=mate;
                slot.snapDistance=.12f; slot.snapAngle=85;
                var guide = Ring("Placement outline",mate,.14f,.004f,teal); guide.transform.localPosition=Vector3.zero;
                slot.guide=guide.GetComponent<Renderer>(); slot.ShowGuide(false); experience.slots[i]=slot;
                var go = new GameObject(names[i]); go.transform.SetParent(workspace,false); go.transform.localPosition=homes[i];
                if (i==0)
                {
                    Primitive("Core body",PrimitiveType.Cylinder,go.transform,Vector3.zero,new Vector3(.23f,.06f,.23f),colors[i],false);
                    Primitive("Key",PrimitiveType.Cube,go.transform,new Vector3(0,.062f,0),new Vector3(.07f,.02f,.07f),metal,false);
                    var c=go.AddComponent<CapsuleCollider>(); c.radius=.115f; c.height=.14f;
                }
                else if(i==1)
                {
                    Ring("Collar",go.transform,.108f,.019f,colors[i]);
                    // Four small primitive colliders preserve the opening without a non-convex dynamic collider.
                    foreach(var p in new[]{new Vector3(-.105f,0,0),new Vector3(.105f,0,0),new Vector3(0,0,-.105f),new Vector3(0,0,.105f)})
                    {var c=go.AddComponent<BoxCollider>();c.center=p;c.size=Mathf.Abs(p.x)>.01f?new Vector3(.04f,.038f,.17f):new Vector3(.17f,.038f,.04f);}
                }
                else
                {
                    Primitive("Cap body",PrimitiveType.Cylinder,go.transform,Vector3.zero,new Vector3(.245f,.02f,.245f),colors[i],false);
                    Primitive("Cap inset",PrimitiveType.Cylinder,go.transform,new Vector3(0,.021f,0),new Vector3(.16f,.002f,.16f),graphite,false);
                    var c=go.AddComponent<CapsuleCollider>();c.radius=.1225f;c.height=.04f;c.direction=1;
                    // A convex mesh matches a thin disc better than CapsuleCollider's minimum diameter.
                    Object.DestroyImmediate(c); var mesh=go.AddComponent<MeshCollider>();mesh.sharedMesh=DiscMesh(.1225f,.02f);mesh.convex=true;
                }
                go.layer=8;
                var rb=go.AddComponent<Rigidbody>();rb.mass=.25f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var grabbable=go.AddComponent<Grabbable>();grabbable.InjectOptionalRigidbody(rb);grabbable.InjectOptionalThrowWhenUnselected(false);
                var hand=go.AddComponent<HandGrabInteractable>();hand.InjectRigidbody(rb);hand.InjectSupportedGrabTypes(GrabTypeFlags.All);hand.InjectOptionalPointableElement(grabbable);
                var controller=go.AddComponent<GrabInteractable>();controller.InjectRigidbody(rb);controller.InjectOptionalPointableElement(grabbable);
                var grip=GripAuthoring.Bind(go);foreach(var definition in grip.grips) definition.policy=GripContactPolicy.FingersOnly;
                var transformer=go.AddComponent<AuthoredGripTransformer>();transformer.target=grip;transformer.handGrab=hand;
                grabbable.InjectOptionalOneGrabTransformer(transformer);grabbable.InjectOptionalTwoGrabTransformer(transformer);
                var part=go.AddComponent<ExperiencePart>();part.partId=ids[i];part.displayName=names[i];part.inspectionText=descriptions[i];part.workspace=workspace;part.tableHome=home;
                part.body=rb;part.grabbable=grabbable;part.handGrab=hand;part.controllerGrab=controller;part.feedback=feedback;part.surfaces=go.GetComponentsInChildren<Renderer>();part.slots=new[]{slot};
                experience.parts[i]=part;
            }
            experience.inspectionPose=Empty("Inspection pose",references,new Vector3(-.57f,1.22f,.8f));
            var rig=BuildRig();
            var cameraGO=new GameObject("Desktop preview camera",typeof(Camera),typeof(AudioListener));
            var cam=cameraGO.GetComponent<Camera>();cam.transform.position=new Vector3(0,1.65f,-1.25f);cam.transform.LookAt(new Vector3(0,1.08f,1));cam.fieldOfView=48;cam.nearClipPlane=.03f;cam.farClipPlane=50;cam.backgroundColor=new Color(.035f,.05f,.075f);cam.clearFlags=CameraClearFlags.SolidColor;
            var desktopInput=new GameObject("Desktop UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var input=desktopInput.GetComponent<InputSystemUIInputModule>();input.AssignDefaultActions();
            var actions=Object.Instantiate(input.actionsAsset);actions.name="Desktop UI";AssetDatabase.CreateAsset(actions,Root+"/Input/DesktopUI.asset");input.actionsAsset=actions;
            var xrInput=new GameObject("XR UI EventSystem",typeof(EventSystem),typeof(PointableCanvasModule));
            var preview=services.AddComponent<DesktopPreview>();preview.xrRig=rig;preview.desktopCamera=cam;preview.desktopInput=desktopInput;preview.xrInput=xrInput;
            BuildUI(experience,cam);
            desktopInput.SetActive(false); xrInput.SetActive(false); // Awake enables exactly the selected input mode.
            Light light=new GameObject("Key light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.Soft;light.color=new Color(1,.94f,.85f);
            Light fill=new GameObject("Cool fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.6f;fill.transform.rotation=Quaternion.Euler(32,140,0);fill.color=new Color(.62f,.82f,1);
            GripAuthoring.BindSceneVisuals();
            PrefabUtility.SaveAsPrefabAsset(rig,Root+"/Prefabs/StarterXR Rig.prefab");
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("STARTER_DEMO_CREATED");
        }

        static GameObject BuildRig()
        {
            var root=new GameObject("XR Rig - Meta hands and controllers");
            var camera=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab"));camera.transform.SetParent(root.transform,false);
            var rig=camera.GetComponent<OVRCameraRig>();var manager=camera.GetComponent<OVRManager>();manager.trackingOriginType=OVRManager.TrackingOrigin.FloorLevel;
            foreach(var pair in new[]{(anchor:rig.leftHandAnchor,side:0),(anchor:rig.rightHandAnchor,side:1)})
            {
                var hand=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRHandPrefab.prefab"));hand.transform.SetParent(pair.anchor,false);hand.name=pair.side==0?"Left raw hand source":"Right raw hand source";
                var so=new SerializedObject(hand.GetComponent<OVRHand>());so.FindProperty("HandType").intValue=pair.side;so.ApplyModifiedPropertiesWithoutUndo();
                foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
                foreach(var renderer in hand.GetComponentsInChildren<OVRMeshRenderer>(true)) renderer.enabled=false;
            }
            var interactions=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.interaction.ovr/Runtime/Prefabs/OVRInteractionComprehensive.prefab"));interactions.transform.SetParent(root.transform,false);
            foreach(var r in interactions.GetComponentsInChildren<OVRCameraRigRef>(true)) r.InjectAllOVRCameraRigRef(rig,true);
            foreach(var c in camera.GetComponentsInChildren<Camera>(true)){c.nearClipPlane=.03f;c.farClipPlane=50;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.035f,.05f,.075f);}
            return root;
        }

        public static void FinalizeRigSourcesAndLayout()
        {
            StarterValidation.OpenDemoSafely();
            foreach(var hand in Object.FindObjectsOfType<OVRHand>(true)) DisableRawVisual(hand.gameObject);
            const string prefabPath=Root+"/Prefabs/StarterXR Rig.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try{foreach(var hand in prefab.GetComponentsInChildren<OVRHand>(true))DisableRawVisual(hand.gameObject);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var experience=Object.FindObjectOfType<AssemblyExperience>();
            for(int i=0;i<3;i++)experience.explodedPoses[i].position=experience.slots[i].matingPose.position+Vector3.up*(.08f+i*.04f);
            GameObject.Find("Experience panel - world space").transform.localPosition=new Vector3(0,1.64f,1.43f);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static void DisableRawVisual(GameObject hand)
        {
            foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            foreach(var renderer in hand.GetComponentsInChildren<OVRMeshRenderer>(true)){renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
        }

        static void BuildUI(AssemblyExperience experience,Camera camera)
        {
            var go=new GameObject("Experience panel - world space",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));go.transform.SetParent(workspace,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rt=(RectTransform)go.transform;rt.sizeDelta=new Vector2(1000,420);rt.localScale=Vector3.one*.0012f;rt.localPosition=new Vector3(0,1.64f,1.43f);
            var bg=go.AddComponent<Image>();bg.color=new Color(.025f,.045f,.065f,.98f);
            Label(rt,"Eyebrow","XR EXPERIENCE  /  STARTER KIT",new Vector2(-450,165),new Vector2(900,28),18,new Color(.22f,.8f,.82f));
            Label(rt,"Title","Explore. Assemble. Understand.",new Vector2(-450,116),new Vector2(900,50),36,Color.white);
            experience.statusText=Label(rt,"Status","Pick up a part. Release near its matching outline.",new Vector2(-450,64),new Vector2(900,42),21,new Color(.65f,.75f,.81f));
            var track=Rect("Progress track",rt,new Vector2(0,24),new Vector2(900,4));track.gameObject.AddComponent<Image>().color=new Color(.12f,.2f,.25f);
            var fill=Rect("Progress fill",track,Vector2.zero,new Vector2(900,4));experience.progressFill=fill.gameObject.AddComponent<Image>();experience.progressFill.color=new Color(.05f,.78f,.75f);experience.progressFill.type=Image.Type.Filled;experience.progressFill.fillMethod=Image.FillMethod.Horizontal;experience.progressFill.fillAmount=0;
            experience.progressFill.sprite=ProgressSprite();
            Button(rt,"Explode",new Vector2(-338,-29),experience.Explode,experience.feedback,true);
            Button(rt,"Reassemble",new Vector2(-112,-29),experience.Reassemble,experience.feedback,true);
            Button(rt,"Inspect",new Vector2(112,-29),experience.InspectSelected,experience.feedback,false);
            Button(rt,"Reset all",new Vector2(338,-29),experience.ResetExperience,experience.feedback,false);
            experience.inspectionTitle=Label(rt,"Part title","ROTOR CORE",new Vector2(-450,-83),new Vector2(800,26),18,new Color(.25f,.8f,.82f));
            experience.inspectionBody=Label(rt,"Part description","Grab a part to inspect it.",new Vector2(-450,-123),new Vector2(900,65),18,new Color(.68f,.77f,.83f));
            Label(rt,"Input hint","HANDS / CONTROLLERS: grab + ray or poke    |    DESKTOP: drag, wheel depth, Q/E rotate",new Vector2(-450,-188),new Vector2(920,25),14,new Color(.4f,.55f,.64f));
            // Package-owned UI surfaces; serialized prefab references stay in UPM, never vendored.
            foreach(string guid in new[]{"8369d93f7b6b99742bbea0649a41b7b1","4db41829582c7d24f80ee9603868dd67"})
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
                var rect=obj.GetComponent<RectTransform>();rect.SetParent(rt,false);rect.localPosition=Vector3.zero;rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                obj.GetComponent<PointableCanvas>().InjectCanvas(canvas);
            }
        }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;}
        public static Sprite ProgressSprite()
        {
            const string path=Root+"/Materials/Progress pixel.asset";
            var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(existing)return existing;
            var texture=new Texture2D(8,8,TextureFormat.RGBA32,false){name="Progress pixel"};texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();AssetDatabase.CreateAsset(texture,path);
            var sprite=Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name="Solid fill";AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.SaveAssets();return sprite;
        }
        static Text Label(Transform parent,string name,string content,Vector2 pos,Vector2 size,int fontSize,Color color)
        {var rt=Rect(name,parent,pos,size);rt.pivot=new Vector2(0,.5f);var t=rt.gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.text=content;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
        static void Button(Transform parent,string label,Vector2 pos,UnityAction action,InteractionFeedback feedback,bool primary)
        {
            var r=Rect(label,parent,pos,new Vector2(208,58));var img=r.gameObject.AddComponent<Image>();img.color=primary?new Color(.02f,.38f,.42f):new Color(.1f,.16f,.21f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;var colors=b.colors;colors.highlightedColor=new Color(.65f,1,1);colors.pressedColor=new Color(.4f,.7f,.8f);b.colors=colors;
            UnityEventTools.AddPersistentListener(b.onClick,action);r.gameObject.AddComponent<DemoButton>().feedback=feedback;
            var t=Label(r,"Label",label,Vector2.zero,new Vector2(208,58),22,Color.white);t.rectTransform.pivot=new Vector2(.5f,.5f);t.alignment=TextAnchor.MiddleCenter;
        }
        static void WorldText(string name,string text,Transform parent,Vector3 p,Quaternion q,int size,Vector2 bounds,float scale,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=q;go.transform.localScale=Vector3.one*scale;go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=bounds;Label(go.transform,"Text",text,Vector2.zero,bounds,size,color);}
        static Transform Empty(string name,Transform parent,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
        static Material Mat(string name,Color color,float metallic,float smooth)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);m.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;}
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material material,bool collider=true)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static GameObject Ring(string name,Transform parent,float radius,float thickness,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);var mesh=new Mesh(){name=name};int n=64,m=10;var v=new Vector3[(n+1)*(m+1)];var tris=new int[n*m*6];int k=0;
            for(int i=0;i<=n;i++)for(int j=0;j<=m;j++){float a=i*2*Mathf.PI/n,b=j*2*Mathf.PI/m;v[i*(m+1)+j]=new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(b)*thickness,Mathf.Sin(a)*(radius+Mathf.Cos(b)*thickness));}
            for(int i=0;i<n;i++)for(int j=0;j<m;j++){int a=i*(m+1)+j,b=a+m+1;tris[k++]=a;tris[k++]=a+1;tris[k++]=b;tris[k++]=b;tris[k++]=a+1;tris[k++]=b+1;}
            mesh.vertices=v;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Root+"/Meshes/"+name+".asset"));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static Mesh DiscMesh(float radius,float halfHeight)
        {
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cylinder);var mesh=Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);mesh.name="Cap collision";var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]=Vector3.Scale(v[i],new Vector3(radius*2,halfHeight,radius*2));mesh.vertices=v;mesh.RecalculateBounds();Object.DestroyImmediate(temp);AssetDatabase.CreateAsset(mesh,Root+"/Meshes/Cap collision.asset");return mesh;
        }

        public static void ConfigureInputBootstrap()
        {
            StarterValidation.OpenDemoSafely();
            var preview = Object.FindObjectOfType<DesktopPreview>(true);
            Undo.RecordObjects(new Object[] { preview.desktopInput, preview.xrInput }, "Configure input startup");
            preview.desktopInput.SetActive(false); preview.xrInput.SetActive(false);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
        }

        public static void ConfigureProject()
        {
            PlayerSettings.companyName="XR Starter";PlayerSettings.productName="XR Experience Starter";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.xrstarter.experience");PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone,"com.xrstarter.experience");
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            var ps=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);ps.FindProperty("activeInputHandler").intValue=1;ps.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.antiAliasing=4;QualitySettings.vSyncCount=0;QualitySettings.shadowDistance=8;QualitySettings.shadows=ShadowQuality.All;
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Grabbable";tags.ApplyModifiedPropertiesWithoutUndo();
            if(!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey,out var perTarget))
            {perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(perTarget,Root+"/Settings/XRGeneralSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);}
            foreach(var group in new[]{BuildTargetGroup.Android,BuildTargetGroup.Standalone})
            {
                if(!perTarget.HasSettingsForBuildTarget(group))perTarget.CreateDefaultSettingsForBuildTarget(group);
                if(!perTarget.HasManagerSettingsForBuildTarget(group))perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                var general=perTarget.SettingsForBuildTarget(group);general.InitManagerOnStart=true;
                if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group))throw new InvalidOperationException("OpenXR loader assignment failed for "+group);
                FeatureHelpers.RefreshFeatures(group);
                var featureSet=OpenXRFeatureSetManager.GetFeatureSetWithId(group,"com.meta.openxr.featureset.metaxr");
                if(featureSet!=null){featureSet.isEnabled=true;OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(group);}
                var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(group);xr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
                foreach(var feature in xr.GetFeatures<OpenXRFeature>())
                {
                    string name=feature.GetType().Name;feature.enabled=name=="OculusTouchControllerProfile"||name=="MetaXRFeature"||name=="MetaXRFoveationFeature"||(group==BuildTargetGroup.Android&&name=="MetaQuestFeature");
                    if(name=="MetaQuestFeature")
                    {
                        var serialized=new SerializedObject(feature);var devices=serialized.FindProperty("targetDevices");
                        for(int i=0;i<devices.arraySize;i++){var device=devices.GetArrayElementAtIndex(i);device.FindPropertyRelative("enabled").boolValue=device.FindPropertyRelative("manifestName").stringValue!="quest";}
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(feature);
                }
                EditorUtility.SetDirty(xr);EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
            }
            EditorUtility.SetDirty(perTarget);
            var config=OVRProjectConfig.CachedProjectConfig;config.handTrackingSupport=OVRProjectConfig.HandTrackingSupport.ControllersAndHands;config.handTrackingFrequency=OVRProjectConfig.HandTrackingFrequency.HIGH;
            config.targetDeviceTypes=new System.Collections.Generic.List<OVRProjectConfig.DeviceType>{OVRProjectConfig.DeviceType.Quest2,OVRProjectConfig.DeviceType.QuestPro,OVRProjectConfig.DeviceType.Quest3,OVRProjectConfig.DeviceType.Quest3S};
            config.insightPassthroughSupport=OVRProjectConfig.FeatureSupport.None;config.anchorSupport=OVRProjectConfig.AnchorSupport.Disabled;OVRProjectConfig.CommitProjectConfig(config);
            AssetDatabase.SaveAssets();
        }
    }
}
