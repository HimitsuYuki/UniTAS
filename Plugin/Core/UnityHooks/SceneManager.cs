﻿using Core.UnityHooks.Helpers;
using System;
using System.Reflection;

namespace Core.UnityHooks;

internal class SceneManager : Base<SceneManager>
{
    static MethodInfo loadScene__int;

    protected override void InitByUnityVersion(Type objType, UnityVersion version)
    {
        switch (version)
        {
            case UnityVersion.v2021_2_14:
                loadScene__int = objType.GetMethod("LoadScene", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(int) }, null);
                break;
            case UnityVersion.v2018_4_25:
                loadScene__int = objType.GetMethod("LoadScene", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(int) }, null);
                break;
        }
    }

    internal static void LoadScene(int sceneBuildIndex)
    {
        loadScene__int.Invoke(null, new object[] { sceneBuildIndex });
    }
}