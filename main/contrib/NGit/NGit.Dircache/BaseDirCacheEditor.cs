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
using NGit.Dircache;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// Generic update/editing support for
	/// <see cref="DirCache">DirCache</see>
	/// .
	/// <p>
	/// The different update strategies extend this class to provide their own unique
	/// services to applications.
	/// </summary>
	public abstract class BaseDirCacheEditor
	{
		/// <summary>
		/// The cache instance this editor updates during
		/// <see cref="Finish()">Finish()</see>
		/// .
		/// </summary>
		protected internal DirCache cache;

		/// <summary>
		/// Entry table this builder will eventually replace into
		/// <see cref="cache">cache</see>
		/// .
		/// <p>
		/// Use
		/// <see cref="FastAdd(DirCacheEntry)">FastAdd(DirCacheEntry)</see>
		/// or
		/// <see cref="FastKeep(int, int)">FastKeep(int, int)</see>
		/// to
		/// make additions to this table. The table is automatically expanded if it
		/// is too small for a new addition.
		/// <p>
		/// Typically the entries in here are sorted by their path names, just like
		/// they are in the DirCache instance.
		/// </summary>
		protected internal DirCacheEntry[] entries;

		/// <summary>
		/// Total number of valid entries in
		/// <see cref="entries">entries</see>
		/// .
		/// </summary>
		protected internal int entryCnt;

		/// <summary>Construct a new editor.</summary>
		/// <remarks>Construct a new editor.</remarks>
		/// <param name="dc">the cache this editor will eventually update.</param>
		/// <param name="ecnt">
		/// estimated number of entries the editor will have upon
		/// completion. This sizes the initial entry table.
		/// </param>
		protected internal BaseDirCacheEditor(DirCache dc, int ecnt)
		{
			cache = dc;
			entries = new DirCacheEntry[ecnt];
		}

		/// <returns>
		/// the cache we will update on
		/// <see cref="Finish()">Finish()</see>
		/// .
		/// </returns>
		public virtual DirCache GetDirCache()
		{
			return cache;
		}

		/// <summary>Append one entry into the resulting entry list.</summary>
		/// <remarks>
		/// Append one entry into the resulting entry list.
		/// <p>
		/// The entry is placed at the end of the entry list. The caller is
		/// responsible for making sure the final table is correctly sorted.
		/// <p>
		/// The
		/// <see cref="entries">entries</see>
		/// table is automatically expanded if there is
		/// insufficient space for the new addition.
		/// </remarks>
		/// <param name="newEntry">the new entry to add.</param>
		protected internal virtual void FastAdd(DirCacheEntry newEntry)
		{
			if (entries.Length == entryCnt)
			{
				DirCacheEntry[] n = new DirCacheEntry[(entryCnt + 16) * 3 / 2];
				System.Array.Copy(entries, 0, n, 0, entryCnt);
				entries = n;
			}
			entries[entryCnt++] = newEntry;
		}

		/// <summary>Add a range of existing entries from the destination cache.</summary>
		/// <remarks>
		/// Add a range of existing entries from the destination cache.
		/// <p>
		/// The entries are placed at the end of the entry list, preserving their
		/// current order. The caller is responsible for making sure the final table
		/// is correctly sorted.
		/// <p>
		/// This method copies from the destination cache, which has not yet been
		/// updated with this editor's new table. So all offsets into the destination
		/// cache are not affected by any updates that may be currently taking place
		/// in this editor.
		/// <p>
		/// The
		/// <see cref="entries">entries</see>
		/// table is automatically expanded if there is
		/// insufficient space for the new additions.
		/// </remarks>
		/// <param name="pos">first entry to copy from the destination cache.</param>
		/// <param name="cnt">number of entries to copy.</param>
		protected internal virtual void FastKeep(int pos, int cnt)
		{
			if (entryCnt + cnt > entries.Length)
			{
				int m1 = (entryCnt + 16) * 3 / 2;
				int m2 = entryCnt + cnt;
				DirCacheEntry[] n = new DirCacheEntry[Math.Max(m1, m2)];
				System.Array.Copy(entries, 0, n, 0, entryCnt);
				entries = n;
			}
			cache.ToArray(pos, entries, entryCnt, cnt);
			entryCnt += cnt;
		}

		/// <summary>
		/// Finish this builder and update the destination
		/// <see cref="DirCache">DirCache</see>
		/// .
		/// <p>
		/// When this method completes this builder instance is no longer usable by
		/// the calling application. A new builder must be created to make additional
		/// changes to the index entries.
		/// <p>
		/// After completion the DirCache returned by
		/// <see cref="GetDirCache()">GetDirCache()</see>
		/// will
		/// contain all modifications.
		/// <p>
		/// <i>Note to implementors:</i> Make sure
		/// <see cref="entries">entries</see>
		/// is fully sorted
		/// then invoke
		/// <see cref="Replace()">Replace()</see>
		/// to update the DirCache with the new table.
		/// </summary>
		public abstract void Finish();

		/// <summary>
		/// Update the DirCache with the contents of
		/// <see cref="entries">entries</see>
		/// .
		/// <p>
		/// This method should be invoked only during an implementation of
		/// <see cref="Finish()">Finish()</see>
		/// , and only after
		/// <see cref="entries">entries</see>
		/// is sorted.
		/// </summary>
		protected internal virtual void Replace()
		{
			if (entryCnt < entries.Length / 2)
			{
				DirCacheEntry[] n = new DirCacheEntry[entryCnt];
				System.Array.Copy(entries, 0, n, 0, entryCnt);
				entries = n;
			}
			cache.Replace(entries, entryCnt);
		}

		/// <summary>Finish, write, commit this change, and release the index lock.</summary>
		/// <remarks>
		/// Finish, write, commit this change, and release the index lock.
		/// <p>
		/// If this method fails (returns false) the lock is still released.
		/// <p>
		/// This is a utility method for applications as the finish-write-commit
		/// pattern is very common after using a builder to update entries.
		/// </remarks>
		/// <returns>
		/// true if the commit was successful and the file contains the new
		/// data; false if the commit failed and the file remains with the
		/// old data.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">the lock is not held.</exception>
		/// <exception cref="System.IO.IOException">
		/// the output file could not be created. The caller no longer
		/// holds the lock.
		/// </exception>
		public virtual bool Commit()
		{
			Finish();
			cache.Write();
			return cache.Commit();
		}
	}
}
