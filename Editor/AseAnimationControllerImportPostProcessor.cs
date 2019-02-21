using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AseImport
{
  public class AseAnimationControllerImportPostProcessor : AssetPostprocessor
  {
    static void OnPostprocessAllAssets(string[] importedAssets,
                                       string[] deletedAssets,
                                       string[] movedAssets,
                                       string[] movedFromAssetPaths)
    {
      if (true)
      {
        return;
      }

      foreach (string str in importedAssets)
      {
        if (IsAseFile(str))
        {
          Debug.Log("Reimported Asset: " + str);
          HandleImport(str);
        }
      }

      foreach (string str in deletedAssets)
      {
        if (IsAseFile(str))
        {
          Debug.Log("Deleted Asset: " + str);

          var controller = DeriveControllerPath(str);
          AssetDatabase.DeleteAsset(controller);
        }
      }

      for (int i = 0; i < movedAssets.Length; i++)
      {
        if (IsAseFile(movedAssets[i]))
        {
          Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
          HandleMove(movedAssets[i], movedFromAssetPaths[i]);
        }
      }
    }

    static void HandleImport(string name)
    {
      if (name == null)
      {
        throw new ArgumentNullException();
      }

      var allAnimations = GetSubObjectsOfTypeAsType<AnimationClip>(name);
      if (allAnimations.Count == 0)
      {
        return;
      }

      var path = DeriveControllerPath(name);
      Debug.Log("Path to import: " + name);
      AnimationControllerImporter.GenerateAnimController(path, allAnimations);
    }

    public static List<T1> GetSubObjectsOfTypeAsType<T1>(string asset) where T1 : Object
    {
      var objs = AssetDatabase.LoadAllAssetsAtPath(asset);
      return objs.OfType<T1>().ToList();
    }

    static bool IsAseFile(string name)
    {
      return name.EndsWith(".ase") || name.EndsWith(".aseprite");
    }

    static string GetPathWithoutExtension(string path)
    {
      int length;
      if ((length = path.LastIndexOf('.')) == -1)
      {
        return path;
      }

      return path.Substring(0, length);
    }

    static string DeriveControllerPath(string path)
    {
      return GetPathWithoutExtension(path) + "AnimationController.controller";
    }

    static void HandleMove(string toPath, string fromPath)
    {
      var fromControllerPath = DeriveControllerPath(fromPath);
      var toControllerPath = DeriveControllerPath(toPath);
      if (AssetDatabase.LoadAssetAtPath<AnimatorController>(fromControllerPath) != null)
      {
        AssetDatabase.MoveAsset(fromControllerPath, toControllerPath);
      }
    }
  }
}
