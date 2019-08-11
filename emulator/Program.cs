using System;
using System.Diagnostics;
using SDL2;

namespace JustinCredible.c8emu
{
    class Program
    {
        // private static Emulator _emulator;
        private static int pointX = 31;
        private static int pointY = 17;
        private static bool decrementX = false;
        private static bool decrementY = false;

        public static void Main(string[] args)
        {
            // _emulator = new Emulator();
            // var rom = System.IO.File.ReadAllBytes("../test.rom");
            // _emulator.LoadRom(rom);

            var gui = new GUI();
            gui.Initialize("CHIP-8 Emulator", 640, 320, 10, 10);
            gui.OnTick += GUI_OnTick;
            gui.StartLoop();
            gui.Dispose();
        }

        private static void GUI_OnTick(GUITickEventArgs eventArgs)
        {
            // _emulator.Step(eventArgs.ElapsedMilliseconds, eventArgs.Keys);
            // eventArgs.FrameBuffer = _emulator.FrameBuffer;
            // eventArgs.PlaySound = _emulator.PlaySound
            // eventArgs.ShouldQuit = _emulator.Finished;

            if (eventArgs.FrameBuffer == null)
                eventArgs.FrameBuffer = new byte[64, 32];

            var frameBuffer = eventArgs.FrameBuffer;

            #region Test Program

            if (pointX >= 64)
                decrementX = true;
            else if (pointX <= 0)
                decrementX = false;

            if (pointY >= 32)
                decrementY = true;
            else if (pointY <= 0)
                decrementY = false;

            if (decrementX)
                pointX--;
            else
                pointX++;

            if (decrementY)
                pointY--;
            else
                pointY++;

            for (var x = 0; x < 64; x++)
            {
                for (var y = 0; y < 32; y++)
                {
                    frameBuffer[x, y] = (byte)((x == pointX && y == pointY) ? 1 : 0);
                }
            }

            #endregion

            eventArgs.FrameBuffer = frameBuffer;
        }
    }
}
