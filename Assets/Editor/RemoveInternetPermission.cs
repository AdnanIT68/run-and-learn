using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Run and Learn never goes online, and a children's app should not ask for
    /// the network. Unity adds android.permission.INTERNET to every build, so the
    /// launcher manifest tells the Gradle merge to drop it again.
    /// </summary>
    internal class RemoveInternetPermission : IPostGenerateGradleAndroidProject
    {
        private const string Removal =
            "<uses-permission android:name=\"android.permission.INTERNET\" tools:node=\"remove\" />";

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string manifest = Path.Combine(unityLibraryPath, "..", "launcher", "src", "main", "AndroidManifest.xml");

            if (!File.Exists(manifest))
            {
                Debug.LogWarning("[RemoveInternetPermission] Launcher manifest not found: " + manifest);
                return;
            }

            string xml = File.ReadAllText(manifest);

            if (xml.Contains("android.permission.INTERNET\" tools:node=\"remove\""))
            {
                return;
            }

            if (!xml.Contains("xmlns:tools="))
            {
                xml = xml.Replace("<manifest ", "<manifest xmlns:tools=\"http://schemas.android.com/tools\" ");
            }

            int application = xml.IndexOf("<application");
            xml = application >= 0
                ? xml.Insert(application, Removal + "\n  ")
                : xml.Replace("</manifest>", "  " + Removal + "\n</manifest>");
            File.WriteAllText(manifest, xml);
        }
    }
}
