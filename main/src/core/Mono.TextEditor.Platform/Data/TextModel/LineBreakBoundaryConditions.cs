namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// Describe the context of some text change with respect to compound line breaks.
    /// </summary>
    [Flags]
    internal enum LineBreakBoundaryConditions : byte
    {
        /// <summary>
        /// The change is neither preceded by a return character nor followed by a newline character.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// The change is immediately preceded by a return character.
        /// </summary>
        PrecedingReturn = 0x1,

        /// <summary>
        /// The change is followed immediately by a newline character.
        /// </summary>
        SucceedingNewline = 0x2
    }
}