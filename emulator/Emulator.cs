using System;
using System.Diagnostics;

namespace JustinCredible.c8emu
{
    class Emulator
    {
        private static readonly UInt16 MIN_STACK = 0xEA0;
        private static readonly UInt16 MAX_STACK = 0xEFF;

        // Indicates the ROM has finished executing via a 0x00EE opcode.
        // Step should not be called again without first calling Reset.
        public bool Finished { get; private set; }

        // For storing the pixels to be displayed. The CHIP-8 has a resolution of 64x32 with
        // monochrome color. This is a two dimensional array where each byte represents a pixel.
        // A zero indicates an empty pixel and one indicates a filled in pixel.
        public byte[,] FrameBuffer { get; private set; }

        // Indicates the frame buffer has been updated since the last call to Step().
        public bool FrameBufferUpdated { get; private set; }

        // The CHIP-8 could only play a single beep sound. This flag indicates one should be played.
        public bool PlaySound { get; private set; }

        // 4K of memory; the first 512 bytes are reserved for
        // 0x000-0x1FF - Chip 8 interpreter (contains font set in emu)
        // 0x050-0x0A0 - Used for the built in 4x5 pixel font set (0-F)
        // 0x200-0xFFF - Program ROM and work RAM
        private byte[] _memory;

        // Registers V0-VF
        // V0-VE: general purpose
        // VF: Used for special operations
        // Each register is 1 byte (8 bits)
        private byte[] _registers;

        // Register I: Index / Address register
        // 2 bytes (16 bits wide)
        private UInt16 _indexRegister;

        // Program Counter is 2 bytes
        // Possible values: 0x000 - 0xFFF
        private UInt16 _programCounter;

        // Points to the top of the stack.
        // The stack is reserved at memory locations 0xEA0-0xEFF.
        private UInt16 _stackPointer;

        // Used by CXNN	(Rand) opcode to generate random numbers.
        private Random _random;

        // If non-zero, decrements at 60hz. For use with the FX15 and FX07 opcodes.
        // In otherwords, this decrements once for every 16.6 ms of time ellapsed between opcodes.
        private byte _delayTimer;

        // Used to measure time ellapsed between steps so we can update the delay timer value.
        // For use with the FX15 and FX07 opcodes. This will only accumulate time between steps
        // once a delay timer value is set via the FX15 opcode is executed, and will reset and
        // stop accumulating once the delay timer has reached zero.
        private double _accumulatedMilliseconds = 0;

        public Emulator()
        {
            this.Reset();
        }

        public void Reset()
        {
            // Initialize the regisgters and memory.
            _indexRegister = 0x00;
            _registers = new byte[16];
            _memory = new byte[4096];

            // The first 512 bytes are reserved for the interpreter on real hardware.
            // Therefore program data starts at 512.
            _programCounter = 0x200;

            // Initialize the stack pointer.
            _stackPointer = MIN_STACK;

            // Initialize the native random object which is used by the CXNN (Rand) opcode.
            _random = new Random();

            // Reset the delay timer.
            _delayTimer = 0x00;
            _accumulatedMilliseconds = 0;

            // Reset the flag that indicates that the ROM has finished executing.
            Finished = false;

            // Reset the frame buffer (clear the screen).
            FrameBuffer = new byte[64, 32];
            FrameBufferUpdated = true;
        }

        private void LoadMemory(byte[] memory)
        {
            // This expects the ROM loaded starting at 0x200.
            _memory = memory;

            // Copy in the built-in font, which is located at 0x050 through 0x0A0.
            for (var i = 0; i < Font.Bytes.Length; i++)
                memory[Font.MemoryLocation + i] = Font.Bytes[i];
        }

        public void LoadRom(byte[] rom)
        {
            // Ensure the ROM data is not larger than we can load.
            //  4096 : Total Memory (bytes)
            // - 512 : First 512 reserved for interpreter
            // - 256 : Uppermost 256 reserved for display refresh
            // - 96  : 96 below that reserved for call stack, internal use, and other variables
            //  3232 : Remaining available memory for user ROM
            if (rom.Length > 3232)
                throw new Exception("ROM filesize cannot exceed 3,232 bytes.");

            var memory = new byte[4096];

            // We skip the first 512 bytes which is where the CHIP-8 interpreter is stored on real hardware.
            var romStartIndex = 0x200;

            // Copy the bytes over.
            for (var i = 0; i < rom.Length; i++)
                memory[romStartIndex + i] = rom[i];

            LoadMemory(memory);
        }

