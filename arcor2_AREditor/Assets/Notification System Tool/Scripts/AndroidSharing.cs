using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;

namespace Base
{
    public class AndroidSharing
    {
        private string _shareEmailAddress = "kiko.klein@gmail.com";
        private string _fileName;
        public void ShareTextAsLogFile(string textToShare, string fileName = "error_log.log")
        {
            if (Application.platform == RuntimePlatform.Android)
            {
#if UNITY_ANDROID
                // Creaton .log file
                _fileName = fileName;
                string filePath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(filePath, textToShare);


                //Creation intent for sharing file
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");

                intent.Call<AndroidJavaObject>("setAction", intent.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");

                // URI creation for file with FileProvider
                using AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath);
                using AndroidJavaClass fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");
                string authority = Application.identifier + ".provider";
                using AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, file);

                // Added URI to intent and run share dialog
                intent.Call<AndroidJavaObject>("putExtra", intent.GetStatic<string>("EXTRA_STREAM"), uri);
                intent.Call<AndroidJavaObject> ("putExtra", intent.GetStatic<string> ("EXTRA_BCC"), _shareEmailAddress);

                intent.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
                AndroidJavaObject chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Share Error Log");
                currentActivity.Call("startActivity", chooser);
#endif
            }
            else
            {
                Debug.LogWarning("AndroidSharing.ShareTextAsLogFile() is only available on Android devices.");
            }
        }

        public IEnumerator SaveLogFileToDevice(string logContent, string fileName = "error_log.log")
        {
            string filePath;

            if (Application.platform == RuntimePlatform.Android)
            {
                // For Android
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                {
                    Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                    yield return new WaitForSeconds(0.5f);
                }

                if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                {
                    using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
                    {
                        using (AndroidJavaObject downloadsFolder = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", environment.GetStatic<string>("DIRECTORY_DOWNLOADS")))
                        {
                            filePath = Path.Combine(downloadsFolder.Call<string>("getAbsolutePath"), fileName);
                            File.WriteAllText(filePath, logContent);
                            Debug.Log("LogFile Saved at: " + filePath);
                            ShowAndroidNotification("Downloading completed", "Log saved at: " + filePath);
                        }
                    }
                }
                else
                {
                    Debug.LogError("LogFile Not Saved - External Storage Write Permission Denied");
                }
            }
            else
            {
                // For other platforms
                filePath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(filePath, logContent);
                Debug.Log("LogFile Saved at: " + filePath);
            }
        }

        private void ShowAndroidNotification(string title, string message)
        {
            if (Application.platform != RuntimePlatform.Android) return;

            using (AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass toastClass = new("android.widget.Toast"))
                    {
                        AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", context, message, toastClass.GetStatic<int>("LENGTH_LONG"));
                        toastObject.Call("show");
                    }
                }
            }
        }

    }


}
