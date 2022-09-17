﻿using System;
using System.Linq;

namespace Core.UnityHooks;

public static class Main
{
    public static void Init()
    {
        Logger.Log.LogDebug("Calling UnityHooks.Main()");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var unityCoreModules = assemblies.Where(a => a.GetName().Name == "UnityEngine.CoreModule");

        if (unityCoreModules.Count() == 0)
        {
            Logger.Log.LogError("Found no UnityEngine.CoreModule assembly, dumping all found assemblies");
            Logger.Log.LogError(assemblies.Select(a => a.GetName().FullName));
            // TODO stop TAS tool from turning into a blackhole
            return;
        }

        var unityCoreModule = unityCoreModules.ElementAt(0);
        var types = unityCoreModule.GetTypes().ToList();

        Type keyCode = null;
        Type cursor = null;
        Type cursorLockMode = null;
        Type monoBehaviour = null;
        Type @object = null;
        Type scene = null;
        Type sceneManager = null;
        Type vector2 = null;
        Type time = null;

        keyCode = types.Find(t => t.FullName == "UnityEngine.KeyCode");
        cursor = types.Find(t => t.FullName == "UnityEngine.Cursor");
        cursorLockMode = types.Find(t => t.FullName == "UnityEngine.CursorLockMode");
        monoBehaviour = types.Find(t => t.FullName == "UnityEngine.MonoBehaviour");
        @object = types.Find(t => t.FullName == "UnityEngine.Object");
        sceneManager = types.Find(t => t.FullName == "UnityEngine.SceneManagement.SceneManager");
        scene = types.Find(t => t.FullName == "UnityEngine.SceneManagement.Scene");
        vector2 = types.Find(t => t.FullName == "UnityEngine.Vector2");
        time = types.Find(t => t.FullName == "UnityEngine.Time");
        /*
        switch (version)
        {
            case UnityVersion.v2018_4_25:
            case UnityVersion.v2021_2_14:
                break;
        }
        */

        if (keyCode == null)
            throw new Exception("UnityEngine.KeyCode not found");
        if (cursor == null)
            throw new Exception("UnityEngine.Cursor not found");
        if (cursorLockMode == null)
            throw new Exception("UnityEngine.CursorLockMode not found");
        if (monoBehaviour == null)
            throw new Exception("UnityEngine.MonoBehaviour not found");
        if (@object == null)
            throw new Exception("UnityEngine.Object not found");
        if (scene == null)
            throw new Exception("UnityEngine.SceneManagement.Scene not found");
        if (sceneManager == null)
            throw new Exception("UnityEngine.SceneManagement.SceneManager not found");
        if (vector2 == null)
            throw new Exception("UnityEngine.Vector2 not found");
        if (time == null)
            throw new Exception("UnityEngine.Time not found");

        //      /InputLegacy
        new InputLegacy.KeyCode("").Init(keyCode, PluginInfo.UnityVersion);
        //      /
        new Cursor().Init(cursor, PluginInfo.UnityVersion);
        new CursorLockMode("").Init(cursorLockMode, PluginInfo.UnityVersion);
        new MonoBehavior().Init(monoBehaviour, PluginInfo.UnityVersion);
        new Object().Init(@object, PluginInfo.UnityVersion);
        new SceneManager().Init(sceneManager, PluginInfo.UnityVersion);
        new Scene().Init(scene, PluginInfo.UnityVersion);
        new Time().Init(time, PluginInfo.UnityVersion);
        new Vector2().Init(vector2, PluginInfo.UnityVersion);
    }
}
