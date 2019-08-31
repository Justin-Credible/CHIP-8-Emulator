using System;
using System.Collections.Generic;
using JustinCredible.c8emu;

namespace JustinCredible.c8emu
{
    public class UnitTestEmulatorState : EmulatorState
    {
        // The values of the program counter at each emulator step that occurred.
        public UInt16[] ProgramCounterAddresses { get; set; }

        public UnitTestEmulatorState(EmulatorState state)
        {
            this.Memory = state.Memory;
            this.Registers = state.Registers;
            this.IndexRegister = state.IndexRegister;
            this.ProgramCounter = state.ProgramCounter;
            this.StackPointer = state.StackPointer;
        }
    }
}
