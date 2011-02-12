namespace Sharpen
{
	using System;
	using System.IO;

	internal class FileChannel
	{
		private FileStream s;
		byte[] buffer;
		bool isOpen;

		internal FileChannel (FileStream s)
		{
			this.s = s;
			isOpen = true;
		}
		
		internal FileStream Stream {
			get { return s; }
		}

		public void Close ()
		{
			isOpen = false;
			s.Close ();
		}
		
		public bool IsOpen ()
		{
			return isOpen;
		}

		public void Force (bool f)
		{
			s.Flush ();
		}

		public MappedByteBuffer Map ()
		{
			throw new NotImplementedException ();
		}

		public MappedByteBuffer Map (MapMode mode, long pos, int size)
		{
			throw new NotImplementedException ();
		}

		public int Read (byte[] buffer)
		{
			return s.Read (buffer, 0, buffer.Length);
		}

		public int Read (ByteBuffer buffer)
		{
			int offset = buffer.Position () + buffer.ArrayOffset ();
			int num2 = s.Read (buffer.Array (), offset, (buffer.Limit () + buffer.ArrayOffset ()) - offset);
			buffer.Position (buffer.Position () + num2);
			return num2;
		}

		public long Size ()
		{
			return s.Length;
		}

		public FileLock TryLock ()
		{
			try {
				s.Lock (0, int.MaxValue);
				return new FileLock (s);
			} catch (IOException) {
				return null;
			}
		}

		public int Write (byte[] buffer)
		{
			s.Write (buffer, 0, buffer.Length);
			return buffer.Length;
		}

		public int Write (ByteBuffer buffer)
		{
			int offset = buffer.Position () + buffer.ArrayOffset ();
			int count = (buffer.Limit () + buffer.ArrayOffset ()) - offset;
			s.Write (buffer.Array (), offset, count);
			buffer.Position (buffer.Position () + count);
			return count;
		}
		
		public long TransferFrom (FileChannel src, long pos, long count)
		{
			if (buffer == null)
				buffer = new byte [8092];
			int nr = src.s.Read (buffer, 0, (int) Math.Min (buffer.Length, count));
			long curPos = s.Position;
			s.Position = pos;
			s.Write (buffer, 0, nr);
			s.Position = curPos;
			return nr;
		}

		public enum MapMode
		{
			READ_ONLY
		}
	}
}
