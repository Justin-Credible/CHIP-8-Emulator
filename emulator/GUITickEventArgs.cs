using System;
using System.Diagnostics;
using SDL2;

namespace JustinCredible.c8emu
{
    class GUITickEventArgs : EventArgs
    {
        // Out
        public double ElapsedMilliseconds { get; set; }
        // public ??? Keys { get; set; } // TODO

        // In
        public byte[,] FrameBuffer { get; set; }
        public bool ShouldRender { get; set; }
        public bool PlaySound { get; set; }
        public bool ShouldQuit { get; set; }
    }
}
