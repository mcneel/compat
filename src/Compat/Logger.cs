using System;

namespace Compat
{
  partial class Program
  {
    class Logger
    {
      public enum LogLevel
      {
        ERROR,
        WARNING,
        INFO,
        DEBUG
      }

      public LogLevel Level = LogLevel.DEBUG;

      public void Debug(string format, params object[] args)
      {
        if (Level >= LogLevel.DEBUG)
          Console.WriteLine("DEBUG " + format, args);
      }

      public void Info(string format, params object[] args)
      {
        if (Level >= LogLevel.INFO)
          Console.WriteLine(format, args);
      }

      public void Warning(string format, params object[] args)
      {
        if (Level >= LogLevel.WARNING)
          WriteLine(string.Format(format, args), ConsoleColor.DarkYellow);
      }

      public void Error(string format, params object[] args)
      {
        if (Level >= LogLevel.ERROR)
          WriteLine(string.Format(format, args), ConsoleColor.Red);
      }

      void WriteLine(string message, ConsoleColor color)
      {
        Console.ForegroundColor = color;
        try
        {
          Console.WriteLine(message);
        }
        finally
        {
          Console.ResetColor();
        }
      }
    }
  }
}
