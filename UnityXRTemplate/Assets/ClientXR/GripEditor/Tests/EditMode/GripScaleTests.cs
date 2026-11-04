using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ClientXR.Grips.Editor;
using Oculus.Interaction.Input;

namespace ClientXR.Grips.Tests
{
 public sealed class GripScaleTests
 {
  [Test] public void ScalePreviewHandlesApplyUndoAndReferenceDataStayConsistent()
  {
   var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Resizable test wrench");SceneManager.MoveGameObjectToScene(go,scene);
   var owner=go.AddComponent<GripTarget>();owner.scaleGripPositions=true;owner.gripReferenceScale=1;
   var grip=new GripDefinition{side=Handedness.Left,basePose=GripAuthoring.DefaultBase(Handedness.Left),positionMetres=new Vector3(.07f,.06f,-.24f),rotation=Quaternion.Euler(8,19,23)};grip.curl[1]=32;owner.grips.Add(grip);
   var preview=new GripPreview();var draft=ScriptableObject.CreateInstance<GripProfile>();int undo=-1;
   try
   {
    var saved=JsonUtility.ToJson(grip);var joints=grip.Compose().JointRotations;
    Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.RecordObject(go.transform,"Resize wrench");go.transform.localScale=Vector3.one*1.5f;Undo.FlushUndoRecordObjects();
    preview.Show(go.transform,grip);Assert.That(Vector3.Distance(preview.Ghost.transform.position,go.transform.position+go.transform.rotation*(grip.positionMetres*1.5f)),Is.LessThan(.0001f));Assert.That(preview.Ghost.transform.localScale,Is.EqualTo(Vector3.one));
    Assert.That(JsonUtility.ToJson(grip),Is.EqualTo(saved));Undo.PerformUndo();Assert.That(owner.GripPositionScale,Is.EqualTo(1));Undo.PerformRedo();Assert.That(owner.GripPositionScale,Is.EqualTo(1.5f));
    var world=GripMath.ToWorld(go.transform,grip);var round=GripMath.ToLocal(go.transform,world);Assert.That(Vector3.Distance(round.position,grip.positionMetres),Is.LessThan(.00001f));
    draft.grips.Add(grip.Clone());draft.grips[0].positionMetres=GripMath.ToLocal(go.transform,new Pose(world.position+go.transform.right*.03f,world.rotation)).position;
    GripAuthoring.Apply(owner,draft,false);preview.Show(go.transform,owner.grips[0]);Assert.That(Vector3.Distance(preview.Ghost.transform.position,world.position+go.transform.right*.03f),Is.LessThan(.0001f));
    Assert.That(owner.grips[0].Compose().JointRotations,Is.EqualTo(joints),"Resizing/placement never changes finger joints");
    go.transform.localScale=Vector3.one*2;preview.Show(go.transform,owner.grips[0]);Assert.That(Vector3.Distance(preview.Ghost.transform.position,go.transform.position+go.transform.rotation*(owner.grips[0].positionMetres*2)),Is.LessThan(.0001f),"Editing at an enlarged size must not double-apply scale");
    go.transform.localScale=new Vector3(2,1,2);Assert.That(GripMath.ValidateFrame(go.transform),Is.Not.Null);
   }
   finally{preview.Clear();if(undo>=0)Undo.RevertAllDownToGroup(undo);Object.DestroyImmediate(draft);EditorSceneManager.ClosePreviewScene(scene);}
  }
  [Test] public void ScaleIsOptInAndHandSizeCompensationRemainsInHandMetres()
  {
   var parent=new GameObject("Parent");var go=new GameObject("Tool");go.transform.SetParent(parent.transform,false);var owner=go.AddComponent<GripTarget>();
   try
   {
    var grip=new GripDefinition{positionMetres=new Vector3(.1f,.2f,.3f),axisEnabled=true,axisPivot=new Vector3(.03f,0,0),axisDirection=Vector3.up,contactEnabled=true,contactWristLocal=new Vector3(.02f,.03f,0)};
    go.transform.localScale=Vector3.one*100;var ordinary=owner.ResolvePose(grip,30,.8f);Assert.That(Vector3.Distance(ordinary.position,GripPoseResolver.Resolve(grip,30,.8f).position),Is.LessThan(.000001f),"Other existing imported-scale targets unchanged");
    owner.scaleGripPositions=true;owner.gripReferenceScale=100;go.transform.localScale*=1.5f;parent.transform.localScale=Vector3.one*1.1f;
    var turn=Quaternion.AngleAxis(30,Vector3.up);var expected=(grip.axisPivot+turn*(grip.positionMetres-grip.axisPivot))*1.65f+turn*grip.contactWristLocal*.2f;
    Assert.That(Vector3.Distance(owner.ResolvePose(grip,30,.8f).position,expected),Is.LessThan(.00001f));
   }finally{Object.DestroyImmediate(parent);}
  }
 }
}
