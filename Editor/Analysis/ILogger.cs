/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Yarn.Unity
{
#nullable enable
    public interface ILogger : IDisposable
    {
        void Write(object obj);
        void WriteLine(object obj);
        void WriteException(System.Exception ex, string? message = null);
    }

    public class FileLogger : ILogger
    {
        System.IO.TextWriter writer;

        public FileLogger(System.IO.TextWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }

        public void Write(object text)
        {
            writer.Write(text);
        }

        public void WriteLine(object text)
        {
            writer.WriteLine(text);
        }
        public void WriteException(System.Exception ex, string? message)
        {
            if (message == null)
            {
                writer.WriteLine($"Exception: {ex.Message}");
            }
            else
            {
                writer.WriteLine($"{message}: {ex.Message}");
            }
        }
    }

    public class UnityLogger : ILogger
    {
        public void Dispose() { }

        public void Write(object text)
        {
            WriteLine(text);
        }

        public void WriteLine(object text)
        {
#if UNITY_EDITOR
            Debug.LogWarning(text.ToString());
#endif
        }

        public void WriteException(System.Exception ex, string? message = null)
        {
#if UNITY_EDITOR
            Debug.LogException(ex);
#endif
        }
    }

    public class NullLogger : ILogger
    {
        public void Dispose() { }

        public void Write(object text) { }

        public void WriteLine(object text) { }

        public void WriteException(System.Exception ex, string? message = null) { }
    }
}
