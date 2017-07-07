using System.Windows.Media;
using Clio.Utilities;
using ff14bot.Helpers;

namespace LeveGen.Utils
{
    internal static class Logger
    {
       
        private static string Prefix => $"[{LeveGen.PluginName}] ";

        [StringFormatMethod("format")]
        internal static void Error(string message, params object[] args)
        {
            Log(Colors.Red, message, args);
        }

        private static void Log(Color c, string message, params object[] args)
        {
                Logging.Write(c, Prefix + string.Format(message, args));
        }

        [StringFormatMethod("format")]
        internal static void Info(string message, params object[] args)
        {
            Log(Colors.Teal, message, args);
        }

        [StringFormatMethod("format")]
        internal static void Verbose(string format, params object[] args)
        {
              Log(Colors.CornflowerBlue, format, args);
        }

        [StringFormatMethod("format")]
        internal static void Warn(string format, params object[] args)
        {
            Log(Colors.YellowGreen, format, args);
        }
    }
}