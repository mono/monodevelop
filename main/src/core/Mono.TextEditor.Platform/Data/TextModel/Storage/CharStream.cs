using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Stream that converts between characters and bytes.
    /// </summary>
    internal class CharStream : Stream
    {
        char[] data;
        int length;         // *byte* length of data
        int position = 0;   // *byte* offset into data
        byte? pendingByte;

        public CharStream(char[] data, int length)
        {
            this.data = data;
            this.length = 2 * length;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        public override void Flush()
        {
        }

        public override long Length { get { return (int)this.length; } }

        public override long Position
        {
            get { return this.position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int actualCount = Math.Min(count, this.length - this.position);
            if (actualCount > 0)
            {
                int residue = actualCount;
                byte hi, lo;
                if (this.position % 2 == 1)
                {
                    Split(this.data[this.position / 2], out hi, out lo);
                    buffer[offset++] = hi;
                    this.position++;
                    residue--;
                }
                for (int i = 0; i < residue / 2; ++i)
                {
                    Split(this.data[this.position / 2], out hi, out lo);
                    buffer[offset++] = hi;
                    buffer[offset++] = lo;
                    this.position += 2;
                }
                if (residue % 2 == 1)
                {
                    Split(this.data[this.position / 2], out hi, out lo);
                    buffer[offset++] = lo;
                    this.position++;
                }
            }
            return actualCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }

            if (this.pendingByte.HasValue)
            {
                // finish previous character
                Debug.Assert(this.position % 2 == 1);
                this.data[this.position / 2] = Make(this.pendingByte.Value, buffer[offset]);
                this.position++;
                offset++;
                count--;
            }

            for (int i = 0; i < count / 2; ++i)
            {
                this.data[this.position / 2] = Make(buffer[offset], buffer[offset + 1]);
                this.position += 2;
                offset += 2;
            }

            if (count % 2 == 0)
            {
                this.pendingByte = null;
            }
            else
            {
                this.pendingByte = buffer[offset];
                this.position++;
            }
        }

        private char Make(byte hi, byte lo)
        {
            return (char)((hi << 8) | lo);
        }

        private void Split(char c, out byte hi, out byte lo)
        {
            hi = (byte)(c >> 8);
            lo = (byte)(c & 255);
        }
    }
}
