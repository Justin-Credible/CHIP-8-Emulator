using System;
using static SDL2.SDL;

namespace JustinCredible.c8emu
{
    class GUITickEventArgs : EventArgs
    {
        // Out
        public SDL_Keycode? keyDown { get; set; }

        // In
        public byte[,] FrameBuffer { get; set; }
        public bool ShouldRender { get; set; }
        public bool PlaySound { get; set; }
        public bool ShouldQuit { get; set; }
    }
}
