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

using NGit;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Keeps track of a
	/// <see cref="PackFile">PackFile</see>
	/// 's associated <code>.keep</code> file.
	/// </summary>
	public class PackLock
	{
		private readonly FilePath keepFile;

		private readonly FS fs;

		/// <summary>Create a new lock for a pack file.</summary>
		/// <remarks>Create a new lock for a pack file.</remarks>
		/// <param name="packFile">location of the <code>pack-*.pack</code> file.</param>
		/// <param name="fs">the filesystem abstraction used by the repository.</param>
		public PackLock(FilePath packFile, FS fs)
		{
			FilePath p = packFile.GetParentFile();
			string n = packFile.GetName();
			keepFile = new FilePath(p, Sharpen.Runtime.Substring(n, 0, n.Length - 5) + ".keep"
				);
			this.fs = fs;
		}

		/// <summary>Create the <code>pack-*.keep</code> file, with the given message.</summary>
		/// <remarks>Create the <code>pack-*.keep</code> file, with the given message.</remarks>
		/// <param name="msg">message to store in the file.</param>
		/// <returns>true if the keep file was successfully written; false otherwise.</returns>
		/// <exception cref="System.IO.IOException">the keep file could not be written.</exception>
		public virtual bool Lock(string msg)
		{
			if (msg == null)
			{
				return false;
			}
			if (!msg.EndsWith("\n"))
			{
				msg += "\n";
			}
			LockFile lf = new LockFile(keepFile, fs);
			if (!lf.Lock())
			{
				return false;
			}
			lf.Write(Constants.Encode(msg));
			return lf.Commit();
		}

		/// <summary>Remove the <code>.keep</code> file that holds this pack in place.</summary>
		/// <remarks>Remove the <code>.keep</code> file that holds this pack in place.</remarks>
		public virtual void Unlock()
		{
			keepFile.Delete();
		}
	}
}
