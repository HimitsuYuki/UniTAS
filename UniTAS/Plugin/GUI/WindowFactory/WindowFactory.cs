﻿using System.Diagnostics.CodeAnalysis;
using StructureMap;

namespace UniTAS.Plugin.GUI.WindowFactory;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class WindowFactory : IWindowFactory
{
    private readonly IContainer _container;

    public WindowFactory(IContainer container)
    {
        _container = container;
    }

    public T Create<T>(string windowName = null) where T : Window
    {
        if (windowName == null)
        {
            return _container.GetInstance<T>();
        }

        return _container.With("windowName").EqualTo(windowName).GetInstance<T>();
    }
}