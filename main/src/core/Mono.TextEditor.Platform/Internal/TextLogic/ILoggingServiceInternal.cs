using System;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Allows code in src/Platform to log events.
    /// </summary>
    /// <remarks>
    /// For example, the VS Provider of this inserts data points into the SQM data stream.
    /// </remarks>
    [CLSCompliant(false)]
    public interface ILoggingServiceInternal
    {
        [CLSCompliant(false)]
        void IncrementDatapoint(uint datapointID, uint incrementBy);

        [CLSCompliant(false)]
        void AddValueToStream(uint streamID, uint numCols, uint value);

        [CLSCompliant(false)]
        void AddToStreamString(uint streamID, uint numCols, string value);
    }
}