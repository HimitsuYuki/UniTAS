﻿using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StructureMap;
using UniTASPlugin.FakeGameState.GameFileSystem;
using UniTASPlugin.GameEnvironment;
using UniTASPlugin.GameOverlay;
using UniTASPlugin.Interfaces.Update;
using UniTASPlugin.VersionSafeWrapper;
using UnityEngine;
using SystemInfo = UniTASPlugin.FakeGameState.SystemInfo;

namespace UniTASPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static readonly IContainer Kernel = ContainerRegister.Init();

    private ManualLogSource _logger;

    private static Plugin instance;

    public static ManualLogSource Log => instance._logger;

    private IOnFixedUpdate[] _onFixedUpdates;

    private void Awake()
    {
        if (instance != null)
            return;
        instance = this;
        _logger = Logger;

        _onFixedUpdates = Kernel.GetAllInstances<IOnFixedUpdate>().ToArray();

        Logger.LogInfo("init patch");
        Harmony harmony = new($"{MyPluginInfo.PLUGIN_GUID}HarmonyPatch");
        harmony.PatchAll();
        Logger.LogInfo("post init patch");

        // init fake file system
        // TODO way of getting device type
        FileSystem.Init(DeviceType.Windows);

        Logger.LogInfo($"Internally found unity version: {Helper.GetUnityVersion()}");
        Logger.LogInfo($"Game product name: {AppInfo.ProductName()}");
        // TODO complete fixing this
        var companyNameProperty = Traverse.Create(typeof(Application)).Property("companyName");
        if (companyNameProperty.PropertyExists())
            Logger.LogInfo(
                $"Game company name: {companyNameProperty.GetValue<string>()}"); //product name: {Application.productName}, version: {Application.version}");

        // TODO all axis names for help

        // init random seed
        var env = Kernel.GetInstance<IVirtualEnvironmentFactory>().GetVirtualEnv();
        RandomWrap.InitState((int)env.Seed);

        GameTracker.Init();
        SystemInfo.Init();
        Overlay.Init();

        // TODO remove this test
        var pluginType = typeof(Plugin);
        var methods = pluginType.GetMethods();
        if (methods.Any(x => x.Name == "Update") && pluginType.IsAssignableFrom(typeof(MonoBehaviour)))
        {
            Logger.LogDebug("Plugin has Update method");
        }

        Logger.LogDebug($"Plugin assignable from MonoBehaviour: {pluginType.IsAssignableFrom(typeof(MonoBehaviour))}");
        Logger.LogDebug($"Plugin is subclass of MonoBehaviour: {pluginType.IsSubclassOf(typeof(MonoBehaviour))}");

        foreach (var method in Traverse.Create<Plugin>().Methods())
        {
            Logger.LogDebug($"Plugin method: {method}");
        }

        Logger.LogInfo($"System time: {DateTime.Now}");
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
    }

    // execution order is Awake -> FixedUpdate -> Update
    private void FixedUpdate()
    {
        foreach (var update in _onFixedUpdates)
        {
            update.FixedUpdate();
        }

        // this needs to be called before checking pending soft restart or it will cause a 1 frame desync
        //TAS.FixedUpdate();
    }

    private void LateUpdate()
    {
        GameTracker.LateUpdate();
    }

    private void OnGUI()
    {
        Overlay.OnGUI();
    }
}