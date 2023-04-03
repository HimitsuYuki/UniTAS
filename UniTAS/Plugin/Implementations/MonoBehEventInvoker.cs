using System;
using System.Collections.Generic;
using UniTAS.Patcher.Shared;
using UniTAS.Plugin.Interfaces.DependencyInjection;
using UniTAS.Plugin.Interfaces.Events.MonoBehaviourEvents.RunEvenPaused;
using UniTAS.Plugin.Services;
using UniTAS.Plugin.Services.EventSubscribers;

namespace UniTAS.Plugin.Implementations;

// ReSharper disable once ClassNeverInstantiated.Global
[Singleton]
public class MonoBehEventInvoker : IMonoBehEventInvoker, IUpdateEvents
{
    private readonly IMonoBehaviourController _monoBehaviourController;

    public MonoBehEventInvoker(IEnumerable<IOnAwakeUnconditional> onAwakesUnconditional,
        IEnumerable<IOnStartUnconditional> onStartsUnconditional,
        IEnumerable<IOnEnableUnconditional> onEnablesUnconditional,
        IEnumerable<IOnPreUpdatesUnconditional> onPreUpdatesUnconditional,
        IEnumerable<IOnUpdateUnconditional> onUpdatesUnconditional,
        IEnumerable<IOnFixedUpdateUnconditional> onFixedUpdatesUnconditional,
        IEnumerable<IOnGUIUnconditional> onGUIsUnconditional,
        IMonoBehaviourController monoBehaviourController)
    {
        _monoBehaviourController = monoBehaviourController;
        foreach (var onAwake in onAwakesUnconditional)
        {
            MonoBehaviourEvents.OnAwakeUnconditional += onAwake.AwakeUnconditional;
        }

        foreach (var onStart in onStartsUnconditional)
        {
            MonoBehaviourEvents.OnStartUnconditional += onStart.StartUnconditional;
        }

        foreach (var onEnable in onEnablesUnconditional)
        {
            MonoBehaviourEvents.OnEnableUnconditional += onEnable.OnEnableUnconditional;
        }

        foreach (var onPreUpdate in onPreUpdatesUnconditional)
        {
            MonoBehaviourEvents.OnPreUpdateUnconditional += onPreUpdate.PreUpdateUnconditional;
        }

        foreach (var onUpdate in onUpdatesUnconditional)
        {
            MonoBehaviourEvents.OnUpdateUnconditional += onUpdate.UpdateUnconditional;
        }

        foreach (var onFixedUpdate in onFixedUpdatesUnconditional)
        {
            MonoBehaviourEvents.OnFixedUpdateUnconditional += onFixedUpdate.FixedUpdateUnconditional;
        }

        foreach (var onGui in onGUIsUnconditional)
        {
            MonoBehaviourEvents.OnGUIUnconditional += onGui.OnGUIUnconditional;
        }

        MonoBehaviourEvents.OnGUIUnconditional += () => OnGUIEventUnconditional?.Invoke();
    }

    public void Update()
    {
        MonoBehaviourEvents.InvokeUpdateUnconditional();
        if (_monoBehaviourController.PausedExecution) return;
        MonoBehaviourEvents.InvokeUpdateActual();
    }

    public void FixedUpdate()
    {
        MonoBehaviourEvents.InvokeFixedUpdateUnconditional();
        if (_monoBehaviourController.PausedExecution) return;
        MonoBehaviourEvents.InvokeFixedUpdateActual();
    }

    public void OnGUI()
    {
        MonoBehaviourEvents.InvokeOnGUIUnconditional();
        if (_monoBehaviourController.PausedExecution) return;
        MonoBehaviourEvents.InvokeOnGUIActual();
    }

    public void LateUpdate()
    {
        MonoBehaviourEvents.InvokeLateUpdateUnconditional();
        if (_monoBehaviourController.PausedExecution) return;
        MonoBehaviourEvents.InvokeLateUpdateActual();
    }

    public event Action OnGUIEventUnconditional;
}