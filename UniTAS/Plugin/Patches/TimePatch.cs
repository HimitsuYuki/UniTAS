using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using UniTAS.Plugin.Interfaces.Patches.PatchTypes;
using UniTAS.Plugin.Services;
using UniTAS.Plugin.Services.VirtualEnvironment;
using UniTAS.Plugin.Utils;
using UnityEngine;

namespace UniTAS.Plugin.Patches;

[RawPatch]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public class TimePatch
{
    private static readonly IPatchReverseInvoker
        ReverseInvoker = Plugin.Kernel.GetInstance<IPatchReverseInvoker>();

    private static readonly VirtualEnvironment VirtualEnvironment =
        Plugin.Kernel.GetInstance<VirtualEnvironment>();

    private static bool CalledFromFixedUpdate()
    {
        var frames = new StackTrace().GetFrames();
        if (frames == null) return false;

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method?.Name is "FixedUpdate" && method.DeclaringType?.IsSubclassOf(typeof(MonoBehaviour)) is true)
                return true;
        }

        return false;
    }

    [HarmonyPatch(typeof(Time), nameof(Time.captureFramerate), MethodType.Setter)]
    private class set_captureFramerate
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix()
        {
            return ReverseInvoker.InnerCall();
        }

        private static void Postfix()
        {
            ReverseInvoker.Return();
        }
    }

    [HarmonyPatch(typeof(Time), "captureDeltaTime", MethodType.Setter)]
    private class set_captureDeltaTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix()
        {
            return ReverseInvoker.InnerCall();
        }

        private static void Postfix()
        {
            ReverseInvoker.Return();
        }
    }

    [HarmonyPatch(typeof(Time), "fixedUnscaledTime", MethodType.Getter)]
    private class get_fixedUnscaledTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            __result = (float)VirtualEnvironment.GameTime.FixedUnscaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "fixedUnscaledTimeAsDouble", MethodType.Getter)]
    private class get_fixedUnscaledTimeAsDouble
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref double __result)
        {
            __result = VirtualEnvironment.GameTime.FixedUnscaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "unscaledTime", MethodType.Getter)]
    private class get_unscaledTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static readonly Traverse fixedUnscaledTime =
            Traverse.Create(typeof(Time)).Property("fixedUnscaledTime");

        private static bool Prefix(ref float __result)
        {
            // When called from inside MonoBehaviour's FixedUpdate, it returns Time.fixedUnscaledTime
            __result = CalledFromFixedUpdate()
                ? fixedUnscaledTime.GetValue<float>()
                : (float)VirtualEnvironment.GameTime.UnscaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "unscaledTimeAsDouble", MethodType.Getter)]
    private class get_unscaledTimeAsDouble
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static readonly Traverse fixedUnscaledTimeAsDouble =
            Traverse.Create(typeof(Time)).Property("fixedUnscaledTimeAsDouble");

        private static bool Prefix(ref double __result)
        {
            // When called from inside MonoBehaviour's FixedUpdate, it returns Time.fixedUnscaledTimeAsDouble
            __result = CalledFromFixedUpdate()
                ? fixedUnscaledTimeAsDouble.GetValue<double>()
                : VirtualEnvironment.GameTime.UnscaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), nameof(Time.frameCount), MethodType.Getter)]
    private class get_frameCount
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static void Postfix(ref int __result)
        {
            var gameTime = VirtualEnvironment.GameTime;
            __result = (int)((ulong)__result - gameTime.FrameCountRestartOffset);
        }
    }

    [HarmonyPatch(typeof(Time), nameof(Time.renderedFrameCount), MethodType.Getter)]
    private class get_renderedFrameCount
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static void Postfix(ref int __result)
        {
            var gameTime = VirtualEnvironment.GameTime;
            __result = (int)((ulong)__result - gameTime.RenderedFrameCountOffset);
        }
    }

    [HarmonyPatch(typeof(Time), nameof(Time.realtimeSinceStartup), MethodType.Getter)]
    private class get_realtimeSinceStartup
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            if (ReverseInvoker.InnerCall())
            {
                return false;
            }

            __result = (float)VirtualEnvironment.GameTime.SecondsSinceStartUp;
            return false;
        }

        private static void Postfix()
        {
            ReverseInvoker.Return();
        }
    }

    [HarmonyPatch(typeof(Time), "realtimeSinceStartupAsDouble", MethodType.Getter)]
    private class get_realtimeSinceStartupAsDouble
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref double __result)
        {
            __result = VirtualEnvironment.GameTime.SecondsSinceStartUp;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), nameof(Time.time), MethodType.Getter)]
    private class get_time
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            __result = CalledFromFixedUpdate() ? Time.fixedTime : (float)VirtualEnvironment.GameTime.ScaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "timeAsDouble", MethodType.Getter)]
    private class get_timeAsDouble
    {
        private static readonly Traverse fixedTimeAsDouble =
            Traverse.Create(typeof(Time)).Property("fixedTimeAsDouble");

        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref double __result)
        {
            __result = CalledFromFixedUpdate()
                ? fixedTimeAsDouble.GetValue<double>()
                : VirtualEnvironment.GameTime.ScaledTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), nameof(Time.fixedTime), MethodType.Getter)]
    private class get_fixedTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            __result = (float)VirtualEnvironment.GameTime.ScaledFixedTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "fixedTimeAsDouble", MethodType.Getter)]
    private class get_fixedTimeAsDouble
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref double __result)
        {
            var gameTime = VirtualEnvironment.GameTime;
            __result = gameTime.ScaledFixedTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "fixedUnscaledDeltaTime", MethodType.Getter)]
    private class get_fixedUnscaledDeltaTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            __result = Time.fixedDeltaTime;
            return false;
        }
    }

    [HarmonyPatch(typeof(Time), "unscaledDeltaTime", MethodType.Getter)]
    private class get_unscaledDeltaTime
    {
        private static Exception Cleanup(MethodBase original, Exception ex)
        {
            return PatchHelper.CleanupIgnoreFail(original, ex);
        }

        private static bool Prefix(ref float __result)
        {
            __result = Time.deltaTime;
            return false;
        }
    }
}