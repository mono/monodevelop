/*
Copyright (c) 2006-2010 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

This code is based on jsch (http://www.jcraft.com/jsch).
All credit should go to the authors of jsch.
*/

using System;
using NSch;
using Sharpen;

namespace NSch
{
	public abstract class KeyPair
	{
		public const int ERROR = 0;

		public const int DSA = 1;

		public const int RSA = 2;

		public const int UNKNOWN = 3;

		internal const int VENDOR_OPENSSH = 0;

		internal const int VENDOR_FSECURE = 1;

		internal int vendor = VENDOR_OPENSSH;

		private static readonly byte[] cr = Util.Str2byte("\n");

		/// <exception cref="NSch.JSchException"></exception>
		public static NSch.KeyPair GenKeyPair(JSch jsch, int type)
		{
			return GenKeyPair(jsch, type, 1024);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public static NSch.KeyPair GenKeyPair(JSch jsch, int type, int key_size)
		{
			NSch.KeyPair kpair = null;
			if (type == DSA)
			{
				kpair = new KeyPairDSA(jsch);
			}
			else
			{
				if (type == RSA)
				{
					kpair = new KeyPairRSA(jsch);
				}
			}
			if (kpair != null)
			{
				kpair.Generate(key_size);
			}
			return kpair;
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal abstract void Generate(int key_size);

		internal abstract byte[] GetBegin();

		internal abstract byte[] GetEnd();

		internal abstract int GetKeySize();

		internal JSch jsch = null;

		private NSch.Cipher cipher;

		private HASH hash;

		private Random random;

		private byte[] passphrase;

		public KeyPair(JSch jsch)
		{
			this.jsch = jsch;
		}

		internal static byte[][] header = new byte[][] { Util.Str2byte("Proc-Type: 4,ENCRYPTED"
			), Util.Str2byte("DEK-Info: DES-EDE3-CBC,") };

		internal abstract byte[] GetPrivateKey();

		public virtual void WritePrivateKey(OutputStream @out)
		{
			byte[] plain = GetPrivateKey();
			byte[][] _iv = new byte[1][];
			byte[] encoded = Encrypt(plain, _iv);
			if (encoded != plain)
			{
				Util.Bzero(plain);
			}
			byte[] iv = _iv[0];
			byte[] prv = Util.ToBase64(encoded, 0, encoded.Length);
			try
			{
				@out.Write(GetBegin());
				@out.Write(cr);
				if (passphrase != null)
				{
					@out.Write(header[0]);
					@out.Write(cr);
					@out.Write(header[1]);
					for (int i = 0; i < iv.Length; i++)
					{
						@out.Write(B2a(unchecked((byte)((iv[i] >> 4) & unchecked((int)(0x0f))))));
						@out.Write(B2a(unchecked((byte)(iv[i] & unchecked((int)(0x0f))))));
					}
					@out.Write(cr);
					@out.Write(cr);
				}
				int i_1 = 0;
				while (i_1 < prv.Length)
				{
					if (i_1 + 64 < prv.Length)
					{
						@out.Write(prv, i_1, 64);
						@out.Write(cr);
						i_1 += 64;
						continue;
					}
					@out.Write(prv, i_1, prv.Length - i_1);
					@out.Write(cr);
					break;
				}
				@out.Write(GetEnd());
				@out.Write(cr);
			}
			catch (Exception)
			{
			}
		}

		private static byte[] space = Util.Str2byte(" ");

		//out.close();
		internal abstract byte[] GetKeyTypeName();

		public abstract int GetKeyType();

		public virtual byte[] GetPublicKeyBlob()
		{
			return publickeyblob;
		}

		public virtual void WritePublicKey(OutputStream @out, string comment)
		{
			byte[] pubblob = GetPublicKeyBlob();
			byte[] pub = Util.ToBase64(pubblob, 0, pubblob.Length);
			try
			{
				@out.Write(GetKeyTypeName());
				@out.Write(space);
				@out.Write(pub, 0, pub.Length);
				@out.Write(space);
				@out.Write(Util.Str2byte(comment));
				@out.Write(cr);
			}
			catch (Exception)
			{
			}
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WritePublicKey(string name, string comment)
		{
			FileOutputStream fos = new FileOutputStream(name);
			WritePublicKey(fos, comment);
			fos.Close();
		}

		public virtual void WriteSECSHPublicKey(OutputStream @out, string comment)
		{
			byte[] pubblob = GetPublicKeyBlob();
			byte[] pub = Util.ToBase64(pubblob, 0, pubblob.Length);
			try
			{
				@out.Write(Util.Str2byte("---- BEGIN SSH2 PUBLIC KEY ----"));
				@out.Write(cr);
				@out.Write(Util.Str2byte("Comment: \"" + comment + "\""));
				@out.Write(cr);
				int index = 0;
				while (index < pub.Length)
				{
					int len = 70;
					if ((pub.Length - index) < len)
					{
						len = pub.Length - index;
					}
					@out.Write(pub, index, len);
					@out.Write(cr);
					index += len;
				}
				@out.Write(Util.Str2byte("---- END SSH2 PUBLIC KEY ----"));
				@out.Write(cr);
			}
			catch (Exception)
			{
			}
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WriteSECSHPublicKey(string name, string comment)
		{
			FileOutputStream fos = new FileOutputStream(name);
			WriteSECSHPublicKey(fos, comment);
			fos.Close();
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WritePrivateKey(string name)
		{
			FileOutputStream fos = new FileOutputStream(name);
			WritePrivateKey(fos);
			fos.Close();
		}

		public virtual string GetFingerPrint()
		{
			if (hash == null)
			{
				hash = GenHash();
			}
			byte[] kblob = GetPublicKeyBlob();
			if (kblob == null)
			{
				return null;
			}
			return GetKeySize() + " " + Util.GetFingerPrint(hash, kblob);
		}

		private byte[] Encrypt(byte[] plain, byte[][] _iv)
		{
			if (passphrase == null)
			{
				return plain;
			}
			if (cipher == null)
			{
				cipher = GenCipher();
			}
			byte[] iv = _iv[0] = new byte[cipher.GetIVSize()];
			if (random == null)
			{
				random = GenRandom();
			}
			random.Fill(iv, 0, iv.Length);
			byte[] key = GenKey(passphrase, iv);
			byte[] encoded = plain;
			{
				// PKCS#5Padding
				//int bsize=cipher.getBlockSize();
				int bsize = cipher.GetIVSize();
				byte[] foo = new byte[(encoded.Length / bsize + 1) * bsize];
				System.Array.Copy(encoded, 0, foo, 0, encoded.Length);
				int padding = bsize - encoded.Length % bsize;
				for (int i = foo.Length - 1; (foo.Length - padding) <= i; i--)
				{
					foo[i] = unchecked((byte)padding);
				}
				encoded = foo;
			}
			try
			{
				cipher.Init(NSch.Cipher.ENCRYPT_MODE, key, iv);
				cipher.Update(encoded, 0, encoded.Length, encoded, 0);
			}
			catch (Exception)
			{
			}
			//System.err.println(e);
			Util.Bzero(key);
			return encoded;
		}

		internal abstract bool Parse(byte[] data);

		private byte[] Decrypt(byte[] data, byte[] passphrase, byte[] iv)
		{
			try
			{
				byte[] key = GenKey(passphrase, iv);
				cipher.Init(NSch.Cipher.DECRYPT_MODE, key, iv);
				Util.Bzero(key);
				byte[] plain = new byte[data.Length];
				cipher.Update(data, 0, data.Length, plain, 0);
				return plain;
			}
			catch (Exception)
			{
			}
			//System.err.println(e);
			return null;
		}

		internal virtual int WriteSEQUENCE(byte[] buf, int index, int len)
		{
			buf[index++] = unchecked((int)(0x30));
			index = WriteLength(buf, index, len);
			return index;
		}

		internal virtual int WriteINTEGER(byte[] buf, int index, byte[] data)
		{
			buf[index++] = unchecked((int)(0x02));
			index = WriteLength(buf, index, data.Length);
			System.Array.Copy(data, 0, buf, index, data.Length);
			index += data.Length;
			return index;
		}

		internal virtual int CountLength(int len)
		{
			int i = 1;
			if (len <= unchecked((int)(0x7f)))
			{
				return i;
			}
			while (len > 0)
			{
				len = (int)(((uint)len) >> 8);
				i++;
			}
			return i;
		}

		internal virtual int WriteLength(byte[] data, int index, int len)
		{
			int i = CountLength(len) - 1;
			if (i == 0)
			{
				data[index++] = unchecked((byte)len);
				return index;
			}
			data[index++] = unchecked((byte)(unchecked((int)(0x80)) | i));
			int j = index + i;
			while (i > 0)
			{
				data[index + i - 1] = unchecked((byte)(len & unchecked((int)(0xff))));
				len = (int)(((uint)len) >> 8);
				i--;
			}
			return j;
		}

		private Random GenRandom()
		{
			if (random == null)
			{
				try
				{
					Type c = Sharpen.Runtime.GetType(JSch.GetConfig("random"));
					random = (Random)(System.Activator.CreateInstance(c));
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("connect: random " + e);
				}
			}
			return random;
		}

		private HASH GenHash()
		{
			try
			{
				Type c = Sharpen.Runtime.GetType(JSch.GetConfig("md5"));
				hash = (HASH)(System.Activator.CreateInstance(c));
				hash.Init();
			}
			catch (Exception)
			{
			}
			return hash;
		}

		private NSch.Cipher GenCipher()
		{
			try
			{
				Type c;
				c = Sharpen.Runtime.GetType(JSch.GetConfig("3des-cbc"));
				cipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
			}
			catch (Exception)
			{
			}
			return cipher;
		}

		internal virtual byte[] GenKey(byte[] passphrase, byte[] iv)
		{
			lock (this)
			{
				if (cipher == null)
				{
					cipher = GenCipher();
				}
				if (hash == null)
				{
					hash = GenHash();
				}
				byte[] key = new byte[cipher.GetBlockSize()];
				int hsize = hash.GetBlockSize();
				byte[] hn = new byte[key.Length / hsize * hsize + (key.Length % hsize == 0 ? 0 : 
					hsize)];
				try
				{
					byte[] tmp = null;
					if (vendor == VENDOR_OPENSSH)
					{
						for (int index = 0; index + hsize <= hn.Length; )
						{
							if (tmp != null)
							{
								hash.Update(tmp, 0, tmp.Length);
							}
							hash.Update(passphrase, 0, passphrase.Length);
							hash.Update(iv, 0, iv.Length);
							tmp = hash.Digest();
							System.Array.Copy(tmp, 0, hn, index, tmp.Length);
							index += tmp.Length;
						}
						System.Array.Copy(hn, 0, key, 0, key.Length);
					}
					else
					{
						if (vendor == VENDOR_FSECURE)
						{
							for (int index = 0; index + hsize <= hn.Length; )
							{
								if (tmp != null)
								{
									hash.Update(tmp, 0, tmp.Length);
								}
								hash.Update(passphrase, 0, passphrase.Length);
								tmp = hash.Digest();
								System.Array.Copy(tmp, 0, hn, index, tmp.Length);
								index += tmp.Length;
							}
							System.Array.Copy(hn, 0, key, 0, key.Length);
						}
					}
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine(e);
				}
				return key;
			}
		}

		public virtual void SetPassphrase(string passphrase)
		{
			if (passphrase == null || passphrase.Length == 0)
			{
				SetPassphrase((byte[])null);
			}
			else
			{
				SetPassphrase(Util.Str2byte(passphrase));
			}
		}

		public virtual void SetPassphrase(byte[] passphrase)
		{
			if (passphrase != null && passphrase.Length == 0)
			{
				passphrase = null;
			}
			this.passphrase = passphrase;
		}

		private bool encrypted = false;

		private byte[] data = null;

		private byte[] iv = null;

		private byte[] publickeyblob = null;

		public virtual bool IsEncrypted()
		{
			return encrypted;
		}

		public virtual bool Decrypt(string _passphrase)
		{
			if (_passphrase == null || _passphrase.Length == 0)
			{
				return !encrypted;
			}
			return Decrypt(Util.Str2byte(_passphrase));
		}

		public virtual bool Decrypt(byte[] _passphrase)
		{
			if (!encrypted)
			{
				return true;
			}
			if (_passphrase == null)
			{
				return !encrypted;
			}
			byte[] bar = new byte[_passphrase.Length];
			System.Array.Copy(_passphrase, 0, bar, 0, bar.Length);
			_passphrase = bar;
			byte[] foo = Decrypt(data, _passphrase, iv);
			Util.Bzero(_passphrase);
			if (Parse(foo))
			{
				encrypted = false;
			}
			return !encrypted;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public static NSch.KeyPair Load(JSch jsch, string prvkey)
		{
			string pubkey = prvkey + ".pub";
			if (!new FilePath(pubkey).Exists())
			{
				pubkey = null;
			}
			return Load(jsch, prvkey, pubkey);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public static NSch.KeyPair Load(JSch jsch, string prvkey, string pubkey)
		{
			byte[] iv = new byte[8];
			// 8
			bool encrypted = true;
			byte[] data = null;
			byte[] publickeyblob = null;
			int type = ERROR;
			int vendor = VENDOR_OPENSSH;
			try
			{
				FilePath file = new FilePath(prvkey);
				FileInputStream fis = new FileInputStream(prvkey);
				byte[] buf = new byte[(int)(file.Length())];
				int len = 0;
				while (true)
				{
					int i = fis.Read(buf, len, buf.Length - len);
					if (i <= 0)
					{
						break;
					}
					len += i;
				}
				fis.Close();
				int i_1 = 0;
				while (i_1 < len)
				{
					if (buf[i_1] == 'B' && buf[i_1 + 1] == 'E' && buf[i_1 + 2] == 'G' && buf[i_1 + 3]
						 == 'I')
					{
						i_1 += 6;
						if (buf[i_1] == 'D' && buf[i_1 + 1] == 'S' && buf[i_1 + 2] == 'A')
						{
							type = DSA;
						}
						else
						{
							if (buf[i_1] == 'R' && buf[i_1 + 1] == 'S' && buf[i_1 + 2] == 'A')
							{
								type = RSA;
							}
							else
							{
								if (buf[i_1] == 'S' && buf[i_1 + 1] == 'S' && buf[i_1 + 2] == 'H')
								{
									// FSecure
									type = UNKNOWN;
									vendor = VENDOR_FSECURE;
								}
								else
								{
									//System.err.println("invalid format: "+identity);
									throw new JSchException("invalid privatekey: " + prvkey);
								}
							}
						}
						i_1 += 3;
						continue;
					}
					if (buf[i_1] == 'C' && buf[i_1 + 1] == 'B' && buf[i_1 + 2] == 'C' && buf[i_1 + 3]
						 == ',')
					{
						i_1 += 4;
						for (int ii = 0; ii < iv.Length; ii++)
						{
							iv[ii] = unchecked((byte)(((A2b(buf[i_1++]) << 4) & unchecked((int)(0xf0))) + (A2b
								(buf[i_1++]) & unchecked((int)(0xf)))));
						}
						continue;
					}
					if (buf[i_1] == unchecked((int)(0x0d)) && i_1 + 1 < buf.Length && buf[i_1 + 1] ==
						 unchecked((int)(0x0a)))
					{
						i_1++;
						continue;
					}
					if (buf[i_1] == unchecked((int)(0x0a)) && i_1 + 1 < buf.Length)
					{
						if (buf[i_1 + 1] == unchecked((int)(0x0a)))
						{
							i_1 += 2;
							break;
						}
						if (buf[i_1 + 1] == unchecked((int)(0x0d)) && i_1 + 2 < buf.Length && buf[i_1 + 2
							] == unchecked((int)(0x0a)))
						{
							i_1 += 3;
							break;
						}
						bool inheader = false;
						for (int j = i_1 + 1; j < buf.Length; j++)
						{
							if (buf[j] == unchecked((int)(0x0a)))
							{
								break;
							}
							//if(buf[j]==0x0d) break;
							if (buf[j] == ':')
							{
								inheader = true;
								break;
							}
						}
						if (!inheader)
						{
							i_1++;
							encrypted = false;
							// no passphrase
							break;
						}
					}
					i_1++;
				}
				if (type == ERROR)
				{
					throw new JSchException("invalid privatekey: " + prvkey);
				}
				int start = i_1;
				while (i_1 < len)
				{
					if (buf[i_1] == unchecked((int)(0x0a)))
					{
						bool xd = (buf[i_1 - 1] == unchecked((int)(0x0d)));
						System.Array.Copy(buf, i_1 + 1, buf, i_1 - (xd ? 1 : 0), len - i_1 - 1 - (xd ? 1 : 
							0));
						if (xd)
						{
							len--;
						}
						len--;
						continue;
					}
					if (buf[i_1] == '-')
					{
						break;
					}
					i_1++;
				}
				data = Util.FromBase64(buf, start, i_1 - start);
				if (data.Length > 4 && data[0] == unchecked((byte)unchecked((int)(0x3f))) && data
					[1] == unchecked((byte)unchecked((int)(0x6f))) && data[2] == unchecked((byte)unchecked(
					(int)(0xf9))) && data[3] == unchecked((byte)unchecked((int)(0xeb))))
				{
					// FSecure
					Buffer _buf = new Buffer(data);
					_buf.GetInt();
					// 0x3f6ff9be
					_buf.GetInt();
					byte[] _type = _buf.GetString();
					//System.err.println("type: "+new String(_type)); 
					byte[] _cipher = _buf.GetString();
					string cipher = Util.Byte2str(_cipher);
					//System.err.println("cipher: "+cipher); 
					if (cipher.Equals("3des-cbc"))
					{
						_buf.GetInt();
						byte[] foo = new byte[data.Length - _buf.GetOffSet()];
						_buf.GetByte(foo);
						data = foo;
						encrypted = true;
						throw new JSchException("unknown privatekey format: " + prvkey);
					}
					else
					{
						if (cipher.Equals("none"))
						{
							_buf.GetInt();
							_buf.GetInt();
							encrypted = false;
							byte[] foo = new byte[data.Length - _buf.GetOffSet()];
							_buf.GetByte(foo);
							data = foo;
						}
					}
				}
				if (pubkey != null)
				{
					try
					{
						file = new FilePath(pubkey);
						fis = new FileInputStream(pubkey);
						buf = new byte[(int)(file.Length())];
						len = 0;
						while (true)
						{
							i_1 = fis.Read(buf, len, buf.Length - len);
							if (i_1 <= 0)
							{
								break;
							}
							len += i_1;
						}
						fis.Close();
						if (buf.Length > 4 && buf[0] == '-' && buf[1] == '-' && buf[2] == '-' && buf[3] ==
							 '-')
						{
							// FSecure's public key
							bool valid = true;
							i_1 = 0;
							do
							{
								i_1++;
							}
							while (buf.Length > i_1 && buf[i_1] != unchecked((int)(0x0a)));
							if (buf.Length <= i_1)
							{
								valid = false;
							}
							while (valid)
							{
								if (buf[i_1] == unchecked((int)(0x0a)))
								{
									bool inheader = false;
									for (int j = i_1 + 1; j < buf.Length; j++)
									{
										if (buf[j] == unchecked((int)(0x0a)))
										{
											break;
										}
										if (buf[j] == ':')
										{
											inheader = true;
											break;
										}
									}
									if (!inheader)
									{
										i_1++;
										break;
									}
								}
								i_1++;
							}
							if (buf.Length <= i_1)
							{
								valid = false;
							}
							start = i_1;
							while (valid && i_1 < len)
							{
								if (buf[i_1] == unchecked((int)(0x0a)))
								{
									System.Array.Copy(buf, i_1 + 1, buf, i_1, len - i_1 - 1);
									len--;
									continue;
								}
								if (buf[i_1] == '-')
								{
									break;
								}
								i_1++;
							}
							if (valid)
							{
								publickeyblob = Util.FromBase64(buf, start, i_1 - start);
								if (type == UNKNOWN)
								{
									if (publickeyblob[8] == 'd')
									{
										type = DSA;
									}
									else
									{
										if (publickeyblob[8] == 'r')
										{
											type = RSA;
										}
									}
								}
							}
						}
						else
						{
							if (buf[0] == 's' && buf[1] == 's' && buf[2] == 'h' && buf[3] == '-')
							{
								i_1 = 0;
								while (i_1 < len)
								{
									if (buf[i_1] == ' ')
									{
										break;
									}
									i_1++;
								}
								i_1++;
								if (i_1 < len)
								{
									start = i_1;
									while (i_1 < len)
									{
										if (buf[i_1] == ' ')
										{
											break;
										}
										i_1++;
									}
									publickeyblob = Util.FromBase64(buf, start, i_1 - start);
								}
							}
						}
					}
					catch (Exception)
					{
					}
				}
			}
			catch (Exception e)
			{
				if (e is JSchException)
				{
					throw (JSchException)e;
				}
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
			NSch.KeyPair kpair = null;
			if (type == DSA)
			{
				kpair = new KeyPairDSA(jsch);
			}
			else
			{
				if (type == RSA)
				{
					kpair = new KeyPairRSA(jsch);
				}
			}
			if (kpair != null)
			{
				kpair.encrypted = encrypted;
				kpair.publickeyblob = publickeyblob;
				kpair.vendor = vendor;
				if (encrypted)
				{
					kpair.iv = iv;
					kpair.data = data;
				}
				else
				{
					if (kpair.Parse(data))
					{
						return kpair;
					}
					else
					{
						throw new JSchException("invalid privatekey: " + prvkey);
					}
				}
			}
			return kpair;
		}

		private static byte A2b(byte c)
		{
			if ('0' <= c && ((sbyte)c) <= '9')
			{
				return unchecked((byte)(c - '0'));
			}
			return unchecked((byte)(c - 'a' + 10));
		}

		private static byte B2a(byte c)
		{
			if (0 <= c && ((sbyte)c) <= 9)
			{
				return unchecked((byte)(c + '0'));
			}
			return unchecked((byte)(c - 10 + 'A'));
		}

		public virtual void Dispose()
		{
			Util.Bzero(passphrase);
		}

		~KeyPair()
		{
			Dispose();
		}
	}
}
