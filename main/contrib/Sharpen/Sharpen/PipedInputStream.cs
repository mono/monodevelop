
using System;
using System.Threading;

namespace Sharpen
{
	internal class PipedInputStream : InputStream
	{
		private byte[] oneBuffer;
		public const int PIPE_SIZE = 1024;
		
		protected byte[] buffer;
		private bool closed;
		private ManualResetEvent dataEvent;
		private int end;
		private int start;
		private object thisLock;
		private bool allowGrow = false;
		
		public int @in {
			get { return start; }
			set { start = value; }
		}
		
		public int @out {
			get { return end; }
			set { end = value; }
		}

		public PipedInputStream ()
		{
			thisLock = new object ();
			dataEvent = new ManualResetEvent (false);
			buffer = new byte[PIPE_SIZE + 1];
		}

		public PipedInputStream (PipedOutputStream os): this ()
		{
			os.Attach (this);
		}

		public override void Close ()
		{
			lock (thisLock) {
				closed = true;
				dataEvent.Set ();
			}
		}

		public override int Available ()
		{
			lock (thisLock) {
				if (start <= end) {
					return (end - start);
				}
				return ((buffer.Length - start) + end);
			}
		}

		public override int Read ()
		{
			if (oneBuffer == null)
				oneBuffer = new byte[1];
			if (Read (oneBuffer, 0, 1) == -1)
				return -1;
			return oneBuffer[0];
		}

		public override int Read (byte[] b, int offset, int len)
		{
			int length = 0;
			do {
				dataEvent.WaitOne ();
				lock (thisLock) {
					if (closed && Available () == 0) {
						return -1;
					}
					if (start < end) {
						length = Math.Min (len, end - start);
						Array.Copy (buffer, start, b, offset, length);
						start += length;
					} else if (start > end) {
						length = Math.Min (len, buffer.Length - start);
						Array.Copy (buffer, start, b, offset, length);
						len -= length;
						start = (start + length) % buffer.Length;
						if (len > 0) {
							int i = Math.Min (len, end);
							Array.Copy (buffer, 0, b, offset + length, i);
							start += i;
							length += i;
						}
					}
					if (start == end && !closed) {
						dataEvent.Reset ();
					}
					Monitor.PulseAll (thisLock);
				}
			} while (length == 0);
			return length;
		}
		
		private int Allocate (int len)
		{
			int alen;
			while ((alen = TryAllocate (len)) == 0) {
				// Wait until somebody reads data
				try {
					Monitor.Wait (thisLock);
				} catch {
					closed = true;
					dataEvent.Set ();
					throw;
				}
			}
			return alen;
		}
		
		int TryAllocate (int len)
		{
			int free;
			if (start <= end) {
				free = (buffer.Length - end) + start;
			} else {
				free = start - end;
			}
			if (free <= len) {
				if (!allowGrow)
					return free > 0 ? free - 1 : 0;
				int sizeInc = (len - free) + 1;
				byte[] destinationArray = new byte[buffer.Length + sizeInc];
				if (start <= end) {
					Array.Copy (buffer, start, destinationArray, start, end - start);
				} else {
					Array.Copy (buffer, 0, destinationArray, 0, end);
					Array.Copy (buffer, start, destinationArray, start + sizeInc, buffer.Length - start);
					start += sizeInc;
				}
				buffer = destinationArray;
			}
			return len;
		}
		
		internal void Write (int b)
		{
			lock (thisLock) {
				Allocate (1);
				buffer[end] = (byte)b;
				end = (end + 1) % buffer.Length;
				dataEvent.Set ();
			}
		}
		
		internal void Write (byte[] b, int offset, int len)
		{
			do {
				lock (thisLock) {
					int alen = Allocate (len);
					int length = Math.Min (buffer.Length - end, alen);
					Array.Copy (b, offset, buffer, end, length);
					end = (end + length) % buffer.Length;
					if (length < alen) {
						Array.Copy (b, offset + length, buffer, 0, alen - length);
						end += alen - length;
					}
					dataEvent.Set ();
					len -= alen;
					offset += alen;
				}
			} while (len > 0);
		}
	}
}
