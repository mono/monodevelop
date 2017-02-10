//*****************************************************************************
// Diff.cs
// 
// Defines a base and concrete implementation for a class which computes the
// differences between two input sequences as well as utility classes and
// interfaces.
//
// Copyright (C) 2008 Microsoft Corporation
//*****************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

//*************************************************************************
// The code from this point on is a soure-port of the TFS diff algorithm, to be available
// in cases where we don't have access to the TFS diff assembly (i.e. outside of Visual Studio).
//*************************************************************************

namespace Microsoft.TeamFoundation.Diff.Copy
{
    //*************************************************************************
    /// <summary>
    /// A predicate used by Microsoft.TeamFoundation.Diff.DiffFinder
    /// that allows callers to stop differencing prematurely.
    /// </summary>
    /// <param name="originalIndex">The current index in the original sequence being differenced.</param>
    /// <param name="originalSequence">The original sequence being differenced.</param>
    /// <param name="longestMatchSoFar">The length of the longest match so far.</param>
    /// <returns>true if the algorithm should continue processing, false to stop the algorithm.</returns>
    /// <remarks>
    /// When false is returned, the algorithm stops searching for matches and uses
    /// the information it has computed so far to create the Microsoft.TeamFoundation.Diff.IDiffChange[] array
    /// that will be returned.
    /// </remarks>
    //*************************************************************************
    public delegate bool ContinueDifferencePredicate<T>(int originalIndex,
                                IList<T> originalSequence, int longestMatchSoFar);

    //*************************************************************************
    /// <summary>
    /// An enumeration of the possible change types for a difference operation.
    /// </summary>
    //*************************************************************************
    public enum DiffChangeType
    {
        //*********************************************************************
        /// <summary>
        /// Content was inserted into the modified sequence.
        /// </summary>
        //*********************************************************************
        Insert,

        //*********************************************************************
        /// <summary>
        /// Content was deleted from the original sequence.
        /// </summary>
        //*********************************************************************
        Delete,

        //*********************************************************************
        /// <summary>
        /// Content from the original sequence has changed in the modified
        /// sequence.
        /// </summary>
        //*********************************************************************
        Change
    }

    //*************************************************************************
    /// <summary>
    /// Represents information about a specific difference between two sequences.
    /// </summary>
    //*************************************************************************
    public interface IDiffChange
    {
        //*********************************************************************
        /// <summary>
        /// The type of difference.
        /// </summary>
        //*********************************************************************
        DiffChangeType ChangeType { get; }

        //*********************************************************************
        /// <summary>
        /// The position of the first element in the original sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        int OriginalStart { get; }

        //*********************************************************************
        /// <summary>
        /// The number of elements from the original sequence which were
        /// affected (deleted).
        /// </summary>
        //*********************************************************************
        int OriginalLength { get; }

        //*********************************************************************
        /// <summary>
        /// The position of the last element in the original sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        int OriginalEnd { get; }

        //*********************************************************************
        /// <summary>
        /// The position of the first element in the modified sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        int ModifiedStart { get; }

        //*********************************************************************
        /// <summary>
        /// The number of elements from the modified sequence which were
        /// affected (added).
        /// </summary>
        //*********************************************************************
        int ModifiedLength { get; }

        //*********************************************************************
        /// <summary>
        /// The position of the last element in the modified sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        int ModifiedEnd { get; }

        /// <summary>
        /// This methods combines two IDiffChange objects into one
        /// </summary>
        /// <param name="diffChange">The diff change to add</param>
        /// <returns>An IDiffChange that represnets this + diffChange</returns>
        IDiffChange Add(IDiffChange diffChange);
    }

    //*************************************************************************
    /// <summary>
    /// Represents information about a specific difference between two sequences.
    /// </summary>
    //*************************************************************************
    internal class DiffChange : IDiffChange
    {
        //*********************************************************************
        /// <summary>
        /// Constructs a new DiffChange with the given sequence information
        /// and content.
        /// </summary>
        /// <param name="originalStart">The start position of the difference
        /// in the original sequence.</param>
        /// <param name="originalLength">The number of elements of the difference
        /// from the original sequence.</param>
        /// <param name="modifiedStart">The start position of the difference
        /// in the modified sequence.</param>
        /// <param name="modifiedLength">The number of elements of the difference
        /// from the modified sequence.</param>
        //*********************************************************************
        internal DiffChange(int originalStart, int originalLength, int modifiedStart, int modifiedLength)
        {
            Debug.Assert(originalLength > 0 || modifiedLength > 0, "originalLength and modifiedLength cannot both be <= 0");

            m_originalStart = originalStart;
            m_originalLength = originalLength;
            m_modifiedStart = modifiedStart;
            m_modifiedLength = modifiedLength;
            UpdateChangeType();
        }

