using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class CompressedTextStorageLoader : BaseTextStorageLoader
    {
        public CompressedTextStorageLoader(TextReader reader, int fileSize, string id)
            : base(reader, fileSize, id)
        {
        }

        protected override IEnumerable<ITextStorage> DoLoad()
        {
            PageManager manager = new PageManager();

            string firstLineBreak = null;
            int cumulativeLineLength = 0;
            bool pendingNewline = false;
            int page = 0;

            while (true)
            {
                char[] buffer = new char[TextModelOptions.CompressedStoragePageSize];
                int read = this.reader.ReadBlock(buffer, 0, buffer.Length);
                Debug.Assert(read >= 0);
                if (read == 0)
                {
                    this.loadCompleted = true;
                    yield break;
                }
                else
                {
                    List<uint> lineBreaks = new List<uint>();
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
                            lineBreaks.Add(EncodedLineBreaks.EncodePosition(c, isSingleCharLineBreak: breakLength == 1));
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
                                Debug.Assert(c == 0 && buffer[0] == '\n', "Pending new line inconsistency");
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
                                    if (firstLineBreak != null && firstLineBreak != "\r")
                                    {
                                        this.hasConsistentLineEndings = false;
                                    }
                                }
                                else
                                {
                                    char nextC = (char)peeky;
                                    if (nextC == '\n')
                                    {
                                        pendingNewline = true;
                                        if (firstLineBreak == null)
                                        {
                                            firstLineBreak = "\r\n";
                                        }
                                        else if (firstLineBreak.Length != 2)
                                        {
                                            this.hasConsistentLineEndings = false;
                                        }
                                        // the line break list thinks this break has length one, and that is correct as
                                        // far as the current storage element is concerned, so we leave it alone.
                                    }
                                    else
                                    {
                                        if (firstLineBreak == null)
                                        {
                                            firstLineBreak = "\r";
                                        }
                                        else if (firstLineBreak != "\r")
                                        {
                                            this.hasConsistentLineEndings = false;
                                        }
                                    }
                                }
                            }

                            else if (firstLineBreak == null)
                            {
                                firstLineBreak = new string(buffer, c, breakLength);
                            }
                            else if (breakLength != firstLineBreak.Length)
                            {
                                this.hasConsistentLineEndings = false;
                            }
                            else if ((breakLength) == 1 && (firstLineBreak[0] != buffer[c]))
                            {
                                this.hasConsistentLineEndings = false;
                            }
                            c += breakLength;
                        }
                    }
                    this.longestLineLength = Math.Max(this.longestLineLength, cumulativeLineLength);

                    yield return new CompressedTextStorage(manager, id + "-" + page.ToString(), buffer, read, lineBreaks, keepActive: page == 0);
                    
                    page++;
                    if (read < buffer.Length)
                    {
                        this.loadCompleted = true;
                        yield break;
                    }
                }
            }
        }
    }
}