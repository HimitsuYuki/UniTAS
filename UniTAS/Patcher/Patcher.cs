﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using Mono.Cecil;
using UniTAS.Patcher.PreloadPatchUtils;

namespace UniTAS.Patcher;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class Patcher
{
    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(Utils.ProjectAssembly);

    // List of assemblies to patch
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static IEnumerable<string> TargetDLLs => PreloadPatcherProcessor.TargetDLLs;

    private static readonly PreloadPatcherProcessor PreloadPatcherProcessor = new();

    // Called before the assemblies are patched
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void Initialize()
    {
        Logger.LogInfo($"Found {PreloadPatcherProcessor.PreloadPatchers.Length} preload patchers");
        Logger.LogDebug($"Target DLLs: {string.Join(", ", PreloadPatcherProcessor.TargetDLLs)}");
    }

    // Patches the assemblies
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void Patch(ref AssemblyDefinition assembly)
    {
        foreach (var patcher in PreloadPatcherProcessor.PreloadPatchers)
        {
            patcher.Patch(ref assembly);
        }
    }
}