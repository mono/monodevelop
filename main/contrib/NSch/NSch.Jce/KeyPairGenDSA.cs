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

using Sharpen;

namespace NSch.Jce
{
	public class KeyPairGenDSA : NSch.KeyPairGenDSA
	{
		internal byte[] x;

		internal byte[] y;

		internal byte[] p;

		internal byte[] q;

		internal byte[] g;

		// private
		// public
		/// <exception cref="System.Exception"></exception>
		public virtual void Init(int key_size)
		{
			KeyPairGenerator keyGen = KeyPairGenerator.GetInstance("DSA");
			keyGen.Initialize(key_size, new SecureRandom());
			Sharpen.KeyPair pair = keyGen.GenerateKeyPair();
			PublicKey pubKey = pair.GetPublic();
			PrivateKey prvKey = pair.GetPrivate();
			x = ((DSAPrivateKey)prvKey).GetX().GetBytes();
			y = ((DSAPublicKey)pubKey).GetY().GetBytes();
			DSAParams @params = ((DSAKey)prvKey).GetParams();
			p = @params.GetP().GetBytes();
			q = @params.GetQ().GetBytes();
			g = @params.GetG().GetBytes();
		}

		public virtual byte[] GetX()
		{
			return x;
		}

		public virtual byte[] GetY()
		{
			return y;
		}

		public virtual byte[] GetP()
		{
			return p;
		}

		public virtual byte[] GetQ()
		{
			return q;
		}

		public virtual byte[] GetG()
		{
			return g;
		}
	}
}
