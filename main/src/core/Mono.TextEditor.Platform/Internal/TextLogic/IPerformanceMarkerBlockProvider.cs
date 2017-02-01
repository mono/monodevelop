using System;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Allows marking actions for performance logging.
    /// </summary>
    /// <remarks>
    /// For example, the VS editor adapters return MeasurementBlock instances
    /// that log ETW events.
    /// </remarks>
    public interface IPerformanceMarkerBlockProvider
    {
        IDisposable CreateBlock(string blockName);
    }
}