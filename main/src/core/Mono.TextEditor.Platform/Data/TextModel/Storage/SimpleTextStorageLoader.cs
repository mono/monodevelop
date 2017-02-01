using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class SimpleTextStorageLoader : BaseTextStorageLoader
    {
        public int BlockSize = 16384;   // public for testing with different sizes

        private static char[] pooledBuffer;

        private struct LineBreak
        {
            private readonly char firstChar;
            private readonly short length;

            public LineBreak(int length, char firstChar)
            {
                Debug.Assert(length == 1 || (length == 2 && firstChar == '\r'));
                Debug.Assert(firstChar != '\0');
                this.firstChar = firstChar;
                this.length = (short)length;
            }

            public bool IsInitialized
            {
                get { return length != 0; }
            }

            public bool IsCr
            {
                get { return firstChar == '\r' && length == 1; }
            }

            public bool IsCrLf
            {
                get { return length == 2; }
            }

            public bool IsConsistentWith(int breakLength, char[] buffer, int offset)
            {
                Debug.Assert(IsInitialized);
                Debug.Assert(breakLength == 1 || (breakLength == 2 && buffer[offset] == '\r'));

                return breakLength == length && firstChar == buffer[offset];
            }

            public static LineBreak Cr
            {
                get { return new LineBreak(1, '\r'); }
            }

            public static LineBreak CrLf
            {
                get { return new LineBreak(2, '\r'); }
            }
        }

        public SimpleTextStorageLoader(TextReader reader, int fileSize = -1)
            : base(reader, fileSize, "")
        {
        }

        protected override IEnumerable<ITextStorage> DoLoad()
        {
            LineBreak firstLineBreak = default(LineBreak);
            int cumulativeLineLength = 0;
            bool pendingNewline = false;

            char[] buffer = AcquireBuffer(BlockSize);
            try
            {
                while (true)
                {
                    int read = this.reader.ReadBlock(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        this.loadCompleted = true;
                        yield break;
                    }
                    else
                    {
                        List<int> lineBreaks = new List<int>();
                        int c = 0;
                        while (c < read)
                        {
                            int breakLength = TextUtilities.LengthOfLineBreak(buffer, c, read);

                            if (breakLength == 0)
                            {
                                ++c;
                                cumulativeLineLength++;
                            }
                            else
                            {
                                lineBreaks.Add(c);
                                // update information about consistent line endings and line lengths.
                                // this is made complicated by the possibility that \r\n straddles a block boundary.

                                // Todo: might as well handle issues of undecodable codes here instead of the
                                // guessing that we currently do elsewhere.

                                this.longestLineLength = Math.Max(this.longestLineLength, cumulativeLineLength);
                                cumulativeLineLength = 0;

                                if (pendingNewline)
                                {
                                    // we've already done consistency checking for this newline, which was part of a 
                                    // return-newline pair crossing a block boundary.
                                    Debug.Assert(c == 0 && buffer[0] == '\n');
                                    pendingNewline = false;
                                }
                                else if (c == read - 1 && buffer[c] == '\r')
                                {
                                    // we are on the last character of the block, but it might be the
                                    // first character in a two-character line break that crosses a block
                                    // boundary. We don't care about that for purposes of constructing the list
                                    // of line breaks, but we do care in the context of determining whether line
                                    // breaks are consistent.

                                    int peeky = this.reader.Peek();
                                    if (peeky < 0)
                                    {
                                        // end of file.
                                        if (firstLineBreak.IsInitialized && !firstLineBreak.IsCr)
                                        {
                                            this.hasConsistentLineEndings = false;
                                        }
                                    }
                                    else
                                    {
                                        if (peeky == '\n')
                                        {
                                            pendingNewline = true;
                                            if (!firstLineBreak.IsInitialized)
                                            {
                                                firstLineBreak = LineBreak.CrLf;
                                            }
                                            else if (!firstLineBreak.IsCrLf)
                                            {
                                                this.hasConsistentLineEndings = false;
                                            }
                                        }
                                        else
                                        {
                                            if (!firstLineBreak.IsInitialized)
                                            {
                                                firstLineBreak = LineBreak.Cr;
                                            }
                                            else if (!firstLineBreak.IsCr)
                                            {
                                                this.hasConsistentLineEndings = false;
                                            }
                                        }
                                    }
                                }

                                // common cases follow
                                else if (!firstLineBreak.IsInitialized)
                                {
                                    firstLineBreak = new LineBreak(breakLength, buffer[c]);
                                }
                                else if (!firstLineBreak.IsConsistentWith(breakLength, buffer, c))
                                {
                                    this.hasConsistentLineEndings = false;
                                }

                                c += breakLength;
                            }
                        }
                        this.longestLineLength = Math.Max(this.longestLineLength, cumulativeLineLength);

                        yield return SimpleTextStorage.Create(new string(buffer, 0, read), lineBreaks);

                        if (read < buffer.Length)
                        {
                            this.loadCompleted = true;
                            yield break;
                        }
                    }
                }
            }
            finally
            {
                ReleaseBuffer(buffer);
            }
        }

        private static char[] AcquireBuffer(int size)
        {
            char[] buffer = Volatile.Read(ref pooledBuffer);
            if (buffer != null && buffer.Length >= size)
            {
                if (buffer == Interlocked.CompareExchange(ref pooledBuffer, null, buffer))
                {
                    return buffer;
                }
            }

            return new char[size];
        }

        private static void ReleaseBuffer(char[] buffer)
        {
            Interlocked.CompareExchange(ref pooledBuffer, buffer, null);
        }
    }
}