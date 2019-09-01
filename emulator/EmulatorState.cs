using System;
using System.Collections.Generic;
using JustinCredible.c8emu;

namespace JustinCredible.c8emu
{
    public class EmulatorState
    {
        public byte[] Memory { get; set; }
        public byte[] Registers { get; set; }
        public UInt16 IndexRegister { get; set; }
        public UInt16 ProgramCounter { get; set; }
        public UInt16 StackPointer { get; set; }
        public byte[,] FrameBuffer { get; set; }
    }
}
