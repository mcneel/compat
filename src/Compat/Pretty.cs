using System;

namespace Compat
{
  partial class Program
  {
    static class Pretty
    {
      static public void Class(string format, params object[] args)
      {
        WriteColor(format, args, ConsoleColor.Magenta);
      }

      static public void Method(string format, params object[] args)
      {
        WriteColor("  " + format, args, ConsoleColor.DarkCyan);
      }

      static public void Instruction(ResolutionStatus status, string scope, string format, params object[] args)
      {
        string indent = "    ";
        format += " < " + scope;
        if (status == ResolutionStatus.Success)
        {
          if (!quiet)
            WriteColor(indent + "\u2713 PASS " + format, args, ConsoleColor.Green);
        }
        else if (status == ResolutionStatus.Failure)
          WriteColor(indent + "\u2717 FAIL " + format, args, ConsoleColor.Red);
        else if (status == ResolutionStatus.PInvoke)
          WriteColor(indent + "\u2192 PINV " + format, args, ConsoleColor.DarkYellow);
        else if (status == ResolutionStatus.Warning)
          WriteColor(indent + "\u26A0 WARN " + format, args, ConsoleColor.DarkYellow);
        else // skipped
        {
          if (!quiet)
            WriteColor(indent + "\u271D SKIP " + format, args, ConsoleColor.Gray);
        }
      }

      static void WriteColor(string format, object[] args, ConsoleColor color)
      {
        Console.ForegroundColor = color;
        try
        {
          Console.WriteLine(format, args);
        }
        finally
        {
          Console.ResetColor();
        }
      }
    }
  }
}
