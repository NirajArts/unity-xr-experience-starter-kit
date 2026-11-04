using System;
using System.Linq;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientXR.Grips.Editor
{
    public static class GripAuthoring
    {
        public const string Torch="Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/Props/Torch/Torch.prefab";
        public static HandGrabPose DefaultBase(Handedness side)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Torch);
            return asset?asset.GetComponentsInChildren<HandGrabPose>(true).FirstOrDefault(p=>p.HandPose!=null && p.HandPose.Handedness==side):null;
        }
        public static string Unsupported(GameObject go)
        {
            if(!go)return "Select a scene object or open an editable prefab.";
            if(PrefabUtility.IsPartOfModelPrefab(go) && EditorUtility.IsPersistent(go))return "Imported model is read-only. Select its editable scene instance or create an editable prefab wrapper.";
            if(EditorUtility.IsPersistent(go))return "Open this prefab in Prefab Mode before binding or editing.";
            if(!go.GetComponent<HandGrabInteractable>())return "Select the object owning Meta HandGrabInteractable. A model child alone is not a grip target.";
            return GripMath.ValidateFrame(go.transform);
        }
        public static GripTarget Bind(GameObject go)
        {
            string problem=Unsupported(go);if(problem!=null)throw new InvalidOperationException(problem);
            int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Set up grip target");
            var target=go.GetComponent<GripTarget>();if(target)return target;
            target=Undo.AddComponent<GripTarget>(go);target.interactable=go.GetComponent<HandGrabInteractable>();
            foreach(var side in new[]{Handedness.Left,Handedness.Right})
            {
                var existing=target.interactable.HandGrabPoses.FirstOrDefault(p=>p && p.HandPose!=null && p.HandPose.Handedness==side);
                var grip=new GripDefinition{side=side,basePose=existing?existing:DefaultBase(side)};
                if(existing){var pose=GripMath.ToLocal(go.transform,existing.transform.GetPose());grip.positionMetres=pose.position;grip.rotation=pose.rotation;}
                target.grips.Add(grip);
            }
            EditorUtility.SetDirty(target);PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            if(PrefabStageUtility.GetCurrentPrefabStage()==null)BindSceneVisuals();
            Undo.CollapseUndoOperations(group);return target;
        }
        public static int BindSceneVisuals()
        {
            int count=0;
            foreach(var visual in Resources.FindObjectsOfTypeAll<HandGrabStateVisual>().Where(v=>v.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(v)))
            {
                var serialized=new SerializedObject(visual);
                var source=serialized.FindProperty("_handGrabState").objectReferenceValue as HandGrabInteractor;
                if(!source)continue; // Preserve distance/use/non-near adapters.
                var adapter=visual.GetComponent<GripVisualState>()??Undo.AddComponent<GripVisualState>(visual.gameObject);
                Undo.RecordObject(adapter,"Bind grip visual source");adapter.source=source;adapter.visual=visual;
                serialized.FindProperty("_handGrabState").objectReferenceValue=adapter;serialized.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(adapter);PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);count++;
            }
            return count;
        }
        public static string Destination(GripTarget target)
        {
            if(!target)return "No target";
            if(target.sharedProfile)return "SHARED profile: "+AssetDatabase.GetAssetPath(target.sharedProfile);
            var stage=PrefabStageUtility.GetCurrentPrefabStage();
            if(stage!=null && target.gameObject.scene==stage.scene)return "Prefab-owned: "+stage.assetPath;
            return "Scene instance (no apply to base prefab): "+target.gameObject.scene.path;
        }
        public static void Apply(GripTarget target,GripProfile draft,bool permitShared)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            foreach(var g in draft.grips){string e=g.policy==GripContactPolicy.Existing?null:g.Validate();if(e!=null)throw new InvalidOperationException(e);}
            string frame=GripMath.ValidateFrame(target.transform);if(frame!=null)throw new InvalidOperationException(frame);
            if(target.sharedProfile && !permitShared)throw new InvalidOperationException("Shared editing must be explicitly enabled or duplicate/rebind first.");
            UnityEngine.Object owner=target.sharedProfile?(UnityEngine.Object)target.sharedProfile:target;
            Undo.RecordObject(owner,"Apply authored grips");
            var copy=draft.grips.Select(g=>g.Clone()).ToList();
            if(target.sharedProfile)target.sharedProfile.grips=copy;else target.grips=copy;
            EditorUtility.SetDirty(owner);PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            if(!target.sharedProfile)EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
    }
    static class GripTransformExtensions
    {
        public static Pose GetPose(this Transform t)=>new Pose(t.position,t.rotation);
    }
    [CustomEditor(typeof(GripTarget))]
    public sealed class GripTargetInspector:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var t=(GripTarget)target;
            EditorGUILayout.HelpBox(GripAuthoring.Destination(t),MessageType.Info);
            if(GUILayout.Button("Open Grip Editor"))GripEditorWindow.Open(t);
            var e=t.Validate();if(e!=null)EditorGUILayout.HelpBox(e,MessageType.Warning);
            DrawDefaultInspector();
        }
    }
}