        public void Step(double elapsedMilliseconds)
        {
            // Sanity check.
            if (Finished)
                throw new Exception("Program has finished execution; Reset() must be invoked before invoking Step() again.");

            FrameBufferUpdated = false;

            // Indicates if we should increment the program counter by the standard 2 bytes
            // after each fetch/decode/execute cycle. Some opcodes (jump etc) may modify the
            // program counter directly, and therefore will want to skip this.
            var incrementProgramCounter = true;

            // Update the countdown timer used by FX07 and FX15 opcodes.
            UpdateTimer(elapsedMilliseconds);

            // Fetch, decode, and execute the next opcode.
            UInt16 opcode = Fetch(_programCounter);

            // Useful for adding to IDE's watched variables during debugging.
            // var d_opcode = String.Format("0x{0:X4}", opcode);
            // var d_pc = String.Format("0x{0:X4}", _programCounter);
            // var d_i = String.Format("0x{0:X4}", _indexRegister);
            // var d_v0 = String.Format("0x{0:X2}", _registers[0]);
            // var d_v1 = String.Format("0x{0:X2}", _registers[1]);
            // var d_v2 = String.Format("0x{0:X2}", _registers[2]);
            // var d_v3 = String.Format("0x{0:X2}", _registers[3]);
            // var d_v4 = String.Format("0x{0:X2}", _registers[4]);
            // var d_v5 = String.Format("0x{0:X2}", _registers[5]);
            // var d_v6 = String.Format("0x{0:X2}", _registers[6]);
            // var d_v7 = String.Format("0x{0:X2}", _registers[7]);
            // var d_v8 = String.Format("0x{0:X2}", _registers[8]);
            // var d_v9 = String.Format("0x{0:X2}", _registers[9]);
            // var d_vA = String.Format("0x{0:X2}", _registers[10]);
            // var d_vB = String.Format("0x{0:X2}", _registers[11]);
            // var d_vC = String.Format("0x{0:X2}", _registers[12]);
            // var d_vD = String.Format("0x{0:X2}", _registers[13]);
            // var d_vE = String.Format("0x{0:X2}", _registers[14]);
            // var d_vF = String.Format("0x{0:X2}", _registers[15]);

            // For the opcodes comments below.
            // NNN: address
            // NN: 8-bit constant
            // N: 4-bit constant
            // X and Y: 4-bit register identifier
            // PC : Program Counter
            // I : 16bit register (For memory address) (Similar to void pointer)
            // VN: One of the 16 available variables. N may be 0 to F (hexadecimal)

            // Decode and execute opcode.
            // There are 30 opcodes; each is two bytes and stored big-endian.
            if (opcode == 0x00EE)
            {
                // 00EE	Flow	return;	Returns from a subroutine.

                // If the stack pointer was at the bottom of the stack and it was empty
                // then assume we weren't in a subroutine call and exit the program.
                if (_stackPointer == MIN_STACK && Fetch(_stackPointer) == 0x0000)
                {
                    Finished = true;
                    return;
                }

                // Otherwise grab the address that the stack pointer is pointing to,
                // which is the subroutine return address.
                var returnAddress = Fetch(_stackPointer);

                // Clear out the value the stack pointer is pointing to.
                _memory[_stackPointer] = 0x0000;
                _memory[_stackPointer + 1] = 0x0000;

                // If we weren't at the minimum location, then move the stack pointer back.
                if (_stackPointer != MIN_STACK)
                    _stackPointer = (UInt16)(_stackPointer - 0x0002);

                // Jump back to the return address.
                // This will return back to the 2NNN subroutine opcode which jumped us to
                // the subroutine in the first place, so we want to allow the PC to be
                // incremented below.
                _programCounter = returnAddress;
            }
            else if (opcode == 0x00E0)
            {
                // 00E0	Display	disp_clear()	Clears the screen.
                Array.Clear(FrameBuffer, 0, FrameBuffer.Length);
                FrameBufferUpdated = true;
            }
            else if ((opcode & 0xF000) == 0x0000)
            {
                // 0NNN	Call		Calls RCA 1802 program at address NNN. Not necessary for most ROMs.
                throw new NotImplementedException(String.Format("RCA 1802 execution (opcode 0x{0:X4}) not supported.", opcode));
            }
            else if ((opcode & 0xF000) == 0x1000)
            {
                // 1NNN	Flow	goto NNN;	Jumps to address NNN.
                var address = (opcode & 0x0FFF);
                _programCounter = (UInt16)address;
                incrementProgramCounter = false;
            }
            else if ((opcode & 0xF000) == 0x2000)
            {
                // 2NNN	Flow	*(0xNNN)()	Calls subroutine at NNN.
                var address = opcode & 0x0FFF;

                if (_stackPointer == MAX_STACK)
                    throw new Exception("CHIP-8 stack overflow.");

                // Increment the stack pointer (unless this we're at the bottom of the stack and it
                // is empty, which is a special case).
                if (_stackPointer != MIN_STACK || (_stackPointer == MIN_STACK && Fetch(_stackPointer) != 0x0000))
                    _stackPointer = (UInt16)(_stackPointer + 0x0002);

                // Store the address on the stack.
                _memory[_stackPointer] = (byte)((_programCounter & 0xFF00) >> 8);
                _memory[_stackPointer + 1] = (byte)(_programCounter & 0x00FF);

                // Jump to the given address.
                _programCounter = (UInt16)address;
                incrementProgramCounter = false;
            }
            else if ((opcode & 0xF000) == 0x3000)
            {
                // 3XNN	Cond	if(Vx==NN)	Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                var value = opcode & 0x00FF;

                if (valueX == value)
                {
                    // Increment the program counter once for this instruction and a second
                    // time because of the outcome of this opcode.
                    _programCounter = (UInt16)(_programCounter + 0x0004);

                    // We've already adjusted it, so don't do it again below.
                    incrementProgramCounter = false;
                }
            }
            else if ((opcode & 0xF000) == 0x4000)
            {
                // 4XNN	Cond	if(Vx!=NN)	Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                var value = opcode & 0x00FF;

                if (valueX != value)
                {
                    // Increment the program counter once for this instruction and a second
                    // time because of the outcome of this opcode.
                    _programCounter = (UInt16)(_programCounter + 0x0004);

                    // We've already adjusted it, so don't do it again below.
                    incrementProgramCounter = false;
                }
            }
            else if ((opcode & 0xF00F) == 0x5000)
            {
                // 5XY0	Cond	if(Vx==Vy)	Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];

                if (valueX == valueY)
                {
                    // Increment the program counter once for this instruction and a second
                    // time because of the outcome of this opcode.
                    _programCounter = (UInt16)(_programCounter + 0x0004);

                    // We've already adjusted it, so don't do it again below.
                    incrementProgramCounter = false;
                }
            }
            else if ((opcode & 0xF000) == 0x6000)
            {
                // 6XNN	Const	Vx = NN	Sets VX to NN.
                var registerIndex = (opcode & 0x0F00) >> 8;
                var value = opcode & 0x00FF;
                _registers[registerIndex] = (byte)value;
            }
            else if ((opcode & 0xF000) == 0x7000)
            {
                // 7XNN	Const	Vx += NN	Adds NN to VX. (Carry flag is not changed)
                var registerIndex = (opcode & 0x0F00) >> 8;
                var value = _registers[registerIndex];
                _registers[registerIndex] = (byte)(value + opcode);
            }
            else if ((opcode & 0xF00F) == 0x8000)
            {
                // 8XY0	Assign	Vx=Vy	Sets VX to the value of VY.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var value = _registers[registerXIndex];
                _registers[registerYIndex] = value;
            }
            else if ((opcode & 0xF00F) == 0x8001)
            {
                // 8XY1	BitOp	Vx=Vx|Vy	Sets VX to VX or VY. (Bitwise OR operation)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                _registers[registerXIndex] = (byte)(valueX | valueY);
            }
            else if ((opcode & 0xF00F) == 0x8002)
            {
                // 8XY2	BitOp	Vx=Vx&Vy	Sets VX to VX and VY. (Bitwise AND operation)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                _registers[registerXIndex] = (byte)(valueX & valueY);
            }
            else if ((opcode & 0xF00F) == 0x8003)
            {
                // 8XY3	BitOp	Vx=Vx^Vy	Sets VX to VX xor VY.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                _registers[registerXIndex] = (byte)(valueX ^ valueY);
            }
            else if ((opcode & 0xF00F) == 0x8004)
            {
                // 8XY4	Math	Vx += Vy	Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                var result = valueX + valueY;
                var carryOccurred = (result & 0x0100) == 0x0100;
                _registers[15] = (byte)(carryOccurred ? 0x01 : 0x00);
                _registers[registerXIndex] = (byte)(result & 0x00FF);
            }
            else if ((opcode & 0xF00F) == 0x8005)
            {
                // 8XY5	Math	Vx -= Vy	VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                // TODO: I'm not sure if this is correct for the borrow case; compare against another implementation.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                var borrowOccurred = valueY > valueX;
                var result = valueX - valueY;
                _registers[15] = (byte)(borrowOccurred ? 0x00 : 0x01);
                _registers[registerXIndex] = (byte)(result & 0x00FF);
            }
            else if ((opcode & 0xF00F) == 0x8006)
            {
                // 8XY6	BitOp	Vx = Vy >> 1	Store the value of register VY shifted right one bit in register VX. Set register VF to the least significant bit prior to the shift.
                // NOTE: Wikipedia description is wrong. Search for "misconception" on this page: http://mattmik.com/files/chip8/mastering/chip8.html
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueY = _registers[registerYIndex];
                _registers[registerXIndex] = (byte)(valueY >> 1);
                _registers[15] = (byte)(valueY & 0x01);
            }
            else if ((opcode & 0xF00F) == 0x8007)
            {
                // 8XY7	Math	Vx=Vy-Vx	Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                // TODO: I'm not sure if this is correct for the borrow case; compare against another implementation.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];
                var borrowOccurred = valueX > valueY;
                var result = valueY - valueX;
                _registers[15] = (byte)(borrowOccurred ? 0x00 : 0x01);
                _registers[registerXIndex] = (byte)(result & 0x00FF);
            }
            else if ((opcode & 0xF00F) == 0x800E)
            {
                // 8XYE	BitOp	Vx = Vy << 1    Store the value of register VY shifted left one bit in register VX. Set register VF to the most significant bit prior to the shift.
                // NOTE: Wikipedia description is wrong. Search for "misconception" on this page: http://mattmik.com/files/chip8/mastering/chip8.html
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueY = _registers[registerYIndex];
                _registers[registerXIndex] = (byte)(valueY << 1);
                _registers[15] = (byte)(valueY & 0x80);
            }
            else if ((opcode & 0xF00F) == 0x9000)
            {
                // 9XY0	Cond	if(Vx!=Vy)	Skips the next instruction if VX doesn't equal VY. (Usually the next instruction is a jump to skip a code block)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var valueX = _registers[registerXIndex];
                var valueY = _registers[registerYIndex];

                if (valueX != valueY)
                {
                    // Increment the program counter once for this instruction and a second
                    // time because of the outcome of this opcode.
                    _programCounter = (UInt16)(_programCounter + 0x0004);

                    // We've already adjusted it, so don't do it again below.
                    incrementProgramCounter = false;
                }
            }
            else if ((opcode & 0xF000) == 0xA000)
            {
                // ANNN	MEM	I = NNN	Sets I to the address NNN.
                _indexRegister = (UInt16)(opcode & 0x0FFF);
            }
            else if ((opcode & 0xF000) == 0xB000)
            {
                // BNNN	Flow	PC=V0+NNN	Jumps to the address NNN plus V0.
                var baseAddress = (UInt16)(opcode & 0x0FFF);
                var address = baseAddress + _registers[0];
                _programCounter = (UInt16)address;
                incrementProgramCounter = false;
            }
            else if ((opcode & 0xF000) == 0xC000)
            {
                // CXNN	Rand	Vx=rand()&NN	Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var value = (opcode & 0x00FF);
                _registers[registerXIndex] = (byte)(_random.Next() & value);
            }
            else if ((opcode & 0xF000) == 0xD000)
            {
                // DXYN	Disp	draw(Vx,Vy,N)	Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height
                // of N pixels. Each row of 8 pixels is read as bit-coded starting from memory location I; I value doesn’t change
                // after the execution of this instruction. As described above, VF is set to 1 if any screen pixels are flipped
                // from set to unset when the sprite is drawn, and to 0 if that doesn’t happen
                // TODO: TEST
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var registerYIndex = (opcode & 0x00F0) >> 4;
                var x = _registers[registerXIndex];
                var y = _registers[registerYIndex];
                var height = opcode & 0x000F;
                var sprite = _memory[_indexRegister];

                // We're going to draw a row of pixels up to the given height.
                for (var row = 0; row < height; row++)
                {
                    // Don't try to fill pixels outside of the frame buffer.
                    if (y + row >= 32)
                        continue;

                    // For each pixel we're drawing, we XOR the pixel from the sprite against the pixel at the coordinates in
                    // the framebuffer.
                    for (var pixelIndex = 0;  pixelIndex < 8; pixelIndex++)
                    {
                        // Don't try to fill pixels outside of the frame buffer.
                        if (x + pixelIndex >= 64)
                            continue;

                        // Is the pixel at the coordinate in the frame buffer already set?
                        var isSet = FrameBuffer[x + pixelIndex, y + row] == 1;

                        // Looking at the pixel at the given index in the sprite, is it set?
                        //  Here we use some clever bitshifting to mask out just the bit we want.
                        var shouldBeSet = (sprite & (1 << pixelIndex)) != 0;

                        if (isSet && shouldBeSet)
                        {
                            // If the pixel is set in the frame buffer as well as the sprite, then we set VF to indicate a
                            // "collision" (which can be used for collision detection by the program) and then flip the pixel.
                            _registers[15] = 1;
                            FrameBuffer[x + pixelIndex, y + row] = 0;
                            FrameBufferUpdated = true;
                        }
                        else if (!isSet && shouldBeSet)
                        {
                            // If the pixel wasn't set in the frame buffer, but it was in the sprite, then set it.
                            FrameBuffer[x + pixelIndex, y + row] = 1;
                            FrameBufferUpdated = true;
                        }
                    }
                }
            }
            else if ((opcode & 0xF0FF) == 0xE09E)
            {
                // EX9E	KeyOp	if(key()==Vx)	Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block)
                // TODO
            }
            else if ((opcode & 0xF0FF) == 0xE0A1)
            {
                // EXA1	KeyOp	if(key()!=Vx)	Skips the next instruction if the key stored in VX isn't pressed. (Usually the next instruction is a jump to skip a code block)
                // TODO
            }
            else if ((opcode & 0xF0FF) == 0xF007)
            {
                // FX07	Timer	Vx = get_delay()	Sets VX to the value of the delay timer.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                _registers[registerXIndex] = _delayTimer;
            }
            else if ((opcode & 0xF0FF) == 0xF00A)
            {
                // FX0A	KeyOp	Vx = get_key()	A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event)
                // TODO
            }
            else if ((opcode & 0xF0FF) == 0xF015)
            {
                // FX15	Timer	delay_timer(Vx)	Sets the delay timer to VX.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                _delayTimer = _registers[registerXIndex];
            }
            else if ((opcode & 0xF0FF) == 0xF018)
            {
                // FX18	Sound	sound_timer(Vx)	Sets the sound timer to VX.
                // TODO
            }
            else if ((opcode & 0xF0FF) == 0xF01E)
            {
                // FX1E	MEM	I +=Vx	Adds VX to I.
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                _indexRegister = (UInt16)(_indexRegister + valueX);
            }
            else if ((opcode & 0xF0FF) == 0xF029)
            {
                // FX29	MEM	I=sprite_addr[Vx]	Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
                // TODO: TEST
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];

                // The built in font is the chars 0-9, A-F. Each is 4x5 pixels.
                // They're stored sequentially. Here we lookup each character
                // by multiplying by 5 (since there are 5 bytes/rows for each).
                _indexRegister = (byte)(Font.MemoryLocation + (valueX * 5));
            }
            else if ((opcode & 0xF0FF) == 0xF033)
            {
                // FX33	BCD	set_BCD(Vx);
                // *(I+0)=BCD(3);
                // *(I+1)=BCD(2);
                // *(I+2)=BCD(1);
                // Stores the binary-coded decimal representation of VX, with the most significant of three digits at the address
                // in I, the middle digit at I plus 1, and the least significant digit at I plus 2. (In other words, take the
                // decimal representation of VX, place the hundreds digit in memory at location in I, the tens digit at location
                // I+1, and the ones digit at location I+2.)
                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                _memory[_indexRegister] = (byte)DigitAtPosition(valueX, 3);
                _memory[_indexRegister + 1] = (byte)DigitAtPosition(valueX, 2);
                _memory[_indexRegister + 2] = (byte)DigitAtPosition(valueX, 1);
            }
            else if ((opcode & 0xF0FF) == 0xF055)
            {
                // FX55	MEM	reg_dump(Vx,&I)	Stores V0 to VX (including VX) in memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.

                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                var index = _indexRegister;

                for (var i = 0; i <= valueX; i++)
                {
                    var registerValue = _registers[i];
                    _memory[index + i] = registerValue;
                }

                // NOTE: Discrepancy between sources; this page indicates I is, in fact, modified after this operation.
                // http://mattmik.com/files/chip8/mastering/chip8.html
                _indexRegister = (UInt16)(_indexRegister + valueX + 1);
            }
            else if ((opcode & 0xF0FF) == 0xF065)
            {
                // FX65	MEM	reg_load(Vx,&I)	Fills V0 to VX (including VX) with values from memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.

                var registerXIndex = (opcode & 0x0F00) >> 8;
                var valueX = _registers[registerXIndex];
                var index = _indexRegister;

                for (var i = 0; i <= valueX; i++)
                {
                    var registerValue = _memory[index + i];
                    _registers[i] = registerValue;
                }

                // NOTE: Discrepancy between sources; this page indicates I is, in fact, modified after this operation.
                // http://mattmik.com/files/chip8/mastering/chip8.html
                _indexRegister = (UInt16)(_indexRegister + valueX + 1);
            }
            else if (opcode == 0xFFFE)
            {
                // Temporary debugging opcode for invoking a breakpoint.
                System.Diagnostics.Debugger.Break();
            }
            else if (opcode == 0xFFFF)
            {
                // Temporary debugging opcode for writing to stdout.
                Console.WriteLine($"{DateTime.Now}: DEBUG opcode hit!");
            }
            else
            {
                throw new NotImplementedException(String.Format("Attempted to execute unknown opcode 0x{0:X4} at memory address 0x{0:X4}", opcode, _programCounter));
            }

