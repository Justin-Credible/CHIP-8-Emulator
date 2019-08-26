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
        private static readonly Dictionary<string, int> _instructions = new Dictionary<string, int>()
        {
            { "ADD", 2 },
            { "AND", 2 },
            { "CALL", 1 },
            { "CLS", 0 },
            { "DRW", 3 },
            { "JP", 1 },
            { "LD", 2 },
            { "RET", 0 },
            { "RND", 2 },
            { "SE", 2 },
            { "SKNP", 1 },
            { "SNE", 2 },
            { "SUB", 2 },
            { "SYS", 1 },
            { "XOR", 2 },
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
                case "RET":
                    return 0x00EE;

                // 00E0	Display	disp_clear()	Clears the screen.
                case "CLS":
                    return 0x00E0;

                // 0NNN	Call		Calls RCA 1802 program at address NNN. Not necessary for most ROMs.
                case "SYS":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x0000 | address);
                }

                // 1NNN	Flow	goto NNN;	Jumps to address NNN.
                case "JP":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x1000 | address);
                }

                // 2NNN	Flow	*(0xNNN)()	Calls subroutine at NNN.
                case "CALL":
                {
                    var address = ParseAddress(operand1, labels);
                    return (UInt16)(0x2000 | address);
                }

                // 3XNN	Cond	if(Vx==NN)	Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)
                case "SE":
                {
                    var vIndex = ParseRegisterIndex(operand1);
                    var value = ParseLiteralValue(operand2);
                    return (UInt16)(0x3000 | (vIndex << 8) | value);
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
