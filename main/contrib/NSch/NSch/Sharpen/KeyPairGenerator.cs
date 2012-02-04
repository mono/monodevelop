// 
// KeyPairGenerator.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Security.Cryptography;
using Mono.Security.Cryptography;
using Mono.Math;

namespace Sharpen
{
	public abstract class KeyPairGenerator
	{
		public KeyPairGenerator ()
		{
		}
		
		public static KeyPairGenerator GetInstance (string name)
		{
			switch (name.ToUpper ()) {
			case "DH": return new DHKeyPairGenerator ();
			case "DSA": return new DSAKeyPairGenerator ();
			case "RSA": return new RSAKeyPairGenerator ();
			}
			throw new NotSupportedException ();
		}
		
		public virtual void Initialize (AlgorithmParameterSpec pars)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void Initialize (int keySize, SecureRandom random)
		{
			throw new NotSupportedException ();
		}
		
		public abstract KeyPair GenerateKeyPair ();
	}
	
	class DSAKeyPairGenerator: KeyPairGenerator
	{
		int keySize = 512;
		
		public DSAKeyPairGenerator ()
		{
		}
		
		public override void Initialize (int keySize, SecureRandom random)
		{
			this.keySize = keySize;
		}
		
		public override KeyPair GenerateKeyPair ()
		{
			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider (keySize);
			DSAParameters dsaPars = dsa.ExportParameters (true);
			return new KeyPair (new DSAPrivateKey (dsaPars), new DSAPublicKey (dsaPars));
		}
	}
	
	class RSAKeyPairGenerator: KeyPairGenerator
	{
		int keySize = 512;
		
		public override void Initialize (int keySize, SecureRandom random)
		{
			this.keySize = keySize;
		}
		
		public override KeyPair GenerateKeyPair ()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider (keySize);
			RSAParameters dsaPars = rsa.ExportParameters (true);
			return new KeyPair (new RSAPrivateKey (dsaPars), new RSAPublicKey (dsaPars));
		}
	}
	
	class DHKeyPairGenerator: KeyPairGenerator
	{
		DHParameterSpec pspec;
		
		public override void Initialize (AlgorithmParameterSpec pars)
		{
			pspec = (DHParameterSpec) pars;
		}
		
		public override KeyPair GenerateKeyPair ()
		{
			DiffieHellmanManaged dh = new DiffieHellmanManaged (pspec.P.GetBytes (), pspec.G.GetBytes (), 0);
			DHParameters dhpars = dh.ExportParameters (true);
			BigInteger y = new BigInteger (dh.CreateKeyExchange ());
			return new KeyPair (new DHPrivateKey (dhpars), new DHPublicKey (y));
		}
	}
}

