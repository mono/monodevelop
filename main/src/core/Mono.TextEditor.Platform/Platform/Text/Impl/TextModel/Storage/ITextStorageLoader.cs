using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    public interface ITextStorageLoader
    {
        /// <summary>
        /// Converts a input text into a set of <see cref="ITextStorage"/> objects. It is up to
        /// this class to decide how to partition the text into storage objects; it may return one or more
        /// objects. The objects are used to initialize a piece table.
        /// </summary>
        /// <returns>An enumerable set of <see cref="ITextStorage"/> objects containing the input text.</returns>
        /// <exception cref="InvalidOperationException">if this method has previously been called.</exception>
        /// <remarks>
        /// The source of the input text is not specified here.
        /// </remarks>
        IEnumerable<ITextStorage> Load();

        /// <summary>
        /// Indicates whether all lines in the input text are terminated by the same line ending characters.
        /// </summary>
        /// <exception cref="InvalidOperationException">if loading is not complete.</exception>
        bool HasConsistentLineEndings { get; }

        /// <summary>
        /// Indicates the longest line encountered while consuming the input.
        /// </summary>
        /// <exception cref="InvalidOperationException">if loading is not complete.</exception>
        int LongestLineLength { get; }
    }
}