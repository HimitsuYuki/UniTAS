﻿using Core.TAS;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace v2021_2_14.Patches.TASInput.__UnityEngine.__EventSystems;

#pragma warning disable IDE1006

[HarmonyPatch(typeof(EventSystem), "OnApplicationFocus")]
class OnApplicationFocus
{
    static void Prefix(ref bool hasFocus)
    {
        if (Main.Running)
        {
            // we dont want to lose focus while running the TAS
            hasFocus = true;
        }
    }
}

[HarmonyPatch(typeof(EventSystem), nameof(EventSystem.isFocused), MethodType.Getter)]
class isFocusedGetter
{
    static bool Prefix(ref bool __result)
    {
        if (Main.Running)
        {
            __result = true;

            return false;
        }

        return true;
    }
}