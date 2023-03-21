﻿using System.Collections.Generic;

namespace UniTAS.Plugin.Services.VirtualEnvironment.InnerState.Input;

public class AxisState : InputDeviceBase
{
    public Dictionary<string, float> Values { get; }

    public AxisState()
    {
        Values = new();
    }

    public override void Update()
    {
    }

    public override void ResetState()
    {
        Values.Clear();
    }
}