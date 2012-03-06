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
	internal class IdentityFile : Identity
	{
		internal string identity;

		internal byte[] key;

		internal byte[] iv;

		private JSch jsch;

		private HASH hash;

		private byte[] encoded_data;

		private NSch.Cipher cipher;

		private byte[] P_array;

		private byte[] Q_array;

		private byte[] G_array;

		private byte[] pub_array;

		private byte[] prv_array;

		private byte[] n_array;

		private byte[] e_array;

		private byte[] d_array;

		private string algname = "ssh-rsa";

		private const int ERROR = 0;

		private const int RSA = 1;

		private const int DSS = 2;

		private const int UNKNOWN = 3;

		private const int OPENSSH = 0;

		private const int FSECURE = 1;

		private const int PUTTY = 2;

		private int type = ERROR;

		private int keytype = OPENSSH;

		private byte[] publickeyblob = null;

		private bool encrypted = true;

		// DSA
		// RSA
		// modulus
		// public exponent
		// private exponent
		//  private String algname="ssh-dss";
		/// <exception cref="NSch.JSchException"></exception>
		internal static NSch.IdentityFile NewInstance(string prvfile, string pubfile, JSch
			 jsch)
		{
			byte[] prvkey = null;
			byte[] pubkey = null;
			FilePath file = null;
			FileInputStream fis = null;
			try
			{
				file = new FilePath(prvfile);
				fis = new FileInputStream(prvfile);
				prvkey = new byte[(int)(file.Length())];
				int len = 0;
				while (true)
				{
					int i = fis.Read(prvkey, len, prvkey.Length - len);
					if (i <= 0)
					{
						break;
					}
					len += i;
				}
				fis.Close();
			}
			catch (Exception e)
			{
				try
				{
					if (fis != null)
					{
						fis.Close();
					}
				}
				catch (Exception)
				{
				}
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
			string _pubfile = pubfile;
			if (pubfile == null)
			{
				_pubfile = prvfile + ".pub";
			}
			try
			{
				file = new FilePath(_pubfile);
				fis = new FileInputStream(_pubfile);
				pubkey = new byte[(int)(file.Length())];
				int len = 0;
				while (true)
				{
					int i = fis.Read(pubkey, len, pubkey.Length - len);
					if (i <= 0)
					{
						break;
					}
					len += i;
				}
				fis.Close();
			}
			catch (Exception e)
			{
				try
				{
					if (fis != null)
					{
						fis.Close();
					}
				}
				catch (Exception)
				{
				}
				if (pubfile != null)
				{
					// The pubfile is explicitry given, but not accessible.
					if (e is Exception)
					{
						throw new JSchException(e.ToString(), (Exception)e);
					}
					throw new JSchException(e.ToString());
				}
			}
			return NewInstance(prvfile, prvkey, pubkey, jsch);
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal static NSch.IdentityFile NewInstance(string name, byte[] prvkey, byte[] 
			pubkey, JSch jsch)
		{
			try
			{
				return new NSch.IdentityFile(name, prvkey, pubkey, jsch);
			}
			finally
			{
				Util.Bzero(prvkey);
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		private IdentityFile(string name, byte[] prvkey, byte[] pubkey, JSch jsch)
		{
			this.identity = name;
			this.jsch = jsch;
			// prvkey from "ssh-add" command on the remote.
			if (pubkey == null && prvkey != null && (prvkey.Length > 11 && prvkey[0] == 0 && 
				prvkey[1] == 0 && prvkey[2] == 0 && prvkey[3] == 7))
			{
				Buffer buf = new Buffer(prvkey);
				string _type = Sharpen.Runtime.GetStringForBytes(buf.GetString());
				// ssh-rsa
				if (_type.Equals("ssh-rsa"))
				{
					type = RSA;
					n_array = buf.GetString();
					e_array = buf.GetString();
					d_array = buf.GetString();
					buf.GetString();
					buf.GetString();
					buf.GetString();
					this.identity += Sharpen.Runtime.GetStringForBytes(buf.GetString());
				}
				else
				{
					if (_type.Equals("ssh-dss"))
					{
						type = DSS;
						P_array = buf.GetString();
						Q_array = buf.GetString();
						G_array = buf.GetString();
						pub_array = buf.GetString();
						prv_array = buf.GetString();
						this.identity += Sharpen.Runtime.GetStringForBytes(buf.GetString());
					}
					else
					{
						throw new JSchException("privatekey: invalid key " + Sharpen.Runtime.GetStringForBytes
							(prvkey, 4, 7));
					}
				}
				encoded_data = prvkey;
				encrypted = false;
				keytype = OPENSSH;
				return;
			}
			try
			{
				Type c;
				c = Sharpen.Runtime.GetType((string)JSch.GetConfig("3des-cbc"));
				cipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
				key = new byte[cipher.GetBlockSize()];
				// 24
				iv = new byte[cipher.GetIVSize()];
				// 8
				c = Sharpen.Runtime.GetType((string)JSch.GetConfig("md5"));
				hash = (HASH)(System.Activator.CreateInstance(c));
				hash.Init();
				byte[] buf = prvkey;
				int len = buf.Length;
				int i = 0;
				while (i < len)
				{
					if (buf[i] == '-' && i + 4 < len && buf[i + 1] == '-' && buf[i + 2] == '-' && buf
						[i + 3] == '-' && buf[i + 4] == '-')
					{
						break;
					}
					i++;
				}
				while (i < len)
				{
					if (buf[i] == 'B' && i + 3 < len && buf[i + 1] == 'E' && buf[i + 2] == 'G' && buf
						[i + 3] == 'I')
					{
						i += 6;
						if (buf[i] == 'D' && buf[i + 1] == 'S' && buf[i + 2] == 'A')
						{
							type = DSS;
						}
						else
						{
							if (buf[i] == 'R' && buf[i + 1] == 'S' && buf[i + 2] == 'A')
							{
								type = RSA;
							}
							else
							{
								if (buf[i] == 'S' && buf[i + 1] == 'S' && buf[i + 2] == 'H')
								{
									// FSecure
									type = UNKNOWN;
									keytype = FSECURE;
								}
								else
								{
									//System.err.println("invalid format: "+identity);
									throw new JSchException("invalid privatekey: " + identity);
								}
							}
						}
						i += 3;
						continue;
					}
					if (buf[i] == 'A' && i + 7 < len && buf[i + 1] == 'E' && buf[i + 2] == 'S' && buf
						[i + 3] == '-' && buf[i + 4] == '2' && buf[i + 5] == '5' && buf[i + 6] == '6' &&
						 buf[i + 7] == '-')
					{
						i += 8;
						if (Session.CheckCipher((string)JSch.GetConfig("aes256-cbc")))
						{
							c = Sharpen.Runtime.GetType((string)JSch.GetConfig("aes256-cbc"));
							cipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
							key = new byte[cipher.GetBlockSize()];
							iv = new byte[cipher.GetIVSize()];
						}
						else
						{
							throw new JSchException("privatekey: aes256-cbc is not available " + identity);
						}
						continue;
					}
					if (buf[i] == 'A' && i + 7 < len && buf[i + 1] == 'E' && buf[i + 2] == 'S' && buf
						[i + 3] == '-' && buf[i + 4] == '1' && buf[i + 5] == '9' && buf[i + 6] == '2' &&
						 buf[i + 7] == '-')
					{
						i += 8;
						if (Session.CheckCipher((string)JSch.GetConfig("aes192-cbc")))
						{
							c = Sharpen.Runtime.GetType((string)JSch.GetConfig("aes192-cbc"));
							cipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
							key = new byte[cipher.GetBlockSize()];
							iv = new byte[cipher.GetIVSize()];
						}
						else
						{
							throw new JSchException("privatekey: aes192-cbc is not available " + identity);
						}
						continue;
					}
					if (buf[i] == 'A' && i + 7 < len && buf[i + 1] == 'E' && buf[i + 2] == 'S' && buf
						[i + 3] == '-' && buf[i + 4] == '1' && buf[i + 5] == '2' && buf[i + 6] == '8' &&
						 buf[i + 7] == '-')
					{
						i += 8;
						if (Session.CheckCipher((string)JSch.GetConfig("aes128-cbc")))
						{
							c = Sharpen.Runtime.GetType((string)JSch.GetConfig("aes128-cbc"));
							cipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
							key = new byte[cipher.GetBlockSize()];
							iv = new byte[cipher.GetIVSize()];
						}
						else
						{
							throw new JSchException("privatekey: aes128-cbc is not available " + identity);
						}
						continue;
					}
					if (buf[i] == 'C' && i + 3 < len && buf[i + 1] == 'B' && buf[i + 2] == 'C' && buf
						[i + 3] == ',')
					{
						i += 4;
						for (int ii = 0; ii < iv.Length; ii++)
						{
							iv[ii] = unchecked((byte)(((A2b(buf[i++]) << 4) & unchecked((int)(0xf0))) + (A2b(
								buf[i++]) & unchecked((int)(0xf)))));
						}
						continue;
					}
					if (buf[i] == unchecked((int)(0x0d)) && i + 1 < len && buf[i + 1] == unchecked((int
						)(0x0a)))
					{
						i++;
						continue;
					}
					if (buf[i] == unchecked((int)(0x0a)) && i + 1 < len)
					{
						if (buf[i + 1] == unchecked((int)(0x0a)))
						{
							i += 2;
							break;
						}
						if (buf[i + 1] == unchecked((int)(0x0d)) && i + 2 < len && buf[i + 2] == unchecked(
							(int)(0x0a)))
						{
							i += 3;
							break;
						}
						bool inheader = false;
						for (int j = i + 1; j < len; j++)
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
							i++;
							encrypted = false;
							// no passphrase
							break;
						}
					}
					i++;
				}
				if (type == ERROR)
				{
					throw new JSchException("invalid privatekey: " + identity);
				}
				int start = i;
				while (i < len)
				{
					if (buf[i] == unchecked((int)(0x0a)))
					{
						bool xd = (buf[i - 1] == unchecked((int)(0x0d)));
						System.Array.Copy(buf, i + 1, buf, i - (xd ? 1 : 0), len - i - 1 - (xd ? 1 : 0));
						if (xd)
						{
							len--;
						}
						len--;
						continue;
					}
					if (buf[i] == '-')
					{
						break;
					}
					i++;
				}
				encoded_data = Util.FromBase64(buf, start, i - start);
				if (encoded_data.Length > 4 && encoded_data[0] == unchecked((byte)unchecked((int)
					(0x3f))) && encoded_data[1] == unchecked((byte)unchecked((int)(0x6f))) && encoded_data
					[2] == unchecked((byte)unchecked((int)(0xf9))) && encoded_data[3] == unchecked((
					byte)unchecked((int)(0xeb))))
				{
					// FSecure
					Buffer _buf = new Buffer(encoded_data);
					_buf.GetInt();
					// 0x3f6ff9be
					_buf.GetInt();
					byte[] _type = _buf.GetString();
					//System.err.println("type: "+new String(_type)); 
					byte[] _cipher = _buf.GetString();
					string cipherStr = Util.Byte2str(_cipher);
					//System.err.println("cipher: "+cipher); 
					if (cipherStr.Equals("3des-cbc"))
					{
						_buf.GetInt();
						byte[] foo = new byte[encoded_data.Length - _buf.GetOffSet()];
						_buf.GetByte(foo);
						encoded_data = foo;
						encrypted = true;
						throw new JSchException("unknown privatekey format: " + identity);
					}
					else
					{
						if (cipherStr.Equals("none"))
						{
							_buf.GetInt();
							//_buf.getInt();
							encrypted = false;
							byte[] foo = new byte[encoded_data.Length - _buf.GetOffSet()];
							_buf.GetByte(foo);
							encoded_data = foo;
						}
					}
				}
				if (pubkey == null)
				{
					return;
				}
				buf = pubkey;
				len = buf.Length;
				if (buf.Length > 4 && buf[0] == '-' && buf[1] == '-' && buf[2] == '-' && buf[3] ==
					 '-')
				{
					// FSecure's public key
					i = 0;
					do
					{
						i++;
					}
					while (len > i && buf[i] != unchecked((int)(0x0a)));
					if (len <= i)
					{
						return;
					}
					while (i < len)
					{
						if (buf[i] == unchecked((int)(0x0a)))
						{
							bool inheader = false;
							for (int j = i + 1; j < len; j++)
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
								i++;
								break;
							}
						}
						i++;
					}
					if (len <= i)
					{
						return;
					}
					start = i;
					while (i < len)
					{
						if (buf[i] == unchecked((int)(0x0a)))
						{
							System.Array.Copy(buf, i + 1, buf, i, len - i - 1);
							len--;
							continue;
						}
						if (buf[i] == '-')
						{
							break;
						}
						i++;
					}
					publickeyblob = Util.FromBase64(buf, start, i - start);
					if (type == UNKNOWN && publickeyblob.Length > 8)
					{
						if (publickeyblob[8] == 'd')
						{
							type = DSS;
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
				else
				{
					if (buf[0] != 's' || buf[1] != 's' || buf[2] != 'h' || buf[3] != '-')
					{
						return;
					}
					i = 0;
					while (i < len)
					{
						if (buf[i] == ' ')
						{
							break;
						}
						i++;
					}
					i++;
					if (i >= len)
					{
						return;
					}
					start = i;
					while (i < len)
					{
						if (buf[i] == ' ' || buf[i] == '\n')
						{
							break;
						}
						i++;
					}
					publickeyblob = Util.FromBase64(buf, start, i - start);
					if (publickeyblob.Length < 4 + 7)
					{
						// It must start with "ssh-XXX".
						if (JSch.GetLogger().IsEnabled(Logger.WARN))
						{
							JSch.GetLogger().Log(Logger.WARN, "failed to parse the public key");
						}
						publickeyblob = null;
					}
				}
			}
			catch (Exception e)
			{
				//System.err.println("IdentityFile: "+e);
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
		}

		public virtual string GetAlgName()
		{
			if (type == RSA)
			{
				return "ssh-rsa";
			}
			return "ssh-dss";
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual bool SetPassphrase(byte[] _passphrase)
		{
			try
			{
				if (encrypted)
				{
					if (_passphrase == null)
					{
						return false;
					}
					byte[] passphrase = _passphrase;
					int hsize = hash.GetBlockSize();
					byte[] hn = new byte[key.Length / hsize * hsize + (key.Length % hsize == 0 ? 0 : 
						hsize)];
					byte[] tmp = null;
					if (keytype == OPENSSH)
					{
						for (int index = 0; index + hsize <= hn.Length; )
						{
							if (tmp != null)
							{
								hash.Update(tmp, 0, tmp.Length);
							}
							hash.Update(passphrase, 0, passphrase.Length);
							hash.Update(iv, 0, iv.Length > 8 ? 8 : iv.Length);
							tmp = hash.Digest();
							System.Array.Copy(tmp, 0, hn, index, tmp.Length);
							index += tmp.Length;
						}
						System.Array.Copy(hn, 0, key, 0, key.Length);
					}
					else
					{
						if (keytype == FSECURE)
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
					Util.Bzero(passphrase);
				}
				if (Decrypt())
				{
					encrypted = false;
					return true;
				}
				P_array = Q_array = G_array = pub_array = prv_array = null;
				return false;
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
		}

		public virtual byte[] GetPublicKeyBlob()
		{
			if (publickeyblob != null)
			{
				return publickeyblob;
			}
			if (type == RSA)
			{
				return GetPublicKeyBlob_rsa();
			}
			return GetPublicKeyBlob_dss();
		}

		internal virtual byte[] GetPublicKeyBlob_rsa()
		{
			if (e_array == null)
			{
				return null;
			}
			Buffer buf = new Buffer("ssh-rsa".Length + 4 + e_array.Length + 4 + n_array.Length
				 + 4);
			buf.PutString(Util.Str2byte("ssh-rsa"));
			buf.PutString(e_array);
			buf.PutString(n_array);
			return buf.buffer;
		}

		internal virtual byte[] GetPublicKeyBlob_dss()
		{
			if (P_array == null)
			{
				return null;
			}
			Buffer buf = new Buffer("ssh-dss".Length + 4 + P_array.Length + 4 + Q_array.Length
				 + 4 + G_array.Length + 4 + pub_array.Length + 4);
			buf.PutString(Util.Str2byte("ssh-dss"));
			buf.PutString(P_array);
			buf.PutString(Q_array);
			buf.PutString(G_array);
			buf.PutString(pub_array);
			return buf.buffer;
		}

		public virtual byte[] GetSignature(byte[] data)
		{
			if (type == RSA)
			{
				return GetSignature_rsa(data);
			}
			return GetSignature_dss(data);
		}

		internal virtual byte[] GetSignature_rsa(byte[] data)
		{
			try
			{
				Type c = Sharpen.Runtime.GetType((string)JSch.GetConfig("signature.rsa"));
				NSch.SignatureRSA rsa = (NSch.SignatureRSA)(System.Activator.CreateInstance(c));
				rsa.Init();
				rsa.SetPrvKey(d_array, n_array, e_array);
				rsa.Update(data);
				byte[] sig = rsa.Sign();
				Buffer buf = new Buffer("ssh-rsa".Length + 4 + sig.Length + 4);
				buf.PutString(Util.Str2byte("ssh-rsa"));
				buf.PutString(sig);
				return buf.buffer;
			}
			catch (Exception)
			{
			}
			return null;
		}

		internal virtual byte[] GetSignature_dss(byte[] data)
		{
			try
			{
				Type c = Sharpen.Runtime.GetType((string)JSch.GetConfig("signature.dss"));
				NSch.SignatureDSA dsa = (NSch.SignatureDSA)(System.Activator.CreateInstance(c));
				dsa.Init();
				dsa.SetPrvKey(prv_array, P_array, Q_array, G_array);
				dsa.Update(data);
				byte[] sig = dsa.Sign();
				Buffer buf = new Buffer("ssh-dss".Length + 4 + sig.Length + 4);
				buf.PutString(Util.Str2byte("ssh-dss"));
				buf.PutString(sig);
				return buf.buffer;
			}
			catch (Exception)
			{
			}
			//System.err.println("e "+e);
			return null;
		}

		public virtual bool Decrypt()
		{
			if (type == RSA)
			{
				return Decrypt_rsa();
			}
			return Decrypt_dss();
		}

		internal virtual bool Decrypt_rsa()
		{
			byte[] p_array;
			byte[] q_array;
			byte[] dmp1_array;
			byte[] dmq1_array;
			byte[] iqmp_array;
			try
			{
				byte[] plain;
				if (encrypted)
				{
					if (keytype == OPENSSH)
					{
						cipher.Init(NSch.Cipher.DECRYPT_MODE, key, iv);
						plain = new byte[encoded_data.Length];
						cipher.Update(encoded_data, 0, encoded_data.Length, plain, 0);
					}
					else
					{
						if (keytype == FSECURE)
						{
							for (int i = 0; i < iv.Length; i++)
							{
								iv[i] = 0;
							}
							cipher.Init(NSch.Cipher.DECRYPT_MODE, key, iv);
							plain = new byte[encoded_data.Length];
							cipher.Update(encoded_data, 0, encoded_data.Length, plain, 0);
						}
						else
						{
							return false;
						}
					}
				}
				else
				{
					if (n_array != null)
					{
						return true;
					}
					plain = encoded_data;
				}
				if (keytype == FSECURE)
				{
					// FSecure   
					Buffer buf = new Buffer(plain);
					int foo = buf.GetInt();
					if (plain.Length != foo + 4)
					{
						return false;
					}
					e_array = buf.GetMPIntBits();
					d_array = buf.GetMPIntBits();
					n_array = buf.GetMPIntBits();
					byte[] u_array = buf.GetMPIntBits();
					p_array = buf.GetMPIntBits();
					q_array = buf.GetMPIntBits();
					return true;
				}
				int index = 0;
				int length = 0;
				if (plain[index] != unchecked((int)(0x30)))
				{
					return false;
				}
				index++;
				// SEQUENCE
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				if (plain[index] != unchecked((int)(0x02)))
				{
					return false;
				}
				index++;
				// INTEGER
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				index += length;
				//System.err.println("int: len="+length);
				//System.err.print(Integer.toHexString(plain[index-1]&0xff)+":");
				//System.err.println("");
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				n_array = new byte[length];
				System.Array.Copy(plain, index, n_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				e_array = new byte[length];
				System.Array.Copy(plain, index, e_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				d_array = new byte[length];
				System.Array.Copy(plain, index, d_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				p_array = new byte[length];
				System.Array.Copy(plain, index, p_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				q_array = new byte[length];
				System.Array.Copy(plain, index, q_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				dmp1_array = new byte[length];
				System.Array.Copy(plain, index, dmp1_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				dmq1_array = new byte[length];
				System.Array.Copy(plain, index, dmq1_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				iqmp_array = new byte[length];
				System.Array.Copy(plain, index, iqmp_array, 0, length);
				index += length;
			}
			catch (Exception)
			{
				//System.err.println(e);
				return false;
			}
			return true;
		}

		internal virtual bool Decrypt_dss()
		{
			try
			{
				byte[] plain;
				if (encrypted)
				{
					if (keytype == OPENSSH)
					{
						cipher.Init(NSch.Cipher.DECRYPT_MODE, key, iv);
						plain = new byte[encoded_data.Length];
						cipher.Update(encoded_data, 0, encoded_data.Length, plain, 0);
					}
					else
					{
						if (keytype == FSECURE)
						{
							for (int i = 0; i < iv.Length; i++)
							{
								iv[i] = 0;
							}
							cipher.Init(NSch.Cipher.DECRYPT_MODE, key, iv);
							plain = new byte[encoded_data.Length];
							cipher.Update(encoded_data, 0, encoded_data.Length, plain, 0);
						}
						else
						{
							return false;
						}
					}
				}
				else
				{
					if (P_array != null)
					{
						return true;
					}
					plain = encoded_data;
				}
				if (keytype == FSECURE)
				{
					// FSecure   
					Buffer buf = new Buffer(plain);
					int foo = buf.GetInt();
					if (plain.Length != foo + 4)
					{
						return false;
					}
					P_array = buf.GetMPIntBits();
					G_array = buf.GetMPIntBits();
					Q_array = buf.GetMPIntBits();
					pub_array = buf.GetMPIntBits();
					prv_array = buf.GetMPIntBits();
					return true;
				}
				int index = 0;
				int length = 0;
				if (plain[index] != unchecked((int)(0x30)))
				{
					return false;
				}
				index++;
				// SEQUENCE
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				if (plain[index] != unchecked((int)(0x02)))
				{
					return false;
				}
				index++;
				// INTEGER
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				P_array = new byte[length];
				System.Array.Copy(plain, index, P_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				Q_array = new byte[length];
				System.Array.Copy(plain, index, Q_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				G_array = new byte[length];
				System.Array.Copy(plain, index, G_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				pub_array = new byte[length];
				System.Array.Copy(plain, index, pub_array, 0, length);
				index += length;
				index++;
				length = plain[index++] & unchecked((int)(0xff));
				if ((length & unchecked((int)(0x80))) != 0)
				{
					int foo = length & unchecked((int)(0x7f));
					length = 0;
					while (foo-- > 0)
					{
						length = (length << 8) + (plain[index++] & unchecked((int)(0xff)));
					}
				}
				prv_array = new byte[length];
				System.Array.Copy(plain, index, prv_array, 0, length);
				index += length;
			}
			catch (Exception)
			{
				//System.err.println(e);
				//e.printStackTrace();
				return false;
			}
			return true;
		}

		public virtual bool IsEncrypted()
		{
			return encrypted;
		}

		public virtual string GetName()
		{
			return identity;
		}

		private byte A2b(byte c)
		{
			if ('0' <= c && ((sbyte)c) <= '9')
			{
				return unchecked((byte)(c - '0'));
			}
			if ('a' <= c && ((sbyte)c) <= 'z')
			{
				return unchecked((byte)(c - 'a' + 10));
			}
			return unchecked((byte)(c - 'A' + 10));
		}

		public override bool Equals(object o)
		{
			if (!(o is NSch.IdentityFile))
			{
				return base.Equals(o);
			}
			NSch.IdentityFile foo = (NSch.IdentityFile)o;
			return GetName().Equals(foo.GetName());
		}

		public virtual void Clear()
		{
			Util.Bzero(encoded_data);
			Util.Bzero(prv_array);
			Util.Bzero(d_array);
			Util.Bzero(key);
			Util.Bzero(iv);
		}

		~IdentityFile()
		{
			Clear();
		}
	}
}
