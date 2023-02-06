using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using UniTASPlugin.GameEnvironment;
using UniTASPlugin.Patches.PatchGroups;
using UniTASPlugin.Patches.PatchTypes;
using UniTASPlugin.ReverseInvoker;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

// ReSharper disable ClassNeverInstantiated.Global

namespace UniTASPlugin.Patches.Modules;

[MscorlibPatch(true)]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public class SystemTimeOverrideModule
{
    private static readonly IPatchReverseInvoker ReverseInvoker =
        Plugin.Kernel.GetInstance<IPatchReverseInvoker>();

    private static readonly VirtualEnvironment VirtualEnvironment =
        Plugin.Kernel.GetInstance<VirtualEnvironment>();

    [MscorlibPatchGroup]
    private class AllVersions
    {
        [HarmonyPatch(typeof(DateTime), nameof(DateTime.Now), MethodType.Getter)]
        private class get_Now
        {
            private static Exception Cleanup(MethodBase original, Exception ex)
            {
                return PatchHelper.CleanupIgnoreFail(original, ex);
            }

            private static bool Prefix(ref DateTime __result)
            {
                if (ReverseInvoker.InnerCall())
                    return true;
                var gameTime = VirtualEnvironment.GameTime;
                __result = gameTime.CurrentTime;
                return false;
            }

            private static void Postfix()
            {
                ReverseInvoker.Return();
            }
        }

        [HarmonyPatch(typeof(Environment), nameof(Environment.TickCount), MethodType.Getter)]
        private class get_TickCount
        {
            private static Exception Cleanup(MethodBase original, Exception ex)
            {
                return PatchHelper.CleanupIgnoreFail(original, ex);
            }

            private static bool Prefix(ref int __result)
            {
                var gameTime = VirtualEnvironment.GameTime;
                __result = (int)(gameTime.RealtimeSinceStartup * 1000f);
                return false;
            }
        }
    }
}