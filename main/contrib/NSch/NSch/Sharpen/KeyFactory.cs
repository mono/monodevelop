// 
// KeyFactory.cs
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
	public abstract class KeyFactory
	{
		public KeyFactory ()
		{
		}
		
		public static KeyFactory GetInstance (string id)
		{
			switch (id.ToUpper ()) {
			case "DSA": return new DSAKeyFactory ();
			case "DH": return new DHKeyFactory ();
			case "RSA": return new RSAKeyFactory ();
			}
			throw new NotSupportedException ();
		}
		
		public abstract PublicKey GeneratePublic (KeySpec key);
		
		public abstract PrivateKey GeneratePrivate (KeySpec key);
	}
	
	class DSAKeyFactory: KeyFactory
	{
		public override PublicKey GeneratePublic (KeySpec key)
		{
			DSAPublicKeySpec spec = (DSAPublicKeySpec) key;
			DSAParameters dsp = new DSAParameters ();
			dsp.G = spec.GetG ().GetBytes ();
			dsp.P = spec.GetP ().GetBytes ();
			dsp.Q = spec.GetQ ().GetBytes ();
			dsp.Y = spec.GetY ().GetBytes ();
			return new DSAPublicKey (dsp);
		}
		
		public override PrivateKey GeneratePrivate (KeySpec key)
		{
			DSAPrivateKeySpec spec = (DSAPrivateKeySpec) key;
			DSAParameters dsp = new DSAParameters ();
			dsp.G = spec.GetG ().GetBytes ();
			dsp.P = spec.GetP ().GetBytes ();
			dsp.Q = spec.GetQ ().GetBytes ();
			dsp.X = spec.GetX ().GetBytes ();
			return new DSAPrivateKey (dsp);
		}
	}
	
	class DHKeyFactory: KeyFactory
	{
		public override PublicKey GeneratePublic (KeySpec key)
		{
			DHPublicKeySpec spec = (DHPublicKeySpec) key;
			return new DHPublicKey (spec.Y);
		}
		
		public override PrivateKey GeneratePrivate (KeySpec key)
		{
			throw new NotSupportedException ();
		}
	}
	
	class RSAKeyFactory: KeyFactory
	{
		public override PublicKey GeneratePublic (KeySpec key)
		{
			RSAPublicKeySpec spec = (RSAPublicKeySpec) key;
			RSAParameters dparams = new RSAParameters ();
			dparams.Modulus = spec.GetModulus ().GetBytes ();
			dparams.Exponent = spec.GetPublicExponent ().GetBytes ();
			return new RSAPublicKey (dparams);
		}
		
		public override PrivateKey GeneratePrivate (KeySpec key)
		{
			RSAPrivateKeySpec spec = (RSAPrivateKeySpec) key;
			RSAParameters dparams = new RSAParameters ();
			dparams.Modulus = spec.GetModulus ().GetBytes ();
			dparams.D = spec.GetPrivateExponent ().GetBytes ();
			dparams.Exponent = spec.GetPublicExponent ().GetBytes ();
			return new RSAPrivateKey (dparams);
		}
	}
}

