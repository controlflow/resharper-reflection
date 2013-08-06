using System;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation
{
  [Serializable]
  public sealed class CompilationError
  {
    private readonly int myLine, myColumn, myOffset;
    private readonly string myErrorText;

    public CompilationError(
      int line, int column, string errorText, int offset = -1)
    {
      if (errorText == null) throw new ArgumentNullException("errorText");

      myLine = (line <= 0) ? 0 : line - 1;
      myColumn = (column <= 0) ? 0 : column - 1;
      myErrorText = errorText;
      myOffset = -1;
    }

    public int Line
    {
      get { return myLine; }
    }

    public int Column
    {
      get { return myColumn; }
    }

    public int Offset
    {
      get { return myOffset; }
    }

    public string ErrorText
    {
      get { return myErrorText; }
    }
  }
}