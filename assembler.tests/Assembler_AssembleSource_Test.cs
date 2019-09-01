using System;
using Xunit;

namespace JustinCredible.c8asm.Tests
{
    public class Assembler_AssembleSource_Test
    {
        [Fact]
        public void EnsureSimpleSourceCanBeAssembled()
        {
            var source = @"
                LOAD V1, 1
                LOAD V2, #0A
                LOAD VB, 15
                LOAD VF, #FE
                RTS
            ";

            var rom = Assembler.AssembleSource(source);

            var expected = Utilities.SplitWords(new UInt16[]
            {
                0x6101,
                0x620A,
                0x6B0F,
                0x6FFE,
                0x00EE,
            });

            Assert.Equal(expected, rom);
        }

        [Fact]
        public void EnsureWhitespaceCommentsAreRemoved()
        {
            var source = @"


            ; this is a sample program


                    LOAD V1, 1 ; Load dec 1 into reg 1

  LOAD V2, #0A ; Load hex 0A into reg 2


               LOAD VB, 15      ; Load dec 15 into reg 11
                  LOAD VF, #FE
; nothing to see here
                RTS ;end!

                   ;
                ;
             ; nothing to see here
            ";

            var rom = Assembler.AssembleSource(source);

            var expected = Utilities.SplitWords(new UInt16[]
            {
                0x6101,
                0x620A,
                0x6B0F,
                0x6FFE,
                0x00EE,
            });

            Assert.Equal(expected, rom);
        }

        [Fact]
        public void EnsureDirectivesAreIgnored()
        {
            var source = @"
                option something_cool
                LOAD V1, 1
                RTS
            ";

            var rom = Assembler.AssembleSource(source);

            var expected = Utilities.SplitWords(new UInt16[]
            {
                0x6101,
                0x00EE,
            });

            Assert.Equal(expected, rom);
        }

        [Fact]
        public void EnsureLabelsAreResolved()
        {
            var source = String.Join(Environment.NewLine, new string[]
            {
                "START:",       // $200
                "LOAD V1, 1",
                "JUMP MIDDLE",  // $202
                "LOAD V1, 1",   // $204
                "MIDDLE:",      // $206
                "LOAD V1, 1",
                "JUMP END",     // $208
                "LOAD V1, 1",   // $20A
                "END:",         // $20C
                "RTS",          // $20E
                "JUMP START",   // $210
            });

            var rom = Assembler.AssembleSource(source);

            var expected = Utilities.SplitWords(new UInt16[]
            {
                0x6101, // LOAD V1, 1
                0x1206, // JUMP MIDDLE
                0x6101, // LOAD V1, 1
                0x6101, // LOAD V1, 1
                0x120C, // JUMP END
                0x6101, // LOAD V1, 1
                0x00EE, // RTS
                0x1200, // JUMP START
            });

            Assert.Equal(expected, rom);
        }

        [Fact]
        public void EnsureDataAndLabelsAreResolvedCorrectly()
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
                DB $..111111    ;       Hex Value: #3F
                DB $.....1..    ; $211  Hex Value: #04
                DB $.1...1..    ; $212  Hex Value: #44
                DB $.11111..    ; $213  Hex Value: #7C
                MASK:           ; $214
                ;DB $..111111
                ;DB $........
                ;DB $........
                ;DB $11111111
                DW #3F00        ; Use hex equivalent to binary above
                DW #00FF        ; $216
            ";

            var rom = Assembler.AssembleSource(source);

            // NOTE: Below you'll see subtraction of 0x200 to since we haven't loaded into RAM yet,
            // this is the ROM address on disk.

            // First check that the labels resolved correctly.

            // LOADI SPRITE at $200
            // SPRITE label is at $210
            // ANNN => 0xA210
            Assert.Equal(0xA2, rom[0x202 - 0x200]);
            Assert.Equal(0x10, rom[0x203 - 0x200]);

            // LOADI MASK at $20A
            // MASK label is at $214
            // ANNN => 0xA214
            // Subtract 0x200 to since we haven't loaded into RAM yet, this is the ROM address on disk.
            Assert.Equal(0xA2, rom[0x20A - 0x200]);
            Assert.Equal(0x14, rom[0x20B - 0x200]);

            // Check binary data inserted correctly.
            Assert.Equal(0x3F, rom[0x210 - 0x200]);
            Assert.Equal(0x04, rom[0x211 - 0x200]);
            Assert.Equal(0x44, rom[0x212 - 0x200]);
            Assert.Equal(0x7C, rom[0x213 - 0x200]);

            // Check hex data inserted correctly.
            Assert.Equal(0x3F, rom[0x214 - 0x200]);
            Assert.Equal(0x00, rom[0x215 - 0x200]);
            Assert.Equal(0x00, rom[0x216 - 0x200]);
            Assert.Equal(0xFF, rom[0x217 - 0x200]);
        }
    }
}
