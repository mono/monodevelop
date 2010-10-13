namespace Sharpen
{
	using System;
	using System.IO;

	internal class ObjectOutputStream : OutputStream
	{
		private BinaryWriter bw;

		public ObjectOutputStream (OutputStream os)
		{
			this.bw = new BinaryWriter (os.GetWrappedStream ());
		}

		public virtual void WriteInt (int i)
		{
			this.bw.Write (i);
		}
	}
}
