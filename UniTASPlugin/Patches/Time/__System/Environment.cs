﻿using HarmonyLib;
using System;

namespace UniTASPlugin.Patches.Time.__System;

[HarmonyPatch(typeof(Environment), nameof(Environment.TickCount), MethodType.Getter)]
class TickCountGetter
{
    static bool Prefix(ref int __result)
    {
        if (TAS.Main.Running)
        {
            var totalSeconds = TAS.Main.Time;
            var totalMilliseconds = totalSeconds * 1000;
            __result = (int)totalMilliseconds;

            return false;
        }

        return true;
    }
}