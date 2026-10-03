using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Tidebreak.Editor
{
    public static class PackageInstaller
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Install()
        {
            request=Client.AddAndRemove(new[]{"com.unity.modules.physics@1.0.0","com.unity.modules.screencapture@1.0.0","com.unity.modules.imageconversion@1.0.0"});
            deadline=EditorApplication.timeSinceStartup+180;
            EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(!request.IsCompleted) { if(EditorApplication.timeSinceStartup>deadline){Debug.LogError("Module installation timeout");EditorApplication.Exit(2);}return; }
            EditorApplication.update-=Poll;
            if(request.Status==StatusCode.Success){Debug.Log("TIDEBREAK_MODULES_READY");EditorApplication.Exit(0);}
            else {Debug.LogError(request.Error.message);EditorApplication.Exit(1);}
        }
    }
}
