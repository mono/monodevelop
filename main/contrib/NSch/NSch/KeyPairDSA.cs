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
	public class KeyPairDSA : NSch.KeyPair
	{
		private byte[] P_array;

		private byte[] Q_array;

		private byte[] G_array;

		private byte[] pub_array;

		private byte[] prv_array;

		private int key_size = 1024;

		public KeyPairDSA(JSch jsch) : base(jsch)
		{
		}

		//private int key_size=0;
		/// <exception cref="NSch.JSchException"></exception>
		internal override void Generate(int key_size)
		{
			this.key_size = key_size;
			try
			{
				Type c = Sharpen.Runtime.GetType(JSch.GetConfig("keypairgen.dsa"));
				NSch.KeyPairGenDSA keypairgen = (NSch.KeyPairGenDSA)(System.Activator.CreateInstance
					(c));
				keypairgen.Init(key_size);
				P_array = keypairgen.GetP();
				Q_array = keypairgen.GetQ();
				G_array = keypairgen.GetG();
				pub_array = keypairgen.GetY();
				prv_array = keypairgen.GetX();
				keypairgen = null;
			}
			catch (Exception e)
			{
				//System.err.println("KeyPairDSA: "+e); 
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
		}

		private static readonly byte[] begin = Util.Str2byte("-----BEGIN DSA PRIVATE KEY-----"
			);

		private static readonly byte[] end = Util.Str2byte("-----END DSA PRIVATE KEY-----"
			);

		internal override byte[] GetBegin()
		{
			return begin;
		}

		internal override byte[] GetEnd()
		{
			return end;
		}

		internal override byte[] GetPrivateKey()
		{
			int content = 1 + CountLength(1) + 1 + 1 + CountLength(P_array.Length) + P_array.
				Length + 1 + CountLength(Q_array.Length) + Q_array.Length + 1 + CountLength(G_array
				.Length) + G_array.Length + 1 + CountLength(pub_array.Length) + pub_array.Length
				 + 1 + CountLength(prv_array.Length) + prv_array.Length;
			// INTEGER
			// INTEGER  P
			// INTEGER  Q
			// INTEGER  G
			// INTEGER  pub
			// INTEGER  prv
			int total = 1 + CountLength(content) + content;
			// SEQUENCE
			byte[] plain = new byte[total];
			int index = 0;
			index = WriteSEQUENCE(plain, index, content);
			index = WriteINTEGER(plain, index, new byte[1]);
			// 0
			index = WriteINTEGER(plain, index, P_array);
			index = WriteINTEGER(plain, index, Q_array);
			index = WriteINTEGER(plain, index, G_array);
			index = WriteINTEGER(plain, index, pub_array);
			index = WriteINTEGER(plain, index, prv_array);
			return plain;
		}

		internal override bool Parse(byte[] plain)
		{
			try
			{
				if (vendor == VENDOR_FSECURE)
				{
					if (plain[0] != unchecked((int)(0x30)))
					{
						// FSecure
						Buffer buf = new Buffer(plain);
						buf.GetInt();
						P_array = buf.GetMPIntBits();
						G_array = buf.GetMPIntBits();
						Q_array = buf.GetMPIntBits();
						pub_array = buf.GetMPIntBits();
						prv_array = buf.GetMPIntBits();
						return true;
					}
					return false;
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

		public override byte[] GetPublicKeyBlob()
		{
			byte[] foo = base.GetPublicKeyBlob();
			if (foo != null)
			{
				return foo;
			}
			if (P_array == null)
			{
				return null;
			}
			Buffer buf = new Buffer(sshdss.Length + 4 + P_array.Length + 4 + Q_array.Length +
				 4 + G_array.Length + 4 + pub_array.Length + 4);
			buf.PutString(sshdss);
			buf.PutString(P_array);
			buf.PutString(Q_array);
			buf.PutString(G_array);
			buf.PutString(pub_array);
			return buf.buffer;
		}

		private static readonly byte[] sshdss = Util.Str2byte("ssh-dss");

		internal override byte[] GetKeyTypeName()
		{
			return sshdss;
		}

		public override int GetKeyType()
		{
			return DSA;
		}

		internal override int GetKeySize()
		{
			return key_size;
		}

		public override void Dispose()
		{
			base.Dispose();
			Util.Bzero(prv_array);
		}
	}
}
