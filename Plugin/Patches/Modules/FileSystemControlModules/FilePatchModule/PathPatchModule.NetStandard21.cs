using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UniTASPlugin.Patches.PatchGroups;

namespace UniTASPlugin.Patches.Modules.FileSystemControlModules.FilePatchModule;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public partial class PathPatchModule
{
    [MscorlibPatchGroup(null, null, "2.1.0.0")]
    private class NetStandard21
    {
        [HarmonyPatch]
        private class get_temp_path
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Path), "get_temp_path");
            }

            private static bool Prefix(ref string __result)
            {
                var rev = ReverseInvokerFactory.GetReverseInvoker();
                if (rev.Invoking) return true;

                FileSystemManager.GetTempPath(out __result);

                return false;
            }
        }

        [HarmonyPatch]
        private class GetFullPathName
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Path), "GetFullPathName", new[]
                {
                    typeof(string), typeof(int), typeof(StringBuilder), typeof(IntPtr)
                });
            }

            private static bool Prefix( /*string path, int numBufferChars, StringBuilder buffer,
                ref IntPtr lpFilePartOrNull, */ ref int __result)
            {
                var rev = ReverseInvokerFactory.GetReverseInvoker();
                if (rev.Invoking) return true;

                // TODO implement this
                __result = 0;

                return false;
            }
        }
    }
}