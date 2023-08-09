using System;
using UniTAS.Patcher.Models.Customization;
using UniTAS.Patcher.Models.GlobalHotkeyListener;

namespace UniTAS.Patcher.Interfaces.GlobalHotkeyListener;

public interface IGlobalHotkey
{
    void AddGlobalHotkey(GlobalHotkey config);
}