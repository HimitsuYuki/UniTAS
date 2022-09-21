﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniTASPlugin;

public static class GameTracker
{
    /// <summary>
    /// Scene loading count status. 0 means there are no scenes loading, 1 means there is one scene loading, 2 means there are two scenes loading, etc.
    /// </summary>
    public static int LoadingSceneCount { get; private set; } = 0;
    /// <summary>
    /// Scene unloading count status. 0 means there are no scenes unloading, 1 means there is one scene unloading, 2 means there are two scenes unloading, etc.
    /// </summary>
    public static int UnloadingSceneCount { get; private set; } = 0;
    public static List<int> FirstObjIDs { get; } = new();
    public static List<int> DontDestroyOnLoadIDs { get; private set; } = new();
    static Dictionary<System.Type, List<KeyValuePair<FieldInfo, object>>> initialValues = new();

    public static void Init()
    {
        Object[] objs = Object.FindObjectsOfType(typeof(MonoBehaviour));
        foreach (Object obj in objs)
        {
            int id = obj.GetInstanceID();
            if (DontDestroyOnLoadIDs.Contains(id))
            {
                FirstObjIDs.Add(id);
            }
        }
        var pluginObj = Object.FindObjectOfType(typeof(Plugin));
        if (pluginObj == null)
            Plugin.Log.LogError("Plugin object not found, this should never happen");
        else
            FirstObjIDs.Add(pluginObj.GetInstanceID());

        // initial game state values
        var gameAssemblyNames = new string[] {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "Assembly-UnityScript",
            "Assembly-UnityScript-firstpass"
        };
        // excludes all types in the namespace
        var exclusionNamespaces = new string[]
        {
            "Steamworks*"
        };
        // game specific type exclusion
        var exclusionGameAndType = new Dictionary<string, List<string>>
        {
            { "It Steals", new List<string>()
                {
                    "SteamManager"
                }
            },
            { "ULTRAKILL", new List<string>()
                {
                    "SteamController"
                }
            }
        };

        // actual values to save depending on games
        var saveGameTypesFields = new Dictionary<string, List<KeyValuePair<string, List<string>>>>
        {
            { "Keep Talking and Nobody Explodes", new List<KeyValuePair<string, List<string>>>()
                {
                    new KeyValuePair<string, List<string>>("GameplayState", new List<string>()
                    {
                        "BombSeedToUse",
                    })
                }
            }
        };

        var gameAssemblies = AccessTools.AllAssemblies().Where(a => gameAssemblyNames.Contains(a.GetName().Name)).ToArray();
        var allGameTypes = gameAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !exclusionNamespaces
            .Any(ex => t.Namespace != null && (ex == t.Namespace || (ex.EndsWith("*") && t.Namespace.StartsWith(ex.Remove(ex.Length - 1, 1))))));
        if (exclusionGameAndType.ContainsKey(Helper.GameName()))
        {
            var excludeNames = exclusionGameAndType[Helper.GameName()];
            allGameTypes =
                allGameTypes.Where(t => !excludeNames
                .Any(ex => t.FullName == ex || ex.EndsWith("*") && t.FullName.StartsWith(ex.Remove(ex.Length - 1, 1))));
        }

        List<KeyValuePair<string, List<string>>> saveTypesAndFields = new();
        if (saveGameTypesFields.Keys.Contains(Helper.GameName()))
            saveTypesAndFields = saveGameTypesFields[Helper.GameName()];

        /*
         * BUG: THIS TYPE CRASHES THE PLUGIN
        [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    	[NativeClass("Vector2f")]
	    [Il2CppEagerStaticClassConstruction]
	    public struct UnityEngine.Vector2 : IEquatable<Vector2>, IFormattable
        */

