using System;
using System.Diagnostics;
using SDL2;

namespace JustinCredible.c8emu
{
    class GUI : IDisposable
    {
        IntPtr _window = IntPtr.Zero;
        IntPtr _renderer = IntPtr.Zero;

        public delegate void TickEvent(GUITickEventArgs e);
        public event TickEvent OnTick;

        public void Initialize(string title, int width = 640, int height = 480, float scaleX = 1, float scaleY = 1)
        {
            var initResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            if (initResult < 0)
                throw new Exception(String.Format("Failure while initializing SDL. Error: {0}", SDL.SDL_GetError()));

            _window = SDL.SDL_CreateWindow(title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                width,
                height,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_window == IntPtr.Zero)
                throw new Exception(String.Format("Unable to create a window. SDL Error: {0}", SDL.SDL_GetError()));

            _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (_renderer == IntPtr.Zero)
                throw new Exception(String.Format("Unable to create a renderer. SDL Error: {0}", SDL.SDL_GetError()));

            SDL.SDL_RenderSetScale(_renderer, scaleX, scaleY);
        }

        public void StartLoop()
        {
            // Used to keep track of the time elapsed in each loop iteration. This is used to
            // notify the OnTick handlers so they can update their simulation, as well as throttle
            // the update loop to 60hz if needed.
            var stopwatch = new Stopwatch();

            // Structure used to pass data to and from the OnTick handlers. We initialize it once
            // outside of the loop to avoid eating a ton of memory putting GC into a tailspin.
            var tickEventArgs = new GUITickEventArgs();

            // Indicates the loop should continue to execute.
            var run = true;

            // The SDL event polled for in each iteration of the loop.
            SDL.SDL_Event sdlEvent;

            while (run)
            {
                while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                {
                    switch (sdlEvent.type)
                    {
                       case SDL.SDL_EventType.SDL_QUIT:
                           run = false;
                           break;
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                        {
                            switch (sdlEvent.key.keysym.sym)
                            {
                                case SDL.SDL_Keycode.SDLK_q:
                                    run = false;
                                    break;
                            }

                            break;
                        }
                    }
                }

                // Update the event arguments that will be sent with the event handler.

                tickEventArgs.PlaySound = false;

                // Send the keys that are currently pressed.
                // TODO: Set pressed keys.
                // tickEventArgs.Keys = ???

                // Send the time elapsed since the last iteration so the simulation can be adjusted.
                tickEventArgs.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                // Delegate out to the event handler so work can be done.
                if (OnTick != null)
                    OnTick(tickEventArgs);

                // Clear the screen.
                SDL.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 0);
                SDL.SDL_RenderClear(_renderer);

                // Render screen from the updated the frame buffer.

                SDL.SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);

                var frameBuffer = tickEventArgs.FrameBuffer;

                if (frameBuffer != null)
                {
                    for (var x = 0; x < frameBuffer.GetLength(0); x++)
                    {
                        for (var y = 0; y < frameBuffer.GetLength(1); y++)
                        {
                            if (frameBuffer[x, y] == 1)
                                SDL.SDL_RenderDrawPoint(_renderer, x, y);
                        }
                    }
                }

                SDL.SDL_RenderPresent(_renderer);

                // If the event handler indicated a should needs to be played, do it now.
                if (tickEventArgs.PlaySound)
                {
                    // TODO: Beep
                }

                // See if we need to delay to keep locked to ~ 60 FPS.

                if (stopwatch.Elapsed.TotalMilliseconds < (1000 / 60))
                {
                    var delay = (1000 / 60) - stopwatch.Elapsed.TotalMilliseconds;
                    Console.WriteLine($"Throttled: {delay}");
                    SDL.SDL_Delay((uint)delay);
                }

                // If the event handler indicated we should quit, then stop.
                if (tickEventArgs.ShouldQuit)
                {
                    run = false;
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (_renderer != IntPtr.Zero)
                SDL.SDL_DestroyRenderer(_renderer);

            if (_window != IntPtr.Zero)
                SDL.SDL_DestroyWindow(_window);
        }
    }
}
