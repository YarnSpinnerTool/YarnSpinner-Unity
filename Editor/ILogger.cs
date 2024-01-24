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
    public interface ILogger : IDisposable
    {
        void Write(object obj);
        void WriteLine(object obj);
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
    }

    public class NullLogger : ILogger
    {
        public void Dispose() { }

        public void Write(object text) { }

        public void WriteLine(object text) { }
    }
}
