using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientXR.Grips;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;

namespace XRStarter.Editor
{
    public static class StarterValidation
    {
        public static string ReportRoot => Path.GetFullPath(Path.Combine(Application.dataPath,"../../Reports"));
        public static void OpenDemoSafely()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("A Play session is active. Do not interrupt user-owned Play Mode.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene found; no automatic save/discard.");
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
        }
        public static void BatchValidate(){OpenDemoSafely();ValidateLoadedDemo();}
        [MenuItem("XR Starter/Validate Loaded Demo")]
        public static void ValidateLoadedDemo()
        {
            if(SceneManager.GetActiveScene().path!=DemoBuilder.ScenePath)throw new InvalidOperationException("Open the starter demo first.");
            var failures=new List<string>();var observations=new List<string>();
            void Check(bool pass,string label){(pass?observations:failures).Add(label);}
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            Check(all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No missing scene scripts");
            int broken=0;
            foreach(var c in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c))
            {
                var so=new SerializedObject(c);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==null&&p.objectReferenceInstanceIDValue!=0){broken++;failures.Add(c.name+"."+p.propertyPath+" has a broken reference");}
            }
            Check(broken==0,"No broken serialized scene references");
            var exp=UnityEngine.Object.FindObjectOfType<AssemblyExperience>(true);Check(exp&&exp.parts.Length==3&&exp.slots.Length==3,"Three-part generic assembly is wired");
            if(exp)
            {
                foreach(var part in exp.parts)
                {
                    Check(part&&part.body&&part.grabbable&&part.handGrab&&part.controllerGrab&&part.tableHome&&part.workspace,"Grabbing, body and recovery references: "+part.name);
                    var grip=part.GetComponent<GripTarget>();Check(grip&&grip.grips.Count==2&&grip.grips.All(g=>g.Validate()==null),"Valid left/right authored grip data: "+part.name);
                    Check(part.slots.All(s=>s&&s.matingPose&&s.compatibleId==part.partId),"Compatible mating reference: "+part.name);
                    Check(part.GetComponents<Collider>().All(c=>c.enabled)&&part.GetComponents<Collider>().Length>0,"Enabled collision shapes: "+part.name);
                }
                Check(exp.feedback&&exp.feedback.clips.Length==6&&exp.feedback.clips.All(c=>c),"Six original feedback clips assigned");
                Check(exp.inspectionPose&&exp.explodedPoses.All(p=>p),"Inspection and exploded poses separate from table homes");
            }
            var rig=UnityEngine.Object.FindObjectOfType<OVRCameraRig>(true);Check(rig&&rig.centerEyeAnchor&&rig.leftHandAnchor&&rig.rightHandAnchor,"OVRCameraRig has required tracking anchors");
            var rigRefs=UnityEngine.Object.FindObjectsOfType<OVRCameraRigRef>(true);Check(rigRefs.Length>0&&rigRefs.All(r=>r.CameraRig==rig&&r.LeftHand&&r.RightHand),"Meta interaction sources reference the correct camera and both hands");
            Check(UnityEngine.Object.FindObjectsOfType<HandGrabInteractor>(true).Length>=2,"Hand grab interactors present");
            Check(UnityEngine.Object.FindObjectsOfType<GrabInteractor>(true).Length>=2,"Controller grab interactors present");
            Check(UnityEngine.Object.FindObjectsOfType<GripVisualState>(true).Length>=2,"Runtime authored finger visual adapters bound");
            Check(UnityEngine.Object.FindObjectsOfType<OVRHand>(true).All(h=>h.GetComponentsInChildren<OVRMeshRenderer>(true).All(r=>!r.enabled)),"Raw hand renderers disabled; SDK synthetic visuals remain the visual owner");
            Check(UnityEngine.Object.FindObjectsOfType<PointableCanvas>(true).Length==2,"World UI has ray and poke surfaces");
            var input=UnityEngine.Object.FindObjectOfType<InputSystemUIInputModule>(true);Check(input&&input.actionsAsset&&input.actionsAsset.actionMaps.Count>0&&input.actionsAsset.bindings.Count()>0,"Portable desktop UI input actions assigned");
            var buttons=UnityEngine.Object.FindObjectsOfType<Button>(true);Check(buttons.Length==4&&buttons.All(b=>b.onClick.GetPersistentEventCount()==1&&b.onClick.GetPersistentTarget(0)),"Four controls have persistent experience callbacks");
            foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android})
            {
                var xr=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);Check(xr&&xr.Manager&&xr.Manager.activeLoaders.Any(l=>l.GetType().Name=="OpenXRLoader"),group+" OpenXR loader configured");
                var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);Check(settings&&settings.renderMode==OpenXRSettings.RenderMode.SinglePassInstanced,group+" single pass stereo configured");
                var enabled=settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>().Where(f=>f.enabled).Select(f=>f.GetType().Name).ToArray();
                Check(enabled.Contains("OculusTouchControllerProfile")&&enabled.Contains("MetaXRFeature"),group+" controller profile and Meta runtime feature enabled");
                if(group==BuildTargetGroup.Android)Check(enabled.Contains("MetaQuestFeature"),"Android Meta Quest loader/manifest support enabled");
            }
            Check(PlayerSettings.colorSpace==ColorSpace.Linear&&!UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline,"Built-in pipeline / Linear colour");
            string[] deps=AssetDatabase.GetDependencies(DemoBuilder.ScenePath,true);Check(deps.All(p=>p.StartsWith("Assets/")||p.StartsWith("Packages/")||p=="Resources/unity_builtin_extra"||p=="Library/unity default resources"),"Scene dependency paths are project/package relative");
            Check(File.Exists("../Docs/PROJECT_JOURNAL.md")&&new FileInfo("../Docs/PROJECT_JOURNAL.md").Length==0,"Starter project journal is empty");
            Directory.CreateDirectory(ReportRoot);
            File.WriteAllText(Path.Combine(ReportRoot,"EDITOR_VALIDATION.md"),"# Editor validation\n\nUTC: "+DateTime.UtcNow.ToString("O")+"\n\nUnity: "+Application.unityVersion+"\nScene GUID: "+AssetDatabase.AssetPathToGUID(DemoBuilder.ScenePath)+"\n\n"+string.Join("\n",observations.Select(s=>"- PASS: "+s))+"\n\n"+string.Join("\n",failures.Select(s=>"- FAIL: "+s))+"\n\nThese are Editor structural checks, not headset acceptance.\n");
            if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));Debug.Log("STARTER_VALIDATION_PASSED: "+observations.Count);
        }
        public static void CaptureLoadedDemo()
        {
            if(SceneManager.GetActiveScene().path!=DemoBuilder.ScenePath)throw new InvalidOperationException("Open the demo first.");
            var preview=UnityEngine.Object.FindObjectOfType<DesktopPreview>(true);Capture(preview.desktopCamera,EditorApplication.isPlaying?"runtime":"editor");
        }
        public static void Capture(Camera camera,string label)
        {
            Directory.CreateDirectory(Path.Combine(ReportRoot,"Screenshots"));string path=Path.Combine(ReportRoot,"Screenshots",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+label+".png");
            var rt=new RenderTexture(1600,1000,24);var oldTarget=camera.targetTexture;var old=RenderTexture.active;
            try{Canvas.ForceUpdateCanvases();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
            finally{camera.targetTexture=oldTarget;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);}
            Debug.Log("STARTER_CAPTURE: "+path);
        }
        public static void BatchCapture(){OpenDemoSafely();CaptureLoadedDemo();}
        public static void BuildWindows(){Build(BuildTarget.StandaloneWindows64,"../Builds/Windows/XRStarter.exe");}
        public static void BuildAndroid(){Build(BuildTarget.Android,"../Builds/Android/XRStarter.apk");}
        static void Build(BuildTarget target,string output)
        {
            OpenDemoSafely();ValidateLoadedDemo();Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Meta 77 copies this generated desktop sidecar without overwrite support.
            // Remove only the previous build artifact, never a project input or user settings.
            if(target==BuildTarget.StandaloneWindows64)
            {
                string buildRoot=Path.GetFullPath("../Builds")+Path.DirectorySeparatorChar;
                string sidecar=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)),"RuntimeActionBindings.json");
                if(!sidecar.StartsWith(buildRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Build output must remain under Builds.");
                if(File.Exists(sidecar))File.Delete(sidecar);
            }
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{DemoBuilder.ScenePath},target=target,locationPathName=output,options=BuildOptions.Development});
            Directory.CreateDirectory(ReportRoot);File.WriteAllText(Path.Combine(ReportRoot,"BUILD_"+target+".md"),"# Build result\n\n"+DateTime.UtcNow.ToString("O")+"\n\n"+report.summary.result+"; errors: "+report.summary.totalErrors+"; bytes: "+report.summary.totalSize+"\n\nBuild success does not establish device acceptance.\n");
            if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors>0)throw new InvalidOperationException("Build failed or reported errors: "+report.summary.result);
        }
    }
}
