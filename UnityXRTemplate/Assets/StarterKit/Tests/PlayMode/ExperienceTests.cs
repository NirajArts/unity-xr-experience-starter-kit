using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace XRStarter.Tests
{
    public class ExperienceTests
    {
        AssemblyExperience experience;
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("ExperienceDemo",LoadSceneMode.Single);
            yield return null;yield return null;
            experience=Object.FindObjectOfType<AssemblyExperience>();
            Assert.NotNull(experience);experience.motionSeconds=.12f;
        }
        void Event(ExperiencePart part,int id,PointerEventType type,Vector3 position)
        { part.grabbable.ProcessPointerEvent(new PointerEvent(id,type,new Pose(position,Quaternion.identity))); }
        void Grab(ExperiencePart p,int id)
        {Event(p,id,PointerEventType.Hover,p.transform.position);Event(p,id,PointerEventType.Select,p.transform.position);}
        void Release(ExperiencePart p,int id)
        {Event(p,id,PointerEventType.Unselect,p.transform.position);Event(p,id,PointerEventType.Unhover,p.transform.position);}

        [UnityTest] public IEnumerator SDKGrabMovesReleaseSeatsAndRegrabVacates()
        {
            var p=experience.parts[0];Grab(p,71);Assert.IsTrue(p.Held);
            var desired=experience.slots[0].matingPose.position;
            Event(p,71,PointerEventType.Move,desired);yield return null;
            Assert.That(Vector3.Distance(p.transform.position,desired),Is.LessThan(.015f));
            Release(p,71);yield return null;yield return null;
            Assert.AreEqual(PartState.Seated,p.State);Assert.AreSame(p,experience.slots[0].Occupant);
            Grab(p,72);yield return null;Assert.IsNull(experience.slots[0].Occupant);Release(p,72);
        }
        [UnityTest] public IEnumerator TwoPointerGrabDoesNotScaleOrJumpOnHandover()
        {
            var p=experience.parts[0];Grab(p,81);var position=p.transform.position;
            Event(p,82,PointerEventType.Select,position+Vector3.right*.15f);
            Event(p,82,PointerEventType.Move,position+Vector3.right*.3f);yield return null;
            Assert.That(Vector3.Distance(p.transform.localScale,Vector3.one),Is.LessThan(.0001f));
            var before=p.transform.position;Release(p,82);yield return null;
            Assert.That(Vector3.Distance(before,p.transform.position),Is.LessThan(.02f));Release(p,81);
        }
        [UnityTest] public IEnumerator ResetCancelsAnActiveSDKSelection()
        {
            var p=experience.parts[1];Grab(p,93);experience.ResetExperience();yield return null;
            Assert.IsFalse(p.Held);Assert.AreEqual(PartState.Free,p.State);
            Assert.That(Vector3.Distance(p.transform.position,p.tableHome.position),Is.LessThan(.03f));
            Assert.IsTrue(p.handGrab.enabled&&p.controllerGrab.enabled&&p.grabbable.enabled);
        }
        [UnityTest] public IEnumerator WrongAndOccupiedSlotsRejectParts()
        {
            Assert.IsFalse(experience.parts[0].Seat(experience.slots[1]));
            Assert.IsTrue(experience.parts[0].Seat(experience.slots[0]));
            var other=experience.parts[1];string id=other.partId;other.partId=experience.parts[0].partId;
            Assert.IsFalse(other.Seat(experience.slots[0]));other.partId=id;yield return null;
        }
        [UnityTest] public IEnumerator FallenPartRecoversEvenWhenRestingOnFloor()
        {
            var p=experience.parts[2];p.transform.position=p.workspace.TransformPoint(new Vector3(.5f,.05f,.4f));
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(Vector3.Distance(p.transform.position,p.tableHome.position),Is.LessThan(.06f));
        }
        [UnityTest] public IEnumerator ControlsCompleteExplodeInspectAndReset()
        {
            var buttons=Object.FindObjectsOfType<Button>();
            buttons.Single(b=>b.name=="Reassemble").onClick.Invoke();yield return new WaitForSeconds(.25f);
            Assert.AreEqual(3,experience.SeatedCount);Assert.IsFalse(experience.Busy);Assert.AreEqual(1,experience.progressFill.fillAmount);
            buttons.Single(b=>b.name=="Explode").onClick.Invoke();yield return new WaitForSeconds(.25f);
            Assert.AreEqual(0,experience.SeatedCount);
            for(int i=0;i<3;i++)Assert.That(Vector3.Distance(experience.parts[i].transform.position,experience.explodedPoses[i].position),Is.LessThan(.001f));
            buttons.Single(b=>b.name=="Inspect").onClick.Invoke();yield return null;
            Assert.That(Vector3.Distance(experience.SelectedPart.transform.position,experience.inspectionPose.position),Is.LessThan(.001f));
            buttons.Single(b=>b.name=="Reset all").onClick.Invoke();yield return null;Assert.AreEqual(0,experience.SeatedCount);
        }
        [UnityTest] public IEnumerator RotatedWorkspacePreservesMatingAndResetFrames()
        {
            var frame=experience.parts[0].workspace;frame.SetPositionAndRotation(new Vector3(2,0,1),Quaternion.Euler(0,53,0));
            experience.Reassemble();yield return new WaitForSeconds(.25f);
            for(int i=0;i<3;i++)Assert.That(Vector3.Distance(experience.parts[i].transform.position,experience.slots[i].matingPose.position),Is.LessThan(.001f));
            experience.ResetExperience();yield return null;
            foreach(var p in experience.parts)Assert.That(Vector3.Distance(p.transform.position,p.tableHome.position),Is.LessThan(.04f));
        }
        [UnityTest] public IEnumerator MechanicalFeedbackDoesNotDependOnGuidance()
        {
            experience.narration.SetGuided(false);int before=experience.feedback.PlayedCount;
            Grab(experience.parts[0],101);yield return null;
            Assert.Greater(experience.feedback.PlayedCount,before);Release(experience.parts[0],101);
        }
        [UnityTest] public IEnumerator ResetInterruptsAnimationWithoutLeavingLockedParts()
        {
            experience.motionSeconds=3;experience.Reassemble();yield return null;experience.ResetExperience();yield return null;
            Assert.IsFalse(experience.Busy);foreach(var p in experience.parts){Assert.AreEqual(PartState.Free,p.State);Assert.IsTrue(p.grabbable.enabled);Assert.IsNull(p.Slot);}
        }
        [UnityTest] public IEnumerator WorldUIRaycastFindsAndInvokesTheVisibleControl()
        {
            Assert.AreEqual(1, Object.FindObjectsOfType<EventSystem>().Length, "Exactly one input mode owns UI events.");
            var button=Object.FindObjectsOfType<Button>().Single(b=>b.name=="Reassemble");
            var preview=Object.FindObjectOfType<DesktopPreview>();Canvas.ForceUpdateCanvases();
            var eventData=new PointerEventData(EventSystem.current){position=preview.desktopCamera.WorldToScreenPoint(button.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(eventData,hits);
            Assert.IsTrue(hits.Any(h=>h.gameObject==button.gameObject),"The visible world-space button must be raycastable through the active UI module.");
            ExecuteEvents.Execute(button.gameObject,eventData,ExecuteEvents.pointerClickHandler);
            yield return new WaitUntil(()=>!experience.Busy);Assert.AreEqual(3,experience.SeatedCount);
        }
        [UnityTest] public IEnumerator CaptureDemoStates()
        {
#if UNITY_EDITOR
            var camera=Object.FindObjectOfType<DesktopPreview>().desktopCamera;
            yield return new WaitForSeconds(.2f);Capture(camera,"01-staged");
            experience.Reassemble();yield return new WaitUntil(()=>!experience.Busy);yield return null;Capture(camera,"02-assembled");
            experience.Explode();yield return new WaitUntil(()=>!experience.Busy);yield return null;Capture(camera,"03-exploded");
            experience.InspectSelected();yield return null;Capture(camera,"04-inspection");
#else
            yield return null;
#endif
        }
        static void Capture(Camera camera,string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var target=new RenderTexture(1600,1000,24);var old=camera.targetTexture;var active=RenderTexture.active;
            try
            {Canvas.ForceUpdateCanvases();camera.targetTexture=target;camera.Render();RenderTexture.active=target;var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Reports/Screenshots"));Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,name+".png"),texture.EncodeToPNG());Object.Destroy(texture);}
            finally{camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(target);}
        }
    }
}
