// 
// KeyAgreement.cs
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
using Mono.Security.Cryptography;

namespace Sharpen
{
	public abstract class KeyAgreement
	{
		public static KeyAgreement GetInstance (string id)
		{
			if (id.ToUpper () == "DH")
				return new DHKeyAgreement ();
			throw new NotSupportedException ();
		}
		
		public abstract void Init (Key key);
		
		public abstract Key DoPhase (Key key, bool lastPhase);
		
		public abstract byte[] GenerateSecret ();
	}
	
	class DHKeyAgreement: KeyAgreement
	{
		DiffieHellmanManaged dh;
		DHPublicKey pubk;
		
		public override void Init (Key key)
		{
			DHPrivateKey pk = (DHPrivateKey) key;
			dh = new DiffieHellmanManaged ();
			dh.ImportParameters (pk.Parameters);
		}
		
		
		public override Key DoPhase (Key key, bool lastPhase)
		{
			pubk = (DHPublicKey) key;
			return null;
		}
		
		
		public override byte[] GenerateSecret ()
		{
			return dh.DecryptKeyExchange (pubk.GetY ().GetBytes ());
		}
	}
}

