using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace JustinCredible.c8asm
{
    public class Utilities
    {
        public static string AppVersion
        {
            get
            {
                return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            }
        }

        public static byte[] SplitWords(UInt16[] words)
        {
            var bytes = new List<byte>();

            foreach (var word in words)
            {
                bytes.Add((byte)((word & 0xFF00) >> 8));
                bytes.Add((byte)(word & 0x00FF));
            }

            return bytes.ToArray();
        }

        public static string FormatAsOpcodeGroups(byte[] bytes)
        {
            var output = new StringBuilder();

            var space = false;

            foreach (var singleByte in bytes)
            {
                output.AppendFormat("{0:X2}", singleByte);
                
                if (space)
                    output.Append(" ");

                space = !space;
            }

            return output.ToString();
        }
    }
}
