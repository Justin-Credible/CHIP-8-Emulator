using System;
using System.Diagnostics;
using JustinCredible.c8asm;
using Xunit;

namespace JustinCredible.c8emu.Tests
{
    public class Emulator_Opcode_Test
    {
        [Fact]
        public void Opcode_6XNN_SetsDecimalAndHexLiteralBytes()
        {
            var source = @"
                LOAD V1, 1
                LOAD V2, #0A
                LOAD VB, 15
                LOAD VF, #FE
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(1, state.Registers[1]);
            Assert.Equal(10, state.Registers[2]);
            Assert.Equal(15, state.Registers[11]);
            Assert.Equal(254, state.Registers[15]);
        }

        private EmulatorState Execute(string source)
        {
            var rom = Assembler.AssembleSource(source);

            var emulator = new Emulator();
            emulator.LoadRom(rom);

            var iteration = 0;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!emulator.Finished)
            {
                if (iteration > 100)
                    throw new Exception("More than 100 iterations occurred.");

                var elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                emulator.Step(elapsedMilliseconds);

                iteration++;
            }

            return emulator.DumpState();
        }
    }
}
