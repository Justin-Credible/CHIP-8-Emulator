using System;
using System.Collections.Generic;
using Xunit;

namespace JustinCredible.c8asm.Tests
{
    public class Assembler_AssembleInstruction_Test
    {
        [Fact]
        public void HandlesRET()
        {
            var instruction = "RET";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x00EE, result);
        }

        [Fact]
        public void HandlesCLS()
        {
            var instruction = "CLS";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x00E0, result);
        }

        [Fact]
        public void HandlesSYS()
        {
            var instruction = "SYS $A23";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x0A23, result);
        }

        [Fact]
        public void HandlesSYS_WithLabel()
        {
            var labels = new Dictionary<string, UInt16>() { { "RCA_PROGRAM", 0xE6F } };
            var instruction = "SYS RCA_PROGRAM";
            var result = Assembler.AssembleInstruction(instruction, labels);
            Assert.Equal(0x0E6F, result);
        }

        [Fact]
        public void HandlesJP()
        {
            var instruction = "JP $A23";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x1A23, result);
        }

        [Fact]
        public void HandlesJP_WithLabel()
        {
            var labels = new Dictionary<string, UInt16>() { { "MY_ROUTINE", 0xE6F } };
            var instruction = "JP MY_ROUTINE";
            var result = Assembler.AssembleInstruction(instruction, labels);
            Assert.Equal(0x1E6F, result);
        }

        [Fact]
        public void HandlesCALL()
        {
            var instruction = "CALL $A23";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x2A23, result);
        }

        [Fact]
        public void HandlesCALL_WithLabel()
        {
            var labels = new Dictionary<string, UInt16>() { { "MY_ROUTINE", 0xE6F } };
            var instruction = "CALL MY_ROUTINE";
            var result = Assembler.AssembleInstruction(instruction, labels);
            Assert.Equal(0x2E6F, result);
        }

        [Fact]
        public void HandlesSE_WithHexLiteral()
        {
            var instruction = "SE VA, #6E";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x3A6E, result);
        }

        [Fact]
        public void HandlesSE_WithHexLiteralTooLarge()
        {
            var instruction = "SE VA, #6EF";
            Assert.Throws<Exception>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesSE_WithDecLiteral()
        {
            var instruction = "SE VA, 167";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x3AA7, result);
        }

        [Fact]
        public void HandlesSE_WithDecLiteralTooLarge()
        {
            var instruction = "SE VA, 270";
            Assert.Throws<OverflowException>(() => Assembler.AssembleInstruction(instruction, null));
        }
    }
}
