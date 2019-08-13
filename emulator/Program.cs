using System;
using System.IO;

namespace JustinCredible.c8emu
{
    class Program
    {
        private static Emulator _emulator;

        public static void Main(string[] args)
        {
            byte[] rom;

            if (args.Length != 1)
                throw new ArgumentException("Pass the path to a ROM file.");

            if (File.Exists(args[0]))
                rom = System.IO.File.ReadAllBytes(args[0]);
            else
                throw new Exception($"Could not locate a ROM file at path {args[0]}");

            // TODO: Get ROM file path via standard File > Open dialog if one not specified
            // via the command line arguments.
            _emulator = new Emulator();
            _emulator.LoadRom(rom);

            var gui = new GUI();
            gui.Initialize("CHIP-8 Emulator", 640, 320, 10, 10);
            gui.OnTick += GUI_OnTick;
            gui.StartLoop();
            gui.Dispose();
        }

        private static void GUI_OnTick(GUITickEventArgs eventArgs)
        {
            _emulator.Step(eventArgs.ElapsedMilliseconds/*, eventArgs.Keys*/);
            eventArgs.FrameBuffer = _emulator.FrameBuffer;
            eventArgs.PlaySound = _emulator.PlaySound;
            eventArgs.ShouldQuit = _emulator.Finished;
        }
    }
}
