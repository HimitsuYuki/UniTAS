﻿using HarmonyLib;
using System.IO;

namespace UniTASPlugin.VersionSafeWrapper;

public static class AppInfo
{
    static Traverse application()
    {
        return Traverse.CreateWithType("UnityEngine.Application");
    }

    static string productNameCache = null;

    public static string ProductName()
    {
        Traverse productNameTraverse = application().Property("productName");
        if (productNameTraverse.PropertyExists())
            return productNameTraverse.GetValue<string>();

        // fallback, try get in c# way
        string crashHandlerExe = "UnityCrashHandler64.exe";
        string foundExe = "";
        bool foundMultipleExe = false;
        string rootDir = Helper.GameRootDir();
        string[] rootFiles = Directory.GetFiles(rootDir);

        // iterate over exes in game root dir
        foreach (string path in rootFiles)
        {
            if (path == crashHandlerExe)
                continue;

            if (path.EndsWith(".exe"))
            {
                if (foundExe != "")
                {
                    foundMultipleExe = true;
                    break;
                }
                foundExe = path;
            }
        }

        if (foundExe == "" && !foundMultipleExe)
            throw new System.Exception("Could not find exe in game root dir");

        if (!foundMultipleExe)
        {
            productNameCache = Path.GetFileNameWithoutExtension(foundExe);
            return productNameCache;
        }

        // use game dir name and see if it matches exe
        string gameDirName = new DirectoryInfo(rootDir).Name;

        if (File.Exists(Path.Combine(rootDir, $"{gameDirName}.exe")))
        {
            productNameCache = gameDirName;
            return gameDirName;
        }

        return null;
    }
}