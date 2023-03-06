﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UniTAS.Plugin.Interfaces.Update;
using UniTAS.Plugin.Logger;
using UnityEngine;

namespace UniTAS.Plugin.GameVideoRender;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class GameRender : IGameRender, IOnLastUpdate
{
    private readonly Process _ffmpeg;

    private const int Fps = 60;
    private readonly int _width = Screen.width;
    private readonly int _height = Screen.height;
    private const string OutputPath = "output.mp4";

    private Texture2D _texture2D;
    private byte[] _bytes;
    private Color32[] _colors;
    private int _totalBytes;

    private bool _isRecording;

    private readonly ILogger _logger;

    public GameRender(ILogger logger)
    {
        _logger = logger;

        _ffmpeg = new();
        // TODO check if ffmpeg is installed
        _ffmpeg.StartInfo.FileName = "ffmpeg";
        // ffmpeg gets fed raw video data from unity
        // game fps could be variable, but output fps is fixed
        // output is flipped vertically
        _ffmpeg.StartInfo.Arguments =
            $"-y -f rawvideo -vcodec rawvideo -pix_fmt rgb24 -s {_width}x{_height} -r {Fps} -i - -an -vcodec libx264 -pix_fmt yuv420p -preset ultrafast -crf 0 -vf vflip {OutputPath}";
        _ffmpeg.StartInfo.UseShellExecute = false;
        _ffmpeg.StartInfo.RedirectStandardInput = true;
        _ffmpeg.StartInfo.RedirectStandardOutput = true;
        _ffmpeg.StartInfo.RedirectStandardError = true;

        // _ffmpeg.ErrorDataReceived += (_, args) =>
        // {
        //     if (args.Data != null)
        //     {
        //         _logger.LogError(args.Data);
        //     }
        // };
        //
        // _ffmpeg.OutputDataReceived += (_, args) =>
        // {
        //     if (args.Data != null)
        //     {
        //         _logger.LogDebug(args.Data);
        //     }
        // };
    }

    public void Start()
    {
        _logger.LogDebug("Setting up recording");
        // TODO let it able to change resolution
        _texture2D = new(_width, _height, TextureFormat.RGB24, false);
        _totalBytes = _width * _height * 3;
        _bytes = new byte[_totalBytes];

        _ffmpeg.Start();
        _isRecording = true;
        _logger.LogDebug("Started recording");
    }

    public void Stop()
    {
        _logger.LogDebug("Stopping recording");
        _isRecording = false;
        _ffmpeg.StandardInput.Close();
        _ffmpeg.WaitForExit();

        // TODO log error if exit code is not 0

        _logger.LogDebug("Successfully stopped recording");
    }

    public void OnLastUpdate()
    {
        if (!_isRecording) return;

        _texture2D.ReadPixels(new(0, 0, _width, _height), 0, 0);

        _colors = _texture2D.GetPixels32();
        for (var i = 0; i < _colors.Length; i++)
        {
            var color = _colors[i];
            _bytes[i * 3] = color.r;
            _bytes[i * 3 + 1] = color.g;
            _bytes[i * 3 + 2] = color.b;
        }

        _ffmpeg.StandardInput.BaseStream.Write(_bytes, 0, _totalBytes);
        _ffmpeg.StandardInput.BaseStream.Flush();
    }
}