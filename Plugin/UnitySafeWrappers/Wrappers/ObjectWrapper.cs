using System;
using System.Collections.Generic;
using UniTASPlugin.UnitySafeWrappers.Interfaces;
using Object = UnityEngine.Object;

namespace UniTASPlugin.UnitySafeWrappers.Wrappers;

public class ObjectWrapper : IObjectWrapper
{
    public void Destroy(object obj)
    {
        Object.Destroy((Object)obj);
    }

    public IEnumerable<object> FindObjectsOfType(Type type)
    {
        return Object.FindObjectsOfType(type);
    }
}