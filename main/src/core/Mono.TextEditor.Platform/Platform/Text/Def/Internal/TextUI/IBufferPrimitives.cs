using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents common buffer primitives and an extensible mechanism for replacing their values and adding new options.
    /// </summary>
    public interface IBufferPrimitives
    {
        /// <summary>
        /// Gets the <see cref="TextBuffer"/> primitive used for text manipulation.
        /// </summary>
        TextBuffer Buffer { get; }
    }
}
