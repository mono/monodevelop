using System;
using System.IO;

namespace MonoDevelop.Gui.Utils.ReportingStream {

	public delegate void ReadNotification (object arg, int amount);

	public class ReportingStream : Stream, IDisposable {
		Stream s;
		ReadNotification haveRead;
		object arg;
		private int count = 0;
		private readonly int reportHurdle;
		
		public ReportingStream(Stream stream, ReadNotification DataRead, object DataReadArg) : this (stream, DataRead, DataReadArg, .01) { }
		
		public ReportingStream(Stream stream, ReadNotification DataRead, object DataReadArg, double reportPercentage)
		{
			this.s = stream;
			this.haveRead = DataRead;
			this.arg = DataReadArg;
			this.reportHurdle = (int)(reportPercentage * stream.Length);
		}

		private void haveReadSomeData(int bytes)
		{
			this.count += bytes;
			if (this.count > reportHurdle){
				haveRead(arg, this.count);
				this.count = 0;
			}
		}

		public override bool CanRead { 
			get { return s.CanRead; }
		}

		public override bool CanSeek {
			get { return s.CanSeek; }
		}

		public override bool CanWrite {
			get { return s.CanWrite; }
		}

		public override long Length {
			get { return s.Length; }
		}
	
		public override long Position {
			get { return s.Position; }
			set { s.Position = value; }
		}

		public override void Close()
		{
			s.Close();
		}

		public override void Flush()
		{
			s.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int amountRead = s.Read(buffer, offset, count);
			if (amountRead > 0)
				haveReadSomeData(amountRead);
			return amountRead;
		}

		public override int ReadByte()
		{
			int result = s.ReadByte();
			if (result != -1)
				haveReadSomeData(1);
			return result;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return s.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			s.SetLength(value);
		}

		void IDisposable.Dispose()
		{
			(s as IDisposable).Dispose();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			s.Write(buffer, offset, count);
		}

		public override void WriteByte(byte value)
		{
			s.WriteByte(value);
		}
	}
}
