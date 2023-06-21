using System;
using UniTAS.Patcher.Interfaces.DependencyInjection;
using UniTAS.Patcher.Services;
using UniTAS.Patcher.Services.Logging;
using UniTAS.Patcher.Utils;

namespace UniTAS.Patcher.Implementations;

// ReSharper disable once ClassNeverInstantiated.Global
[Singleton]
[ExcludeRegisterIfTesting]
public class StaticFieldStorage : IStaticFieldManipulator
{
    private readonly ILogger _logger;
    private readonly IFinalizeSuppressor _finalizeSuppressor;

    public StaticFieldStorage(ILogger logger, IFinalizeSuppressor finalizeSuppressor)
    {
        _logger = logger;
        _finalizeSuppressor = finalizeSuppressor;
    }

    public void ResetStaticFields()
    {
        _logger.LogDebug("resetting static fields");

        UnityEngine.Resources.UnloadUnusedAssets();

        var staticFieldStorage = Tracker.StaticFields;

        // this is stupid. i just set this to false when i need to invoke directly in this class which is really really dumb
        _finalizeSuppressor.DisableFinalizeInvoke = true;

        foreach (var field in staticFieldStorage)
        {
            var typeName = field.DeclaringType?.FullName ?? "unknown_type";
            _logger.LogDebug($"resetting static field: {typeName}.{field.Name}");

            // only dispose if disposable in is in system namespace
            if (field.GetValue(null) is IDisposable disposable &&
                field.FieldType.Namespace?.StartsWith("System") is true)
            {
                _logger.LogDebug("disposing object via IDisposable");
                _finalizeSuppressor.DisableFinalizeInvoke = false;
                disposable.Dispose();
                _finalizeSuppressor.DisableFinalizeInvoke = true;
            }

            field.SetValue(null, null);
        }

        _finalizeSuppressor.DisableFinalizeInvoke = false;

        // idk if this even works
        GC.Collect();
        GC.WaitForPendingFinalizers();

        _logger.LogDebug("calling static constructors");
        var staticCtorInvokeOrderListCount = Tracker.StaticCtorInvokeOrder.Count;
        for (var i = 0; i < staticCtorInvokeOrderListCount; i++)
        {
            var staticCtorType = Tracker.StaticCtorInvokeOrder[i];
            var cctor = staticCtorType.TypeInitializer;
            if (cctor == null) continue;
            _logger.LogDebug($"Calling static constructor for type: {cctor.DeclaringType?.FullName ?? "unknown_type"}");
            try
            {
                cctor.Invoke(null, default);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"Exception thrown while calling static constructor: {e}");
            }
        }
    }
}