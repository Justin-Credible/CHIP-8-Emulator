using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;

namespace JustinCredible.c8asm
{
    public class Assembler
    {
        private static readonly Regex _labelRegEx = new Regex("([A-Za-z_]):");
        private static readonly Regex _dataHexRegEx = new Regex("DW #([0-9A-F]{4})");
        private static readonly Regex _dataBinaryRegEx = new Regex("DB $([01.]{8})");

        private static readonly Regex _instructionRegEx = new Regex("^(?'inst'[A-Za-z]+)(?:\\s+(?'opand1'[A-Za-z0-9#$_]+)(?:,\\s+(?'opand2'[A-Za-z0-9#$_]+)(?:,\\s+(?'opand3'[A-Za-z0-9#$_]+))?)?)?$");
        private static readonly Regex _addressRegEx = new Regex("^\\$[0-9A-F]{3}$");
        private static readonly Regex _hexLiteralValueRegEx = new Regex("^#[0-9A-F]{2}$");
        private static readonly Regex _decLiteralValueRegEx = new Regex("^[0-9]{1,3}$");
        private static readonly Regex _registerRegEx = new Regex("^V[0-9A-F]$");

        // Instructions and how many operands they expect.
        // Using the easier list from here: https://github.com/craigthomas/Chip8Assembler#chip-8-mnemonics
        // just to get up and running for now. Ultimately this is the list that should be implemented for
        // the widest compatibility: http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
        private static readonly Dictionary<string, int> _instructions = new Dictionary<string, int>()
        {
            { "SYS", 1 },
            { "CLR", 0 },
            { "RTS", 0 },
            { "JUMP", 1 },
            { "CALL", 1 },
            { "SKE", 2 },
            { "SKNE", 2 },
            { "SKRE", 2 },
            { "LOAD", 2 },
            { "ADD", 2 },
            { "COPY", 2 }, // Named MOVE in the spec, but is actually a COPY.
            { "OR", 2 },
            { "AND", 2 },
            { "XOR", 2 },
            { "ADDR", 2 },
            { "SUB", 2 },
            { "SHR", 2 },
            { "SUBN", 2 }, // Missing from the spec.
            { "SHL", 2 },
            { "SKRNE", 2 },
            { "LOADI", 1 },
            { "JUMPI", 1 },
            { "RAND", 2 },
            { "DRAW", 3 },
            { "SKPR", 1 },
            { "SKUP", 1 },
            { "MOVED", 1 },
            { "KEYD", 1 },
            { "LOADD", 1 },
            { "LOADS", 1 },
            { "ADDI", 1 },
            { "LDSPR", 1 },
            { "BCD", 1 },
            { "STOR", 1 },
            { "READ", 1 },
        };

        public static byte[] AssembleSource(string source)
        {
            var rom = new List<byte>();

            UInt16 pointer = 0x200;
            var labels = new Dictionary<string, UInt16>();

            // TODO: Detect line ending type and use it to split.
            var sourceLines = source.Split(Environment.NewLine);

            // First pass; calculate memory addresses for labels. Look at each line to see if
            // it is an instruction or literal data and incremenet the counter. If a label is
            // encountered store the current location of the pointer.
            for (var i = 0; i < sourceLines.Length; i++)
            {
                var lineNumber = i + 1;
                var rawSourceLine = sourceLines[i];
                var sourceLine = TrimAndStripComments(rawSourceLine);

                if (String.IsNullOrEmpty(sourceLine) || IsDirective(sourceLine))
                {
                    // Ignore empty lines, comments, and directives.
                    // These will not be included in the ROM so we don't need to count them.
                    continue;
                }
                else if (IsLabel(sourceLine))
                {
                    // Parse the label name and store the current memory address.
                    var matches = _labelRegEx.Match(sourceLine);

                    var label = matches.Groups[1].Value;

                    if (labels.ContainsKey(label))
                        throw new Exception($"Duplicate label '{label}' encountered on line {lineNumber}.");
                    
                    labels.Add(label, pointer);
                }
                else if (IsInstruction(sourceLine) || IsData(sourceLine))
                {
                    // If this is a valid instruction or data that will be embedded in the ROM,
                    // then increment the pointer to account for it, and continue on.
                    pointer += 2;
                    continue;
                }
                else
                {
                    // Either the assembler encountered a syntax error or unhandled instruction.
                    throw new Exception($"Unexpected or unknown instruction '{sourceLine}' during label pass on line {lineNumber}.");
                }
            }

            // Second pass; assemble opcodes from instructions.
            for (var i = 0; i < sourceLines.Length; i++)
            {
                var lineNumber = i + 1;
                var rawSourceLine = sourceLines[i];
                var sourceLine = TrimAndStripComments(rawSourceLine);

                if (String.IsNullOrEmpty(sourceLine) || IsDirective(sourceLine))
                {
                    // Ignore empty lines and comments.
                    // Directives are not currently supported.
                    continue;
                }
                else if (IsLabel(sourceLine))
                {
                    // Labels are not included in the ROM output.
                    continue;
                }
                else if (IsInstruction(sourceLine))
                {
                    // Parse the instruction and assemble into an opcode.
                    UInt16 opcode;

                    try
                    {
                        opcode = AssembleInstruction(sourceLine, labels);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"An error occurred while assembling instruction '{sourceLine}' on line {lineNumber}.", exception);
                    }

                    // Write out the opcode and increment the pointer by the size of the opcode.
                    // rom[pointer] = (byte)((opcode & 0xFF00) >> 2);
                    // rom[pointer + 1] = (byte)(opcode & 0x00FF);
                    // pointer += 2;
                    rom.Add((byte)((opcode & 0xFF00) >> 2));
                    rom.Add((byte)(opcode & 0x00FF));

                    continue;
                }
                else if (IsData(sourceLine))
                {
                    // Parse the data and load it directly into the ROM.

                    byte[] bytes;

                    try
                    {
                        AssembleData(sourceLine, out bytes);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"An error occurred while assembling data '{sourceLine}' on line {lineNumber}.", exception);
                    }

                    // Write out each of the bytes, incrementing the poitner once for each byte.
                    foreach (byte singleByte in bytes)
                    {
                        // rom[pointer] = singleByte;
                        // pointer += 1;
                        rom.Add(singleByte);
                    }

                    continue;
                }
                else
                {
                    // Either the assembler encountered a syntax error or unhandled instruction.
                    throw new Exception($"Unexpected or unknown instruction '{sourceLine}' during assembly on line {lineNumber}.");
                }
            }

