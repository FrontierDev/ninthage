using System;
using System.Linq;
using UnityEditor;
using Game.Shared.Data;

public static class DialogueEditorUtils
{
    public static string GenerateId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public static void MarkDirty(DialogueDefinition def)
    {
        if (def == null) return;
        Undo.RecordObject(def, "Dialogue Change");
        EditorUtility.SetDirty(def);
    }
}
