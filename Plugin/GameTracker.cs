﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniTASPlugin;

internal static class GameTracker
{
    internal static List<int> FirstObjIDs { get; } = new();
    internal static List<int> DontDestroyOnLoadIDs { get; private set; } = new();
    internal static Dictionary<System.Type, List<KeyValuePair<FieldInfo, object>>> InitialValues { get; private set; } = new();

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

        // initial game state values
        var gameAssemblyNames = new string[] {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "Assembly-UnityScript",
            "Assembly-UnityScript-firstpass"
        };
        // excludes all types
        var exclusionTypes = new string[]
        {
            "Steamworks*",
            "UniRx*",
            // ignoring vr stuff
            "OVRSimpleJSON*",
            "OVRManager*",
            "OVRInput*",
            "OVRGearVrControllerTest*",
            "OVRHandTest*",
            "OVRResources*",
            "OVRPlugin*",
            "OVRMixedReality*",
            "OVROverlay*",
            "OVRHaptics*",
            "OVRBoundary*",
            "OVRPlatformMenu*",
            "OVRRaycaster*",
            "OVR.OpenVR.OpenVR*",
            "GvrAudio*",
            "GvrControllerInput*",
            "GvrKeyboard*",
            "GvrVideoPlayerTexture*",
            "GvrExecuteEventsExtension*",
            "GvrMathHelpers*",
            "GvrDaydreamApi*",
            "GvrPointerGraphicRaycaster*",
            "UnityEngine.EventSystems.OVRPhysicsRaycaster",
            //
            "DarkTonic.MasterAudio*",
            // HACK: need to look into how this one works, its an input manager
            "InControl*",
        };
        // game specific type exclusion
        var exclusionGameAndType = new Dictionary<string, List<string>>
        {
            { "It Steals", new List<string>()
            {
                "SteamManager",
            }
            },
            { "Keep Talking and Nobody Explodes", new List<string>()
            {
                /*
                "Oculus.Platform*",
                "DigitalOpus.MB.Core*",
                "LTGUI*",
                "LeanTween*",
                "LTDescr*",
                "LTRect*",
                "LeanTest*",
                "BindingsExample*",
                "I2.Loc*",
                */
            }
            },
        };
        // game specific field exclusion
        var exclusionGameAndField = new Dictionary<string, List<string>>
        {
            { "Keep Talking and Nobody Explodes", new List<string>()
            {
                "InControl.TouchManager.OnSetup",
            }
            },
        };
        // TODO does System.Action affect stuff?

