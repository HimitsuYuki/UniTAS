﻿using System.Collections.Generic;
using UniTASPlugin.FakeGameState.InputLegacy;
using UniTASPlugin.TASMovie;
using UniTASPlugin.VersionSafeWrapper;
using UnityEngine;

namespace UniTASPlugin;

public static class TAS
{
    static bool _running = false;
    public static bool Running
    {
        // TODO private set
        get => _running; set
        {
            RunInitOrStopping = true;
            if (value)
            {
                CursorWrap.visible = false;
            }
            else
            {
                //Cursor.visible = VirtualCursor.Visible;
                TimeWrap.captureFrametime = 0;
            }
            _running = value;
            RunInitOrStopping = false;
        }
    }
    public static bool RunInitOrStopping { get; private set; }
    public static Movie CurrentMovie { get; private set; }
    public static ulong FrameCountMovie { get; private set; }
    static int currentFramebulkIndex;
    static int currentFramebulkFrameIndex;
    static int pendingMovieStartFixedUpdate = -1;
    public static bool PreparingRun { get; private set; } = false;

    public static void Update()
    {
        SaveState.Main.Update();
        UpdateMovie();
        Main.Update();
    }

    public static void FixedUpdate()
    {
        if (pendingMovieStartFixedUpdate > -1)
        {
            if (pendingMovieStartFixedUpdate == 0)
            {
                RunMoviePending();
            }
            pendingMovieStartFixedUpdate--;
        }
    }

    static void UpdateMovie()
    {
        if (!Running)
            return;

        FrameCountMovie++;
        if (!CheckCurrentMovieEnd())
            return;

        Framebulk fb = CurrentMovie.Framebulks[currentFramebulkIndex];
        if (currentFramebulkFrameIndex >= fb.FrameCount)
        {
            currentFramebulkIndex++;
            if (!CheckCurrentMovieEnd())
                return;

            currentFramebulkFrameIndex = 0;
            fb = CurrentMovie.Framebulks[currentFramebulkIndex];
        }

        TimeWrap.captureFrametime = fb.Frametime;
        GameControl(fb);

        currentFramebulkFrameIndex++;
    }

    static bool CheckCurrentMovieEnd()
    {
        if (currentFramebulkIndex >= CurrentMovie.Framebulks.Count)
        {
            Running = false;

            Plugin.Log.LogInfo("Movie end");

            return false;
        }

        return true;
    }

    static void GameControl(Framebulk fb)
    {
        FakeGameState.InputLegacy.Mouse.Position = new Vector2(fb.Mouse.X, fb.Mouse.Y);
        FakeGameState.InputLegacy.Mouse.LeftClick = fb.Mouse.Left;
        FakeGameState.InputLegacy.Mouse.RightClick = fb.Mouse.Right;
        FakeGameState.InputLegacy.Mouse.MiddleClick = fb.Mouse.Middle;

        List<string> axisMoveSetDefault = new();
        foreach (KeyValuePair<string, float> pair in Axis.Values)
        {
            string key = pair.Key;
            if (!fb.Axises.AxisMove.ContainsKey(key))
                axisMoveSetDefault.Add(key);
        }
        foreach (string key in axisMoveSetDefault)
        {
            if (Axis.Values.ContainsKey(key))
                Axis.Values[key] = default;
            else
                Axis.Values.Add(key, default);
        }
        foreach (KeyValuePair<string, float> axisValue in fb.Axises.AxisMove)
        {
            string axis = axisValue.Key;
            float value = axisValue.Value;
            if (Axis.Values.ContainsKey(axis))
            {
                Axis.Values[axis] = value;
            }
            else
            {
                Axis.Values.Add(axis, value);
            }
        }
    }

    public static void RunMovie(Movie movie)
    {
        PreparingRun = true;
        FrameCountMovie = 0;
        currentFramebulkIndex = 0;
        currentFramebulkFrameIndex = 1;

        CurrentMovie = movie;

        // force framerate to run fixed for Update and FixedUpdate sync
        if (CurrentMovie.Framebulks.Count > 0)
        {
            Framebulk firstFb = CurrentMovie.Framebulks[0];

            FakeGameState.InputLegacy.Main.Clear();
            TimeWrap.captureFrametime = firstFb.Frametime;
            GameControl(firstFb);

            if (currentFramebulkFrameIndex >= firstFb.FrameCount)
            {
                currentFramebulkFrameIndex = 0;
                currentFramebulkIndex++;
            }
        }

        pendingMovieStartFixedUpdate = 1;
        Plugin.Log.LogInfo("Starting movie, pending FixedUpdate call");
    }

    static void RunMoviePending()
    {
        PreparingRun = false;
        Running = true;

        FakeGameState.SystemInfo.DeviceType = CurrentMovie.DeviceType;
        // TODO fullscreen
        Screen.SetResolution(CurrentMovie.Width, CurrentMovie.Height, false, 60);

        GameRestart.SoftRestart(CurrentMovie.Time);
        Plugin.Log.LogInfo($"Movie start: {CurrentMovie}");
    }
}
