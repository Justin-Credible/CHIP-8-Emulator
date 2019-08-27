using System;
using System.Collections.Generic;
using Xunit;

namespace JustinCredible.c8asm.Tests
{
    public class Assembler_AssembleInstruction_Test
    {
        [Fact]
        public void HandlesRTS()
        {
            var instruction = "RTS";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x00EE, result);
        }

        [Fact]
        public void HandlesCLR()
        {
            var instruction = "CLR";
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
        public void HandlesJUMP()
        {
            var instruction = "JUMP $A23";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x1A23, result);
        }

        [Fact]
        public void HandlesJUMP_WithLabel()
        {
            var labels = new Dictionary<string, UInt16>() { { "MY_ROUTINE", 0xE6F } };
            var instruction = "JUMP MY_ROUTINE";
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
        public void HandlesSKE_WithHexLiteral()
        {
            var instruction = "SKE VA, #6E";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x3A6E, result);
        }

        [Fact]
        public void HandlesSKE_WithHexLiteralTooLarge()
        {
            var instruction = "SKE VA, #6EF";
            Assert.Throws<Exception>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesSKE_WithDecLiteral()
        {
            var instruction = "SKE VA, 167";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x3AA7, result);
        }

        [Fact]
        public void HandlesSKE_WithDecLiteralTooLarge()
        {
            var instruction = "SKE VA, 270";
            Assert.Throws<OverflowException>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesSKNE_WithHexLiteral()
        {
            var instruction = "SKNE VA, #6E";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x4A6E, result);
        }

        [Fact]
        public void HandlesSKNE_WithHexLiteralTooLarge()
        {
            var instruction = "SKNE VA, #6EF";
            Assert.Throws<Exception>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesSKNE_WithDecLiteral()
        {
            var instruction = "SKNE VA, 167";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x4AA7, result);
        }

        [Fact]
        public void HandlesSKNE_WithDecLiteralTooLarge()
        {
            var instruction = "SKE VA, 270";
            Assert.Throws<OverflowException>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesSKRE()
        {
            var instruction = "SKRE V2, VC";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x52C0, result);
        }

        [Fact]
        public void HandlesLOAD_WithHexLiteral()
        {
            var instruction = "LOAD VA, #6E";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x6A6E, result);
        }

        [Fact]
        public void HandlesLOAD_WithHexLiteralTooLarge()
        {
            var instruction = "LOAD VA, #6EF";
            Assert.Throws<Exception>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesLOAD_WithDecLiteral()
        {
            var instruction = "LOAD VA, 167";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x6AA7, result);
        }

        [Fact]
        public void HandlesLOAD_WithDecLiteralTooLarge()
        {
            var instruction = "LOAD VA, 270";
            Assert.Throws<OverflowException>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesADD_WithHexLiteral()
        {
            var instruction = "ADD VA, #6E";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x7A6E, result);
        }

        [Fact]
        public void HandlesADD_WithHexLiteralTooLarge()
        {
            var instruction = "ADD VA, #6EF";
            Assert.Throws<Exception>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesADD_WithDecLiteral()
        {
            var instruction = "ADD VA, 167";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x7AA7, result);
        }

        [Fact]
        public void HandlesADD_WithDecLiteralTooLarge()
        {
            var instruction = "ADD VA, 270";
            Assert.Throws<OverflowException>(() => Assembler.AssembleInstruction(instruction, null));
        }

        [Fact]
        public void HandlesCOPY()
        {
            var instruction = "COPY VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A60, result);
        }

        [Fact]
        public void HandlesOR()
        {
            var instruction = "OR VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A61, result);
        }

        [Fact]
        public void HandlesAND()
        {
            var instruction = "AND VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A62, result);
        }

        [Fact]
        public void HandlesXOR()
        {
            var instruction = "XOR VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A63, result);
        }

        [Fact]
        public void HandlesADDR()
        {
            var instruction = "ADDR VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A64, result);
        }

        [Fact]
        public void HandlesSUB()
        {
            var instruction = "SUB VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A65, result);
        }

        [Fact]
        public void HandlesSHR()
        {
            var instruction = "SHR VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A66, result);
        }

        [Fact]
        public void HandlesSUBN()
        {
            var instruction = "SUBN VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A67, result);
        }

        [Fact]
        public void HandlesSHL()
        {
            var instruction = "SHL VA, V6";
            var result = Assembler.AssembleInstruction(instruction, null);
            Assert.Equal(0x8A6E, result);
        }
    }
}
