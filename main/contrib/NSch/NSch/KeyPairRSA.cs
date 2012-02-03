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
	public class KeyPairRSA : NSch.KeyPair
	{
		private byte[] prv_array;

		private byte[] pub_array;

		private byte[] n_array;

		private byte[] p_array;

		private byte[] q_array;

		private byte[] ep_array;

		private byte[] eq_array;

		private byte[] c_array;

		private int key_size = 1024;

		public KeyPairRSA(JSch jsch) : base(jsch)
		{
		}

		// prime p
		// prime q
		// prime exponent p
		// prime exponent q
		// coefficient
		//private int key_size=0;
		/// <exception cref="NSch.JSchException"></exception>
		internal override void Generate(int key_size)
		{
			this.key_size = key_size;
			try
			{
				Type c = Sharpen.Runtime.GetType(JSch.GetConfig("keypairgen.rsa"));
				NSch.KeyPairGenRSA keypairgen = (NSch.KeyPairGenRSA)(System.Activator.CreateInstance
					(c));
				keypairgen.Init(key_size);
				pub_array = keypairgen.GetE();
				prv_array = keypairgen.GetD();
				n_array = keypairgen.GetN();
				p_array = keypairgen.GetP();
				q_array = keypairgen.GetQ();
				ep_array = keypairgen.GetEP();
				eq_array = keypairgen.GetEQ();
				c_array = keypairgen.GetC();
				keypairgen = null;
			}
			catch (Exception e)
			{
				//System.err.println("KeyPairRSA: "+e); 
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
		}

		private static readonly byte[] begin = Util.Str2byte("-----BEGIN RSA PRIVATE KEY-----"
			);

		private static readonly byte[] end = Util.Str2byte("-----END RSA PRIVATE KEY-----"
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
			int content = 1 + CountLength(1) + 1 + 1 + CountLength(n_array.Length) + n_array.
				Length + 1 + CountLength(pub_array.Length) + pub_array.Length + 1 + CountLength(
				prv_array.Length) + prv_array.Length + 1 + CountLength(p_array.Length) + p_array
				.Length + 1 + CountLength(q_array.Length) + q_array.Length + 1 + CountLength(ep_array
				.Length) + ep_array.Length + 1 + CountLength(eq_array.Length) + eq_array.Length 
				+ 1 + CountLength(c_array.Length) + c_array.Length;
			// INTEGER
			// INTEGER  N
			// INTEGER  pub
			// INTEGER  prv
			// INTEGER  p
			// INTEGER  q
			// INTEGER  ep
			// INTEGER  eq
			// INTEGER  c
			int total = 1 + CountLength(content) + content;
			// SEQUENCE
			byte[] plain = new byte[total];
			int index = 0;
			index = WriteSEQUENCE(plain, index, content);
			index = WriteINTEGER(plain, index, new byte[1]);
			// 0
			index = WriteINTEGER(plain, index, n_array);
			index = WriteINTEGER(plain, index, pub_array);
			index = WriteINTEGER(plain, index, prv_array);
			index = WriteINTEGER(plain, index, p_array);
			index = WriteINTEGER(plain, index, q_array);
			index = WriteINTEGER(plain, index, ep_array);
			index = WriteINTEGER(plain, index, eq_array);
			index = WriteINTEGER(plain, index, c_array);
			return plain;
		}

		internal override bool Parse(byte[] plain)
		{
			try
			{
				int index = 0;
				int length = 0;
				if (vendor == VENDOR_FSECURE)
				{
					if (plain[index] != unchecked((int)(0x30)))
					{
						// FSecure
						Buffer buf = new Buffer(plain);
						pub_array = buf.GetMPIntBits();
						prv_array = buf.GetMPIntBits();
						n_array = buf.GetMPIntBits();
						byte[] u_array = buf.GetMPIntBits();
						p_array = buf.GetMPIntBits();
						q_array = buf.GetMPIntBits();
						return true;
					}
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
				ep_array = new byte[length];
				System.Array.Copy(plain, index, ep_array, 0, length);
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
				eq_array = new byte[length];
				System.Array.Copy(plain, index, eq_array, 0, length);
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
				c_array = new byte[length];
				System.Array.Copy(plain, index, c_array, 0, length);
				index += length;
			}
			catch (Exception)
			{
				//System.err.println(e);
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
			if (pub_array == null)
			{
				return null;
			}
			Buffer buf = new Buffer(sshrsa.Length + 4 + pub_array.Length + 4 + n_array.Length
				 + 4);
			buf.PutString(sshrsa);
			buf.PutString(pub_array);
			buf.PutString(n_array);
			return buf.buffer;
		}

		private static readonly byte[] sshrsa = Util.Str2byte("ssh-rsa");

		internal override byte[] GetKeyTypeName()
		{
			return sshrsa;
		}

		public override int GetKeyType()
		{
			return RSA;
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
