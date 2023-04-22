using UnityEngine;

namespace UniTAS.Plugin.Services.VirtualEnvironment.Input;

public interface IMouseStateEnv
{
    bool MousePresent { get; }
    Vector2 Position { get; set; }
    bool LeftClick { get; set; }
    bool LeftClickDown { get; }
    bool LeftClickUp { get; }
    bool RightClick { get; set; }
    bool RightClickDown { get; }
    bool RightClickUp { get; }
    bool MiddleClick { get; set; }
    bool MiddleClickDown { get; }
    bool MiddleClickUp { get; }
    Vector2 Scroll { get; set; }
}