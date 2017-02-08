using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// A page of storage in a compressing text buffer.
    /// </summary>
    internal class CompressedTextStorage : Page, ITextStorage, ILineBreaks
    {
        private readonly PageManager manager;   // the object used for synchronization purposes
        private readonly string id;
        private readonly int length;
        private readonly byte[] compressed;

        // A note on lineBreaks: in order to burn just one integer per line break, the most
        // significant bit of a lineBreak entry is set if that lineBreak has length one. If
        // the MSB is not set, the line break has length two (and is equal to "\r\n").
        private readonly uint[] lineBreaks;
        private readonly bool endsWithReturn;
        private readonly bool startsWithNewline;

        private WeakReference<char[]> weakUncompressed;
        private char[] uncompressed;

        // Please note: the CompressedStorageRetainWeakReferences option is intended for
        // testing purposes, not as an operational mode. If we were to operate without weak
        // references, we could reuse the character buffer of a page when it is unloaded.
        // However, the design optimizes for the situation where paging buffers are present 
        // but memory is plentiful, so retaining weak references to infrequently used pages
        // can speed things up significantly.

        public CompressedTextStorage(PageManager manager, string id, char[] buffer, int length, List<uint> lineBreaks, bool keepActive)
        {
            this.manager = manager;
            this.id = id;
            this.length = length;
            this.lineBreaks = lineBreaks.ToArray();
            if (length > 0)
            {
                this.startsWithNewline = buffer[0] == '\n';
                this.endsWithReturn = buffer[length - 1] == '\r';
            }

            this.compressed = Compressor.Compress(buffer, length);

            PageManager.Trace(string.Format("Page {0} Uncompressed = {1} Compressed = {2} Ratio = {3}", id,
                                            length * 2,
                                            this.compressed.Length,
                                            (int)(100 * (((double)this.compressed.Length) / ((double)(length * 2))))));

            if (keepActive)
            {
                manager.Add(this);
                this.uncompressed = buffer;
            }
            else if (TextModelOptions.CompressedStorageRetainWeakReferences)
            {
                this.weakUncompressed = new WeakReference<char[]>(buffer);
            }
        }

        public override string Id
        {
            get { return this.id; }
        }

        public int Length
        {
            get { return this.length; }
        }

        public string GetText(int startIndex, int length)
        {
            char[] data = EnsureDecompressed();
            return new string(data, startIndex, length);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            char[] data = EnsureDecompressed();
            Array.Copy(data, sourceIndex, destination, destinationIndex, count);
        }

        public char this[int index]
        {
            get
            {
                char[] data = EnsureDecompressed();
                return data[index];
            }
        }

        private char[] EnsureDecompressed()
        {
            // we may block here while a different thread is decompressing this page, which could
            // take a while -- but in that case we would have to decompress it ourselves synchronously anyhow

            // locking is quite conservative. Contention for this lock will be rare, and uncontended
            // locking is cheap. We could reduce locking by not taking the lock if the page is already 
            // decompressed and is at the head of the MRU list.

            lock (this.manager)
            {
                char[] localUncompressed = this.uncompressed;
                if (localUncompressed == null)
                {
                    this.manager.UnloadPageWhileLocked();	// die, LRU page! 
                    if (this.weakUncompressed != null && this.weakUncompressed.TryGetTarget(out localUncompressed))
                    {
                        PageManager.Trace("Resurrected " + this.Id);
                        this.weakUncompressed.SetTarget(null);
                    }
                    else
                    {
                        localUncompressed = new char[this.length];
                        Compressor.Decompress(this.compressed, this.length, localUncompressed);
                        PageManager.Trace("Decompressed " + this.Id);
                    }
                    this.uncompressed = localUncompressed;
                }
                this.manager.MarkUsedWhileLocked(this);
                return localUncompressed;
            }
        }

        private char[] MaybeGetDecompressed()
        {
            char[] localUncompressed = this.uncompressed;
            if (localUncompressed == null && TextModelOptions.CompressedStorageRetainWeakReferences)
            {
                this.weakUncompressed.TryGetTarget(out localUncompressed);
            }
            return localUncompressed;
        }

        public ILineBreaks LineBreaks
        {
            get { return this; }
        }

        public int StartOfLineBreak(int index)
        {
            // strip MSB
            return EncodedLineBreaks.DecodePosition(this.lineBreaks[index]);
        }

        public int EndOfLineBreak(int index)
        {
            uint encodedPosition = this.lineBreaks[index];
            int decodedPosition = EncodedLineBreaks.DecodePosition(encodedPosition);
            if (EncodedLineBreaks.IsSingleCharLineBreak(encodedPosition))
            {
                return decodedPosition + 1;
            }
            else
            {
                return decodedPosition + 2;
            }
        }

        private enum LineSearchResult { SingleCharLineBreak, DoubleCharLineBreak, NotLineBreak };

        private LineSearchResult LineSearch(int index)
        {
            int lo = 0;
            int hi = this.lineBreaks.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                uint probe = this.lineBreaks[mid];
                int probePosition = EncodedLineBreaks.DecodePosition(probe);
                if (probePosition < index)
                {
                    lo = mid + 1;
                }
                else if (probePosition > index)
                {
                    hi = mid - 1;
                }
                else
                {
                    if (EncodedLineBreaks.IsSingleCharLineBreak(probe))
                    {
                        return LineSearchResult.SingleCharLineBreak;
                    }
                    else
                    {
                        return LineSearchResult.DoubleCharLineBreak;
                    }
                }
            }
            return LineSearchResult.NotLineBreak;
        }

        public bool IsNewLine(int index)
        {
            // try really hard to answer this question without forcing decompression.
            // things will go poorly if index is out of range but all calls are internal
            // and parameters are well vetted by the time we get here.
            if (index == 0)
            {
                return this.startsWithNewline;
            }
            else
            {
                // don't count this query as a page reference for LRU purposes
                char[] localUncompressed = MaybeGetDecompressed();
                if (localUncompressed != null)
                {
                    return localUncompressed[index] == '\n';
                }

                switch (LineSearch(index))
                {
                    case LineSearchResult.SingleCharLineBreak:
                        // line break has length one. we have to decompress
                        // to determine whether it is a newline.
                        return EnsureDecompressed()[index] == '\n';

                    case LineSearchResult.DoubleCharLineBreak:
                        // there is a two-character line break at index, and its first
                        // character is a return, not a newline.
                        return false;

                    default:
                        // no line break at index. check for \r\n at index - 1.
                        Debug.Assert(index > 0);
                        return LineSearch(index - 1) == LineSearchResult.DoubleCharLineBreak;
                }
            }
        }

        public bool IsReturn(int index)
        {
            // try really hard to answer this question without forcing decompression.
            // things will go poorly if index is out of range but all calls are internal
            // and parameters are well vetted by the time we get here.
            if (index == this.length - 1)
            {
                return this.endsWithReturn;
            }
            else
            {
                // don't count this query as a page reference for LRU purposes
                char[] localUncompressed = MaybeGetDecompressed();
                if (localUncompressed != null)
                {
                    return localUncompressed[index] == '\r';
                }

                switch (LineSearch(index))
                {
                    case LineSearchResult.SingleCharLineBreak:
                        // line break has length one - we have to read it
                        // unless we do more encoding tricks
                        return EnsureDecompressed()[index] == '\r';

                    case LineSearchResult.DoubleCharLineBreak:
                        // first character of two-character line break, it must be a return
                        return true;

                    default:
                        // not part of a line break
                        return false;
                }
            }
        }

        int ILineBreaks.Length
        {
            get { return this.lineBreaks.Length; }
        }

        public override bool UnloadWhileLocked()
        {
            char[] localUncompressed = this.uncompressed;
            if (localUncompressed != null)
            {
                if (TextModelOptions.CompressedStorageRetainWeakReferences)
                {
                    if (this.weakUncompressed != null)
                    {
                        this.weakUncompressed.SetTarget(localUncompressed);
                    }
                    else
                    {
                        this.weakUncompressed = new WeakReference<char[]>(localUncompressed);
                    }
                }
                this.uncompressed = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Write(TextWriter writer, int startIndex, int length)
        {
            char[] data = EnsureDecompressed();
            writer.Write(data, startIndex, length);
        }
    }
}
