﻿using System;
using System.Reflection;
using UniTASPlugin.UnityHooks.Helpers;

namespace UniTASPlugin.UnityHooks;

internal class MonoBehavior : Base<MonoBehavior>
{
    static MethodInfo stopAllCoroutines;

    protected override void InitByUnityVersion(Type objType)
    {
        stopAllCoroutines = objType.GetMethod("StopAllCoroutines", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, null);
    }

    internal static void StopAllCoroutines(object instance)
    {
        stopAllCoroutines.Invoke(instance, null);
    }
}
