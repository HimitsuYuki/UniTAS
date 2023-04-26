using UniTAS.Plugin.Models.VirtualEnvironment;
using UnityEngine;

namespace UniTAS.Plugin.Services.VirtualEnvironment.Input;

public interface IMouseStateEnv
{
    bool MousePresent { get; }
    Vector2 Position { get; set; }
    Vector2 Scroll { get; set; }
    bool IsButtonHeld(MouseButton button);
    bool IsButtonDown(MouseButton button);
    bool IsButtonUp(MouseButton button);
    void HoldButton(MouseButton button);
    void ReleaseButton(MouseButton button);
    bool AnyButtonHeld { get; }
    bool AnyButtonDown { get; }
}