            // Increment program counter by two bytes, to the next opcode.
            if (incrementProgramCounter)
                _programCounter = (UInt16)(_programCounter + 0x0002);
        }

        private UInt16 Fetch(UInt16 pointer)
        {
            var firstByte = _memory[pointer];
            var secondByte = _memory[pointer + 1];

            int combined = 0x0000;

            combined = combined | secondByte;

            int firstByteExpanded = (int)firstByte;

            int firstByteShifted = firstByteExpanded << 8;

            combined = combined | firstByteShifted;

            return (UInt16)combined;
        }

        private void UpdateTimer(double elapsedMilliseconds)
        {
            // Update the countdown timer.
            // If the timer has already reached zero, then there is no work to do.
            if (_delayTimer == 0x00)
                return;

            // Keep track of the time ellapsed since the last time we've updated the counter.
            _accumulatedMilliseconds += elapsedMilliseconds;

            // To simulate the timer counting down at 60hz, we look at how much time has ellapsed
            // since the last time we've been through the loop and divide by 16.6 milliseconds (60hz).
            // This will be the amount we need to decrement the counter by.
            var decrementBy = (int)Math.Floor(_accumulatedMilliseconds / 16.6);

            // It's possible that the last opcode took less than one millisecond to complete. In this
            // case let the stopwatch continue to tick and accumulate time.
            if (decrementBy == 0)
                return;

            // Update the delay timer value.
            if (decrementBy >= _delayTimer)
            {
                // If we're decrementing by the same or more than the counter, just set to zero.
                // We're done counting down at this point.
                _delayTimer = 0x00;
                _accumulatedMilliseconds = 0;
            }
            else
            {
                // Tick down by the number we calculated above.
                _delayTimer = (byte)(_delayTimer - decrementBy);

                // Keep the remaining milliseconds to hopefully be slightly more accurate.
                _accumulatedMilliseconds = _accumulatedMilliseconds % 16.6;
            }
        }

        private int DigitAtPosition(int number, int position)
        {
            // Calculate the divisor; this division shifts the digit we're looking
            // for to the right until it is in the ones position.
            var divisor = (int)Math.Pow(10, position - 1);

            // Use the modulo operator to divide by 10 and get the remainder, which
            // will be the digit in the ones position.
            return (number / divisor) % 10;
        }
    }
}
