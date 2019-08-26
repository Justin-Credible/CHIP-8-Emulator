using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace JustinCredible.c8asm
{
    class Program
    {
        private static CommandLineApplication _app;

        public static void Main(string[] args)
        {
            var version = Utilities.AppVersion;

            _app = new CommandLineApplication();
            _app.Name = "c8asm";
            _app.Description = "CHIP-8 Assembler";
            _app.HelpOption("-?|-h|--help");

            _app.VersionOption("-v|--version",

                // Used for HelpOption() header
                $"{_app.Name} {version}",

                // Used for output of --version option.
                version
            );

            // When launched without any commands or options.
            _app.OnExecute(() =>
            {
                _app.ShowHelp();
                return 0;
            });

            _app.Command("assemble", Assemble);

            _app.Execute(args);
        }

        private static void Assemble(CommandLineApplication command)
        {
            command.Description = "Assembles the given source file into a ROM file.";
            command.HelpOption("-?|-h|--help");

            var sourcePathArg = command.Argument("[source path]", "The path to a source file to assemble; will assemble the ROM in the same location with a .ROM extension.");

            var romPathArg = command.Option("-o|--output", "An override for the path where the ROM file should be written.", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                string source;

                if (File.Exists(sourcePathArg.Value))
                    source = System.IO.File.ReadAllText(sourcePathArg.Value);
                else
                    throw new Exception($"Could not locate a source file at path {sourcePathArg.Value}");

                string romPath;

                if (romPathArg.HasValue())
                {
                    romPath = romPathArg.Value();
                }
                else
                {
                    var targetDirPath = Path.GetDirectoryName(sourcePathArg.Value);
                    var fileName = Path.GetFileNameWithoutExtension(sourcePathArg.Value);
                    romPath = Path.Combine(targetDirPath, fileName + ".ROM");
                }

                var rom = Assembler.AssembleSource(source);

                File.WriteAllBytes(romPath, rom);

                return 0;
            });
        }
    }
}
