using System;
using System.Linq;
using NUnit.Framework;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using ClientXR.Grips.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ClientXR.Grips.Tests
{
    public sealed class GripDataTests
    {
        GripDefinition Grip(Handedness side=Handedness.Left)=>new GripDefinition{side=side,basePose=GripAuthoring.DefaultBase(side),policy=GripContactPolicy.FingersOnly};
        [Test] public void IndependentHandsAndFingerControlsDoNotMutateSdk()
        {
            var left=Grip();var right=Grip(Handedness.Right);string baseline=JsonUtility.ToJson(left.basePose.HandPose);var initial=left.Compose();
            left.curl[1]=35;left.spread[2]=12;left.thumbOpposition=22;left.overrideFreedom[1]=true;left.freedom[1]=JointFreedom.Free;
            var result=left.Compose();Assert.That(result.FingersFreedom[1],Is.EqualTo(JointFreedom.Free));
            Assert.That(Enumerable.Range(0,result.JointRotations.Length).Count(i=>Quaternion.Angle(initial.JointRotations[i],result.JointRotations[i])>1),Is.GreaterThan(3));
            Assert.That(right.curl[1],Is.Zero);Assert.That(JsonUtility.ToJson(left.basePose.HandPose),Is.EqualTo(baseline));
        }
        [Test] public void CopyRoundTripAndIncompatibleMappingRejection()
        {
            var g=Grip();g.positionMetres=new Vector3(.1f,-.2f,.3f);g.rotation=Quaternion.Euler(24,-58,17);g.curl[3]=29;
            var copy=g.Clone();Assert.That(JsonUtility.ToJson(copy),Is.EqualTo(JsonUtility.ToJson(g)));copy.curl[3]=0;Assert.That(g.curl[3],Is.EqualTo(29));
            copy.mapping="other-skeleton";Assert.That(copy.Validate(),Does.Contain("Incompatible"));Assert.Throws<InvalidOperationException>(()=>copy.Compose());
            g.basePose=null;Assert.That(g.Validate(),Does.Contain("base reference"));
        }
        [TestCase(Handedness.Left)] [TestCase(Handedness.Right)]
        public void DoubleMirrorRestoresActualSkeletonAndPlacement(Handedness side)
        {
            var g=Grip(side);g.positionMetres=new Vector3(.13f,.02f,-.09f);g.rotation=Quaternion.Euler(31,73,-24);g.curl[1]=28;g.thumbOpposition=20;g.jointDegrees[5]=new Vector3(3,7,12);
            var twice=g.Mirrored().Mirrored();Assert.That(Vector3.Distance(g.positionMetres,twice.positionMetres),Is.LessThan(1e-6));Assert.That(Quaternion.Angle(g.rotation,twice.rotation),Is.LessThan(.03f));
            var a=g.Compose();var b=twice.Compose();for(int i=0;i<a.JointRotations.Length;i++)Assert.That(Quaternion.Angle(a.JointRotations[i],b.JointRotations[i]),Is.LessThan(.03f));
        }
        [Test] public void MetreFrameRoundTripsScale100Minus90AndMovedParents()
        {
            var root=new GameObject("scaled parent");var child=new GameObject("mesh wrapper");
            try
            {
                child.transform.SetParent(root.transform);root.transform.SetPositionAndRotation(new Vector3(3,2,1),Quaternion.Euler(-90,34,12));root.transform.localScale=Vector3.one*100;
                child.transform.localPosition=new Vector3(.01f,.02f,.03f);child.transform.localRotation=Quaternion.Euler(13,67,-8);
                var g=Grip();g.positionMetres=new Vector3(.12f,.02f,-.03f);g.rotation=Quaternion.Euler(22,35,18);
                foreach(var shift in new[]{Vector3.zero,new Vector3(-5,8,3)})
                {root.transform.position+=shift;var world=GripMath.ToWorld(child.transform,g);var local=GripMath.ToLocal(child.transform,world);Assert.That(Vector3.Distance(local.position,g.positionMetres),Is.LessThan(1e-5));Assert.That(Vector3.Distance(world.position,child.transform.position),Is.EqualTo(g.positionMetres.magnitude).Within(1e-5));Assert.That(Quaternion.Angle(local.rotation,g.rotation),Is.LessThan(.03));}
                root.transform.localScale=new Vector3(100,50,100);Assert.That(GripMath.ValidateFrame(child.transform),Is.Not.Null);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(child);}
        }
        [Test] public void PreviewUsesInstalledMappingAndCleansUp()
        {
            var go=new GameObject("Preview test");var preview=new GripPreview();
            try
            {
                var g=Grip();g.curl[2]=35;preview.Show(go.transform,g);Assert.That(preview.Ghost,Is.Not.Null);
                Assert.That(preview.Ghost.gameObject.hideFlags,Is.EqualTo(HideFlags.HideAndDontSave));
                Assert.That(preview.Ghost.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),Is.False);
                Assert.That(preview.Ghost.GetComponent<Oculus.Interaction.HandGrab.Visuals.HandPuppet>().JointMaps.Count,Is.GreaterThan(10));
                var pose=GripMath.ToWorld(go.transform,g);Assert.That(Vector3.Distance(preview.Ghost.transform.position,pose.position),Is.LessThan(1e-5));
                preview.Clear();Assert.That(preview.Ghost,Is.Null);
            }
            finally{preview.Clear();UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ApplyUndoRedoAndSharedIsolation()
        {
            var go=new GameObject("Grip owner");var t=go.AddComponent<GripTarget>();var draft=ScriptableObject.CreateInstance<GripProfile>();var shared=ScriptableObject.CreateInstance<GripProfile>();
            try
            {
                t.grips.Add(Grip());draft.grips.Add(Grip());draft.grips[0].curl[1]=28;
                Undo.IncrementCurrentGroup();GripAuthoring.Apply(t,draft,false);Undo.FlushUndoRecordObjects();Assert.That(t.grips[0].curl[1],Is.EqualTo(28));
                Undo.PerformUndo();Assert.That(t.grips[0].curl[1],Is.Zero);Undo.PerformRedo();Assert.That(t.grips[0].curl[1],Is.EqualTo(28));
                shared.grips.Add(Grip());t.sharedProfile=shared;Assert.Throws<InvalidOperationException>(()=>GripAuthoring.Apply(t,draft,false));Assert.That(shared.grips[0].curl[1],Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(draft);UnityEngine.Object.DestroyImmediate(shared);}
        }
        [Test] public void ExplicitSetupUndoRedoPreservesExistingInteractable()
        {
            var go=new GameObject("Undo setup target");
            try
            {
                var hand=go.AddComponent<HandGrabInteractable>();Undo.IncrementCurrentGroup();var target=GripAuthoring.Bind(go);Undo.FlushUndoRecordObjects();Assert.That(target.grips.Count,Is.EqualTo(2));
                Undo.PerformUndo();Assert.That(go.GetComponent<GripTarget>(),Is.Null);Assert.That(go.GetComponent<HandGrabInteractable>(),Is.SameAs(hand));
                Undo.PerformRedo();Assert.That(go.GetComponent<GripTarget>().grips.Count,Is.EqualTo(2));Assert.That(go.GetComponent<GripTarget>().interactable,Is.SameAs(hand));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void WindowDraftUndoSaveReloadAndRevertAreRealTransactions()
        {
            const string folder="Assets/ClientXR/GripEditor/Tests/TemporaryWindow";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/ClientXR/GripEditor/Tests","TemporaryWindow");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);GripEditorWindow window=null;
            try
            {
                var go=new GameObject("Window persistence target");var target=go.AddComponent<GripTarget>();target.grips.Add(Grip());target.grips.Add(Grip(Handedness.Right));
                EditorSceneManager.SaveScene(scene,folder+"/Roundtrip.unity");window=GripEditorWindow.Open(target);
                Undo.IncrementCurrentGroup();window.Edit(g=>{g.curl[2]=32;g.axisEnabled=true;g.axisPivot=new Vector3(.03f,.04f,.05f);g.contactEnabled=true;g.contactWristLocal=new Vector3(.01f,.02f,.03f);},"Draft finger edit");Undo.FlushUndoRecordObjects();
                Assert.That(target.grips[0].curl[2],Is.Zero);Assert.That(window.hasUnsavedChanges,Is.True);
                Undo.PerformUndo();Assert.That(window.Current.curl[2],Is.Zero);Undo.PerformRedo();Assert.That(window.Current.curl[2],Is.EqualTo(32));
                window.SaveChanges();Assert.That(window.hasUnsavedChanges,Is.False);Assert.That(scene.isDirty,Is.False);Assert.That(target.grips[0].curl[2],Is.EqualTo(32));Assert.That(target.grips[0].axisEnabled,Is.True);Assert.That(target.grips[0].contactEnabled,Is.True);
                window.Edit(g=>g.curl[2]=70,"Temporary draft");window.DiscardChanges();Assert.That(window.Current.curl[2],Is.EqualTo(32));window.Close();window=null;
                scene=EditorSceneManager.OpenScene(folder+"/Roundtrip.unity",OpenSceneMode.Single);
                Assert.That(scene.GetRootGameObjects().Single().GetComponent<GripTarget>().grips[0].curl[2],Is.EqualTo(32));
                Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Any(g=>g.name.StartsWith("Grip Editor Preview")),Is.False);
            }
            finally{if(window){window.DiscardChanges();window.Close();}EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);AssetDatabase.DeleteAsset(folder);}
        }
        [Test] public void PrefabVariantAndSceneSerializationPreserveOwnershipAndReferences()
        {
            const string folder="Assets/ClientXR/GripEditor/Tests/TemporaryPersistence";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/ClientXR/GripEditor/Tests","TemporaryPersistence");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);GameObject go=null,instance=null;
            try
            {
                go=new GameObject("Grip test wrapper");SceneManager.MoveGameObjectToScene(go,scene);var t=go.AddComponent<GripTarget>();t.grips.Add(Grip());
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,folder+"/Base.prefab");instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);var i=instance.GetComponent<GripTarget>();i.grips[0].curl[0]=33;i.grips[0].axisEnabled=true;i.grips[0].axisPivot=new Vector3(.1f,.2f,.3f);PrefabUtility.RecordPrefabInstancePropertyModifications(i);
                var variant=PrefabUtility.SaveAsPrefabAsset(instance,folder+"/Variant.prefab");Assert.That(PrefabUtility.GetPrefabAssetType(variant),Is.EqualTo(PrefabAssetType.Variant));
                Assert.That(variant.GetComponent<GripTarget>().grips[0].axisPivot,Is.EqualTo(new Vector3(.1f,.2f,.3f)));Assert.That(prefab.GetComponent<GripTarget>().grips[0].axisEnabled,Is.False);Assert.That(prefab.GetComponent<GripTarget>().grips[0].curl[0],Is.Zero);Assert.That(variant.GetComponent<GripTarget>().grips[0].curl[0],Is.EqualTo(33));
                Assert.That(EditorSceneManager.SaveScene(scene,folder+"/Roundtrip.unity"),Is.True);EditorSceneManager.CloseScene(scene,true);
                scene=EditorSceneManager.OpenScene(folder+"/Roundtrip.unity",OpenSceneMode.Additive);
                Assert.That(scene.GetRootGameObjects().Any(r=>r.GetComponent<GripTarget>().grips[0].basePose),Is.True);
                Assert.That(scene.GetRootGameObjects().Any(r=>r.name.Contains("Grip Editor Preview")),Is.False);
            }
            finally{if(scene.IsValid())EditorSceneManager.CloseScene(scene,true);AssetDatabase.DeleteAsset(folder);}
        }
    }
}
