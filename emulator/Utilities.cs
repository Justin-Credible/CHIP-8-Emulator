using System.Reflection;

namespace JustinCredible.c8emu
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
    }
}
