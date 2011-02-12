namespace Sharpen
{
	using System;
	using System.IO;
	using System.Security.Cryptography;

	public abstract class MessageDigest
	{
		protected MessageDigest ()
		{
		}
		
		public void Digest (byte[] buffer, int o, int len)
		{
			byte[] d = Digest ();
			d.CopyTo (buffer, o);
		}

		public byte[] Digest (byte[] buffer)
		{
			Update (buffer);
			return Digest ();
		}

		public abstract byte[] Digest ();
		public abstract int GetDigestLength ();
		public static MessageDigest GetInstance (string algorithm)
		{
			switch (algorithm.ToLower ()) {
			case "sha-1":
				return new MessageDigest<SHA1Managed> ();
			case "md5":
				return new MessageDigest<MD5CryptoServiceProvider> ();
			}
			throw new NotSupportedException (string.Format ("The requested algorithm \"{0}\" is not supported.", algorithm));
		}

		public abstract void Reset ();
		public abstract void Update (byte[] b);
		public abstract void Update (byte b);
		public abstract void Update (byte[] b, int offset, int len);
	}


	public class MessageDigest<TAlgorithm> : MessageDigest where TAlgorithm : HashAlgorithm, new()
	{
		private TAlgorithm _hash;
		private CryptoStream _stream;

		public MessageDigest ()
		{
			this.Init ();
		}

		public override byte[] Digest ()
		{
			this._stream.FlushFinalBlock ();
			byte[] hash = this._hash.Hash;
			this.Reset ();
			return hash;
		}

		public void Dispose ()
		{
			if (this._stream != null) {
				this._stream.Dispose ();
			}
			this._stream = null;
		}

		public override int GetDigestLength ()
		{
			return (this._hash.HashSize / 8);
		}

		private void Init ()
		{
			this._hash = Activator.CreateInstance<TAlgorithm> ();
			this._stream = new CryptoStream (Stream.Null, this._hash, CryptoStreamMode.Write);
		}

		public override void Reset ()
		{
			this.Dispose ();
			this.Init ();
		}

		public override void Update (byte[] input)
		{
			this._stream.Write (input, 0, input.Length);
		}

		public override void Update (byte input)
		{
			this._stream.WriteByte (input);
		}

		public override void Update (byte[] input, int index, int count)
		{
			this._stream.Write (input, index, count);
		}
	}
}
