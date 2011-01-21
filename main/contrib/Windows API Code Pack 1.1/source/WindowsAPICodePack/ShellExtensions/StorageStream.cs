using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// A wrapper for the native IStream object.
    /// </summary>
    public class StorageStream : Stream, IDisposable
    {
        IStream _stream;
        private bool _isReadOnly = false;

        internal StorageStream(IStream stream, bool readOnly)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            _isReadOnly = readOnly;
            this._stream = stream;
        }

        /// <summary>
        /// Reads a single byte from the stream, moving the current position ahead by 1.
        /// </summary>
        /// <returns>A single byte from the stream, -1 if end of stream.</returns>
        public override int ReadByte()
        {
            ThrowIfDisposed();

            byte[] buffer = new byte[1];
            if (Read(buffer, 0, 1) > 0) { return buffer[0]; }
            return -1;
        }

        /// <summary>
        /// Writes a single byte to the stream
        /// </summary>
        /// <param name="value">Byte to write to stream</param>
        public override void WriteByte(byte value)
        {
            ThrowIfDisposed();
            byte[] buffer = new byte[] { value };
            Write(buffer, 0, 1);
        }

        /// <summary>
        /// Gets whether the stream can be read from.
        /// </summary>
        public override bool CanRead { get { return _stream != null; } }

        /// <summary>
        /// Gets whether seeking is supported by the stream.
        /// </summary>
        public override bool CanSeek { get { return _stream != null; } }

        /// <summary>
        /// Gets whether the stream can be written to.
        /// Always false.
        /// </summary>
        public override bool CanWrite { get { return _stream != null && !_isReadOnly; } }

        /// <summary>
        /// Reads a buffer worth of bytes from the stream.
        /// </summary>
        /// <param name="buffer">Buffer to fill</param>
        /// <param name="offset">Offset to start filling in the buffer</param>
        /// <param name="count">Number of bytes to read from the stream</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            if (buffer == null) { throw new ArgumentNullException("buffer"); }
            if (offset < 0) { throw new ArgumentOutOfRangeException("offset", LocalizedMessages.StorageStreamOffsetLessThanZero); }
            if (count < 0) { throw new ArgumentOutOfRangeException("count", LocalizedMessages.StorageStreamCountLessThanZero); }
            if (offset + count > buffer.Length) { throw new ArgumentException(LocalizedMessages.StorageStreamBufferOverflow, "count"); }

            int bytesRead = 0;
            if (count > 0)
            {
                IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(ulong));
                try
                {
                    if (offset == 0)
                    {
                        _stream.Read(buffer, count, ptr);
                        bytesRead = (int)Marshal.ReadInt64(ptr);
                    }
                    else
                    {
                        byte[] tempBuffer = new byte[count];
                        _stream.Read(tempBuffer, count, ptr);

                        bytesRead = (int)Marshal.ReadInt64(ptr);
                        if (bytesRead > 0)
                        {
                            Array.Copy(tempBuffer, 0, buffer, offset, bytesRead);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Writes a buffer to the stream if able to do so.
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset in buffer to start writing</param>
        /// <param name="count">Number of bytes to write to the stream</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            if (_isReadOnly) { throw new InvalidOperationException(LocalizedMessages.StorageStreamIsReadonly); }
            if (buffer == null) { throw new ArgumentNullException("buffer"); }
            if (offset < 0) { throw new ArgumentOutOfRangeException("offset", LocalizedMessages.StorageStreamOffsetLessThanZero); }
            if (count < 0) { throw new ArgumentOutOfRangeException("count", LocalizedMessages.StorageStreamCountLessThanZero); }
            if (offset + count > buffer.Length) { throw new ArgumentException(LocalizedMessages.StorageStreamBufferOverflow, "count"); }

            if (count > 0)
            {
                IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(ulong));
                try
                {
                    if (offset == 0)
                    {
                        _stream.Write(buffer, count, ptr);
                    }
                    else
                    {
                        byte[] tempBuffer = new byte[count];
                        Array.Copy(buffer, offset, tempBuffer, 0, count);
                        _stream.Write(tempBuffer, count, ptr);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
            }
        }

        /// <summary>
        /// Gets the length of the IStream
        /// </summary>
        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                const int STATFLAG_NONAME = 1;
                System.Runtime.InteropServices.ComTypes.STATSTG stats;
                _stream.Stat(out stats, STATFLAG_NONAME);
                return stats.cbSize;
            }
        }

        /// <summary>
        /// Gets or sets the current position within the underlying IStream.
        /// </summary>
        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return Seek(0, SeekOrigin.Current);
            }
            set
            {
                ThrowIfDisposed();
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Seeks within the underlying IStream.
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="origin">Where to start seeking</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(long));
            try
            {
                _stream.Seek(offset, (int)origin, ptr);
                return Marshal.ReadInt64(ptr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        /// <summary>
        /// Sets the length of the stream
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            _stream.SetSize(value);
        }

        /// <summary>
        /// Commits data to be written to the stream if it is being cached.
        /// </summary>
        public override void Flush()
        {
            _stream.Commit((int)StorageStreamCommitOptions.None);
        }

        /// <summary>
        /// Disposes the stream.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            _stream = null;
            base.Dispose(disposing);
        }

        private void ThrowIfDisposed() { if (_stream == null) throw new ObjectDisposedException(GetType().Name); }
    }

    /// <summary>
    /// Options for commiting (flushing) an IStream storage stream
    /// </summary>
    [Flags]
    internal enum StorageStreamCommitOptions
    {
        /// <summary>
        /// Uses default options
        /// </summary>
        None = 0,

        /// <summary>
        /// Overwrite option
        /// </summary>
        Overwrite = 1,

        /// <summary>
        /// Only if current
        /// </summary>
        OnlyIfCurrent = 2,

        /// <summary>
        /// Commits to disk cache dangerously
        /// </summary>
        DangerouslyCommitMerelyToDiskCache = 4,

        /// <summary>
        /// Consolidate
        /// </summary>
        Consolidate = 8
    }
}
