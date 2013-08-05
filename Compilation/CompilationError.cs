using System;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation
{
  [Serializable]
  public sealed class CompilationError
  {
    private readonly int myLine;
    private readonly int myColumn;
    private readonly bool myIsWarning;
    private readonly string myErrorText;

    public CompilationError(
      int line, int column, string errorNumber, string errorText, bool isWarning)
    {
      if (errorNumber == null) throw new ArgumentNullException("errorNumber");
      if (errorText == null) throw new ArgumentNullException("errorText");

      myLine = (line <= 0) ? 0 : line - 1;
      myColumn = (column <= 0) ? 0 : column - 1;
      myErrorText = errorNumber + ": " + errorText;
      myIsWarning = isWarning;
    }

    public int Line
    {
      get { return myLine; }
    }

    public int Column
    {
      get { return myColumn; }
    }

    public string ErrorText
    {
      get { return myErrorText; }
    }

    public bool IsWarning
    {
      get { return myIsWarning; }
    }
  }
}