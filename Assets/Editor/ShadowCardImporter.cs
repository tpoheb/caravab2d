using UnityEditor;

public static class ShadowCardImporter
{
    [MenuItem("Tools/Cards/Create Shadow Cards")]
    public static void CreateAll() => DocumentCardImporter.ImportEvents();
}
