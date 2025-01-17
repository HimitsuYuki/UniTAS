using System;
using UniTAS.Patcher.Interfaces.DependencyInjection;
using UniTAS.Patcher.Models.Customization;
using UniTAS.Patcher.Models.DependencyInjection;
using UniTAS.Patcher.Models.UnitySafeWrappers;
using UniTAS.Patcher.Services;
using UniTAS.Patcher.Services.Customization;
using UniTAS.Patcher.Services.GUI;
using UniTAS.Patcher.Services.UnityEvents;
using UniTAS.Patcher.Services.UnitySafeWrappers.Wrappers;
using UniTAS.Patcher.Utils;
using UnityEngine;

namespace UniTAS.Patcher.Implementations.GUI.Windows;

[Singleton(RegisterPriority.ToolBar)]
[ForceInstantiate]
[ExcludeRegisterIfTesting]
public class ToolBar : IToolBar, IActualCursorState
{
    private readonly IWindowFactory _windowFactory;
    private readonly ICursorWrapper _cursorWrapper;

    private readonly GUIStyle _buttonStyle;
    private readonly Texture2D _buttonNormal = new(1, 1);
    private const int ToolbarHeight = 35;

    public CursorLockMode CursorLockState { get; set; } = CursorLockMode.None;
    public bool CursorVisible { get; set; } = true;
    public bool PreventCursorChange { get; private set; }

    private void OnGameRestart(DateTime startupTime, bool preSceneLoad)
    {
        if (!preSceneLoad) return;
        CursorLockState = CursorLockMode.None;
        CursorVisible = true;
    }

    public bool Show
    {
        get => _show;
        private set
        {
            if (_show == value) return;
            _show = value;
            if (_show)
            {
                _cursorWrapper.LockState = CursorLockMode.None;
                _cursorWrapper.Visible = true;
                PreventCursorChange = true;
            }
            else
            {
                PreventCursorChange = false;
                _cursorWrapper.LockState = CursorLockState;
                _cursorWrapper.Visible = CursorVisible;
            }

            OnShowChange?.Invoke(value);
        }
    }

    public event Action<bool> OnShowChange;

    private readonly Bind _toolbarVisibleBind;
    private bool _show;
    private readonly IDropdownList _dropdownList;

    public ToolBar(IWindowFactory windowFactory, IBinds binds, IGUIComponentFactory guiComponentFactory,
        IObjectTrackerManager objectTrackerManager, IUpdateEvents updateEvents, ICursorWrapper cursorWrapper,
        IGameRestart gameRestart)
    {
        _windowFactory = windowFactory;
        _cursorWrapper = cursorWrapper;
        gameRestart.OnGameRestart += OnGameRestart;
        _dropdownList = guiComponentFactory.CreateComponent<IDropdownList>();

        _buttonNormal.SetPixel(0, 0, new(0.129f, 0.149f, 0.263f));
        _buttonNormal.Apply();
        var buttonHold = new Texture2D(1, 1);
        buttonHold.SetPixel(0, 0, new(0.5f, 0.5f, 0.5f));
        buttonHold.Apply();

        _buttonStyle = new()
        {
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = ToolbarHeight - 4,
            padding = new(15, 15, 5, 5),
            margin = new(5, 5, 5, 5),
            normal = { background = _buttonNormal, textColor = Color.white },
            hover = { background = _buttonNormal, textColor = Color.white },
            active = { background = buttonHold, textColor = Color.white }
        };

        _toolbarVisibleBind = binds.Create(new("Toolbar visible", KeyCode.F1, BindCategory.UniTAS));

        // each index corresponds to DropDownSection entry
        _dropdownButtons =
        [
            [
                ("Overlays", () => { _windowFactory.Create<OverlayControlWindow>().Show = true; }),
                ("Terminal", () => { _windowFactory.Create<TerminalWindow>().Show = true; })
            ],
            [
                ("New object tracker", objectTrackerManager.AddNew)
            ],
            [
                ("Key binds", () => { _windowFactory.Create<KeyBindsWindow>().Show = true; })
            ],
        ];

        updateEvents.OnGUIUnconditional += OnGUIUnconditional;
    }

    private enum DropDownSection
    {
        Windows,
        View,
        Settings,
    }

    private DropDownSection? _currentDropDown;
    private bool _gotDropDownRects;
    private readonly Rect[] _dropDownRects = new Rect[Enum.GetValues(typeof(DropDownSection)).Length];
    private Rect _barRect;

    private const float BarWidthPercentage = 0.25f;

    private void OnGUIUnconditional()
    {
        var currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && Event.current.keyCode == _toolbarVisibleBind.Key)
        {
            Show = !Show;
            Event.current.Use();
        }

        if (!Show) return;

        var width = Screen.width;
        var widthModified = width * BarWidthPercentage;

        _barRect = new Rect((width - widthModified) / 2, 20, widthModified, ToolbarHeight);

        UnityEngine.GUI.DrawTexture(_barRect, _buttonNormal);

        GUILayout.BeginArea(_barRect);
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal(GUIUtils.EmptyOptions);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Movie", _buttonStyle, GUIUtils.EmptyOptions))
        {
            _windowFactory.Create<MoviePlayWindow>().Show = true;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("View", _buttonStyle, GUIUtils.EmptyOptions))
        {
            _currentDropDown = DropDownSection.View;
        }

        var updateDropDownRects = !_gotDropDownRects && currentEvent.type == EventType.Repaint;
        if (updateDropDownRects)
        {
            _dropDownRects[(int)DropDownSection.View] = CalcDropDownRect();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Windows", _buttonStyle, GUIUtils.EmptyOptions))
        {
            _currentDropDown = DropDownSection.Windows;
        }

        if (updateDropDownRects)
        {
            _dropDownRects[(int)DropDownSection.Windows] = CalcDropDownRect();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Settings", _buttonStyle, GUIUtils.EmptyOptions))
        {
            _currentDropDown = DropDownSection.Settings;
        }

        if (updateDropDownRects)
        {
            _dropDownRects[(int)DropDownSection.Settings] = CalcDropDownRect();
        }

        if (updateDropDownRects)
            _gotDropDownRects = true;

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndArea();

        if (!_currentDropDown.HasValue) return;

        var currentDropDownValue = (int)_currentDropDown.Value;
        if (_dropdownList.DropdownButtons(_dropDownRects[currentDropDownValue], _dropdownButtons[currentDropDownValue]))
        {
            _currentDropDown = null;
        }
    }

    private Rect CalcDropDownRect()
    {
        var lastRect = GUILayoutUtility.GetLastRect();
        return new(lastRect.x + _barRect.x, lastRect.y + _barRect.y + lastRect.height, 150, 0);
    }

    private readonly (string, Action)[][] _dropdownButtons;
}