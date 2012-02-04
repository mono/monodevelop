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

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Utility for reading reflog entries</summary>
	public class ReflogReader
	{
		private FilePath logName;

		/// <param name="db"></param>
		/// <param name="refname"></param>
		public ReflogReader(Repository db, string refname)
		{
			logName = new FilePath(db.Directory, Constants.LOGS + '/' + refname);
		}

		/// <summary>Get the last entry in the reflog</summary>
		/// <returns>the latest reflog entry, or null if no log</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual ReflogEntry GetLastEntry()
		{
			return GetReverseEntry(0);
		}

		/// <returns>all reflog entries in reverse order</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual IList<ReflogEntry> GetReverseEntries()
		{
			return GetReverseEntries(int.MaxValue);
		}

		/// <summary>
		/// Get specific entry in the reflog relative to the last entry which is
		/// considered entry zero.
		/// </summary>
		/// <remarks>
		/// Get specific entry in the reflog relative to the last entry which is
		/// considered entry zero.
		/// </remarks>
		/// <param name="number"></param>
		/// <returns>reflog entry or null if not found</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual ReflogEntry GetReverseEntry(int number)
		{
			if (number < 0)
			{
				throw new ArgumentException();
			}
			byte[] log;
			try
			{
				log = IOUtil.ReadFully(logName);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			int rs = RawParseUtils.PrevLF(log, log.Length);
			int current = 0;
			while (rs >= 0)
			{
				rs = RawParseUtils.PrevLF(log, rs);
				if (number == current)
				{
					return new ReflogEntry(log, rs < 0 ? 0 : rs + 2);
				}
				current++;
			}
			return null;
		}

		/// <param name="max">max number of entries to read</param>
		/// <returns>all reflog entries in reverse order</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual IList<ReflogEntry> GetReverseEntries(int max)
		{
			byte[] log;
			try
			{
				log = IOUtil.ReadFully(logName);
			}
			catch (FileNotFoundException)
			{
				return Sharpen.Collections.EmptyList<ReflogEntry>();
			}
			int rs = RawParseUtils.PrevLF(log, log.Length);
			IList<ReflogEntry> ret = new AList<ReflogEntry>();
			while (rs >= 0 && max-- > 0)
			{
				rs = RawParseUtils.PrevLF(log, rs);
				ReflogEntry entry = new ReflogEntry(log, rs < 0 ? 0 : rs + 2);
				ret.AddItem(entry);
			}
			return ret;
		}
	}
}
