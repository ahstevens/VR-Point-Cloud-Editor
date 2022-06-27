/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

public class OnlineMapsPackageManager
{
    [MenuItem("GameObject/Infinity Code/Online Maps/Bolt Integration Kit", false, 1)]
    public static void ImportBoltIntegrationKit()
    {
        OnlineMapsEditorUtils.ImportPackage("Packages\\OnlineMaps-Bolt-Integration-Kit.unitypackage", 
            new OnlineMapsEditorUtils.Warning
            {
                title = "Bolt Integration Kit",
                message = "You have Bolt in your project?",
                ok = "Yes, I have a Bolt"
            },
            "Could not find Bolt Integration Kit."
        );
    }

    [MenuItem("GameObject/Infinity Code/Online Maps/Playmaker Integration Kit", false, 1)]
    public static void ImportPlayMakerIntegrationKit()
    {
        OnlineMapsEditorUtils.ImportPackage("Packages\\OnlineMaps-Playmaker-Integration-Kit.unitypackage", 
            new OnlineMapsEditorUtils.Warning
            {
                title = "Playmaker Integration Kit",
                message = "You have Playmaker in your project?",
                ok = "Yes, I have a Playmaker"
            },
            "Could not find Playmaker Integration Kit."
        );
    }
}