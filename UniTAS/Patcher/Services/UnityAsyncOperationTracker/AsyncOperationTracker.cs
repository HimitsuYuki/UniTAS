using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UniTAS.Patcher.Interfaces.DependencyInjection;
using UniTAS.Patcher.Interfaces.Events.SoftRestart;
using UniTAS.Patcher.Interfaces.Events.UnityEvents.DontRunIfPaused;
using UniTAS.Patcher.Interfaces.Events.UnityEvents.RunEvenPaused;
using UniTAS.Patcher.Models.UnitySafeWrappers.SceneManagement;
using UniTAS.Patcher.Services.Logging;
using UniTAS.Patcher.Services.UnitySafeWrappers.Wrappers;
using UniTAS.Patcher.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniTAS.Patcher.Services.UnityAsyncOperationTracker;

// ReSharper disable once ClassNeverInstantiated.Global
[Singleton]
public class AsyncOperationTracker(ISceneWrapper sceneWrapper, ILogger logger)
    : ISceneLoadTracker, IAssetBundleCreateRequestTracker, IAssetBundleRequestTracker,
        IOnLastUpdateUnconditional, IAsyncOperationIsInvokingOnComplete, IOnPreGameRestart, IOnUpdateActual
{
    private readonly HashSet<AsyncOperation> _tracked = new(new HashUtils.ReferenceComparer<AsyncOperation>());

    // any scene related things are stored as a list
    // it is a list of scenes in load order
    private readonly List<AsyncSceneLoadData> _asyncLoads = new();

    // not allowed to be disabled anymore
    private readonly HashSet<AsyncOperation> _allowSceneActivationNotAllowed = new();
    private readonly List<AsyncSceneLoadData> _pendingLoadCallbacks = new();
    private readonly List<AsyncSceneLoadData> _asyncLoadStalls = new();
    private readonly Dictionary<AsyncOperation, AssetBundle> _assetBundleCreateRequests = new();

    private readonly Dictionary<AsyncOperation, AssetBundleRequestData> _assetBundleRequests = new();

    private readonly Dictionary<AsyncOperation, bool> _allowSceneActivationValue =
        new(new HashUtils.ReferenceComparer<AsyncOperation>());

    private readonly HashSet<AsyncOperation> _isDone = new(new HashUtils.ReferenceComparer<AsyncOperation>());

    private class AssetBundleRequestData(Object singleResult = null, Object[] multipleResults = null)
    {
        public Object SingleResult { get; } = singleResult;
        public Object[] MultipleResults { get; } = multipleResults;
    }

    public void OnPreGameRestart()
    {
        _asyncLoads.Clear();
        _pendingLoadCallbacks.Clear();
        _asyncLoadStalls.Clear();
        _assetBundleCreateRequests.Clear();
        _assetBundleRequests.Clear();
        _allowSceneActivationValue.Clear();
        _isDone.Clear();
        _tracked.Clear();
        _allowSceneActivationNotAllowed.Clear();
        LoadingSceneCount = 0;
    }

    public void OnLastUpdateUnconditional()
    {
        if (_asyncLoads.Count == 0) return;

        foreach (var loadData in _asyncLoads)
        {
            logger.LogDebug(
                $"force loading scene via OnLastUpdate, name: {loadData.SceneName}, index: {loadData.SceneBuildIndex}, manually loading in loop");
            sceneWrapper.TrackSceneCount = false;
            sceneWrapper.LoadSceneAsync(loadData.SceneName, loadData.SceneBuildIndex, loadData.LoadSceneMode,
                loadData.LocalPhysicsMode, true);
            sceneWrapper.TrackSceneCount = true;

            _pendingLoadCallbacks.Add(loadData);
            var op = loadData.AsyncOperation;
            if (op == null) continue;
            _allowSceneActivationNotAllowed.Remove(op);
        }

        _asyncLoads.Clear();
    }

    public void UpdateActual()
    {
        // to allow the scene to be findable, invoke when scene loads on update
        var callbacks = new List<AsyncSceneLoadData>(_pendingLoadCallbacks.Count);
        foreach (var data in _pendingLoadCallbacks)
        {
            callbacks.Add(data);
            var op = data.AsyncOperation;
            if (op == null) continue;
            _isDone.Add(op);
        }

        // prevent allowSceneActivation to be False on event callback
        _pendingLoadCallbacks.Clear();

        foreach (var data in callbacks)
        {
            LoadingSceneCount--;
            if (data.LoadSceneMode == LoadSceneMode.Additive)
                sceneWrapper.SceneCount++;
            else
                sceneWrapper.SceneCount = 1;
            var op = data.AsyncOperation;
            if (op == null) continue;
            InvokeOnComplete(op);
        }
    }

    public void NewAssetBundleRequest(AsyncOperation asyncOperation, Object assetBundleRequest)
    {
        _assetBundleRequests.Add(asyncOperation, new(assetBundleRequest));
        _tracked.Add(asyncOperation);
        _isDone.Add(asyncOperation);
        InvokeOnComplete(asyncOperation);
    }

    public void NewAssetBundleRequestMultiple(AsyncOperation asyncOperation, Object[] assetBundleRequestArray)
    {
        _assetBundleRequests.Add(asyncOperation, new(multipleResults: assetBundleRequestArray));
        _tracked.Add(asyncOperation);
        _isDone.Add(asyncOperation);
        InvokeOnComplete(asyncOperation);
    }

    public void NewAssetBundleCreateRequest(AsyncOperation asyncOperation, AssetBundle assetBundle)
    {
        _assetBundleCreateRequests.Add(asyncOperation, assetBundle);
        _tracked.Add(asyncOperation);
        _isDone.Add(asyncOperation);
        InvokeOnComplete(asyncOperation);
    }

    public void AsyncSceneUnload(AsyncOperation asyncOperation)
    {
        logger.LogDebug("async scene unload");

        _tracked.Add(asyncOperation);
        _isDone.Add(asyncOperation);
        InvokeOnComplete(asyncOperation);

        // TODO: extra scene count
    }

    public void AsyncSceneLoad(string sceneName, int sceneBuildIndex, LoadSceneMode loadSceneMode,
        LocalPhysicsMode localPhysicsMode, AsyncOperation asyncOperation)
    {
        LoadingSceneCount++;

        _tracked.Add(asyncOperation);

        logger.LogDebug($"async scene load, {asyncOperation.GetHashCode()}");
        _asyncLoads.Add(new(sceneName, sceneBuildIndex, loadSceneMode, localPhysicsMode, asyncOperation));
    }

    public void NonAsyncSceneLoad(string sceneName, int sceneBuildIndex, LoadSceneMode loadSceneMode,
        LocalPhysicsMode localPhysicsMode)
    {
        LoadingSceneCount++;

        // first, all stalls are moved to load
        _asyncLoads.AddRange(_asyncLoadStalls);
        foreach (var sceneData in _asyncLoads)
        {
            logger.LogDebug(
                $"force loading scene via non-async scene load, name: {sceneData.SceneName}, index: {sceneData.SceneBuildIndex}");
            _allowSceneActivationNotAllowed.Add(sceneData.AsyncOperation);
        }

        _asyncLoadStalls.Clear();

        // next, queue the non async scene load
        logger.LogDebug($"scene load, {sceneName}, index: {sceneBuildIndex}");
        _asyncLoads.Add(new(sceneName, sceneBuildIndex, loadSceneMode, localPhysicsMode, null));
    }

    public void AllowSceneActivation(bool allow, AsyncOperation asyncOperation)
    {
        logger.LogDebug($"allow scene activation {allow}, {new StackTrace()}");

        if (allow)
        {
            var stallIndex = _asyncLoadStalls.FindIndex(d => d.AsyncOperation == asyncOperation);
            if (stallIndex < 0) return;
            StoreAllowSceneActivation(asyncOperation, true);
            if (_allowSceneActivationNotAllowed.Contains(asyncOperation)) return;
            _asyncLoads.Add(_asyncLoadStalls[stallIndex]);
            _asyncLoadStalls.RemoveAt(stallIndex);
            logger.LogDebug("restored scene activation");
        }
        else
        {
            var loadIndex = _asyncLoads.FindIndex(d => d.AsyncOperation == asyncOperation);
            if (loadIndex < 0) return;
            StoreAllowSceneActivation(asyncOperation, false);
            if (_allowSceneActivationNotAllowed.Contains(asyncOperation)) return;
            _asyncLoadStalls.Add(_asyncLoads[loadIndex]);
            _asyncLoads.RemoveAt(loadIndex);
            logger.LogDebug("Added scene to stall list");
        }
    }

    private void StoreAllowSceneActivation(AsyncOperation asyncOperation, bool allow)
    {
        _allowSceneActivationValue[asyncOperation] = allow;
    }

    public bool GetAllowSceneActivation(AsyncOperation asyncOperation, out bool state)
    {
        if (_allowSceneActivationValue.TryGetValue(asyncOperation, out state))
        {
            return true;
        }

        // not found, is it even tracked?
        state = false;
        return _tracked.Contains(asyncOperation);
    }

    public bool IsDone(AsyncOperation asyncOperation, out bool isDone)
    {
        if (_isDone.Contains(asyncOperation))
        {
            isDone = true;
            return true;
        }

        isDone = false;
        return _tracked.Contains(asyncOperation);
    }

    public bool Progress(AsyncOperation asyncOperation, out float progress)
    {
        if (_isDone.Contains(asyncOperation))
        {
            progress = 1f;
            return true;
        }

        progress = 0.9f;
        return _tracked.Contains(asyncOperation);
    }

    public AssetBundle GetAssetBundleCreateRequest(AsyncOperation asyncOperation)
    {
        return _assetBundleCreateRequests.TryGetValue(asyncOperation, out var assetBundle)
            ? assetBundle
            : null;
    }

    public object GetAssetBundleRequest(AsyncOperation asyncOperation)
    {
        return _assetBundleRequests.TryGetValue(asyncOperation, out var assetBundleRequest)
            ? assetBundleRequest.SingleResult
            : null;
    }

    public object GetAssetBundleRequestMultiple(AsyncOperation asyncOperation)
    {
        return _assetBundleRequests.TryGetValue(asyncOperation, out var assetBundleRequest)
            ? assetBundleRequest.MultipleResults
            : null;
    }

    private readonly MethodBase _invokeCompletionEvent =
        AccessTools.Method("UnityEngine.AsyncOperation:InvokeCompletionEvent", Type.EmptyTypes);

    private bool _isInvokingOnComplete;

    public bool IsInvokingOnComplete(AsyncOperation asyncOperation, out bool wasInvoked)
    {
        if (_tracked.Contains(asyncOperation))
        {
            wasInvoked = _isInvokingOnComplete;
            return true;
        }

        wasInvoked = default;
        return false;
    }

    private void InvokeOnComplete(AsyncOperation asyncOperation)
    {
        if (_invokeCompletionEvent == null) return;
        _isInvokingOnComplete = true;
        logger.LogDebug("invoking completion event");
        _invokeCompletionEvent.Invoke(asyncOperation, null);
        _isInvokingOnComplete = false;
    }

    // I don't think I need to track unload, they get invoked immediately, but maybe I should delay unload till end of frame and track till then?
    public int LoadingSceneCount { get; private set; }

    private class AsyncSceneLoadData(
        string sceneName,
        int sceneBuildIndex,
        LoadSceneMode loadSceneMode,
        LocalPhysicsMode localPhysicsMode,
        AsyncOperation asyncOperation)
    {
        public string SceneName { get; } = sceneName;
        public int SceneBuildIndex { get; } = sceneBuildIndex;
        public LoadSceneMode LoadSceneMode { get; } = loadSceneMode;
        public LocalPhysicsMode LocalPhysicsMode { get; } = localPhysicsMode;
        public AsyncOperation AsyncOperation { get; } = asyncOperation;
    }
}