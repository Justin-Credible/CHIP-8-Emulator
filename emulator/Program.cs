using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;

namespace JustinCredible.c8emu
{
    class Program
    {
        private static CommandLineApplication _app;

        // Flags set via command line arguments.
        private static bool _debug = false;
        private static bool _logPerformance = false;

        private static Emulator _emulator;
        private static bool _shouldStep = false;

        private static bool _guiClosed = false;
        private static Stopwatch _guiPerformanceWatch;
        private static int _guiTickCounter;

        // Used to pass data from the emulator thread's loop to the GUI loop.
        private static byte[,] _frameBuffer;
        private static bool _renderFrameNextTick = false;
        private static bool _playSoundNextTick = false;

        public static void Main(string[] args)
        {
            var version = Utilities.AppVersion;

            _app = new CommandLineApplication();
            _app.Name = "c8emu";
            _app.HelpOption("-?|-h|--help");

            _app.VersionOption("-v|--version",

                // Used for HelpOption() header
                $"{_app.Name} {version}",

                // Used for output of --version option.
                version
            );

            // When launched without any commands or options.
            _app.OnExecute(() =>
            {
                _app.ShowHelp();
                return 0;
            });

            _app.Command("run", Run);

            _app.Execute(args);
        }

        private static void Run(CommandLineApplication command)
        {
            command.Description = "Runs the emulator using the given ROM file.";
            command.HelpOption("-?|-h|--help");

            var romPathArg = command.Argument("[ROM path]", "The path to a ROM file to load.");

            var debugOption = command.Option("-d|--debug", "Run in debug mode; dumps registers to console and requires pressing F10 to step the emulator.", CommandOptionType.NoValue);
            var performanceOption = command.Option("-p|--perfmon", "Performance monitor; write stats to the console while running.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                byte[] rom;

                if (File.Exists(romPathArg.Value))
                    rom = System.IO.File.ReadAllBytes(romPathArg.Value);
                else
                    throw new Exception($"Could not locate a ROM file at path {romPathArg}");

                if (debugOption.HasValue())
                    _debug = true;

                if (performanceOption.HasValue())
                    _logPerformance = true;

                // TODO: Get ROM file path via standard File > Open dialog if one not specified
                // via the command line arguments.
                _emulator = new Emulator();
                _emulator.LoadRom(rom);

                var gui = new GUI();
                gui.Initialize("CHIP-8 Emulator", 640, 320, 10, 10);
                gui.OnTick += GUI_OnTick;

                if (_logPerformance)
                {
                    _guiTickCounter = 0;
                    _guiPerformanceWatch = new Stopwatch();
                    _guiPerformanceWatch.Start();
                }

                var emulatorThread = new Thread(new ThreadStart(EmulatorLoop));
                emulatorThread.Start();

                gui.StartLoop();
                gui.Dispose();
                _guiClosed = true;

                return 0;
            });
        }

        private static void EmulatorLoop()
        {
            var stepCounter = 0;
            Stopwatch performanceWatch = null;

            if (_logPerformance)
            {
                performanceWatch = new Stopwatch();
                performanceWatch.Start();
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // TODO: Make step delay configurable so it can be set per ROM.
            var stepDelay = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);

            while (!_emulator.Finished && !_guiClosed)
            {
                var elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

                // If we're running in debug mode, dump the PC, registers, etc and
                // then wait until the step key is pressed before continuing.
                if (_debug)
                {
                    _emulator.PrintDebugSummary();

                    while (!_shouldStep)
                    {
                        if (_guiClosed)
                            return;

                        Thread.Sleep(250);
                    }

                    _shouldStep = false;
                }

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

                if (_logPerformance)
                {
                    stepCounter++;

                    if (performanceWatch.ElapsedMilliseconds >= 1000)
                    {
                        Console.WriteLine("Opcodes per second: " + stepCounter);
                        stepCounter = 0;
                        performanceWatch.Restart();
                    }
                }

                Thread.Sleep(stepDelay);
            }
        }

        private static void GUI_OnTick(GUITickEventArgs eventArgs)
        {
            // TODO: Save off pressed keys.

            if (_debug && eventArgs.keyDown != null && eventArgs.keyDown == SDL2.SDL.SDL_Keycode.SDLK_F10)
                _shouldStep = true;

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

            if (_logPerformance)
            {
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
}
