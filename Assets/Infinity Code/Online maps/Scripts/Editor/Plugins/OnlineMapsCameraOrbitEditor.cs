/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

[CustomEditor(typeof(OnlineMapsCameraOrbit), true)]
public class OnlineMapsCameraOrbitEditor : OnlineMapsFormattedEditor
{
    public SerializedProperty adjustTo;
    public SerializedProperty adjustToGameObject;
    public SerializedProperty distance;
    public SerializedProperty maxRotationX;
    public SerializedProperty rotation;
    public SerializedProperty speed;
    public SerializedProperty lockTilt;
    public SerializedProperty lockPan;

    protected override void CacheSerializedFields()
    {
        adjustTo = serializedObject.FindProperty("adjustTo");
        adjustToGameObject = serializedObject.FindProperty("adjustToGameObject");
        distance = serializedObject.FindProperty("distance");
        maxRotationX = serializedObject.FindProperty("maxRotationX");
        rotation = serializedObject.FindProperty("rotation");
        speed = serializedObject.FindProperty("speed");
        lockTilt = serializedObject.FindProperty("lockTilt");
        lockPan = serializedObject.FindProperty("lockPan");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create(adjustTo);
        rootLayoutItem.Create(adjustToGameObject).OnValidateDraw += () => adjustTo.enumValueIndex == (int)OnlineMapsCameraAdjust.gameObject;
        rootLayoutItem.Create(distance);
        rootLayoutItem.Create(maxRotationX);
        rootLayoutItem.Create(rotation);
        rootLayoutItem.Create(speed);
        rootLayoutItem.Create(lockTilt);
        rootLayoutItem.Create(lockPan);
    }
}