﻿using System.Collections.Generic;
using UnityEngine;

namespace UniTASPlugin.VersionSafeWrapper;

internal static class AssetBundleRequestWrap
{
    public static Dictionary<ulong, KeyValuePair<Object, Object[]>> InstanceTracker { get; private set; } = new();

    public static void NewFakeInstance(AsyncOperationWrap wrap, Object obj)
    {
        InstanceTracker.Add(wrap.UID, new KeyValuePair<Object, Object[]>(obj, null));
    }

    public static void NewFakeInstance(AsyncOperationWrap wrap, Object[] objs)
    {
        InstanceTracker.Add(wrap.UID, new KeyValuePair<Object, Object[]>(null, objs));
    }

    public static void FinalizeCall(ulong uid)
    {
        InstanceTracker.Remove(uid);
    }
}