﻿using System;
using System.Diagnostics.CodeAnalysis;
using UniTAS.Patcher.Interfaces.DependencyInjection;
using UniTAS.Patcher.Interfaces.Events.SoftRestart;
using UniTAS.Patcher.Interfaces.Events.UnityEvents.RunEvenPaused;
using UniTAS.Patcher.Models.DependencyInjection;
using UniTAS.Patcher.Services;
using UniTAS.Patcher.Services.GameExecutionControllers;
using UniTAS.Patcher.Services.Logging;
using UniTAS.Patcher.Services.UnitySafeWrappers.Wrappers;
using UniTAS.Patcher.Services.VirtualEnvironment;
using UniTAS.Patcher.Utils;
using Object = UnityEngine.Object;

namespace UniTAS.Patcher.Implementations.GameRestart;

// ReSharper disable once ClassNeverInstantiated.Global
// target priority to after sync fixed update
[Singleton(RegisterPriority.GameRestart)]
public class GameRestart : IGameRestart, IOnAwakeUnconditional, IOnEnableUnconditional, IOnStartUnconditional,
    IOnFixedUpdateUnconditional
{
    private DateTime _softRestartTime;

    private readonly ISyncFixedUpdateCycle _syncFixedUpdate;
    private readonly ISceneWrapper _sceneWrapper;
    private readonly IMonoBehaviourController _monoBehaviourController;
    private readonly ILogger _logger;
    private readonly IFinalizeSuppressor _finalizeSuppressor;
    private readonly IUpdateInvokeOffset _updateInvokeOffset;

    private readonly IOnPreGameRestart[] _onPreGameRestart;

    private readonly IStaticFieldManipulator _staticFieldManipulator;
    private readonly ITimeEnv _timeEnv;

    private bool _pendingRestart;
    private bool _pendingResumePausedExecution;

    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    public GameRestart(ISyncFixedUpdateCycle syncFixedUpdate, ISceneWrapper sceneWrapper,
        IMonoBehaviourController monoBehaviourController, ILogger logger, IOnGameRestart[] onGameRestart,
        IOnGameRestartResume[] onGameRestartResume, IOnPreGameRestart[] onPreGameRestart,
        IStaticFieldManipulator staticFieldManipulator, ITimeEnv timeEnv, IFinalizeSuppressor finalizeSuppressor,
        IUpdateInvokeOffset updateInvokeOffset)
    {
        _syncFixedUpdate = syncFixedUpdate;
        _sceneWrapper = sceneWrapper;
        _monoBehaviourController = monoBehaviourController;
        _logger = logger;
        _onPreGameRestart = onPreGameRestart;
        _staticFieldManipulator = staticFieldManipulator;
        _timeEnv = timeEnv;
        _finalizeSuppressor = finalizeSuppressor;
        _updateInvokeOffset = updateInvokeOffset;

        foreach (var gameRestartResume in onGameRestartResume)
        {
            OnGameRestartResume += gameRestartResume.OnGameRestartResume;
        }

        foreach (var gameRestart in onGameRestart)
        {
            OnGameRestart += gameRestart.OnGameRestart;
        }
    }

    /// <summary>
    /// Destroys all necessary game objects to reset the game state.
    /// Default behaviour is to destroy all DontDestroyOnLoad objects.
    /// </summary>
    private void DestroyGameObjects()
    {
        var objs = Tracker.DontDestroyOnLoadRootObjects;
        _logger.LogDebug($"Destroying {objs.Count} DontDestroyOnLoad objects");

        foreach (var obj in objs)
        {
            var gameObject = (Object)obj;
            _logger.LogDebug($"Destroying {gameObject.name}");
            Object.Destroy(gameObject);
        }
    }

    /// <summary>
    /// Soft restart the game. This will not reload the game, but tries to reset the game state.
    /// </summary>
    /// <param name="time">Time to start the game at</param>
    public void SoftRestart(DateTime time)
    {
        if (_pendingRestart && !_pendingResumePausedExecution) return;

        _logger.LogInfo("Starting soft restart");

        OnPreGameRestart();

        _pendingRestart = true;
        _softRestartTime = time;

        _logger.LogDebug("Stopping MonoBehaviour execution");
        _monoBehaviourController.PausedExecution = true;

        DestroyGameObjects();

        _logger.LogDebug("Disabling finalize invoke");
        // TODO is this even a good idea
        _finalizeSuppressor.DisableFinalizeInvoke = true;

        _staticFieldManipulator.ResetStaticFields();

        _logger.LogDebug("Enabling finalize invoke");
        _finalizeSuppressor.DisableFinalizeInvoke = false;

        _syncFixedUpdate.OnSync(SoftRestartOperation, -_timeEnv.FrameTime);
    }

    public event GameRestartResume OnGameRestartResume;
    public event Services.GameRestart OnGameRestart;

    protected virtual void InvokeOnGameRestartResume(bool preMonoBehaviourResume)
    {
        OnGameRestartResume?.Invoke(_softRestartTime, preMonoBehaviourResume);
    }

    private void OnPreGameRestart()
    {
        foreach (var gameRestart in _onPreGameRestart)
        {
            gameRestart.OnPreGameRestart();
        }
    }

    private void SoftRestartOperation()
    {
        _logger.LogInfo("Soft restarting");

        OnGameRestart?.Invoke(_softRestartTime, true);
        _sceneWrapper.LoadScene(0);
        OnGameRestart?.Invoke(_softRestartTime, false);

        _pendingRestart = false;
        _pendingResumePausedExecution = true;
    }

    public void AwakeUnconditional()
    {
        PendingResumePausedExecution("Awake");
    }

    public void OnEnableUnconditional()
    {
        PendingResumePausedExecution("OnEnable");
    }

    public void StartUnconditional()
    {
        PendingResumePausedExecution("Start");
    }

    public void FixedUpdateUnconditional()
    {
        PendingResumePausedExecution("FixedUpdate");
    }

    private void PendingResumePausedExecution(string timing)
    {
        if (!_pendingResumePausedExecution) return;
        _pendingResumePausedExecution = false;

        InvokeOnGameRestartResume(true);

        _logger.LogInfo("Finish soft restarting");
        var actualTime = DateTime.Now;
        _logger.LogInfo($"System time: {actualTime}");

        _monoBehaviourController.PausedExecution = false;
        _logger.LogDebug($"Resuming MonoBehaviour execution at {timing}, {_updateInvokeOffset.Offset}");
        InvokeOnGameRestartResume(false);
    }
}