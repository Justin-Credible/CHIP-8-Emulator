using System;
using System.Collections.Generic;
using System.Diagnostics;
using JustinCredible.c8asm;
using Xunit;

namespace JustinCredible.c8emu.Tests
{
    public class Emulator_Opcode_Test
    {
        [Fact]
        public void Opcode_1XNN_JumpsForwardsAndBackwards()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "START:",       // $200
                "LOAD V1, 1",
                "JUMP MIDDLE",  // $202
                "LOAD V2, 1",   // $204
                "SECRET:",      // $206
                "LOAD V3, 1",
                "JUMP END",     // $208
                "MIDDLE:",      // $20A
                "LOAD V4, 1",
                "JUMP SECRET",  // $20C
                "LOAD V5, 1",   // $20E
                "END:",         // $210
                "RTS",
            });

            var state = Execute(source);

            UInt16[] expectedPcAddresses = new UInt16[]
            {
                0x200,
                0x202,
                0x20A,
                0x20C,
                0x206,
                0x208,
                0x210,
            };

            Assert.Equal(0x210, state.ProgramCounter);
            Assert.Equal(1, state.Registers[1]);
            Assert.Equal(0, state.Registers[2]);
            Assert.Equal(1, state.Registers[3]);
            Assert.Equal(1, state.Registers[4]);
            Assert.Equal(0, state.Registers[5]);
            Assert.Equal(expectedPcAddresses, state.ProgramCounterAddresses);
        }

        [Fact]
        public void Opcode_2NNN_00EE_CallsSubroutine()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "START:",           // $200
                "LOAD V1, 1",
                "CALL MY_ROUTINE",  // $202
                "LOAD V2, 1",       // $204
                "JUMP END",         // $206
                "LOAD V3, 1",       // $208
                "MY_ROUTINE:",      // $20A
                "LOAD V4, 1",
                "LOAD V5, 1",       // $20C
                "RTS",              // $20E
                "LOAD V6, 1",       // $210
                "END:",             // $212
                "LOAD V7, 1",
                "RTS",              // $214
            });

            var state = Execute(source);

            UInt16[] expectedPcAddresses = new UInt16[]
            {
                0x200,
                0x202,
                0x20A,
                0x20C,
                0x20E,
                0x204,
                0x206,
                0x212,
                0x214,
            };

            Assert.Equal(0x214, state.ProgramCounter);
            Assert.Equal(1, state.Registers[1]);
            Assert.Equal(1, state.Registers[2]);
            Assert.Equal(0, state.Registers[3]);
            Assert.Equal(1, state.Registers[4]);
            Assert.Equal(1, state.Registers[5]);
            Assert.Equal(0, state.Registers[6]);
            Assert.Equal(1, state.Registers[7]);
            Assert.Equal(expectedPcAddresses, state.ProgramCounterAddresses);
        }

        [Fact]
        public void Opcode_3XNN_SkipsNextInstruction()
        {
            var source = @"
                LOAD V1, #AF
                SKE V1, 175
                LOAD VF, 1
                SKE V1, #AF
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (5 * 2), state.ProgramCounter);
            Assert.Equal(175, state.Registers[1]);
            Assert.Equal(0, state.Registers[15]);
            Assert.Equal(0, state.Registers[14]);
        }

        [Fact]
        public void Opcode_3XNN_DoesntSkipNextInstruction()
        {
            var source = @"
                LOAD V1, #AF
                SKE V1, 50
                LOAD VF, 1
                SKE V1, #AE
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (5 * 2), state.ProgramCounter);
            Assert.Equal(175, state.Registers[1]);
            Assert.Equal(1, state.Registers[15]);
            Assert.Equal(1, state.Registers[14]);
        }

        [Fact]
        public void Opcode_4XNN_SkipsNextInstruction()
        {
            var source = @"
                LOAD V1, #AE
                SKNE V1, 175
                LOAD VF, 1
                SKNE V1, #AF
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (5 * 2), state.ProgramCounter);
            Assert.Equal(174, state.Registers[1]);
            Assert.Equal(0, state.Registers[15]);
            Assert.Equal(0, state.Registers[14]);
        }

        [Fact]
        public void Opcode_4XNN_DoesntSkipNextInstruction()
        {
            var source = @"
                LOAD V1, #AF
                SKNE V1, 175
                LOAD VF, 1
                SKNE V1, #AF
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (5 * 2), state.ProgramCounter);
            Assert.Equal(175, state.Registers[1]);
            Assert.Equal(1, state.Registers[15]);
            Assert.Equal(1, state.Registers[14]);
        }

        [Fact]
        public void Opcode_5XY0_SkipsNextInstruction()
        {
            var source = @"
                LOAD V1, 1
                LOAD V2, 1
                SKRE V1, V2
                LOAD VF, 1
                SKRE V2, V1
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (6 * 2), state.ProgramCounter);
            Assert.Equal(1, state.Registers[1]);
            Assert.Equal(1, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]);
            Assert.Equal(0, state.Registers[14]);
        }

        [Fact]
        public void Opcode_5XY0_DoesntSkipNextInstruction()
        {
            var source = @"
                LOAD V1, 1
                LOAD V2, 2
                SKRE V1, V2
                LOAD VF, 1
                SKRE V2, V1
                LOAD VE, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (6 * 2), state.ProgramCounter);
            Assert.Equal(1, state.Registers[1]);
            Assert.Equal(2, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]);
            Assert.Equal(1, state.Registers[14]);
        }

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

        [Fact]
        public void Opcode_7XNN_AddsWithoutOverflow()
        {
            var source = @"
                LOAD V1, 2
                LOAD V2, #A6
                ADD V1, 13
                ADD V2, #56
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(15, state.Registers[1]);
            Assert.Equal(252, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // no overflow
        }

        [Fact]
        public void Opcode_7XNN_AddsWithOverflow()
        {
            var source = @"
                LOAD VF, 77
                LOAD V1, #FE
                ADD V1, 5
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (3 * 2), state.ProgramCounter);
            Assert.Equal(3, state.Registers[1]);
            Assert.Equal(77, state.Registers[15]); // 7XNN doesn't set carry flag, so VF should remain unchanged.
        }

        [Fact]
        public void Opcode_8XY0_CopiesRegisterValues()
        {
            var source = @"
                LOAD V1, 77
                LOAD V2, #FE
                LOAD V5, #AA
                COPY V3, V1
                COPY V4, V2
                COPY V5, V3
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (6 * 2), state.ProgramCounter);
            Assert.Equal(77, state.Registers[1]);
            Assert.Equal(254, state.Registers[2]);
            Assert.Equal(77, state.Registers[3]);
            Assert.Equal(254, state.Registers[4]);
            Assert.Equal(77, state.Registers[5]);
        }

        [Fact]
        public void Opcode_8XY1_PerformsLogicalOR()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "LOAD V1, #4D", //  77  1001101
                "LOAD V2, #29", //  41  0101001
                "OR   V2, V1",  //  OR  1101101   109 #6D
                "RTS",
            });

            var state = Execute(source);

            Assert.Equal(0x200 + (3 * 2), state.ProgramCounter);
            Assert.Equal(77, state.Registers[1]);
            Assert.Equal(109, state.Registers[2]);
        }

        [Fact]
        public void Opcode_8XY2_PerformsLogicalAND()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "LOAD V1, #4D", //  77  1001101
                "LOAD V2, #29", //  41  0101001
                "AND  V2, V1",  // AND  0001001   0 #09
                "RTS",
            });

            var state = Execute(source);

            Assert.Equal(0x200 + (3 * 2), state.ProgramCounter);
            Assert.Equal(77, state.Registers[1]);
            Assert.Equal(9, state.Registers[2]);
        }

        [Fact]
        public void Opcode_8XY3_PerformsLogicalXOR()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "LOAD V1, #4D", //  77  1001101
                "LOAD V2, #29", //  41  0101001
                "XOR  V2, V1",  // XOR  1100100  100 #64
                "RTS",
            });

            var state = Execute(source);

            Assert.Equal(0x200 + (3 * 2), state.ProgramCounter);
            Assert.Equal(77, state.Registers[1]);
            Assert.Equal(100, state.Registers[2]);
        }

        [Fact]
        public void Opcode_8XY4_AddsWithoutOverflow()
        {
            var source = @"
                LOAD V1, 41
                LOAD V2, 87
                LOAD VF, #AA
                ADDR V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(41, state.Registers[1]);
            Assert.Equal(128, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // no overflow/carry
        }

        [Fact]
        public void Opcode_8XY4_AddsWithOverflow()
        {
            var source = @"
                LOAD V1, #EF
                LOAD V2, #C8
                LOAD VF, #AA
                ADDR V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(239, state.Registers[1]);
            Assert.Equal(183, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // overflow/carry occurred
        }

        [Fact]
        public void Opcode_8XY5_SubtractsWithoutBorrow()
        {
            var source = @"
                LOAD V1, 87
                LOAD V2, 41
                LOAD VF, #AA
                SUB V1, V2
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(46, state.Registers[1]);
            Assert.Equal(41, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // no borrow
        }

        [Fact]
        public void Opcode_8XY5_SubtractsWithBorrow()
        {
            var source = @"
                LOAD V1, 41
                LOAD V2, 87
                LOAD VF, #AA
                SUB V1, V2
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(210, state.Registers[1]);
            Assert.Equal(87, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // borrow
        }

        [Fact]
        public void Opcode_8XY6_ShiftsRightWith1LSB()
        {
            // 41: 101001
            // 41 >> 1: 010100 = 20
            var source = @"
                LOAD V1, 41
                LOAD V2, #AA
                LOAD VF, #AA
                SHR V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(41, state.Registers[1]);
            Assert.Equal(20, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // LSB before shift was 1
        }

        [Fact]
        public void Opcode_8XY6_ShiftsRightWith0LSB()
        {
            // 40: 101000
            // 41 >> 1: 010100 = 20
            var source = @"
                LOAD V1, 40
                LOAD V2, #AA
                LOAD VF, #AA
                SHR V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(40, state.Registers[1]);
            Assert.Equal(20, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // LSB before shift was 0
        }


        [Fact]
        public void Opcode_8XY6_ShiftsRightOnSameRegister()
        {
            // 41: 101001
            // 41 >> 1: 010100 = 20
            var source = @"
                LOAD V1, 41
                LOAD V2, #AA
                LOAD VF, #AA
                SHR V1, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(20, state.Registers[1]);
            Assert.Equal(170, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // LSB before shift was 1
        }

        [Fact]
        public void Opcode_8XY7_SubtractsWithoutBorrow()
        {
            var source = @"
                LOAD V1, 87
                LOAD V2, 41
                LOAD VF, #AA
                SUBN V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(87, state.Registers[1]);
            Assert.Equal(46, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // no borrow
        }

        [Fact]
        public void Opcode_8XY7_SubtractsWithBorrow()
        {
            var source = @"
                LOAD V1, 41
                LOAD V2, 87
                LOAD VF, #AA
                SUBN V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(41, state.Registers[1]);
            Assert.Equal(210, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // borrow
        }

        [Fact]
        public void Opcode_8XY6_ShiftsLeftWith1MSB()
        {
            // 169: 10101001 #A6
            // 169 << 1: 01010010 = 82 #52
            var source = @"
                LOAD V1, 169
                LOAD V2, #AA
                LOAD VF, #AA
                SHL V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(169, state.Registers[1]);
            Assert.Equal(82, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]); // MSB before shift was 1
        }

        [Fact]
        public void Opcode_8XY6_ShiftsLeftWith0MSB()
        {
            // 41: 00101001 #29
            // 41 << 1: 01010010 = 82 #52
            var source = @"
                LOAD V1, 41
                LOAD V2, #AA
                LOAD VF, #AA
                SHL V2, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(41, state.Registers[1]);
            Assert.Equal(82, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // MSB before shift was 0
        }

        [Fact]
        public void Opcode_8XYE_ShiftsLeftOnSameRegister()
        {
            // 41: 00101001 #29
            // 41 << 1: 01010010 = 82 #52
            var source = @"
                LOAD V1, 41
                LOAD V2, #AA
                LOAD VF, #AA
                SHL V1, V1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(82, state.Registers[1]);
            Assert.Equal(170, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]); // MSB before shift was 0
        }

        [Fact]
        public void Opcode_9XY0_SkipsNextInstruction()
        {
            var source = @"
                LOAD V1, #AE
                LOAD V2, #AF
                SKRNE V1, V2
                LOAD VF, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(174, state.Registers[1]);
            Assert.Equal(175, state.Registers[2]);
            Assert.Equal(0, state.Registers[15]);
        }

        [Fact]
        public void Opcode_9XY0_DoesntSkipNextInstruction()
        {
            var source = @"
                LOAD V1, #AE
                LOAD V2, #AE
                SKRNE V1, V2
                LOAD VF, 1
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (4 * 2), state.ProgramCounter);
            Assert.Equal(174, state.Registers[1]);
            Assert.Equal(174, state.Registers[2]);
            Assert.Equal(1, state.Registers[15]);
        }

        [Fact]
        public void Opcode_ANNN_SetsRegisterI()
        {
            var source = @"
                LOADI $123
                RTS
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (1 * 2), state.ProgramCounter);
            Assert.Equal(291, state.IndexRegister);
        }

        [Fact]
        public void Opcode_ANNN_SetsRegisterIWithLabel()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "LOADI MY_DATA",    // $200
                "RTS",              // $202
                "MY_DATA:",         // $204 (516 dec)
            });

            var state = Execute(source);

            Assert.Equal(0x200 + (1 * 2), state.ProgramCounter);
            Assert.Equal(516, state.IndexRegister);
        }

        [Fact]
        public void Opcode_BNNN_JumpsWithWithNoOffset()
        {
            var source = @"
                LOAD V0, 0  ; $200
                JUMPI $208  ; $202
                LOAD V1, 1  ; $204
                LOAD V2, 1  ; $206
                LOAD V3, 1  ; $208
                LOAD V4, 1  ; $20A
                LOAD V5, 1  ; $20C
                LOAD V6, 1  ; $20E
                RTS         ; $210
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (8 * 2), state.ProgramCounter);
            Assert.Equal(0, state.Registers[0]);
            Assert.Equal(0, state.Registers[1]);
            Assert.Equal(0, state.Registers[2]);
            Assert.Equal(1, state.Registers[3]);
            Assert.Equal(1, state.Registers[4]);
            Assert.Equal(1, state.Registers[5]);
            Assert.Equal(1, state.Registers[6]);
        }

        [Fact]
        public void Opcode_BNNN_JumpsWithWithOffset()
        {
            var source = @"
                LOAD V0, 6  ; $200
                JUMPI $208  ; $202
                LOAD V1, 1  ; $204
                LOAD V2, 1  ; $206
                LOAD V3, 1  ; $208
                LOAD V4, 1  ; $20A
                LOAD V5, 1  ; $20C
                LOAD V6, 1  ; $20E
                RTS         ; $210
            ";

            var state = Execute(source);

            Assert.Equal(0x200 + (8 * 2), state.ProgramCounter);
            Assert.Equal(6, state.Registers[0]);
            Assert.Equal(0, state.Registers[1]);
            Assert.Equal(0, state.Registers[2]);
            Assert.Equal(0, state.Registers[3]);
            Assert.Equal(0, state.Registers[4]);
            Assert.Equal(0, state.Registers[5]);
            Assert.Equal(1, state.Registers[6]);
        }

        [Fact]
        public void Opcode_CXNN_GeneratesRandomWithFFMask()
        {
            var source = @"
                RAND V0, #FF
                RTS
            ";

            // Ensure the random number generator is initialized with the
            // same seed during the test so we get the same result each run.
            // Seed 123 will generate the number 3 on the first run.
            var seed = 123;

            var state = Execute(source, seed);

            Assert.Equal(0x200 + (1 * 2), state.ProgramCounter);
            Assert.Equal(3, state.Registers[0]);
        }

        [Fact]
        public void Opcode_CXNN_GeneratesRandomWith00Mask()
        {
            var source = @"
                RAND V0, #00
                RTS
            ";

            // Ensure the random number generator is initialized with the
            // same seed during the test so we get the same result each run.
            // Seed 123 will generate the number 3 on the first run.
            var seed = 123;

            var state = Execute(source, seed);

            Assert.Equal(0x200 + (1 * 2), state.ProgramCounter);
            Assert.Equal(0, state.Registers[0]);
        }

        [Fact]
        public void Opcode_CXNN_GeneratesRandomWithMask()
        {
            var source = @"
                RAND V0, #5D
                RTS
            ";

            // Ensure the random number generator is initialized with the
            // same seed during the test so we get the same result each run.
            // Seed 123 will generate the number 143 on the first run.
            var seed = 4673;

            // 143  #8F     10001111        (first random number)
            //  93  #5D     01011101        (bitmask from opcode)
            //                AND
            //  13  #0D     00001101        (result expected in v0)

            var state = Execute(source, seed);

            Assert.Equal(0x200 + (1 * 2), state.ProgramCounter);
            Assert.Equal(13, state.Registers[0]);
        }

        [Fact]
        public void Opcode_DXYN_DrawsSprite()
        {
            var source = @"
                LOAD VF, #AA    ; $200
                LOADI SPRITE    ; $202
                LOAD V0, 10     ; $204
                LOAD V1, 5      ; $206
                DRAW V0, V1, 4  ; $208
                RTS             ; $20A
                SPRITE:         ; $20C
                DB $..111111    ; (10, 5) to (17, 5)
                DB $.....1..    ; (10, 6) to (17, 6)
                DB $.1...1..    ; (10, 7) to (17, 7)
                DB $.11111..    ; (10, 8) to (17, 8)
            ";

            var bytes = c8asm.Utilities.FormatAsOpcodeGroups(Assembler.AssembleSource(source));

            var state = Execute(source);

            Assert.Equal(0x200 + (5 * 2), state.ProgramCounter);
            Assert.Equal(0x20C, state.IndexRegister);
            Assert.Equal(0, state.Registers[15]); // No collision detected

            // First row (10, 5) to (17, 5)
            Assert.Equal(0, state.FrameBuffer[10, 5]);
            Assert.Equal(0, state.FrameBuffer[11, 5]);
            Assert.Equal(1, state.FrameBuffer[12, 5]);
            Assert.Equal(1, state.FrameBuffer[13, 5]);
            Assert.Equal(1, state.FrameBuffer[14, 5]);
            Assert.Equal(1, state.FrameBuffer[15, 5]);
            Assert.Equal(1, state.FrameBuffer[16, 5]);
            Assert.Equal(1, state.FrameBuffer[17, 5]);

            // Second row (10, 6) to (17, 6)
            Assert.Equal(0, state.FrameBuffer[10, 6]);
            Assert.Equal(0, state.FrameBuffer[11, 6]);
            Assert.Equal(0, state.FrameBuffer[12, 6]);
            Assert.Equal(0, state.FrameBuffer[13, 6]);
            Assert.Equal(0, state.FrameBuffer[14, 6]);
            Assert.Equal(1, state.FrameBuffer[15, 6]);
            Assert.Equal(0, state.FrameBuffer[16, 6]);
            Assert.Equal(0, state.FrameBuffer[17, 6]);

            // Third row (10, 7) to (17, 7)
            Assert.Equal(0, state.FrameBuffer[10, 7]);
            Assert.Equal(1, state.FrameBuffer[11, 7]);
            Assert.Equal(0, state.FrameBuffer[12, 7]);
            Assert.Equal(0, state.FrameBuffer[13, 7]);
            Assert.Equal(0, state.FrameBuffer[14, 7]);
            Assert.Equal(1, state.FrameBuffer[15, 7]);
            Assert.Equal(0, state.FrameBuffer[16, 7]);
            Assert.Equal(0, state.FrameBuffer[17, 7]);

            // Fourth row (10, 8) to (17, 8)
            Assert.Equal(0, state.FrameBuffer[10, 8]);
            Assert.Equal(1, state.FrameBuffer[11, 8]);
            Assert.Equal(1, state.FrameBuffer[12, 8]);
            Assert.Equal(1, state.FrameBuffer[13, 8]);
            Assert.Equal(1, state.FrameBuffer[14, 8]);
            Assert.Equal(1, state.FrameBuffer[15, 8]);
            Assert.Equal(0, state.FrameBuffer[16, 8]);
            Assert.Equal(0, state.FrameBuffer[17, 8]);
        }

        [Fact]
        public void Opcode_DXYN_DrawsSpriteWithCollisions()
        {
            var source = @"
                LOAD VF, #AA    ; $200
                LOADI SPRITE    ; $202
                LOAD V0, 10     ; $204
                LOAD V1, 5      ; $206
                DRAW V0, V1, 4  ; $208
                LOADI MASK      ; $20A
                DRAW V0, V1, 4  ; $20C
                RTS             ; $20E
                SPRITE:         ; $210
                DB $..111111    ; (10, 5) to (17, 5)
                DB $.....1..    ; (10, 6) to (17, 6)    $211
                DB $.1...1..    ; (10, 7) to (17, 7)    $212
                DB $.11111..    ; (10, 8) to (17, 8)    $213
                MASK:           ; $214
                DB $..111111
                DB $........
                DB $........
                DB $11111111
            ";

            var bytes = c8asm.Utilities.FormatAsOpcodeGroups(Assembler.AssembleSource(source));

            var state = Execute(source);

            Assert.Equal(0x200 + (7 * 2), state.ProgramCounter);
            Assert.Equal(0x214, state.IndexRegister);
            Assert.Equal(1, state.Registers[15]); // Collision detected

            // First row (10, 5) to (17, 5)
            Assert.Equal(0, state.FrameBuffer[10, 5]);
            Assert.Equal(0, state.FrameBuffer[11, 5]);
            Assert.Equal(0, state.FrameBuffer[12, 5]);
            Assert.Equal(0, state.FrameBuffer[13, 5]);
            Assert.Equal(0, state.FrameBuffer[14, 5]);
            Assert.Equal(0, state.FrameBuffer[15, 5]);
            Assert.Equal(0, state.FrameBuffer[16, 5]);
            Assert.Equal(0, state.FrameBuffer[17, 5]);

            // Second row (10, 6) to (17, 6)
            Assert.Equal(0, state.FrameBuffer[10, 6]);
            Assert.Equal(0, state.FrameBuffer[11, 6]);
            Assert.Equal(0, state.FrameBuffer[12, 6]);
            Assert.Equal(0, state.FrameBuffer[13, 6]);
            Assert.Equal(0, state.FrameBuffer[14, 6]);
            Assert.Equal(1, state.FrameBuffer[15, 6]);
            Assert.Equal(0, state.FrameBuffer[16, 6]);
            Assert.Equal(0, state.FrameBuffer[17, 6]);

            // Third row (10, 7) to (17, 7)
            Assert.Equal(0, state.FrameBuffer[10, 7]);
            Assert.Equal(1, state.FrameBuffer[11, 7]);
            Assert.Equal(0, state.FrameBuffer[12, 7]);
            Assert.Equal(0, state.FrameBuffer[13, 7]);
            Assert.Equal(0, state.FrameBuffer[14, 7]);
            Assert.Equal(1, state.FrameBuffer[15, 7]);
            Assert.Equal(0, state.FrameBuffer[16, 7]);
            Assert.Equal(0, state.FrameBuffer[17, 7]);

            // Fourth row (10, 8) to (17, 8)
            Assert.Equal(1, state.FrameBuffer[10, 8]);
            Assert.Equal(0, state.FrameBuffer[11, 8]);
            Assert.Equal(0, state.FrameBuffer[12, 8]);
            Assert.Equal(0, state.FrameBuffer[13, 8]);
            Assert.Equal(0, state.FrameBuffer[14, 8]);
            Assert.Equal(0, state.FrameBuffer[15, 8]);
            Assert.Equal(1, state.FrameBuffer[16, 8]);
            Assert.Equal(1, state.FrameBuffer[17, 8]);
        }

        private UnitTestEmulatorState Execute(string source)
        {
            return Execute(source, -1);
        }

        private UnitTestEmulatorState Execute(string source, int seed)
        {
            var rom = Assembler.AssembleSource(source);

            var emulator = new Emulator();
            emulator.Reset(seed);
            emulator.LoadRom(rom);

            var iteration = 0;

            var pcAddresses = new List<UInt16>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!emulator.Finished)
            {
                if (iteration > 100)
                    throw new Exception("More than 100 iterations occurred.");

                pcAddresses.Add(emulator.DumpState().ProgramCounter);

                var elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                emulator.Step(elapsedMilliseconds);

                iteration++;
            }

            var state = emulator.DumpState();

            var extendedState = new UnitTestEmulatorState(state);

            extendedState.ProgramCounterAddresses = pcAddresses.ToArray();

            return extendedState;
        }
    }
}
