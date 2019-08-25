using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;

namespace JustinCredible.c8asm
{
    class Assembler
    {
        private static readonly Regex _labelRegEx = new Regex("([A-Za-z_]):");
        private static readonly Regex _dataHexRegEx = new Regex("DW #([0-9A-F]{4})");
        private static readonly Regex _dataBinaryRegEx = new Regex("DB $([01.]{8})");

        private static readonly List<string> _instructions = new List<string>()
        {
            "LD",
            "DRW",
            "ADD",
            "SE",
            "SNE",
            "JP",
            "RND",
            "SKNP",
            "AND",
            "SUB",
            "XOR",
            "CALL",
            "RET",
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

            // Reset the pointer in preparation for the second pass.
            pointer = 0x200;

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
                    var opcode = AssembleInstruction(sourceLine, labels);

                    // Write out the opcode and increment the pointer by the size of the opcode.
                    rom[pointer] = (byte)((opcode & 0xFF00) >> 2);
                    rom[pointer + 1] = (byte)(opcode & 0x00FF);
                    pointer += 2;

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
                        rom[pointer] = singleByte;
                        pointer += 1;
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
                if (sourceLine.StartsWith(instruction + " "))
                    return true;
            }

            return false;
        }

        private static bool IsData(string sourceLine)
        {
            return _dataHexRegEx.IsMatch(sourceLine) || _dataBinaryRegEx.IsMatch(sourceLine);
        }

        private static UInt16 AssembleInstruction(string sourceLine, Dictionary<string, UInt16> labels)
        {
            // TODO
            throw new NotImplementedException();
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
    }
}