        foreach (var gameType in allGameTypes)
        {
            // get all static fields
            var fields = AccessTools.GetDeclaredFields(gameType);
            var saveFieldsIndex = saveTypesAndFields.FindIndex(tAndf => tAndf.Key == gameType.FullName);

            foreach (var field in fields)
            {
                if (!field.IsStatic)
                    continue;

                var foundFieldIndex = -1;
                if (saveFieldsIndex > 0)
                    foundFieldIndex = saveTypesAndFields[saveFieldsIndex].Value.FindIndex(f => field.Name == f);

                if (foundFieldIndex < 0)
                {
                    Plugin.Log.LogDebug($"skipping saving field {gameType}.{field.Name}");
                    continue;
                }

                var fieldType = field.FieldType;
                if (!initialValues.ContainsKey(gameType))
                    initialValues.Add(gameType, new List<KeyValuePair<FieldInfo, object>>());

                // handling some types by hand since they crash AccessTools.MakeDeepCopy
                object objClone;
                var fieldValue = field.GetValue(null);
                switch (field.FieldType.FullName)
                {
                    case "UnityEngine.Vector2":
                        {
                            var vec = (Vector2)fieldValue;
                            objClone = new Vector2(vec.x, vec.y);
                            break;
                        }
                    case "UnityEngine.Vector2Int":
                        {
                            var type_ = AccessTools.TypeByName("UnityEngine.Vector2Int");
                            var constructor = AccessTools.Constructor(type_, new System.Type[] { typeof(int), typeof(int) });
                            objClone = constructor.Invoke(new object[] { AccessTools.Field(type_, "m_X"), AccessTools.Field(type_, "m_Y") });
                            break;
                        }
                    case "UnityEngine.Vector3":
                        {
                            var vec = (Vector3)fieldValue;
                            objClone = new Vector3(vec.x, vec.y, vec.z);
                            break;
                        }
                    case "UnityEngine.Vector3Int":
                        {
                            var type_ = AccessTools.TypeByName("UnityEngine.Vector3Int");
                            var constructor = AccessTools.Constructor(type_, new System.Type[] { typeof(int), typeof(int) });
                            objClone = constructor.Invoke(new object[] { AccessTools.Field(type_, "m_X"), AccessTools.Field(type_, "m_Y"), AccessTools.Field(type_, "m_Z") });
                            break;
                        }
                    case "UnityEngine.Vector4":
                        {
                            var vec = (Vector4)fieldValue;
                            objClone = new Vector4(vec.x, vec.y, vec.z, vec.w);
                            break;
                        }
                    default:
                        {
                            Plugin.Log.LogDebug($"saving field {gameType}.{field.Name}");
                            objClone = AccessTools.MakeDeepCopy(fieldValue, field.FieldType);
                            break;
                        }
                }
                initialValues[gameType].Add(new KeyValuePair<FieldInfo, object>(field, objClone));
            }
        }
    }

    public static void AsyncSceneLoad(AsyncOperation operation)
    {
        if (operation == null)
            return;
        if (Plugin.Instance == null)
        {
            Plugin.Log.LogError("Plugin is null, this should not happen, skipping scene load tracker");
            LoadingSceneCount = 0;
            return;
        }
        Plugin.Instance.StartCoroutine(AsyncSceneLoadWait(operation));
    }

    static System.Collections.IEnumerator AsyncSceneLoadWait(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        LoadingSceneCount--;
    }

    public static void AsyncSceneUnload(AsyncOperation operation)
    {
        if (operation == null)
            return;
        if (Plugin.Instance == null)
        {
            Plugin.Log.LogError("Plugin is null, this should not happen, skipping scene unload tracker");
            UnloadingSceneCount = 0;
            return;
        }
        Plugin.Instance.StartCoroutine(AsyncSceneUnloadWait(operation));
    }

    static System.Collections.IEnumerator AsyncSceneUnloadWait(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        UnloadingSceneCount--;
    }

    public static void DontDestroyOnLoadCall(Object @object)
    {
        if (@object == null)
            return;
        var id = @object.GetInstanceID();
        if (DontDestroyOnLoadIDs.Contains(id))
            return;
        DontDestroyOnLoadIDs.Add(id);
    }

    public static void DestroyObject(Object @object)
    {
        if (@object == null)
            return;
        DontDestroyOnLoadIDs.Remove(@object.GetInstanceID());
    }
}