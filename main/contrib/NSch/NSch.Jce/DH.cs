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
	public class DH : NSch.DH
	{
		internal BigInteger p;

		internal BigInteger g;

		internal BigInteger e;

		internal byte[] e_array;

		internal BigInteger f;

		internal BigInteger K;

		internal byte[] K_array;

		private KeyPairGenerator myKpairGen;

		private KeyAgreement myKeyAgree;

		// my public key
		// your public key
		// shared secret key
		/// <exception cref="System.Exception"></exception>
		public virtual void Init()
		{
			myKpairGen = KeyPairGenerator.GetInstance("DH");
			//    myKpairGen=KeyPairGenerator.getInstance("DiffieHellman");
			myKeyAgree = KeyAgreement.GetInstance("DH");
		}

		//    myKeyAgree=KeyAgreement.getInstance("DiffieHellman");
		/// <exception cref="System.Exception"></exception>
		public virtual byte[] GetE()
		{
			if (e == null)
			{
				DHParameterSpec dhSkipParamSpec = new DHParameterSpec(p, g);
				myKpairGen.Initialize(dhSkipParamSpec);
				Sharpen.KeyPair myKpair = myKpairGen.GenerateKeyPair();
				myKeyAgree.Init(myKpair.GetPrivate());
				//    BigInteger x=((javax.crypto.interfaces.DHPrivateKey)(myKpair.getPrivate())).getX();
				//byte[] myPubKeyEnc = myKpair.GetPublic().GetEncoded();
				e = ((DHPublicKey)(myKpair.GetPublic())).GetY();
				e_array = e.GetBytes();
			}
			return e_array;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual byte[] GetK()
		{
			if (K == null)
			{
				KeyFactory myKeyFac = KeyFactory.GetInstance("DH");
				DHPublicKeySpec keySpec = new DHPublicKeySpec(f, p, g);
				PublicKey yourPubKey = myKeyFac.GeneratePublic(keySpec);
				myKeyAgree.DoPhase(yourPubKey, true);
				byte[] mySharedSecret = myKeyAgree.GenerateSecret();
				K = new BigInteger(mySharedSecret);
				K_array = K.GetBytes();
				//System.err.println("K.signum(): "+K.signum()+
				//		   " "+Integer.toHexString(mySharedSecret[0]&0xff)+
				//		   " "+Integer.toHexString(K_array[0]&0xff));
				K_array = mySharedSecret;
			}
			return K_array;
		}

		public virtual void SetP(byte[] p)
		{
			SetP(new BigInteger(p));
		}

		public virtual void SetG(byte[] g)
		{
			SetG(new BigInteger(g));
		}

		public virtual void SetF(byte[] f)
		{
			SetF(new BigInteger(f));
		}

		internal virtual void SetP(BigInteger p)
		{
			this.p = p;
		}

		internal virtual void SetG(BigInteger g)
		{
			this.g = g;
		}

		internal virtual void SetF(BigInteger f)
		{
			this.f = f;
		}
	}
}
