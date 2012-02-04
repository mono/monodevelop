// 
// Signature.cs
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
using System.IO;
using Mono.Security.Cryptography;

namespace Sharpen
{
	public abstract class Signature
	{
		public Signature ()
		{
		}
		
		public static Signature GetInstance (string id)
		{
			switch (id) {
			case "SHA1withDSA": return new SHA1withDSASignature ();
			case "SHA1withRSA": return new SHA1withRSASignature ();
			}
			throw new NotSupportedException ();
		}
		
		public abstract byte[] Sign ();
		
		public abstract void Update (byte[] data);
		
		public abstract void InitSign (PrivateKey key);
		
		public abstract void InitVerify (PublicKey key);
		
		public abstract bool Verify (byte[] data);
	}
	
	class SHA1withRSASignature: Signature
	{
		RSAManaged rsa = new RSAManaged ();
		MemoryStream ms = new MemoryStream ();
		
		public override byte[] Sign ()
		{
			try {
				ms.Position = 0;
				HashAlgorithm hash = HashAlgorithm.Create ("SHA1");
				byte[] toBeSigned = hash.ComputeHash (ms);
				ms = new MemoryStream ();
				return PKCS1.Sign_v15 (rsa, hash, toBeSigned);
				
//				byte[] res = rsa.SignData (ms, "sha1");
//				return res;
			} catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			}
		}
		
		public override void Update (byte[] data)
		{
			ms.Write (data, 0, data.Length);
		}
		
		public override void InitSign (PrivateKey key)
		{
			try {
				rsa.ImportParameters (((RSAPrivateKey)key).Parameters);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			}
		}
		
		public override void InitVerify (PublicKey key)
		{
			rsa.ImportParameters (((RSAPublicKey)key).Parameters);
		}
		
		public override bool Verify (byte[] signature)
		{
			HashAlgorithm hash = HashAlgorithm.Create ("SHA1");
			byte[] toBeVerified = hash.ComputeHash (ms.ToArray ());
			return PKCS1.Verify_v15 (rsa, hash, toBeVerified, signature);
//			return rsa.VerifyData (ms.ToArray (), "SHA1", signature);
		}
		
		static byte[] CB (sbyte[] si)
		{
			byte[] s = new byte [si.Length];
			for (int n=0; n<si.Length; n++)
				s[n] = (byte)si[n];
			return s;
		}
	}
	
	class SHA1withDSASignature : Signature
	{
		DSACryptoServiceProvider sa = new DSACryptoServiceProvider ();
		MemoryStream ms = new MemoryStream ();
		
		public override byte[] Sign ()
		{
			ms.Position = 0;
			byte[] res = sa.SignData (ms);
			ms = new MemoryStream ();
			return res;
		}
		
		public override void Update (byte[] data)
		{
			ms.Write (data, 0, data.Length);
		}
		
		public override void InitSign (PrivateKey key)
		{
			sa.ImportParameters (((DSAPrivateKey)key).Parameters);
		}
		
		public override void InitVerify (PublicKey key)
		{
			sa.ImportParameters (((DSAPublicKey)key).Parameters);
		}
		
		public override bool Verify (byte[] signature)
		{
			return sa.VerifyData (ms.ToArray (), signature);
		}

	}
}

