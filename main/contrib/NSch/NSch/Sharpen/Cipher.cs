// 
// Cipher.cs
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
using System.Security.Cryptography;

namespace Sharpen
{
	public class Cipher
	{
		public static int ENCRYPT_MODE = 0;
		public static int DECRYPT_MODE = 1;
		
		public virtual void Init (int mode, Key keyspec)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void Init (int mode, Key keyspec, IvParameterSpec spec)
		{
			throw new NotSupportedException ();
		}
		
		public static Cipher GetInstance (string name)
		{
			switch (name) {
			case "RC4": return new R4Cipher ();
			case "AES/CBC/NoPadding": return new AesCipher (CipherMode.CBC);
			case "AES/CTR/NoPadding": throw new NotSupportedException ();
			case "Blowfish/CBC/NoPadding": return new BlowfishCipher (CipherMode.CBC);
			case "DESede/CBC/NoPadding": return new DESedeCipher (CipherMode.CBC);
			case "DESede/CTR/NoPadding": throw new NotSupportedException ();
			}
			throw new NotSupportedException ();
		}
		
		public virtual void Update(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset)
		{
			throw new NotSupportedException ();
		}
	}
	
	class R4Cipher: Cipher
	{
		RC4 rc4;
		ICryptoTransform transformer;
		
		public R4Cipher()
		{
			rc4 = new ARC4Managed ();
		}
		
		public override void Init (int mode, Key keyspec)
		{
			SecretKeySpec key = (SecretKeySpec)keyspec;
			if (mode == Cipher.ENCRYPT_MODE)
				transformer = rc4.CreateEncryptor (key.Key, new byte[0]);
			else
				transformer = rc4.CreateDecryptor (key.Key, new byte[0]);
		}
		
		public override void Update (byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset)
		{
			transformer.TransformBlock (input, inputOffset, inputLen, output, outputOffset);
		}
	}
	
	class AesCipher: Cipher
	{
		Aes encryptor;
		ICryptoTransform transformer;

		public AesCipher (CipherMode mode)
		{
			encryptor = Aes.Create ();
			encryptor.Mode = mode;
			encryptor.Padding = PaddingMode.None;
		}

		public override void Init (int mode, Key keyspec, IvParameterSpec spec)
		{
			SecretKeySpec key = (SecretKeySpec)keyspec;
			if (mode == Cipher.ENCRYPT_MODE)
				transformer = encryptor.CreateEncryptor (key.Key, spec.Iv);
			else
				transformer = encryptor.CreateDecryptor (key.Key, spec.Iv);
		}

		public override void Update (byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset)
		{
			transformer.TransformBlock (input, inputOffset, inputLen, output, outputOffset);
		}
	}
	
	class BlowfishCipher: Cipher
	{
		public BlowfishCipher (CipherMode mode)
		{
			throw new NotSupportedException ();
		}
	}
	
	class DESedeCipher: Cipher
	{
		TripleDESCryptoServiceProvider des;
		ICryptoTransform transformer;
		
		public DESedeCipher (CipherMode mode)
		{
			des = new TripleDESCryptoServiceProvider ();
			des.Mode = mode;
			des.Padding = PaddingMode.None;
		}
		
		public override void Init (int mode, Key keyspec, IvParameterSpec spec)
		{
			SecretKey key = (SecretKey) keyspec;
			if (mode == Cipher.ENCRYPT_MODE)
				transformer = des.CreateEncryptor (key.Key, spec.Iv);
			else
				transformer = des.CreateDecryptor (key.Key, spec.Iv);
		}
		
		public override void Update (byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset)
		{
			transformer.TransformBlock (input, inputOffset, inputLen, output, outputOffset);
		}
	}
}

