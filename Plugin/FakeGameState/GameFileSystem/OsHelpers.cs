﻿using System;
using System.Collections.Generic;
using System.IO;

namespace UniTASPlugin.FakeGameState.GameFileSystem;

public static partial class FileSystem
{
    class OpenHandle
    {
        public string Path { get; }
        public long Offset { get; set; }
        public FileOptions Options { get; }
        public FileAccess Access { get; }
        public FileShare Share { get; }
        public File File { get; }

        public OpenHandle(string path, long offset, FileOptions options, FileAccess access, FileShare share)
        {
            Path = path;
            Offset = offset;
            Options = options;
            Access = access;
            Share = share;
            File = GetFile(path);
        }

        ~OpenHandle()
        {
            if ((Options & FileOptions.DeleteOnClose) != 0)
                DeleteFile(Path);
        }

        public OpenHandle(string path, FileOptions options, FileAccess access, FileShare share) : this(path, 0, options, access, share) { }
    }

    public static class OsHelpers
    {
        static readonly Dictionary<string, OpenHandle> openHandles = new();

        public static void OpenFile(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            Plugin.Log.LogDebug($"Opening file {path} with mode {mode}, access {access}, share {share}, options {options}");
            File file;
            OpenHandle handle = null;

            switch (mode)
            {
                case FileMode.CreateNew:
                    {
                        var check = GetFile(path);
                        if (check != null)
                            throw new IOException();
                        goto case FileMode.Create;
                    }
                case FileMode.Create:
                    {
                        var pathDir = Directory.GetParent(path);
                        var dir = CreateDir(pathDir.FullName);
                        var filename = Path.GetFileName(path);
                        dir.AddFile(filename);
                        break;
                    }
                case FileMode.Open:
                    {
                        file = GetFile(path);
                        if (file == null)
                            throw new FileNotFoundException();
                        break;
                    }
                case FileMode.OpenOrCreate:
                    {
                        file = GetFile(path);
                        if (file == null)
                            goto case FileMode.Create;
                        break;
                    }
                case FileMode.Truncate:
                    {
                        file = GetFile(path);
                        if (file == null)
                            throw new FileNotFoundException();
                        file.Data = new byte[0];
                        break;
                    }
                case FileMode.Append:
                    {
                        file = GetFile(path);
                        if (file == null)
                            file = CreateDir(Directory.GetParent(path).FullName).AddFile(Path.GetFileName(path));
                        handle = new OpenHandle(path, file.Data.Length, options, access, share);
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }

            if (mode != FileMode.Append)
                handle = new OpenHandle(path, options, access, share);

            openHandles.Add(path, handle);
        }

        static void AccessFile(File file)
        {
            file.AccessTime = DateTime.Now;
            if (file.Parent != null)
                AccessDir(file.Parent);
        }

        static void AccessDir(Dir dir)
        {
            dir.AccessTime = DateTime.Now;
        }

        static void WriteFile(File file)
        {
            AccessFile(file);
            file.WriteTime = DateTime.Now;
            if (file.Parent != null)
                WriteDir(file.Parent);
        }

        static void WriteDir(Dir dir)
        {
            dir.WriteTime = DateTime.Now;
        }

        // TODO file attribute support
        public static void Close(string path)
        {
            openHandles.Remove(path);
        }

        public static long Seek(string path, long bufStart, SeekOrigin seekOrigin)
        {
            if (!openHandles.TryGetValue(path, out var handle))
                throw new IOException();

            switch (seekOrigin)
            {
                case SeekOrigin.Current:
                    bufStart += handle.Offset;
                    break;
                case SeekOrigin.End:
                    bufStart += handle.File.Data.Length;
                    break;
            }

            handle.Offset = bufStart;

            return bufStart;
        }

        public static long Length(string path)
        {
            if (!openHandles.TryGetValue(path, out var handle))
                throw new IOException();

            AccessFile(handle.File);
            return handle.File.Data.Length;
        }

        public static int Write(string path, byte[] src, int src_offset, int count)
        {
            if (!openHandles.TryGetValue(path, out var handle))
                throw new IOException();

            var file = handle.File;
            var offset = handle.Offset;
            var data = file.Data;

            if (offset + count > data.Length)
            {
                var newData = new byte[offset + count];
                Array.Copy(data, newData, data.Length);
                file.Data = newData;
                data = newData;
            }

            Array.Copy(src, src_offset, data, offset, count);
            handle.Offset += count;

            WriteFile(file);
            return count;
        }

        public static void SetLength(string path, long length)
        {
            if (!openHandles.TryGetValue(path, out var handle))
                throw new IOException();

            var file = handle.File;
            var data = file.Data;

            if (length > data.Length)
            {
                var newData = new byte[length];
                Array.Copy(data, newData, data.Length);
                file.Data = newData;
            }
            else
            {
                var newData = new byte[length];
                Array.Copy(data, newData, length);
                file.Data = newData;
            }
            WriteFile(file);
        }

        public static int Read(string path, byte[] dest, int dest_offset, int count)
        {
            if (!openHandles.TryGetValue(path, out var handle))
                throw new IOException();

            var file = handle.File;
            var offset = handle.Offset;
            var data = file.Data;

            if (offset >= data.Length)
                return 0;

            var copyLength = Math.Min(count + offset, data.Length);

            Array.Copy(data, offset, dest, dest_offset, copyLength);
            handle.Offset += copyLength;

            AccessFile(file);
            return (int)copyLength;
        }

        public static void Copy(string src, string dest, bool overwrite)
        {
            var srcFile = GetFile(src);
            if (srcFile == null)
                throw new FileNotFoundException();

            var destDirName = Directory.GetParent(dest).FullName;
            var destDir = GetDir(destDirName);
            if (destDir == null)
                throw new DirectoryNotFoundException();

            var destFileName = Path.GetFileName(dest);
            if (!overwrite && destDir.GetFile(destFileName) != null)
                throw new IOException();

            var destFile = destDir.AddFile(destFileName);
            destFile.Data = srcFile.Data;
            AccessFile(srcFile);
            WriteFile(destFile);
        }

        public static void DeleteFile(string path)
        {
            FileSystem.DeleteFile(path);
        }

        public static FileAttributes? GetFileAttributes(string path)
        {
            var file = GetFile(path);
            if (file == null)
                return null;
            AccessFile(file);
            return file.Attributes;
        }

        public static void SetFileAttributes(string path, FileAttributes attributes)
        {
            var file = GetFile(path);
            if (file == null)
                return;
            WriteFile(file);
            file.Attributes = attributes;
        }

        public static DateTime FileCreationTime(string path)
        {
            var file = GetFile(path);
            if (file == null)
                throw new FileNotFoundException();
            AccessFile(file);
            return file.CreationTime;
        }

        public static bool FileExists(string path)
        {
            return FileSystem.FileExists(path);
        }

        public static bool DirectoryExists(string path)
        {
            return FileSystem.DirectoryExists(path);
        }

        public static bool MoveFile(string source, string dest)
        {
            var sourceDirName = Directory.GetParent(source).FullName;
            var sourceDir = GetDir(sourceDirName);
            if (sourceDir == null)
                throw new DirectoryNotFoundException();

            var file = sourceDir.GetFile(source);
            if (file == null)
                throw new FileNotFoundException();

            var destDirName = Directory.GetParent(dest).FullName;
            var destDir = GetDir(destDirName);
            if (destDir == null)
                throw new DirectoryNotFoundException();

            var destFileName = Path.GetFileName(dest);
            if (destDir.GetFile(destFileName) != null)
                throw new IOException();

            file.Name = destFileName;
            sourceDir.DeleteFile(file);
            destDir.AddEntry(file);
            AccessFile(file);
            return true;
        }

        public static void ReplaceFile(string source, string dest)
        {
            var sourceDirName = Directory.GetParent(source).FullName;
            var sourceDir = GetDir(sourceDirName);
            if (sourceDir == null)
                throw new DirectoryNotFoundException();

            var file = sourceDir.GetFile(source);
            if (file == null)
                throw new FileNotFoundException();

            var destDirName = Directory.GetParent(dest).FullName;
            var destDir = GetDir(destDirName);
            if (destDir == null)
                throw new DirectoryNotFoundException();

            var destFileName = Path.GetFileName(dest);
            var destFile = destDir.GetFile(destFileName);
            if (destFile == null)
                throw new FileNotFoundException();

            file.Name = destFileName;
            sourceDir.DeleteFile(file);
            destDir.DeleteFile(destFile);
            destDir.AddEntry(file);
            AccessFile(file);
        }
    }
}
