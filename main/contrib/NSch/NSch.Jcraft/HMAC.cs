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
using NSch.Jcraft;
using Sharpen;

namespace NSch.Jcraft
{
	public class HMAC
	{
		private const int B = 64;

		private byte[] k_ipad = null;

		private byte[] k_opad = null;

		private MessageDigest md = null;

		private int bsize = 0;

		protected internal virtual void SetH(MessageDigest md)
		{
			this.md = md;
			bsize = md.GetDigestLength();
		}

		public virtual int GetBlockSize()
		{
			return bsize;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Init(byte[] key)
		{
			if (key.Length > bsize)
			{
				byte[] tmp = new byte[bsize];
				System.Array.Copy(key, 0, tmp, 0, bsize);
				key = tmp;
			}
			if (key.Length > B)
			{
				md.Update(key, 0, key.Length);
				key = md.Digest();
			}
			k_ipad = new byte[B];
			System.Array.Copy(key, 0, k_ipad, 0, key.Length);
			k_opad = new byte[B];
			System.Array.Copy(key, 0, k_opad, 0, key.Length);
			for (int i = 0; i < B; i++)
			{
				k_ipad[i] ^= unchecked((byte)unchecked((int)(0x36)));
				k_opad[i] ^= unchecked((byte)unchecked((int)(0x5c)));
			}
			md.Update(k_ipad, 0, B);
		}

		private readonly byte[] tmp = new byte[4];

		public virtual void Update(int i)
		{
			tmp[0] = unchecked((byte)((int)(((uint)i) >> 24)));
			tmp[1] = unchecked((byte)((int)(((uint)i) >> 16)));
			tmp[2] = unchecked((byte)((int)(((uint)i) >> 8)));
			tmp[3] = unchecked((byte)i);
			Update(tmp, 0, 4);
		}

		public virtual void Update(byte[] foo, int s, int l)
		{
			md.Update(foo, s, l);
		}

		public virtual void DoFinal(byte[] buf, int offset)
		{
			byte[] result = md.Digest();
			md.Update(k_opad, 0, B);
			md.Update(result, 0, bsize);
			try
			{
				md.Digest(buf, offset, bsize);
			}
			catch (Exception)
			{
			}
			md.Update(k_ipad, 0, B);
		}
	}
}
