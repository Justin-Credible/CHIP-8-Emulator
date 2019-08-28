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

        // TODO: Test DW and DB
    }
}
