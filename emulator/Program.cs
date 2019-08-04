using System;

namespace JustinCredible.c8emu
{
    class Program
    {
        static void Main(string[] args)
        {
            var emulator = new Emulator();

            // TODO: CLI options
            var rom = System.IO.File.ReadAllBytes("../test.rom");
            emulator.LoadRom(rom);
            emulator.Run();
        }
    }
}