        var gameName = Helper.GameName();
        var gameAssemblies = AccessTools.AllAssemblies().Where(a => gameAssemblyNames.Contains(a.GetName().Name)).ToArray();
        var allGameTypes = gameAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !exclusionTypes
            .Any(ex => t.FullName != null && (ex == t.FullName || (ex.EndsWith("*") && t.FullName.StartsWith(ex.Remove(ex.Length - 1, 1))))));
        if (exclusionGameAndType.ContainsKey(gameName))
        {
            var excludeNames = exclusionGameAndType[gameName];
            allGameTypes =
                allGameTypes.Where(t => !excludeNames
                .Any(ex => t.FullName == ex || ex.EndsWith("*") && t.FullName.StartsWith(ex.Remove(ex.Length - 1, 1))));
        }
        var exclusionFields = new List<string>();
        if (exclusionGameAndField.ContainsKey(gameName))
            exclusionFields = exclusionGameAndField[gameName];

        /*
         * BUG: THIS TYPE CRASHES THE PLUGIN
        [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    	[NativeClass("Vector2f")]
	    [Il2CppEagerStaticClassConstruction]
	    public struct UnityEngine.Vector2 : IEquatable<Vector2>, IFormattable
        */

        //var fieldTypeIgnore = new List<System.Type>();
        foreach (var gameType in allGameTypes)
        {
            // get all static fields
            var fields = AccessTools.GetDeclaredFields(gameType);

            foreach (var field in fields)
            {
                var fieldName = $"{gameType.FullName}.{field.Name}";
                if (!field.IsStatic || field.IsLiteral || field.IsInitOnly)
                    continue;

                // check instance field
                var fieldType = field.FieldType;
                if (fieldType == gameType)
                {
                    Plugin.Log.LogDebug($"Detected instance field: {fieldName}, skipping");
                    continue;
                }
                // check field exclusion
                if (exclusionFields.Contains(fieldName))
                {
                    Plugin.Log.LogDebug($"Exclusion fields contains {fieldName}");
                    continue;
                }
                // check field type exclusion
                /*
                if (fieldTypeIgnore.Contains(fieldType))
                {
                    Plugin.Log.LogDebug($"Ignoring field {fieldName} for safety");
                    continue;
                }
                */

                if (!InitialValues.ContainsKey(gameType))
                    InitialValues.Add(gameType, new List<KeyValuePair<FieldInfo, object>>());

                // handling some types by hand since they crash AccessTools.MakeDeepCopy
                // HACK make my own fixed version of this method
                object objClone = null;
                var failedClone = false;
                // we can't get value of an instance field, it crashes everything
                object fieldValue = null;
                try
                {
                    fieldValue = field.GetValue(null);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"failed to get field value for {fieldName}, ex: {ex}");
                    continue;
                }
                {
                    string fieldValueString;
                    if (fieldValue == null)
                        fieldValueString = "null";
                    // if its an array, convert all values to string
                    /*else if (fieldValue.GetType().IsArray)
                    {
                        Plugin.Log.LogDebug($"trying to convert array type {fieldValue.GetType()} to string");
                        try
                        {
                            fieldValueString = string.Join(", ", ((System.Collections.IEnumerable)fieldValue).Cast<object>()
                                .Select(x => x.ToString())
                                .ToArray());
                        }
                        catch (System.Exception)
                        {
                            Plugin.Log.LogDebug("it failed");
                            fieldValueString = fieldValue.ToString();
                        }
                        fieldValueString = fieldValue.ToString();
                    }*/
                    else
                        fieldValueString = fieldValue.ToString();
                    Plugin.Log.LogDebug($"cloning field {fieldName} with value {fieldValueString}");
                    //System.Threading.Thread.Sleep(50);

                    try
                    {
                        if (fieldValue == null || !typeof(System.Collections.IEnumerable).IsAssignableFrom(fieldType))
                        {
                            objClone = Helper.MakeDeepCopy(fieldValue, fieldType);
                        }
                        /*
                        else if (fieldValue == null)
                        {
                            Plugin.Log.LogDebug("field is null, skipping for safety");
                            continue;
                        }
                        */
                        else
                        {
                            Plugin.Log.LogDebug("skipping collection convertion for now");
                            continue;
                            /*
                            // manually clone field if its a collection
                            var fieldValueAsCollection = ((System.Collections.IEnumerable)fieldValue).Cast<object>();
                            var clonedCollection = new List<object>();
                            foreach (var value in fieldValueAsCollection)
                            {
                                clonedCollection.Add(Helper.MakeDeepCopy(value, value.GetType()));
                            }

                            // convert List to appropriate type if needed to
                            switch (fieldType.GetGenericTypeDefinition().FullName)
                            {
                                case "System.Collections.Generic.Stack`1":
                                    {
                                        var stackValue = new Stack<object>(clonedCollection);
                                        objClone = stackValue;
                                    }
                                    break;
                                default:
                                    {
                                        objClone = clonedCollection;
                                        Plugin.Log.LogDebug($"field type: {fieldType.GetGenericTypeDefinition().FullName}");
                                    }
                                    break;
                            }
                            */
                        }
                    }
                    catch (Exceptions.DeepCopyMaxRecursion)
                    {
                        failedClone = true;
                        Plugin.Log.LogWarning($"max recursion reached, excluding field type {fieldType} from deep copy");
                        //fieldTypeIgnore.Add(fieldType);
                    }
                    catch (System.Exception ex)
                    {
                        failedClone = true;
                        Plugin.Log.LogWarning($"failed to clone field, ex: {ex.Message}");
                    }
                }
                if (!failedClone)
                    InitialValues[gameType].Add(new KeyValuePair<FieldInfo, object>(field, objClone));
                else
                    failedClone = false;
            }
        }
    }

    public static void LateUpdate()
    {
        asyncSceneLoads.Clear();
    }

    struct AsyncSceneLoadData
    {
        public string sceneName;
        public int sceneBuildIndex;
        public object parameters;

        public AsyncSceneLoadData(string sceneName, int sceneBuildIndex, object parameters)
        {
            this.sceneName = sceneName;
            this.sceneBuildIndex = sceneBuildIndex;
            this.parameters = parameters;
        }
    }

    static Queue<AsyncSceneLoadData> asyncSceneLoads = new();
    static Queue<AsyncSceneLoadData> asyncSceneLoadsStall = new();

    // TODO unload too?
    public static void AsyncSceneLoad(string sceneName, int sceneBuildIndex, object parameters)
    {
        asyncSceneLoads.Enqueue(new AsyncSceneLoadData(sceneName, sceneBuildIndex, parameters));
    }

    public static void AllowSceneActivation(bool allow)
    {
        if (allow)
        {
            if (asyncSceneLoadsStall.Count == 0)
                return;
            // TODO different unity versions
            var sceneToLoad = asyncSceneLoadsStall.Dequeue();
            var sceneManager = Traverse.CreateWithType("UnityEngine.SceneManagement.SceneManager");
            var loadSceneParameters = AccessTools.TypeByName("UnityEngine.SceneManagement.LoadSceneParameters");
            var loadInternal = sceneManager.Method("LoadSceneAsyncNameIndexInternal", new System.Type[] { typeof(string), typeof(int), loadSceneParameters, typeof(bool) });
            loadInternal.GetValue(new object[] { sceneToLoad.sceneName, sceneToLoad.sceneBuildIndex, sceneToLoad.parameters, true });
        }
        else
        {
            if (asyncSceneLoads.Count > 0)
                asyncSceneLoadsStall.Enqueue(asyncSceneLoads.Dequeue());
        }
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