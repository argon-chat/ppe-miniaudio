using System;

namespace Miniaudio;

/// <summary>Thrown when a miniaudio call returns a non-success result.</summary>
public sealed class MiniaudioException : Exception
{
    /// <summary>The underlying ma_result code (0 == MA_SUCCESS), or 0 if not applicable.</summary>
    public int Result { get; }

    public MiniaudioException(string message, int result = 0)
        : base(result != 0 ? $"{message} (ma_result={result})" : message)
        => Result = result;
}
