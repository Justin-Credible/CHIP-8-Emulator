using System;
using SDL2;

namespace JustinCredible.c8emu
{
    class GUITickEventArgs : EventArgs
    {
        // Out
        public SDL.SDL_Keycode? keyDown { get; set; }

        // In
        public byte[,] FrameBuffer { get; set; }
        public bool ShouldRender { get; set; }
        public bool PlaySound { get; set; }
        public bool ShouldQuit { get; set; }
    }
}