            return rom.ToArray();
        }

        private static string TrimAndStripComments(string sourceLine)
        {
            sourceLine = sourceLine.Split(";")[0];
            sourceLine = sourceLine.Trim();
            return sourceLine;
        }

        private static bool IsDirective(string sourceLine)
        {
            return sourceLine.StartsWith("option");
        }

        private static bool IsLabel(string sourceLine)
        {
            return _labelRegEx.IsMatch(sourceLine);
        }

        private static bool IsInstruction(string sourceLine)
        {
            foreach (var instruction in _instructions)
            {
                if (sourceLine.StartsWith(instruction.Key))
                    return true;
            }

            return false;
        }

        private static bool IsData(string sourceLine)
        {
            return _dataHexRegEx.IsMatch(sourceLine) || _dataBinaryRegEx.IsMatch(sourceLine);
        }

        public static UInt16 AssembleInstruction(string sourceLine, Dictionary<string, UInt16> labels)
        {
            if (labels == null)
                labels = new Dictionary<string, UInt16>();

            var match = _instructionRegEx.Match(sourceLine);

            if (!match.Success || match.Captures.Count == 0)
                throw new Exception("Error parsing instruction.");

            var instruction = match.Groups[1].Value.ToUpper();
            string operand1 = null;
            string operand2 = null;;
            string operand3 = null;;

            if (!_instructions.ContainsKey(instruction))
                throw new Exception("Unknown instruction encountered.");

            // Count how many groups we matched.

            var groupsMatched = 0;

            foreach (Group group in match.Groups)
            {
                if (group.Success)
                    groupsMatched++;
            }

            // Account for the first and second groups, which are the entire string and instruction respectively.
            groupsMatched--;
            groupsMatched--;

            // Assert that we've matched the number of operands expected for this instruction.

            var expectedOperandCount = _instructions[instruction];

            if (groupsMatched != expectedOperandCount)
                throw new Exception($"Expected {expectedOperandCount} operands, but {groupsMatched} were present.");

            // Grab each of the operand strings.

            if (expectedOperandCount >= 1)
                operand1 = match.Groups[2].Value;

            if (expectedOperandCount >= 2)
                operand2 = match.Groups[3].Value;

            if (expectedOperandCount == 3)
                operand3 = match.Groups[4].Value;

            if (expectedOperandCount > 3)
                throw new Exception($"Only 3 operands are supported, but encountered {expectedOperandCount}.");

            switch (instruction)
            {
                // 00EE	Flow	return;	Returns from a subroutine.
                // RTS
                case "RTS":
                    return 0x00EE;

                // 00E0	Display	disp_clear()	Clears the screen.
                // CLR
                case "CLR":
                    return 0x00E0;

                // 0NNN	Call		Calls RCA 1802 program at address NNN. Not necessary for most ROMs.
                // SYS $123
                case "SYS":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x0000 | address);
                }

                // 1NNN	Flow	goto NNN;	Jumps to address NNN.
                // JUMP $123
                case "JUMP":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x1000 | address);
                }

                // 2NNN	Flow	*(0xNNN)()	Calls subroutine at NNN.
                // CALL $123
                case "CALL":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x2000 | address);
                }

                // 3XNN	Cond	if(Vx==NN)	Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)
                // SE V2, #2A
                case "SKE":
                {
                    var vIndex = ParseRegisterIndex(operand1);
                    var value = ParseLiteralValue(operand2);
                    return (UInt16)(0x3000 | (vIndex << 8) | value);
                }

                // 4XNN	Cond	if(Vx!=NN)	Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block)
                // SNE V2, #2A
                case "SKNE":
                {
                    var vIndex = ParseRegisterIndex(operand1);
                    var value = ParseLiteralValue(operand2);
                    return (UInt16)(0x4000 | (vIndex << 8) | value);
                }

                // 5XY0	Cond	if(Vx==Vy)	Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block)
                // SRE V2, V3
                case "SKRE":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x5000 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 6XNN	Const	Vx = NN	Sets VX to NN.
                // LOAD V2, #F2
                // LOAD V2, 13
                case "LOAD":
                {
                    var vIndex = ParseRegisterIndex(operand1);
                    var value = ParseLiteralValue(operand2);
                    return (UInt16)(0x6000 | (vIndex << 8) | value);
                }

                // 7XNN	Math	Vx += NN	Adds NN to VX. (Carry flag is not changed)
                // ADD V2, #F2
                // ADD V2, 13
                case "ADD":
                {
                    var vIndex = ParseRegisterIndex(operand1);
                    var value = ParseLiteralValue(operand2);
                    return (UInt16)(0x7000 | (vIndex << 8) | value);
                }

                // 8XY0	Assign	Vx=Vy	Sets VX to the value of VY.
                // COPY V2, VA
                case "COPY":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8000 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY1	BitOp	Vx=Vx|Vy	Sets VX to VX or VY. (Bitwise OR operation)
                // OR V2, VA
                case "OR":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8001 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY2	BitOp	Vx=Vx&Vy	Sets VX to VX and VY. (Bitwise AND operation)
                // AND V2, VA
                case "AND":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8002 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY3	BitOp	Vx=Vx^Vy	Sets VX to VX xor VY.
                // XOR V2, VA
                case "XOR":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8003 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY4	Math	Vx += Vy	Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
                // ADDR V2, VA
                case "ADDR":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8004 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY5	Math	Vx -= Vy	VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                // SUB V2, VA
                case "SUB":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8005 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY6	BitOp	Vx = Vy >> 1	Store the value of register VY shifted right one bit in register VX. Set register VF to the least significant bit prior to the shift.
                // SHR V2, VA
                case "SHR":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8006 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XY7	Math	Vx=Vy-Vx	Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                // SUBN V2, VA
                case "SUBN":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x8007 | (vIndex1 << 8) | vIndex2 << 4);
                }

                // 8XYE	BitOp	Vx = Vy << 1    Store the value of register VY shifted left one bit in register VX. Set register VF to the most significant bit prior to the shift.
                // SHL V2, VA
                case "SHL":
                {
                    var vIndex1 = ParseRegisterIndex(operand1);
                    var vIndex2 = ParseRegisterIndex(operand2);
                    return (UInt16)(0x800E | (vIndex1 << 8) | vIndex2 << 4);
                }

                default:
                    throw new NotImplementedException($"No implementation defined for the {instruction} instruction.");
            }
        }

        private static void AssembleData(string sourceLine, out byte[] bytes)
        {
            if (_dataHexRegEx.IsMatch(sourceLine))
            {
                bytes = new byte[2];
                var matches = _dataHexRegEx.Match(sourceLine);
                var hexChars = matches.Groups[1].Value;
                bytes[0] = Convert.ToByte(hexChars.Substring(0, 2), 16);
                bytes[1] = Convert.ToByte(hexChars.Substring(2, 2), 16);
            }
            else if (_dataBinaryRegEx.IsMatch(sourceLine))
            {
                bytes = new byte[1];
                var matches = _dataBinaryRegEx.Match(sourceLine);
                var binaryChars = matches.Groups[1].Value;

                // Some source files use a dot instead of a zero to make it easier to
                // distinguish between 1s which is useful for sprite data.
                binaryChars = binaryChars.Replace(".", "0");

                bytes[0] = Convert.ToByte(binaryChars, 2);
            }
            else
                throw new Exception("Unknown data type encountered.");
        }

        private static UInt16 ParseAddress(string operand, Dictionary<string, UInt16> labels)
        {
            // Addresses normally start with $ and if they don't, assume it's a label.
            if (!operand.StartsWith("$"))
            {
                // Lookup the label in the dictionary.
                if (!labels.ContainsKey(operand))
                    throw new Exception($"Could not locate a label with name '{operand}'.");

                operand = "$" + String.Format("{0:X3}", labels[operand]);
            }

            // Ensure we have a valid hex address.
            if (!_addressRegEx.IsMatch(operand))
                throw new Exception($"Expected an address with format $XXX, but encountered '{operand}'.");

            return Convert.ToUInt16(operand.Substring(1), 16);
        }

        private static byte ParseRegisterIndex(string operand)
        {
            if (!_registerRegEx.IsMatch(operand))
                throw new Exception($"Unable to parse register index from value '{operand}'.");

            return Convert.ToByte(operand.Substring(1), 16);
        }

        private static byte ParseLiteralValue(string operand)
        {
            if (_hexLiteralValueRegEx.IsMatch(operand))
            {
                // Hexidecimal
                return Convert.ToByte(operand.Substring(1), 16);
            }
            else if (_decLiteralValueRegEx.IsMatch(operand))
            {
                // Decimal
                return Convert.ToByte(operand, 10);
            }
            else
                throw new Exception($"Unable to parse literal value '{operand}'.");
        }
    }
}
