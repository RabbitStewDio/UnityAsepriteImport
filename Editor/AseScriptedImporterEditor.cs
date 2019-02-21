using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace AseImport
{
  [CustomEditor(typeof(AseScriptedImporter), true)]
  public class AseScriptedImporterEditor : ScriptedImporterEditor
  {
    public AseScriptedImporterEditor()
    {
    }

    bool IsEnum(string name, out Enum value)
    {
      var fieldType = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
      if (fieldType == null)
      {
        value = default(Enum);
        return false;
      }

      if (fieldType.FieldType.IsEnum)
      {
        value = (Enum) fieldType.GetValue(target);
        return true;
      }

      value = default(Enum);
      return false;
    }

    public override void OnInspectorGUI()
    {
      Debug.Log("Target: " + target.GetType());
      var iterator = this.serializedObject.GetIterator();
      for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
      {
        var name = iterator.name;
        if (nameof(AseScriptedImporter.Alignment).Equals(name))
        {
          Debug.Log(iterator.name + " " + iterator.propertyType + " " + iterator.type);
          DrawEnum(iterator);
        }
        else
        {
          EditorGUILayout.PropertyField(iterator, true);
        }
      }

      this.ApplyRevertGUI();
    }

    void DrawEnum(SerializedProperty property)
    {
      var value = (SpriteAlignment) property.intValue;
      var enumNew = (SpriteAlignment) EditorGUILayout.EnumPopup(property.name, value);
      property.intValue = (int) enumNew;
    }

  }
}
