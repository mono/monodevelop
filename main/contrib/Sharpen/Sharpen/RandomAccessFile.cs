namespace Sharpen
{
	using System;
	using System.IO;

	internal class RandomAccessFile
	{
		private FileStream stream;

		public RandomAccessFile (FilePath file, string mode) : this(file.GetPath (), mode)
		{
		}

		public RandomAccessFile (string file, string mode)
		{
			if (mode.IndexOf ('w') != -1)
				stream = new FileStream (file, System.IO.FileMode.OpenOrCreate, FileAccess.ReadWrite);
			else
				stream = new FileStream (file, System.IO.FileMode.Open, FileAccess.Read);
		}

		public void Close ()
		{
			stream.Close ();
		}

		public FileChannel GetChannel ()
		{
			return new FileChannel (this.stream);
		}

		public long GetFilePointer ()
		{
			return stream.Position;
		}

		public long Length ()
		{
			return stream.Length;
		}

		public int Read (byte[] buffer)
		{
			int r = stream.Read (buffer, 0, buffer.Length);
			return r > 0 ? r : -1;
		}

		public int Read (byte[] buffer, int start, int size)
		{
			return stream.Read (buffer, start, size);
		}

		public void ReadFully (byte[] buffer, int start, int size)
		{
			while (size > 0) {
				int num = stream.Read (buffer, start, size);
				if (num == 0) {
					throw new EOFException ();
				}
				size -= num;
				start += num;
			}
		}

		public void Seek (long pos)
		{
			stream.Position = pos;
		}

		public void SetLength (long len)
		{
			stream.SetLength (len);
		}

		public void Write (byte[] buffer)
		{
			stream.Write (buffer, 0, buffer.Length);
		}

		public void Write (byte[] buffer, int start, int size)
		{
			stream.Write (buffer, start, size);
		}
	}
}
