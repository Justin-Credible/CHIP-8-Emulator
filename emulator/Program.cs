using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace JustinCredible.c8emu
{
    class Program
    {
        private static Emulator _emulator;
        private static bool _guiClosed = false;
        private static Stopwatch _guiPerformanceWatch;
        private static int _guiTickCounter;

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

            _guiTickCounter = 0;
            _guiPerformanceWatch = new Stopwatch();
            _guiPerformanceWatch.Start();

            var emulatorThread = new Thread(new ThreadStart(EmulatorLoop));
            emulatorThread.Start();

            gui.StartLoop();
            gui.Dispose();
            _guiClosed = true;
        }

        private static byte[,] _frameBuffer;
        private static bool _renderFrameNextTick = false;
        private static bool _playSoundNextTick = false;

        private static void EmulatorLoop()
        {
            var stepCounter = 0;
            var performanceWatch = new Stopwatch();
            performanceWatch.Start();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // TODO: Make step delay configurable so it can be set per ROM.
            var stepDelay = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);

            while (!_emulator.Finished && !_guiClosed)
            {
                var elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                // TODO: Pass in pressed keys.
                _emulator.Step(elapsedMilliseconds /*, _pressedKeys */);

                if (_emulator.FrameBufferUpdated)
                {
                    _frameBuffer = _emulator.FrameBuffer;
                    _renderFrameNextTick = true;
                }

                if (_emulator.PlaySound)
                    _playSoundNextTick = true;

                stepCounter++;

                if (performanceWatch.ElapsedMilliseconds >= 1000)
                {
                    Console.WriteLine("Opcodes per second: " + stepCounter);
                    stepCounter = 0;
                    performanceWatch.Restart();
                }

                Thread.Sleep(stepDelay);
            }
        }

        private static void GUI_OnTick(GUITickEventArgs eventArgs)
        {
            // TODO: Save off pressed keys.

            if (_renderFrameNextTick)
            {
                eventArgs.FrameBuffer = _frameBuffer;
                eventArgs.ShouldRender = true;
                _renderFrameNextTick = false;
            }

            if (_playSoundNextTick)
            {
                eventArgs.PlaySound = true;
                _playSoundNextTick = false;
            }

            _guiTickCounter++;

            if (_guiPerformanceWatch.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine("GUI ticks per second: " + _guiTickCounter);
                _guiTickCounter = 0;
                _guiPerformanceWatch.Restart();
            }
        }
    }
}
