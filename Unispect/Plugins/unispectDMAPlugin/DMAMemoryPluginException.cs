using System;
using Unispect;

namespace unispectDMAPlugin
{
    /// <summary>
    /// Custom Exception Class.
    /// Will automatically Log Exceptions generated.
    /// </summary>
    public sealed class DMAMemoryPluginException : Exception
    {
        public DMAMemoryPluginException()
        {
        }

        public DMAMemoryPluginException(string message)
        {
            Log.Add(message);
        }

        public DMAMemoryPluginException(string message, Exception inner)
        {
            Log.Add($"{message}: {inner}");
        }
    }
}
