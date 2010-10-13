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

using Mono.Math;
using Sharpen;

namespace NSch.Jce
{
	public class SignatureRSA : NSch.SignatureRSA
	{
		internal Signature signature;

		internal KeyFactory keyFactory;

		/// <exception cref="System.Exception"></exception>
		public virtual void Init()
		{
			signature = Signature.GetInstance("SHA1withRSA");
			keyFactory = KeyFactory.GetInstance("RSA");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void SetPubKey(byte[] e, byte[] n)
		{
			RSAPublicKeySpec rsaPubKeySpec = new RSAPublicKeySpec(new BigInteger(n), new BigInteger
				(e));
			PublicKey pubKey = keyFactory.GeneratePublic(rsaPubKeySpec);
			signature.InitVerify(pubKey);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void SetPrvKey(byte[] d, byte[] n)
		{
			RSAPrivateKeySpec rsaPrivKeySpec = new RSAPrivateKeySpec(new BigInteger(n), new BigInteger
				(d));
			PrivateKey prvKey = keyFactory.GeneratePrivate(rsaPrivKeySpec);
			signature.InitSign(prvKey);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual byte[] Sign()
		{
			byte[] sig = signature.Sign();
			return sig;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Update(byte[] foo)
		{
			signature.Update(foo);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual bool Verify(byte[] sig)
		{
			int i = 0;
			int j = 0;
			byte[] tmp;
			if (sig[0] == 0 && sig[1] == 0 && sig[2] == 0)
			{
				j = ((sig[i++] << 24) & unchecked((int)(0xff000000))) | ((sig[i++] << 16) & unchecked(
					(int)(0x00ff0000))) | ((sig[i++] << 8) & unchecked((int)(0x0000ff00))) | ((sig[i
					++]) & unchecked((int)(0x000000ff)));
				i += j;
				j = ((sig[i++] << 24) & unchecked((int)(0xff000000))) | ((sig[i++] << 16) & unchecked(
					(int)(0x00ff0000))) | ((sig[i++] << 8) & unchecked((int)(0x0000ff00))) | ((sig[i
					++]) & unchecked((int)(0x000000ff)));
				tmp = new byte[j];
				System.Array.Copy(sig, i, tmp, 0, j);
				sig = tmp;
			}
			//System.err.println("j="+j+" "+Integer.toHexString(sig[0]&0xff));
			return signature.Verify(sig);
		}
	}
}
