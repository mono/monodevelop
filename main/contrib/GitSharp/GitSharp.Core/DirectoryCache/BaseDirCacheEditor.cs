/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using System.IO;

namespace GitSharp.Core.DirectoryCache
{
	/// <summary>
	/// Generic update/editing support for <seealso cref="DirCache"/>.
	/// <para />
	/// The different update strategies extend this class to provide their 
	/// own unique services to applications. 
	/// </summary>
	public abstract class BaseDirCacheEditor
	{
		// The cache instance this editor updates during finish.
		private readonly DirCache _cache;

		///	<summary>
		/// Entry table this builder will eventually replace into <seealso cref="Cache"/>.
		///	<para />
		/// Use <seealso cref="FastAdd(DirCacheEntry)"/> or <seealso cref="FastKeep(int, int)"/> to
		/// make additions to this table. The table is automatically expanded if it
		/// is too small for a new addition.
		/// <para />
		/// Typically the entries in here are sorted by their path names, just like
		/// they are in the DirCache instance.
		/// </summary>
		private DirCacheEntry[] _entries;

		// Total number of valid entries in Entries.
		private int _entryCnt;

		///	<summary>
		/// Construct a new editor.
		///	</summary>
		/// <param name="dc">
		/// the cache this editor will eventually update.
		/// </param>
		///	<param name="ecnt">
		/// estimated number of entries the editor will have upon
		/// completion. This sizes the initial entry table.
		/// </param>
		protected BaseDirCacheEditor(DirCache dc, int ecnt)
		{
			_cache = dc;
			_entries = new DirCacheEntry[ecnt];
		}

		/// <summary>
		/// 
		/// </summary>
		///	<returns> 
		/// The cache we will update on <seealso cref="finish()"/>.
		/// </returns>
		public DirCache getDirCache()
		{
			return _cache;
		}

		///	<summary>
		/// Append one entry into the resulting entry list.
		/// <para />
		/// The entry is placed at the end of the entry list. The caller is
		/// responsible for making sure the final table is correctly sorted.
		/// <para />
		///	The <seealso cref="Entries"/> table is automatically expanded 
		/// if there is insufficient space for the new addition.
		/// </summary>
		/// <param name="newEntry">The new entry to add.</param>
		protected void FastAdd(DirCacheEntry newEntry)
		{
			if (_entries.Length == _entryCnt)
			{
				var n = new DirCacheEntry[(_entryCnt + 16) * 3 / 2];
				Array.Copy(_entries, 0, n, 0, _entryCnt);
				_entries = n;
			}
			_entries[_entryCnt++] = newEntry;
		}

		protected DirCacheEntry[] Entries
		{
			get { return _entries; }
		}

		protected DirCache Cache
		{
			get { return _cache; }
		}

		protected int EntryCnt
		{
			get { return _entryCnt; }
		}

		///	<summary>
		/// Add a range of existing entries from the destination cache.
		/// <para />
		/// The entries are placed at the end of the entry list, preserving their
		/// current order. The caller is responsible for making sure the final table
		/// is correctly sorted.
		/// <para />
		/// This method copies from the destination cache, which has not yet been
		/// updated with this editor's new table. So all offsets into the destination
		/// cache are not affected by any updates that may be currently taking place
		/// in this editor.
		/// <para />
		/// The <seealso cref="Entries"/> table is automatically expanded if there is
		/// insufficient space for the new additions.
		/// </summary>
		/// <param name="pos">First entry to copy from the destination cache. </param>
		/// <param name="cnt">Number of entries to copy.</param>
		protected void FastKeep(int pos, int cnt)
		{
			if (_entryCnt + cnt > _entries.Length)
			{
				int m1 = (_entryCnt + 16) * 3 / 2;
				int m2 = _entryCnt + cnt;
				var n = new DirCacheEntry[Math.Max(m1, m2)];
				Array.Copy(_entries, 0, n, 0, _entryCnt);
				_entries = n;
			}

			_cache.toArray(pos, _entries, _entryCnt, cnt);
			_entryCnt += cnt;
		}

		///	<summary> * Finish this builder and update the destination <seealso cref="DirCache"/>.
		///	<para />
		/// When this method completes this builder instance is no longer usable by
		/// the calling application. A new builder must be created to make additional
		/// changes to the index entries.
		/// <para />
		/// After completion the DirCache returned by <seealso cref="getDirCache()"/> will
		/// contain all modifications.
		/// </summary>
		/// <remarks>
		/// <i>Note to implementors:</i> Make sure <seealso cref="Entries"/> is fully sorted
		/// then invoke <seealso cref="Replace()"/> to update the DirCache with the new table. 
		/// </remarks>
		public abstract void finish();

		///	<summary>
		/// Update the DirCache with the contents of <seealso cref="Entries"/>.
		///	<para />
		/// This method should be invoked only during an implementation of
		/// <seealso cref="finish()"/>, and only after <seealso cref="Entries"/> is sorted.
		/// </summary>
		protected void Replace()
		{
			if (_entryCnt < _entries.Length / 2)
			{
				var n = new DirCacheEntry[_entryCnt];
				Array.Copy(_entries, 0, n, 0, _entryCnt);
				_entries = n;
			}
			_cache.replace(_entries, _entryCnt);
		}

		///	<summary>
		/// Finish, write, commit this change, and release the index lock.
		/// <para />
		/// If this method fails (returns false) the lock is still released.
		/// <para />
		/// This is a utility method for applications as the finish-write-commit
		/// pattern is very common after using a builder to update entries.
		/// </summary>
		/// <returns>
		/// True if the commit was successful and the file contains the new
		/// data; false if the commit failed and the file remains with the
		/// old data.
		/// </returns>
		/// <exception cref="IOException">
		/// The output file could not be created. The caller no longer
		/// holds the lock.
		/// </exception>
		public virtual bool commit()
		{
			finish();
			_cache.write();
			return _cache.commit();
		}
	}
}