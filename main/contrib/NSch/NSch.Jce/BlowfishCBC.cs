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
using NSch.Jce;
using Sharpen;

namespace NSch.Jce
{
	public class BlowfishCBC : NSch.Cipher
	{
		private const int ivsize = 8;

		private const int bsize = 16;

		private Sharpen.Cipher cipher;

		public override int GetIVSize()
		{
			return ivsize;
		}

		public override int GetBlockSize()
		{
			return bsize;
		}

		/// <exception cref="System.Exception"></exception>
		public override void Init(int mode, byte[] key, byte[] iv)
		{
			string pad = "NoPadding";
			//  if(padding) pad="PKCS5Padding";
			byte[] tmp;
			if (iv.Length > ivsize)
			{
				tmp = new byte[ivsize];
				System.Array.Copy(iv, 0, tmp, 0, tmp.Length);
				iv = tmp;
			}
			if (key.Length > bsize)
			{
				tmp = new byte[bsize];
				System.Array.Copy(key, 0, tmp, 0, tmp.Length);
				key = tmp;
			}
			try
			{
				SecretKeySpec skeySpec = new SecretKeySpec(key, "Blowfish");
				cipher = Sharpen.Cipher.GetInstance("Blowfish/CBC/" + pad);
				cipher.Init((mode == ENCRYPT_MODE ? Sharpen.Cipher.ENCRYPT_MODE : Sharpen.Cipher.
					DECRYPT_MODE), skeySpec, new IvParameterSpec(iv));
			}
			catch (Exception e)
			{
				throw;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void Update(byte[] foo, int s1, int len, byte[] bar, int s2)
		{
			cipher.Update(foo, s1, len, bar, s2);
		}

		public override bool IsCBC()
		{
			return true;
		}
	}
}
