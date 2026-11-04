using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oculus.Interaction.Input;
using ClientXR.Grips.Editor;
namespace ClientXR.Grips.Tests
{
 public class GripJointControlsTests
 {
  [TestCase(Handedness.Left)] [TestCase(Handedness.Right)]
  public void IndividualBoneControlsChangeRealPreviewAndPreserveOtherData(Handedness side)
  {
   var userWindows=Resources.FindObjectsOfTypeAll<GripEditorWindow>();var drafts=userWindows.Select(w=>w.Draft?JsonUtility.ToJson(w.Draft):null).ToArray();
   var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=active.isDirty;
   var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Isolated joint-control test");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
   go.transform.SetPositionAndRotation(new Vector3(2,3,4),Quaternion.Euler(21,38,-17));go.transform.localScale=Vector3.one*.8f;
   var target=go.AddComponent<GripTarget>();target.grips.Add(new GripDefinition{side=side,basePose=GripAuthoring.DefaultBase(side),policy=GripContactPolicy.FingersOnly,rotation=Quaternion.Euler(10,-20,30)});
   var window=ScriptableObject.CreateInstance<GripEditorWindow>();int undoGroup=-1;
   try
   {
    window.UseTarget(target);window.Edit(g=>{g.curl[1]=35;g.spread[2]=-9;g.thumbOpposition=12;},"Fixture shortcuts");
    window.SelectJoint(0);var skin=window.SelectedJointTransform.GetComponentInParent<Oculus.Interaction.HandGrab.Visuals.HandPuppet>().GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);var originalVertices=baked.vertices;UnityEngine.Object.DestroyImmediate(baked);
    for(int j=0;j<FingersMetadata.HAND_JOINT_IDS.Length;j++)
    {
     window.SelectJoint(j);Assert.That(window.SelectedJoint,Is.EqualTo(j));Assert.That(window.SelectedJointTransform,Is.Not.Null);var before=window.Current.Clone();var desired=window.SelectedJointTransform.rotation*Quaternion.Euler(19,-13,23);
     window.RotateSelectedJoint(desired);Assert.That(Quaternion.Angle(window.SelectedJointTransform.rotation,desired),Is.LessThan(.08f),side+" "+FingersMetadata.HAND_JOINT_IDS[j]);
     for(int k=0;k<before.jointDegrees.Length;k++)if(k!=j)Assert.That(window.Current.jointDegrees[k],Is.EqualTo(before.jointDegrees[k]),"Other authored joint unchanged");
     Assert.That(window.Current.positionMetres,Is.EqualTo(before.positionMetres));Assert.That(window.Current.rotation,Is.EqualTo(before.rotation));CollectionAssert.AreEqual(before.curl,window.Current.curl);CollectionAssert.AreEqual(before.spread,window.Current.spread);
    }
    baked=new Mesh();skin.BakeMesh(baked);float max=baked.vertices.Select((v,i)=>Vector3.Distance(v,originalVertices[i])).Max();UnityEngine.Object.DestroyImmediate(baked);Assert.That(max,Is.GreaterThan(.001f),"Real preview skin must change");
    var saved=ScriptableObject.CreateInstance<GripProfile>();try{JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(window.Draft),saved);for(int j=0;j<window.Current.jointDegrees.Length;j++)Assert.That(Quaternion.Angle(saved.grips[0].Compose().JointRotations[j],window.Current.Compose().JointRotations[j]),Is.LessThan(.05f));}finally{UnityEngine.Object.DestroyImmediate(saved);}
    Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();var original=window.Current.jointDegrees[3];window.SelectJoint(3);window.RotateSelectedJoint(window.SelectedJointTransform.rotation*Quaternion.Euler(11,14,9));Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(undoGroup);undoGroup=-1;window.Refresh();Assert.That(Vector3.Distance(window.Current.jointDegrees[3],original),Is.LessThan(.001f),"Joint edit supports Undo");
    Assert.That(target.grips[0].jointDegrees.All(v=>v==Vector3.zero),Is.True,"Draft editing does not apply or save");
   }
   finally
   {
    if(undoGroup>=0)Undo.RevertAllDownToGroup(undoGroup);window.DiscardChanges();var draft=window.Draft;UnityEngine.Object.DestroyImmediate(window);if(draft)UnityEngine.Object.DestroyImmediate(draft);UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);
   }
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.EqualTo(active));Assert.That(active.isDirty,Is.EqualTo(dirty));for(int i=0;i<userWindows.Length;i++)Assert.That(userWindows[i].Draft?JsonUtility.ToJson(userWindows[i].Draft):null,Is.EqualTo(drafts[i]),"User draft preserved");
  }
 }
}
