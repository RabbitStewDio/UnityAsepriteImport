using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AseImport
{
  public static class AnimationControllerImporter
  {
    public static AnimatorController CreateAnimatorControllerAtPath(string path)
    {
      AnimatorController animatorController = new AnimatorController();
      animatorController.name = Path.GetFileName(path);
      animatorController.AddLayer("Base Layer");
      AssetDatabase.CreateAsset(animatorController, path);
      return animatorController;
    }

    public static AnimatorController GenerateAnimController(string name, List<AnimationClip> generatedClips)
    {
      var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(name);
      if (controller != null)
      {
        Debug.Log("No valid animation controller: Has no valid layers.");
        return null;
      }

      if (controller == null)
      {
        Debug.Log("Attempting to create new controller at " + name);
        controller = CreateAnimatorControllerAtPath(name);
      }

      var layer = controller.layers[0];

      var stateMap = new Dictionary<string, AnimatorState>();
      PopulateStateTable(stateMap, layer.stateMachine);

      foreach (var pair in generatedClips)
      {
        var frameTag = pair.name;
        var clip = pair;

        if (string.IsNullOrEmpty(pair.name))
        {
          continue;
        }

        AnimatorState st;
        if (!stateMap.TryGetValue(frameTag, out st))
        {
          st = layer.stateMachine.AddState(frameTag);
        }

        st.motion = clip;
      }

      EditorUtility.SetDirty(controller);
      return controller;
    }

    static void PopulateStateTable(Dictionary<string, AnimatorState> table, AnimatorStateMachine machine)
    {
      if (machine == null)
      {
        throw new ArgumentNullException(nameof(machine));
      }

      foreach (var state in machine.states)
      {
        var animState = state.state;
        var name = animState.name;
        if (string.IsNullOrEmpty(name))
        {
          continue;
        }

        if (table.ContainsKey(name))
        {
          Debug.LogWarning("Duplicate state with name " + name + " in animator controller. Behaviour is undefined.");
        }
        else
        {
          table.Add(name, state.state);
        }
      }

      foreach (var subMachine in machine.stateMachines)
      {
        PopulateStateTable(table, subMachine.stateMachine);
      }
    }
  }
}