#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace XRStarter.Editor
{
    /// <summary>OpenXR 1.14.3 adds mandatory eye tracking for every Quest Pro target.
    /// Keep the demo's actual hands/controller contract, without changing package code.</summary>
    public sealed class StarterAndroidManifest : IPostGenerateGradleAndroidProject
    {
        // OVR's OpenXR processor runs at 99999 and removes absent optional tags.
        // Apply the merger exclusions afterwards so it cannot erase them.
        public int callbackOrder => 1000000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            // An experience that explicitly opts into eye tracking owns its permission policy.
            bool requested = config.eyeTrackingSupport != OVRProjectConfig.FeatureSupport.None ||
                settings.GetFeatures<OpenXRFeature>().Any(f => f.enabled &&
                    (f.GetType().Name.Contains("Eye") || f.GetType().Name.Contains("Gaze")));
            if (requested) return;

            string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
            var doc = XDocument.Load(manifest);
            XNamespace android = "http://schemas.android.com/apk/res/android";
            XNamespace tools = "http://schemas.android.com/tools";
            doc.Root.SetAttributeValue(XNamespace.Xmlns + "tools", tools.NamespaceName);
            RemoveThroughMerger(doc.Root, "uses-feature", "oculus.software.eye_tracking", android, tools);
            RemoveThroughMerger(doc.Root, "uses-permission", "com.oculus.permission.EYE_TRACKING", android, tools);
            doc.Save(manifest);
            Debug.Log("STARTER_MANIFEST: excluded unrequested Quest Pro eye-tracking requirement; hand tracking preserved.");
        }

        static void RemoveThroughMerger(XElement root, string element, string name, XNamespace android, XNamespace tools)
        {
            foreach (var old in root.Elements(element).Where(e => (string)e.Attribute(android + "name") == name).ToArray()) old.Remove();
            root.Add(new XElement(element, new XAttribute(android + "name", name), new XAttribute(tools + "node", "remove")));
        }
    }
}
#endif
