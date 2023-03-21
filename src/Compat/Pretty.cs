using System;

namespace Compat
{
  partial class Program
  {
    static class Pretty
    {
      static public void Class(string message)
      {
        WriteColor(message, ConsoleColor.Magenta);
      }

      static public void Method(string message)
      {
        WriteColor("  " + message, ConsoleColor.DarkCyan);
      }

      static public void Instruction(ResolutionStatus status, string scope, string message)
      {
        message += " < " + scope;
        WriteStatus(status, message);
      }

      public static void WriteStatus(ResolutionStatus status, string message)
      {
        string indent = "    ";
        if (status == ResolutionStatus.Success)
        {
          if (!quiet)
            WriteColor(indent + "\u2713 PASS " + message, ConsoleColor.Green);
        }
        else if (status == ResolutionStatus.Failure)
          WriteColor(indent + "\u2717 FAIL " + message, ConsoleColor.Red);
        else if (status == ResolutionStatus.PInvoke)
          WriteColor(indent + "\u2192 PINV " + message, ConsoleColor.DarkYellow);
        else if (status == ResolutionStatus.Warning)
          WriteColor(indent + "\u26A0 WARN " + message, ConsoleColor.DarkYellow);
        else // skipped
        {
          if (!quiet)
            WriteColor(indent + "\u271D SKIP " + message, ConsoleColor.Gray);
        }
      }

      static void WriteColor(string message, ConsoleColor color)
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
