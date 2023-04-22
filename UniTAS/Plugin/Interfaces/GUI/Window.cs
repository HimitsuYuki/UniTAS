﻿using UniTAS.Plugin.Interfaces.Events.MonoBehaviourEvents.RunEvenPaused;
using UniTAS.Plugin.Services.EventSubscribers;
using UnityEngine;

namespace UniTAS.Plugin.Interfaces.GUI;

/// <summary>
/// Base class for all windows.
/// Uses automatic layout.
/// </summary>
public abstract class Window : IOnGUIUnconditional
{
    private Rect _windowRect;

    protected abstract Rect DefaultWindowRect { get; }
    private readonly string _windowName;

    private readonly IUpdateEvents _updateEvents;

    // TODO this is a hack, but it works for now
    private static int _globalId = 23134259;
    private readonly int _id;

    protected Window(IUpdateEvents updateEvents, string windowName = null)
    {
        _updateEvents = updateEvents;
        _windowName = windowName;
        _id = _globalId;
        _globalId++;
    }

    private void Init()
    {
        _windowRect = DefaultWindowRect;
    }

    public void Show()
    {
        Init();
        _updateEvents.OnGUIEventUnconditional += OnGUIUnconditional;
    }

    public void Close()
    {
        _updateEvents.OnGUIEventUnconditional -= OnGUIUnconditional;
    }

    public void OnGUIUnconditional()
    {
        _windowRect = GUILayout.Window(_id, _windowRect, RenderWindow, _windowName);
    }

    private void RenderWindow(int id)
    {
        OnGUI(id);

        // make window draggable
        UnityEngine.GUI.DragWindow();
    }

    protected virtual void OnGUI(int id)
    {
    }
}