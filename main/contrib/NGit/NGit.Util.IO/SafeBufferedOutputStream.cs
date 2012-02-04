/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>
	/// A BufferedOutputStream that throws an error if the final flush fails on
	/// close.
	/// </summary>
	/// <remarks>
	/// A BufferedOutputStream that throws an error if the final flush fails on
	/// close.
	/// <p>
	/// Java's BufferedOutputStream swallows errors that occur when the output stream
	/// tries to write the final bytes to the output during close. This may result in
	/// corrupted files without notice.
	/// </p>
	/// </remarks>
	public class SafeBufferedOutputStream : BufferedOutputStream
	{
		/// <seealso cref="Sharpen.BufferedOutputStream.BufferedOutputStream(Sharpen.OutputStream)
		/// 	">Sharpen.BufferedOutputStream.BufferedOutputStream(Sharpen.OutputStream)</seealso>
		/// <param name="out"></param>
		public SafeBufferedOutputStream(OutputStream @out) : base(@out)
		{
		}

		/// <seealso cref="Sharpen.BufferedOutputStream.BufferedOutputStream(Sharpen.OutputStream, int)
		/// 	">Sharpen.BufferedOutputStream.BufferedOutputStream(Sharpen.OutputStream, int)</seealso>
		/// <param name="out"></param>
		/// <param name="size"></param>
		public SafeBufferedOutputStream(OutputStream @out, int size) : base(@out)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				Flush();
			}
			finally
			{
				base.Close();
			}
		}
	}
}
