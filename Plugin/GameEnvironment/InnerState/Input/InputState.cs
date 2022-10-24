﻿using UniTASPlugin.UpdateHelper;

namespace UniTASPlugin.GameEnvironment.InnerState.Input;

public class InputState : IOnUpdate
{
    public MouseState MouseState { get; }
    public AxisState AxisState { get; }
    public KeyboardState KeyboardState { get; }

    public InputState()
    {
        MouseState = new MouseState();
        AxisState = new AxisState();
        KeyboardState = new KeyboardState();
    }

    public void Update(float deltaTime)
    {
        MouseState.Update(deltaTime);
        KeyboardState.Update(deltaTime);
    }

    public void ResetStates()
    {
        MouseState.ResetState();
        AxisState.ResetState();
        KeyboardState.ResetState();
    }
}