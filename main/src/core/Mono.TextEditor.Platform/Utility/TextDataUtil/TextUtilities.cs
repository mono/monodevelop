// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;

    /// <summary>
    /// Handy text-oriented utilities.
    /// </summary>
    internal static class TextUtilities
    {
        public static Span? Overlap(this Span span, Span? otherSpan)
        {
            return otherSpan != null ? span.Overlap(otherSpan.Value) : null;
        }

        public static int LengthOfLineBreak(string source, int start, int end)
        {
            char c1 = source[start];
            if (c1 == '\r')
            {
                return ((++start < end) && (source[start] == '\n')) ? 2 : 1;
            }
            else if ((c1 == '\n') || (c1 == '\u0085') ||
                     (c1 == '\u2028' /*unicode line separator*/) ||
                     (c1 == '\u2029' /*unicode paragraph separator*/))
            {
                return 1;
            }
            else
                return 0;
        }

        public static int LengthOfLineBreak(char[] source, int start, int end)
        {
            char c1 = source[start];
            if (c1 == '\r')
            {
                return ((++start < end) && (source[start] == '\n')) ? 2 : 1;
            }
            else if ((c1 == '\n') || (c1 == '\u0085') ||
                     (c1 == '\u2028' /*unicode line separator*/) ||
                     (c1 == '\u2029' /*unicode paragraph separator*/))
            {
                return 1;
            }
            else
                return 0;
        }

        /// <summary>
        /// A utility function that returns the number of lines in a given block of 
        /// text
        ///
        /// We honor the MAC, UNIX and PC way of line counting.
        /// 
        /// 0x2028, 0x2929, 0x85, \r, \n and \r\n are considered as valid line breaks.  However, \n\r is not
        /// (that is to say, \n\r comprises TWO line breaks).
        /// </summary>
        static public int ScanForLineCount(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int lines = 0;
            for (int i = 0; i < text.Length; )
            {
                int breakLength = LengthOfLineBreak(text, i, text.Length);
                if (breakLength > 0)
                {
                    ++lines;
                    i += breakLength;
                }
                else
                    ++i;
            }

            return lines;
        }

        /// <summary>
        /// A utility function that returns the number of lines in a given block of text.
        ///
        /// We honor the MAC, UNIX and PC way of line counting.
        /// 
        /// 0x2028, 0x2929, 0x85, \r, \n and \r\n are considered as valid line breaks.  However, \n\r is not
        /// (that is to say, \n\r comprises TWO line breaks).
        /// </summary>
        static public int ScanForLineCount(char[] text, int start, int length)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int lines = 0;
            for (int i = 0; i < length; )
            {
                int breakLength = LengthOfLineBreak(text, start + i, length);
                if (breakLength > 0)
                {
                    ++lines;
                    i += breakLength;
                }
                else
                    ++i;
            }

            return lines;
        }

        /// <summary>
        /// Perform stable merge sort on <paramref name="elements"/>.
        /// </summary>
        /// <param name="elements">List of elements to be sorted.</param>
        /// <param name="comparer">He who knows how to compare elements.</param>
        /// <returns>Array of parameter T sorted in ascending order.</returns>
        static public T[] StableSort<T>(IList<T> elements, Comparison<T> comparer)
        {
            int count = elements.Count;
            T[] target = new T[count];
            bool alreadySorted = true;
            if (count > 0)
            {
                target[0] = elements[0];
                for (int i = 1; i < count; ++i)
                {
                    target[i] = elements[i];
                    // check for already-sorted array; quite likely in applicable text editor scenarios
                    if (comparer(target[i - 1], target[i]) > 0)
                    {
                        alreadySorted = false;
                    }
                }
            }
            if (alreadySorted)
            {
                return target;
            }
            else
            {
                // do a merge sort

                T[] source = new T[count];
                int subcount = 1;     // the length of the subsequences to merge
                while (subcount < count)
                {
                    T[] t = source;
                    source = target;
                    target = t;

                    MergePass(source, target, subcount, comparer);

                    subcount *= 2;
                }
#if DEBUG
                // trust, but verify
                for (int i = 1; i < count; ++i)
                {
                    System.Diagnostics.Debug.Assert(comparer(target[i - 1], target[i]) <= 0);
                }
#endif
                return target;
            }
        }

        /// <summary>
        /// Make one pass over <paramref name="source"/>, merging runs of size <paramref name="subcount"/> into <paramref name="target"/>.
        /// </summary>
        /// <param name="source">Array containing sorted runs of size <paramref name="subcount"/>.</param>
        /// <param name="target">Array into which runs of size 2 * <paramref name="subcount"/> will be placed.</param>
        /// <param name="subcount">Size of sorted runs that will be generated into <paramref name="target"/>.</param>
        /// <param name="comparer">He who knows how to compare elements.</param>
        static private void MergePass<T>(T[] source, T[] target, int subcount, Comparison<T> comparer)
        {
            int offset = 0;     // offset of the pair of subsequences that are being merged
            while (offset <= source.Length - 2 * subcount)
            {
                Merge(source, target, offset, offset + subcount, offset + 2 * subcount, comparer);
                offset += 2 * subcount;
            }
            if (offset + subcount < source.Length)
            {
                // two subsequences remain, but the second one is smaller than subcount
                Merge(source, target, offset, offset + subcount, source.Length, comparer);
            }
            else
            {
                // only one (already sorted) subsequence remains; copy it across
                for (int i = offset; i < source.Length; ++i)
                {
                    target[i] = source[i];
                }
            }
        }

        /// <summary>
        /// Merge two ordered runs from <paramref name="source"/> array into <paramref name="target"/> array.
        /// </summary>
        /// <param name="source">Array containing input runs.</param>
        /// <param name="target">Array into which output run is placed.</param>
        /// <param name="left">Starting position of first source run.</param>
        /// <param name="right">Starting position of second source run.</param>
        /// <param name="end">First position past second source run.</param>
        /// <param name="comparer">He who knows how to compare elements.</param>
        static private void Merge<T>(T[] source, T[] target, int left, int right, int end, Comparison<T> comparer)
        {
            int s1 = left;
            int s2 = right;
            int t = left;
            while (s1 < right && s2 < end)
            {
                target[t++] = (comparer(source[s1], source[s2]) <= 0) ? source[s1++] : source[s2++];
            }
            if (s1 == right)
            {
                // copy any leftovers from second source run.
                for (int i = t; i < end; ++i)
                {
                    target[i] = source[s2++];
                }
            }
            else
            {
                // copy any leftovers from first source run.
                for (int i = t; i < end; ++i)
                {
                    target[i] = source[s1++];
                }
            }
        }

        /// <summary>
        /// Associate a string-valued tag with the buffer for diagnostic purposes.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> of interest.</param>
        /// <param name="tag">Informative tag to associate with the buffer.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="tag"/> is null.</exception>
        /// <remarks>If the buffer already has a tag, the new tag is concatenated to it (with an intervening "::".</remarks>
        public static void TagBuffer(ITextBuffer buffer, string tag)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (tag == null)
            {
                throw new ArgumentNullException("tag");
            }
            string existingTag = "";
            if (!buffer.Properties.TryGetProperty<string>("tag", out existingTag))
            {
                buffer.Properties.AddProperty("tag", tag);
            }
            else
            {
                buffer.Properties["tag"] = existingTag + "::" + tag;
            }
        }

        /// <summary>
        /// Return the string-valued tag previously associated with the buffer by <see cref="TagBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> of interest.</param>
        /// <returns>The previously associated tag, or "" if no tag has been associated.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="buffer"/> is null.</exception>
        public static string GetTag(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            string tag = "";
            buffer.Properties.TryGetProperty<string>("tag", out tag);
            return tag;
        }

        public static string GetTagOrContentType(ITextBuffer buffer)
        {
            return GetTag(buffer) ?? buffer.ContentType.TypeName;
        }

        public static string Escape(string text)
        {
            StringBuilder result = new StringBuilder();
            for (int c = 0; c < text.Length; ++c)
            {
                char ch = text[c];
                AppendChar(result, ch);
            }
            return result.ToString();
        }

        public static string Escape(string text, int truncateLength)
        {
            StringBuilder result = new StringBuilder();
            if (text.Length <= truncateLength)
            {
                return Escape(text);
            }
            else
            {
                for (int c = 0; c < truncateLength / 2; ++c)
                {
                    char ch = text[c];
                    AppendChar(result, ch);
                }
                result.Append(" … ");
                for (int c = text.Length - (truncateLength / 2); c < text.Length; ++c)
                {
                    char ch = text[c];
                    AppendChar(result, ch);
                }
            }
            return result.ToString();
        }

        private static void AppendChar(StringBuilder result, char ch)
        {
            switch (ch)
            {
                case '\\':
                    result.Append(@"\\");
                    break;
                case '\r':
                    result.Append(@"\r");
                    break;
                case '\n':
                    result.Append(@"\n");
                    break;
                case '\t':
                    result.Append(@"\t");
                    break;
                case '"':
                    result.Append("\\\"");
                    break;
                default:
                    result.Append(ch);
                    break;
            }
        }

        internal static void RaiseEvent(object sender, EventHandler eventHandlers, IEnumerable<IExtensionErrorHandler> errorHandlers)
        {
            if (eventHandlers == null)
                return;

            var handlers = eventHandlers.GetInvocationList();

            foreach (EventHandler handler in handlers)
            {
                try
                {
                    handler(sender, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    HandleException(sender, e, errorHandlers);
                }
            }
        }

        internal static void RaiseEvent<TArgs>(object sender, EventHandler<TArgs> eventHandlers, TArgs args,
            IEnumerable<IExtensionErrorHandler> errorHandlers) where TArgs : EventArgs
        {
            if (eventHandlers == null)
                return;

            var handlers = eventHandlers.GetInvocationList();

            foreach (EventHandler<TArgs> handler in handlers)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception e)
                {
                    HandleException(sender, e, errorHandlers);
                }
            }
        }

        internal static void CallExtensionPoint(object errorSource, Action call, IEnumerable<IExtensionErrorHandler> errorHandlers)
        {
            try
            {
                call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e, errorHandlers);
            }
        }

        internal static void HandleException(object errorSource, Exception e, IEnumerable<IExtensionErrorHandler> errorHandlers)
        {
            bool handled = false;
            if (errorHandlers != null)
            {
                foreach (var errorHandler in errorHandlers)
                {
                    try
                    {
                        errorHandler.HandleError(errorSource, e);
                        handled = true;
                    }
                    catch (Exception doubleFaultException)
                    {
                        // TODO: What is the right behavior here?
                        Debug.Fail(doubleFaultException.ToString());
                    }
                }
            }
            if (!handled)
            {
                // TODO: What is the right behavior here?
                Debug.Fail(e.ToString());
            } 
        }
    }
}