        //*********************************************************************
        /// <summary>
        /// Determines the change type from the ranges and updates the value.
        /// </summary>
        //*********************************************************************
        private void UpdateChangeType()
        {
            // Figure out what change type this is
            if (m_originalLength > 0)
            {
                if (m_modifiedLength > 0)
                {
                    m_changeType = DiffChangeType.Change;
                }
                else
                {
                    m_changeType = DiffChangeType.Delete;
                }
            }
            else if (m_modifiedLength > 0)
            {
                m_changeType = DiffChangeType.Insert;
            }
            m_updateChangeType = false;
        }

        //*********************************************************************
        /// <summary>
        /// The type of difference.
        /// </summary>
        //*********************************************************************
        public DiffChangeType ChangeType
        {
            get
            {
                if (m_updateChangeType)
                {
                    UpdateChangeType();
                }

                return m_changeType;
            }
        }
        private DiffChangeType m_changeType;

        //*********************************************************************
        /// <summary>
        /// The position of the first element in the original sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        public int OriginalStart
        {
            get { return m_originalStart; }
            set
            {
                m_originalStart = value;
                m_updateChangeType = true;
            }
        }
        private int m_originalStart;

        //*********************************************************************
        /// <summary>
        /// The number of elements from the original sequence which were
        /// affected.
        /// </summary>
        //*********************************************************************
        public int OriginalLength
        {
            get { return m_originalLength; }
            set
            {
                m_originalLength = value;
                m_updateChangeType = true;
            }
        }
        private int m_originalLength;

        //*********************************************************************
        /// <summary>
        /// The position of the last element in the original sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        public int OriginalEnd
        {
            get { return OriginalStart + OriginalLength; }
        }

        //*********************************************************************
        /// <summary>
        /// The position of the first element in the modified sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        public int ModifiedStart
        {
            get { return m_modifiedStart; }
            set
            {
                m_modifiedStart = value;
                m_updateChangeType = true;
            }
        }
        private int m_modifiedStart;

        //*********************************************************************
        /// <summary>
        /// The number of elements from the modified sequence which were
        /// affected (added).
        /// </summary>
        //*********************************************************************
        public int ModifiedLength
        {
            get { return m_modifiedLength; }
            set
            {
                m_modifiedLength = value;
                m_updateChangeType = true;
            }
        }
        private int m_modifiedLength;

        //*********************************************************************
        /// <summary>
        /// The position of the last element in the modified sequence which
        /// this change affects.
        /// </summary>
        //*********************************************************************
        public int ModifiedEnd
        {
            get { return ModifiedStart + ModifiedLength; }
        }

        /// <summary>
        /// This methods combines two DiffChange objects into one
        /// </summary>
        /// <param name="diffChange">The diff change to add</param>
        /// <returns>A IDiffChange object that represnets this + diffChange</returns>
        public IDiffChange Add(IDiffChange diffChange)
        {
            //If the diff change is null then just return this 
            if (diffChange == null)
            {
                return this;
            }

            int originalStart = Math.Min(this.OriginalStart, diffChange.OriginalStart);
            int originalEnd = Math.Max(this.OriginalEnd, diffChange.OriginalEnd);

            int modifiedStart = Math.Min(this.ModifiedStart, diffChange.ModifiedStart);
            int modifiedEnd = Math.Max(this.ModifiedEnd, diffChange.ModifiedEnd);

            return new DiffChange(originalStart, originalEnd - originalStart, modifiedStart, modifiedEnd - modifiedStart);
        }

        // Member variables
        private bool m_updateChangeType;
    }

