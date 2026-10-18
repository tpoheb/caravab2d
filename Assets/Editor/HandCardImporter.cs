using UnityEditor;

/// <summary>Повторный импорт актуальных карт из документного набора, с сохранением иконок и GUID.</summary>
public static class HandCardImporter
{
    [MenuItem("Tools/1000 Roads/Import Hand Cards")]
    public static void ImportHandCards() => DocumentCardImporter.ImportHandCards();
}
