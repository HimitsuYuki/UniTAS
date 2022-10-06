﻿using UnityEngine;

namespace UniTASPlugin.GameOverlay;

public class ConfirmBox
{
    readonly string title;
    readonly string message;
    Rect defaultRect;
    Rect windowRect;
    public bool Opened { get; private set; }
    bool finalResult;
    bool gotFinalResult;
    readonly int id;
    readonly ConfirmBoxType confirmType;

    public ConfirmBox(string title, string message, Rect windowRect, int id, ConfirmBoxType confirmType)
    {
        this.title = title;
        this.message = message;
        defaultRect = windowRect;
        Opened = false;
        gotFinalResult = true;
        this.id = id;
        this.confirmType = confirmType;
    }

    public void Open()
    {
        windowRect = defaultRect;
        Opened = true;
        gotFinalResult = true;
    }

    public void Update()
    {
        if (!Opened)
            return;

        windowRect = GUILayout.Window(id, windowRect, Window, title);
    }

    void Window(int id)
    {
        GUI.DragWindow(new Rect(0, 0, 20000, 20));

        GUILayout.Label(message);
        GUILayout.BeginHorizontal();

        switch (confirmType)
        {
            case ConfirmBoxType.YesNo:
                {
                    if (GUILayout.Button("Yes"))
                    {
                        finalResult = true;
                        gotFinalResult = false;
                        Opened = false;
                    }
                    if (GUILayout.Button("No"))
                    {
                        finalResult = false;
                        gotFinalResult = false;
                        Opened = false;
                    }
                    break;
                }
            case ConfirmBoxType.Ok:
                {
                    if (GUILayout.Button("Ok"))
                    {
                        finalResult = true;
                        gotFinalResult = false;
                        Opened = false;
                    }
                    break;
                }
        }

        GUILayout.EndHorizontal();
    }

    public bool FinalResult(out bool finalResult)
    {
        if (gotFinalResult)
        {
            finalResult = false;
            return false;
        }
        finalResult = this.finalResult;
        return true;
    }

    public enum ConfirmBoxType
    {
        YesNo,
        Ok
    }
}