    //*************************************************************************
    /// <summary>
    /// The types of End of Line terminators.
    /// </summary>
    //*************************************************************************
    public enum EndOfLineTerminator
    {
        None,                       // EndOfFile
        LineFeed,                   // \n      (UNIX)
        CarriageReturn,             // \r      (Mac)
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LineFeed")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LineFeed")]
        CarriageReturnLineFeed,     // \r\n    (DOS)
        LineSeparator,              // \u2028  (Unicode character)
        ParagraphSeparator,         // \u2029  (Unicode character)
        NextLine                    // \u0085  (Unicode character)
    }

    //*************************************************************************
    /// <summary>
    /// Utility class which tokenizes the given stream into string tokens. Each
    /// token represents a line from the file as delimited by common EOL sequences.
    /// (\n, \r\n, \r, \u2028, \u2029, \u85)
    /// </summary>
    //*************************************************************************
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tokenizer")]
    public class DiffLineTokenizer
    {
        //*********************************************************************
        /// <summary>
        /// Constructs a new DiffLineTokenizer for the given stream and encoding.
        /// </summary>
        /// <param name="stream">The stream to tokenize.</param>
        /// <param name="encoding">The character encoding of the bytes 
        /// in the stream. If null, this will be automatically detected.</param>
        //*********************************************************************
        public DiffLineTokenizer(Stream stream, Encoding encoding)
        {
            if (encoding == null)
            {
                m_streamReader = new StreamReader(stream, true);
            }
            else
            {
                m_streamReader = new StreamReader(stream, encoding, true);
            }

            // Bug 70126: Diff calculated code churn incorrectly for file
            // We don't want to parse a Unicode EOL character if its not Unicode
            int codePage = m_streamReader.CurrentEncoding.CodePage;
            if (codePage == Encoding.Unicode.CodePage || codePage == Encoding.BigEndianUnicode.CodePage ||
                codePage == Encoding.UTF32.CodePage || codePage == Encoding.UTF7.CodePage ||
                codePage == Encoding.UTF8.CodePage)
            {
                m_isUnicodeEncoding = true;
            }

            // StreamReader has a perf optimization if we ask for at least as many chars
            // as the internal byte buffer could produce if it were fully filled.
            // Their internal byte buffer is 1024 bytes.
            m_bufferSize = m_streamReader.CurrentEncoding.GetMaxCharCount(1024);
            m_charBuffer = new char[m_bufferSize];
            m_stringBuilder = new StringBuilder(80);
        }

        //*********************************************************************
        /// <summary>
        /// Fills up the internal buffer with characters from the stream.
        /// Returns the number of characters in the buffer and resets the
        /// current buffer position.
        /// Returns 0 when EOF.
        /// </summary>
        //*********************************************************************
        private int FillBuffer()
        {
            m_currBufferPos = 0;
            m_numCharsInBuffer = m_streamReader.Read(m_charBuffer, 0, m_bufferSize);
            return m_numCharsInBuffer;
        }

        //*********************************************************************
        /// <summary>
        /// Gets the next line token as a string from the stream.
        /// </summary>
        /// <returns>The next token. Returns null when the end of stream has 
        /// been reached.</returns>
        //*********************************************************************
        public string NextLineToken(out EndOfLineTerminator endOfLine)
        {
            // The structure of this code is borrowed from StreamReader.ReadLine()
            // Except that it has been extended out to support Unicode EOL characters.
            m_stringBuilder.Length = 0;
            endOfLine = EndOfLineTerminator.None;

            if (m_currBufferPos == m_numCharsInBuffer && FillBuffer() == 0)
            {
                // If the buffer was empty, and we couldn't refill it, we're done.
                return null;
            }
            do
            {
                int index = m_currBufferPos;
                do
                {
                    char ch = m_charBuffer[index];
                    if (ch == '\r' || ch == '\n' ||
                        (m_isUnicodeEncoding && (ch == LineSeparator || ch == ParagraphSeparator || ch == NextLine)))
                    {
                        switch ((int)ch)
                        {
                            case '\r':
                                endOfLine = EndOfLineTerminator.CarriageReturn;
                                break;
                            case '\n':
                                endOfLine = EndOfLineTerminator.LineFeed;
                                break;
                            case LineSeparator:
                                endOfLine = EndOfLineTerminator.LineSeparator;
                                break;
                            case ParagraphSeparator:
                                endOfLine = EndOfLineTerminator.ParagraphSeparator;
                                break;
                            case NextLine:
                                endOfLine = EndOfLineTerminator.NextLine;
                                break;
                        }

                        String line;
                        if (m_stringBuilder.Length > 0)
                        {
                            m_stringBuilder.Append(m_charBuffer, m_currBufferPos, index - m_currBufferPos);
                            line = m_stringBuilder.ToString();
                        }
                        else
                        {
                            line = new String(m_charBuffer, m_currBufferPos, index - m_currBufferPos);
                        }

                        m_currBufferPos = index + 1;
                        if (ch == '\r' && (m_currBufferPos < m_numCharsInBuffer || FillBuffer() > 0))
                        {
                            if (m_charBuffer[m_currBufferPos] == '\n')
                            {
                                endOfLine = EndOfLineTerminator.CarriageReturnLineFeed;
                                m_currBufferPos++;
                            }
                        }

                        return line;
                    }
                    index++;
                } while (index < m_numCharsInBuffer);
                m_stringBuilder.Append(m_charBuffer, m_currBufferPos, m_numCharsInBuffer - m_currBufferPos);
            } while (FillBuffer() > 0);

            return m_stringBuilder.ToString();
        }

        // Misc Unicode EOL characters.
        private const int LineSeparator = 0x2028;
        private const int ParagraphSeparator = 0x2029;
        private const int NextLine = 0x85;

        // Member variables
        private bool m_isUnicodeEncoding;
        private int m_bufferSize;
        private int m_numCharsInBuffer;
        private int m_currBufferPos;
        private char[] m_charBuffer;
        private StringBuilder m_stringBuilder;
        private StreamReader m_streamReader;
    }

    //*************************************************************************
    /// <summary>
    /// A utility class which helps to create the set of DiffChanges from
    /// a difference operation. This class accepts original DiffElements and
    /// modified DiffElements that are involved in a particular change. The
    /// MarktNextChange() method can be called to mark the seration between 
    /// distinct changes. At the end, the Changes property can be called to retrieve
    /// the constructed changes.
    /// </summary>
    //*************************************************************************
    internal class DiffChangeHelper : IDisposable
    {
        //*********************************************************************
        /// <summary>
        /// Constructs a new DiffChangeHelper for the given DiffSequences.
        /// </summary>
        /// <param name="originalSequence">The original sequence.</param>
        /// <param name="modifiedSequnece">The modified sequence.</param>
        //*********************************************************************
        public DiffChangeHelper()
        {
            m_changes = new List<IDiffChange>();
            m_originalStart = Int32.MaxValue;
            m_modifiedStart = Int32.MaxValue;
        }

        //*********************************************************************
        /// <summary>
        /// Disposes of the resources used by this helper class.
        /// </summary>
        //*********************************************************************
        public void Dispose()
        {
            if (m_changes != null)
            {
                m_changes = null;
            }
            GC.SuppressFinalize(this);
        }

        //*********************************************************************
        /// <summary>
        /// Marks the beginning of the next change in the set of differences.
        /// </summary>
        //*********************************************************************
        public void MarkNextChange()
        {
            // Only add to the list if there is something to add
            if (m_originalCount > 0 || m_modifiedCount > 0)
            {
                // Add the new change to our list
                m_changes.Add(new DiffChange(m_originalStart, m_originalCount,
                                             m_modifiedStart, m_modifiedCount));
            }

            // Reset for the next change
            m_originalCount = 0;
            m_modifiedCount = 0;
            m_originalStart = Int32.MaxValue;
            m_modifiedStart = Int32.MaxValue;
        }

        //*********************************************************************
        /// <summary>
        /// Adds the original element at the given position to the elements
        /// affected by the current change. The modified index gives context
        /// to the change position with respect to the original sequence.
        /// </summary>
        /// <param name="originalIndex">The index of the original element to add.</param>
        /// <param name="modifiedIndex">The index of the modified element that
        /// provides corresponding position in the modified sequence.</param>
        //*********************************************************************
        public void AddOriginalElement(int originalIndex, int modifiedIndex)
        {
            // The 'true' start index is the smallest of the ones we've seen
            m_originalStart = Math.Min(m_originalStart, originalIndex);
            m_modifiedStart = Math.Min(m_modifiedStart, modifiedIndex);

            m_originalCount++;
        }

        //*********************************************************************
        /// <summary>
        /// Adds the modified element at the given position to the elements
        /// affected by the current change. The original index gives context
        /// to the change position with respect to the modified sequence.
        /// </summary>
        /// <param name="originalIndex">The index of the original element that
        /// provides corresponding position in the original sequence.</param>
        /// <param name="modifiedIndex">The index of the modified element to add.</param>
        //*********************************************************************
        public void AddModifiedElement(int originalIndex, int modifiedIndex)
        {
            // The 'true' start index is the smallest of the ones we've seen
            m_originalStart = Math.Min(m_originalStart, originalIndex);
            m_modifiedStart = Math.Min(m_modifiedStart, modifiedIndex);

            m_modifiedCount++;
        }

        //*********************************************************************
        /// <summary>
        /// Retrieves all of the changes marked by the class.
        /// </summary>
        //*********************************************************************
        public IDiffChange[] Changes
        {
            get
            {
                if (m_originalCount > 0 || m_modifiedCount > 0)
                {
                    // Finish up on whatever is left
                    MarkNextChange();
                }

                return m_changes.ToArray();
            }
        }

        //*********************************************************************
        /// <summary>
        /// Retrieves all of the changes marked by the class in the reverse order
        /// </summary>
        //*********************************************************************
        public IDiffChange[] ReverseChanges
        {
            get
            {
                if (m_originalCount > 0 || m_modifiedCount > 0)
                {
                    // Finish up on whatever is left
                    MarkNextChange();
                }

                m_changes.Reverse();
                return m_changes.ToArray();
            }
        }

        // Member variables
        private int m_originalCount;
        private int m_modifiedCount;
        private List<IDiffChange> m_changes;
        private int m_originalStart;
        private int m_modifiedStart;
    }

    //*************************************************************************
    /// <summary>
    /// A base for classes which compute the differences between two input sequences.
    /// </summary>
    //*************************************************************************
    public abstract class DiffFinder<T> : IDisposable
    {
        //*************************************************************************
        /// <summary>
        /// The original sequence
        /// </summary>
        //*************************************************************************
        protected IList<T> OriginalSequence
        {
            get { return m_original; }
        }

        //*************************************************************************
        /// <summary>
        /// The modified sequence
        /// </summary>
        //*************************************************************************
        protected IList<T> ModifiedSequence
        {
            get { return m_modified; }
        }

        //*************************************************************************
        /// <summary>
        /// The element comparer
        /// </summary>
        //*************************************************************************
        protected IEqualityComparer<T> ElementComparer
        {
            get { return m_elementComparer; }
        }

        //*************************************************************************
        /// <summary>
        /// Disposes resources used by this DiffFinder
        /// </summary>
        //*************************************************************************
        public virtual void Dispose()
        {
            if (m_originalIds != null)
            {
                m_originalIds = null;
            }
            if (m_modifiedIds != null)
            {
                m_modifiedIds = null;
            }
            GC.SuppressFinalize(this);
        }

        //*********************************************************************
        /// <summary>
        /// Returns true if the specified original and modified elements are equal.
        /// </summary>
        /// <param name="originalIndex">The index of the original element</param>
        /// <param name="modifiedIndex">The index of the modified element</param>
        /// <returns>True if the specified elements are equal</returns>
        //*********************************************************************
        protected bool ElementsAreEqual(int originalIndex, int modifiedIndex)
        {
            return ElementsAreEqual(originalIndex, true, modifiedIndex, false);
        }

        //*********************************************************************
        /// <summary>
        /// Returns true if the two specified original elements are equal.
        /// </summary>
        /// <param name="firstIndex">The index of the first original element</param>
        /// <param name="secondIndex">The index of the second original element</param>
        /// <returns>True if the specified elements are equal</returns>
        //*********************************************************************
        protected bool OriginalElementsAreEqual(int firstIndex, int secondIndex)
        {
            return ElementsAreEqual(firstIndex, true, secondIndex, true);
        }

        //*********************************************************************
        /// <summary>
        /// Returns true if the two specified modified elements are equal.
        /// </summary>
        /// <param name="firstIndex">The index of the first modified element</param>
        /// <param name="secondIndex">The index of the second modified element</param>
        /// <returns>True if the specified elements are equal</returns>
        //*********************************************************************
        protected bool ModifiedElementsAreEqual(int firstIndex, int secondIndex)
        {
            return ElementsAreEqual(firstIndex, false, secondIndex, false);
        }

        //*********************************************************************
        /// <summary>
        /// Returns true if the specified elements are equal.
        /// </summary>
        /// <param name="firstIndex">The index of the first element</param>
        /// <param name="firstIsOriginal">True if the first element is an original
        /// element, false if modified element</param>
        /// <param name="secondIndex">The index of the second element</param>
        /// <param name="secondIsOriginal">True if the second element is an original
        /// element, false if modified element</param>
        /// <returns>True if the specified elements are equal</returns>
        //*********************************************************************
        private bool ElementsAreEqual(int firstIndex, bool firstIsOriginal,
                                      int secondIndex, bool secondIsOriginal)
        {
            int firstId = firstIsOriginal ? m_originalIds[firstIndex] : m_modifiedIds[firstIndex];
            int secondId = secondIsOriginal ? m_originalIds[secondIndex] : m_modifiedIds[secondIndex];
            if (firstId != 0 && secondId != 0)
            {
                return firstId == secondId;
            }
            else
            {
                T firstElement = firstIsOriginal ? OriginalSequence[firstIndex] : ModifiedSequence[firstIndex];
                T secondElement = secondIsOriginal ? OriginalSequence[secondIndex] : ModifiedSequence[secondIndex];
                return ElementComparer.Equals(firstElement, secondElement);
            }
        }

        //*************************************************************************
        /// <summary>
        /// This method hashes element groups within the specified range
        /// and assigns unique identifiers to identical elements.
        /// 
        /// This greatly speeds up element comparison as we may compare integers
        /// instead of having to look at element content.
        /// </summary>
        //*************************************************************************
        private void ComputeUniqueIdentifiers(int originalStart, int originalEnd,
                                              int modifiedStart, int modifiedEnd)
        {
            Debug.Assert(originalStart >= 0 && originalEnd < OriginalSequence.Count, "Original range is invalid");
            Debug.Assert(modifiedStart >= 0 && modifiedEnd < ModifiedSequence.Count, "Modified range is invalid");

            // Create a new hash table for unique elements from the original
            // sequence.
            Dictionary<T, int> hashTable = new Dictionary<T, int>(OriginalSequence.Count + ModifiedSequence.Count,
                                                                                        ElementComparer);
            int currentUniqueId = 1;

            // Fill up the hash table for unique elements
            for (int i = originalStart; i <= originalEnd; i++)
            {
                T originalElement = OriginalSequence[i];
                if (!hashTable.TryGetValue(originalElement, out m_originalIds[i]))
                {
                    // No entry in the hashtable so this is a new unique element.
                    // Assign the element a new unique identifier and add it to the
                    // hash table
                    m_originalIds[i] = currentUniqueId++;
                    hashTable.Add(originalElement, m_originalIds[i]);
                }
            }

            // Now match up modified elements
            for (int i = modifiedStart; i <= modifiedEnd; i++)
            {
                T modifiedElement = ModifiedSequence[i];
                if (!hashTable.TryGetValue(modifiedElement, out m_modifiedIds[i]))
                {
                    m_modifiedIds[i] = currentUniqueId++;
                    hashTable.Add(modifiedElement, m_modifiedIds[i]);
                }
            }
        }

        //*************************************************************************
        /// <summary>
        /// Computes the differences between the given original and modified sequences.
        /// </summary>
        /// <param name="original">The original sequence.</param>
        /// <param name="modified">The modified sequence.</param>
        /// <param name="elementComparer">The diff element comparer.</param>
        /// <returns>The set of differences between the sequences.</returns>
        //*************************************************************************
        public IDiffChange[] Diff(IList<T> original, IList<T> modified, IEqualityComparer<T> elementComparer)
        {
            return Diff(original, modified, elementComparer, null);
        }

        //*************************************************************************
        /// <summary>
        /// Computes the differences between the given original and modified sequences.
        /// </summary>
        /// <param name="original">The original sequence.</param>
        /// <param name="modified">The modified sequence.</param>
        /// <param name="elementComparer">The diff element comparer.</param>
        /// <returns>The set of differences between the sequences.</returns>
        //*************************************************************************
        public IDiffChange[] Diff(IList<T> original, IList<T> modified, IEqualityComparer<T> elementComparer,
                                  ContinueDifferencePredicate<T> predicate)
        {
            Debug.Assert(original != null, "original is null");
            Debug.Assert(modified != null, "modified is null");
            Debug.Assert(elementComparer != null, "elementComparer is null");

            m_original = original;
            m_modified = modified;
            m_elementComparer = elementComparer;
            m_predicate = predicate;

            m_originalIds = new int[OriginalSequence.Count];
            m_modifiedIds = new int[ModifiedSequence.Count];

            int originalStart = 0;
            int originalEnd = OriginalSequence.Count - 1;
            int modifiedStart = 0;
            int modifiedEnd = ModifiedSequence.Count - 1;

            // Find the start of the differences
            while (originalStart <= originalEnd && modifiedStart <= modifiedEnd &&
                ElementsAreEqual(originalStart, modifiedStart))
            {
                originalStart++;
                modifiedStart++;
            }

            // Find the end of the differences
            while (originalEnd >= originalStart && modifiedEnd >= modifiedStart &&
                ElementsAreEqual(originalEnd, modifiedEnd))
            {
                originalEnd--;
                modifiedEnd--;
            }

            // In the very special case where one of the ranges is negative
            // Either the sequences are identical, or there is exactly one insertion
            // or there is exactly one deletion. We have no need to compute the diffs.
            if (originalStart > originalEnd || modifiedStart > modifiedEnd)
            {
                IDiffChange[] changes;

                if (modifiedStart <= modifiedEnd)
                {
                    Debug.Assert(originalStart == originalEnd + 1, "originalStart should only be one more than originalEnd");

                    // All insertions
                    changes = new IDiffChange[1];
                    changes[0] = new DiffChange(originalStart, 0,
                                                modifiedStart,
                                                modifiedEnd - modifiedStart + 1);
                }
                else if (originalStart <= originalEnd)
                {
                    Debug.Assert(modifiedStart == modifiedEnd + 1, "modifiedStart should only be one more than modifiedEnd");

                    // All deletions
                    changes = new IDiffChange[1];
                    changes[0] = new DiffChange(originalStart,
                                                originalEnd - originalStart + 1,
                                                modifiedStart, 0);
                }
                else
                {
                    Debug.Assert(originalStart == originalEnd + 1, "originalStart should only be one more than originalEnd");
                    Debug.Assert(modifiedStart == modifiedEnd + 1, "modifiedStart should only be one more than modifiedEnd");

                    // Identical sequences - No differences
                    changes = new IDiffChange[0];
                }

                return changes;
            }

            // Now that we have our bounds, calculate unique ids for all
            // IDiffElements in the bounded range. That way, we speed up
            // element comparisons during the diff computation.
            ComputeUniqueIdentifiers(originalStart, originalEnd,
                                     modifiedStart, modifiedEnd);

            // Compute the diffences on the bounded range
            return ComputeDiff(originalStart, originalEnd,
                               modifiedStart, modifiedEnd);
        }

        //*************************************************************************
        /// <summary>
        /// Computes the differences between the original and modified input 
        /// sequences on the bounded range.
        /// </summary>
        /// <returns>An array of the differences between the two input 
        /// sequences.</returns>
        //*************************************************************************
        protected abstract IDiffChange[] ComputeDiff(int originalStart, int originalEnd,
                                                     int modifiedStart, int modifiedEnd);

        //*************************************************************************
        /// <summary>
        /// Gets the LCS Diff implementation.
        /// </summary>
        //*************************************************************************
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lcs")]
        public static DiffFinder<T> LcsDiff
        {
            get { return new LcsDiff<T>(); }
        }

        protected ContinueDifferencePredicate<T> ContinueDifferencePredicate
        {
            get
            {
                return m_predicate;
            }
        }

        // Member variables
        private IList<T> m_original;
        private IList<T> m_modified;
        private int[] m_originalIds;
        private int[] m_modifiedIds;
        private IEqualityComparer<T> m_elementComparer;

        //Early termination predicate
        private ContinueDifferencePredicate<T> m_predicate;
    }
}