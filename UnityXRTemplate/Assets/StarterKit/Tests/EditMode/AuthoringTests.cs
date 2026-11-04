using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using XRStarter.Editor;

namespace XRStarter.Tests
{
    public class AuthoringTests
    {
        [Test] public void SavedDemoPassesReferenceAndXRChecks()
        { EditorSceneManager.OpenScene(DemoBuilder.ScenePath);StarterValidation.ValidateLoadedDemo(); }

        [Test] public void AuthoredWristFitPlacesTheObjectAtThePhysicalWrist()
        {
            var wrist=new Pose(new Vector3(.3f,1.2f,-.4f),Quaternion.Euler(25,35,15));
            var grip=new Pose(new Vector3(.03f,-.04f,.08f),Quaternion.Euler(0,90,20));
            var fit=AuthoredGripTransformer.Fit(wrist,grip);
            Assert.That(Vector3.Distance(fit.position+fit.rotation*grip.position,wrist.position),Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(fit.rotation*grip.rotation,wrist.rotation),Is.LessThan(.01f));
        }
        [Test] public void PairFitStaysCenteredWithoutChangingRigidHandleSpacing()
        {
            var l=new Pose(new Vector3(-.3f,1,0),Quaternion.identity);var r=new Pose(new Vector3(.5f,1,0),Quaternion.identity);
            var lg=new Pose(Vector3.left*.2f,Quaternion.identity);var rg=new Pose(Vector3.right*.2f,Quaternion.identity);
            var fit=AuthoredGripTransformer.FitPair(l,lg,r,rg);
            Assert.That(fit.position.x,Is.EqualTo(.1f).Within(.0001f));
            Assert.That(Vector3.Distance(fit.position+fit.rotation*lg.position,fit.position+fit.rotation*rg.position),Is.EqualTo(.4f).Within(.0001f));
        }
    }
}
