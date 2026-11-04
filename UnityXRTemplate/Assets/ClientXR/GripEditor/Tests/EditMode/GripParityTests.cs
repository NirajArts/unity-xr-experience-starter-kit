using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ClientXR.Grips.Editor;
using Oculus.Interaction.Input;
namespace ClientXR.Grips.Tests
{
    public class GripParityTests
    {
        GripDefinition Grip()=>new GripDefinition{basePose=GripAuthoring.DefaultBase(Handedness.Left),policy=GripContactPolicy.FixedVisualWrist,positionMetres=new Vector3(.12f,.04f,-.08f),rotation=Quaternion.Euler(22,43,-17),axisEnabled=true,axisPivot=new Vector3(-.03f,.02f,.01f),axisDirection=new Vector3(1,2,3).normalized,contactEnabled=true,contactWristLocal=new Vector3(.03f,.04f,.02f)};
        [TestCase(0f)] [TestCase(90f)] [TestCase(-90f)] [TestCase(179.9f)] [TestCase(-179.9f)]
        public void OrbitAndAcquisition(float angle)
        {
            var g=Grip();var pose=GripPoseResolver.Resolve(g,angle);Assert.That((pose.position-g.axisPivot).magnitude,Is.EqualTo((g.positionMetres-g.axisPivot).magnitude).Within(1e-6));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(angle,GripPoseResolver.AcquireAngle(g,pose.rotation))),Is.LessThan(.001f));
            var q=pose.rotation;var negative=new Quaternion(-q.x,-q.y,-q.z,-q.w);Assert.That(Mathf.Abs(Mathf.DeltaAngle(angle,GripPoseResolver.AcquireAngle(g,negative))),Is.LessThan(.001f));
        }
        [Test] public void LimitsAndInvalidData()
        {
            var g=Grip();g.axisLimited=true;g.axisMinimum=-170;g.axisMaximum=30;Assert.That(GripPoseResolver.Clamp(g,179),Is.EqualTo(-170));g.axisDirection=Vector3.zero;Assert.That(g.Validate(),Is.Not.Null);g.axisEnabled=false;Assert.That(g.Validate(),Is.Null);g.contactWristLocal.x=float.NaN;Assert.That(g.Validate(),Is.Not.Null);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MirrorOrbitContactAndDoubleMirror(int plane)
        {
            var g=Grip();g.axisLimited=true;g.axisMinimum=-70;g.axisMaximum=100;g.curl[1]=23;g.jointDegrees[4]=new Vector3(3,12,7);
            var mirrored=g.Mirrored(plane);var normal=plane==0?Vector3.right:plane==1?Vector3.up:Vector3.forward;
            var a=GripPoseResolver.Resolve(g,60,1.2f);var b=GripPoseResolver.Resolve(mirrored,-60,1.2f);
            Assert.That(Vector3.Distance(Vector3.Reflect(a.position,normal),b.position),Is.LessThan(1e-6));
            Assert.That(Vector3.Distance(Vector3.Reflect(a.position+a.rotation*g.contactWristLocal*1.2f,normal),b.position+b.rotation*mirrored.contactWristLocal*1.2f),Is.LessThan(1e-6));
            var twice=mirrored.Mirrored(plane);Assert.That(Vector3.Distance(g.positionMetres,twice.positionMetres),Is.LessThan(1e-6));Assert.That(Quaternion.Angle(g.rotation,twice.rotation),Is.LessThan(.05f));Assert.That(Vector3.Distance(g.contactWristLocal,twice.contactWristLocal),Is.LessThan(1e-6));
            for(int i=0;i<g.jointDegrees.Length;i++)Assert.That(Quaternion.Angle(g.Compose().JointRotations[i],twice.Compose().JointRotations[i]),Is.LessThan(.05f));
        }
        [TestCase(.8f)] [TestCase(1f)] [TestCase(1.2f)]
        public void PreviewContactParityAndProxyReuse(float scale)
        {
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.SetActive(false);target.transform.SetPositionAndRotation(new Vector3(2,3,4),Quaternion.Euler(12,37,-22));target.transform.localScale=Vector3.one*2;
            var preview=new GripPreview();try
            {
                var g=Grip();preview.Show(target.transform,g,true,47,scale);var expected=GripPoseResolver.World(target.transform,GripPoseResolver.Resolve(g,47,scale));
                Assert.That(Vector3.Distance(expected.position,preview.Ghost.transform.position),Is.LessThan(.001));Assert.That(Quaternion.Angle(expected.rotation,preview.Ghost.transform.rotation),Is.LessThan(.5));
                var contact=preview.Ghost.transform.TransformPoint(g.contactWristLocal);var reference=GripPoseResolver.World(target.transform,GripPoseResolver.Resolve(g,47,1));Assert.That(Vector3.Distance(contact,reference.position+reference.rotation*g.contactWristLocal),Is.LessThan(.001));
                var proxy=preview.Geometry;var child=proxy.transform.GetChild(0);g.curl[1]=32;preview.Show(target.transform,g,true,47,scale);Assert.That(preview.Geometry,Is.SameAs(proxy));Assert.That(preview.Geometry.transform.GetChild(0),Is.SameAs(child));
            }finally{preview.Clear();UnityEngine.Object.DestroyImmediate(target);}
        }
        [Test] public void ClipboardRebaseKeepsDestinationAndShape()
        {
            var g=Grip();g.curl[1]=45;g.spread[2]=-12;var dest=Grip();dest.id="other";var result=GripClipboard.Rebase(g.Clone(),dest);Assert.That(result.id,Is.EqualTo("other"));Assert.That(result.basePose,Is.SameAs(dest.basePose));for(int i=0;i<g.jointDegrees.Length;i++)Assert.That(Quaternion.Angle(g.Compose().JointRotations[i],result.Compose().JointRotations[i]),Is.LessThan(.05));
        }
        [TestCase(Handedness.Left)] [TestCase(Handedness.Right)]
        public void JointHandleRoundTripPreservesShortcutsAndOtherJoints(Handedness side)
        {
            var g=Grip();g.side=side;g.curl[1]=100;g.spread[1]=-24;g.thumbOpposition=73;var before=g.Compose();
            for(int j=0;j<g.jointDegrees.Length;j++)
            {
                var copy=g.Clone();var desired=before.JointRotations[j]*Quaternion.Euler(25,11,-32);copy.jointDegrees[j]=GripClipboard.JointAdjustment(copy,j,desired);var after=copy.Compose();Assert.That(Quaternion.Angle(desired,after.JointRotations[j]),Is.LessThan(.05));
                for(int k=0;k<g.jointDegrees.Length;k++)if(k!=j)Assert.That(Quaternion.Angle(before.JointRotations[k],after.JointRotations[k]),Is.LessThan(.05));
            }
        }
        [Test] public void NamedGripManagementIsIndependentAndUndoable()
        {
            var go=new GameObject("Named grip test");var t=go.AddComponent<GripTarget>();t.grips.Add(Grip());var w=GripEditorWindow.Open(t);
            try
            {
                var field=typeof(GripEditorWindow).GetField("gripName",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);field.SetValue(w,"second");Undo.IncrementCurrentGroup();w.ManageGrip(1);Undo.FlushUndoRecordObjects();Assert.That(w.Draft.grips.Count,Is.EqualTo(2));Assert.That(t.grips.Count,Is.EqualTo(1));
                w.Edit(g=>g.curl[1]=72,"Independent fingers");Assert.That(w.Draft.grips[0].curl[1],Is.Zero);field.SetValue(w,"renamed");w.ManageGrip(2);Assert.That(w.Current.id,Is.EqualTo("renamed"));
                w.ManageGrip(3);Assert.That(w.Draft.grips.Count,Is.EqualTo(1));w.ManageGrip(3);Assert.That(w.Draft.grips.Count,Is.EqualTo(1),"Active runtime definition protected");
            }finally{w.DiscardChanges();w.Close();UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void DisabledOptionsPreserveLegacyWrist()
        {
            var g=Grip();g.axisEnabled=false;g.contactEnabled=false;var pose=GripPoseResolver.Resolve(g,90,1.2f);Assert.That(pose.position,Is.EqualTo(g.positionMetres));Assert.That(pose.rotation,Is.EqualTo(g.rotation));
        }
    }
}
