using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XRStarter.Editor
{
    [InitializeOnLoad]
    public static class StarterAutomation
    {
        const string Lease="XRStarter.Automation.PlayLease";
        static StarterAutomation()
        {
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseString(Lease);};
        }
        public static string Execute(string action,string requestId,string token)
        {
            if(SceneManager.GetActiveScene().path!=DemoBuilder.ScenePath)throw new InvalidOperationException("This bounded action requires the starter demo to be active.");
            switch(action)
            {
                case "starter_validate": StarterValidation.ValidateLoadedDemo();return "Editor structure validated. See Reports/EDITOR_VALIDATION.md.";
                case "starter_capture": StarterValidation.CaptureLoadedDemo();return "PNG written under Reports/Screenshots. Open and inspect it; saving alone is not visual validation.";
                case "starter_enter_play":
                    if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play Mode already belongs to another session. It will not be restarted or taken over.");
                    for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: save/discard explicitly before automated Play.");
                    SessionState.SetString(Lease,requestId);EditorApplication.isPlaying=true;return "Play requested. Poll status. SessionToken="+requestId;
                case "starter_exit_play":
                case "starter_reset_demo":
                    if(!EditorApplication.isPlaying||string.IsNullOrEmpty(token)||SessionState.GetString(Lease,"")!=token)throw new InvalidOperationException("A matching automation-owned Play session token is required. User-owned Play is preserved.");
                    if(action=="starter_exit_play"){EditorApplication.isPlaying=false;return "Owned Play session exit requested.";}
                    UnityEngine.Object.FindObjectOfType<AssemblyExperience>().ResetExperience();return "Demo reset within owned Play session.";
                default: throw new InvalidOperationException("Unsupported starter action.");
            }
        }
    }
}
