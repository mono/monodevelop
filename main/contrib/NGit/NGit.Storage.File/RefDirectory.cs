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
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Events;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Traditional file system based
	/// <see cref="NGit.RefDatabase">NGit.RefDatabase</see>
	/// .
	/// <p>
	/// This is the classical reference database representation for a Git repository.
	/// References are stored in two formats: loose, and packed.
	/// <p>
	/// Loose references are stored as individual files within the
	/// <code>refs/</code>
	/// directory. The file name matches the reference name and the file contents is
	/// the current
	/// <see cref="NGit.ObjectId">NGit.ObjectId</see>
	/// in string form.
	/// <p>
	/// Packed references are stored in a single text file named
	/// <code>packed-refs</code>
	/// .
	/// In the packed format, each reference is stored on its own line. This file
	/// reduces the number of files needed for large reference spaces, reducing the
	/// overall size of a Git repository on disk.
	/// </summary>
	public class RefDirectory : RefDatabase
	{
		/// <summary>Magic string denoting the start of a symbolic reference file.</summary>
		/// <remarks>Magic string denoting the start of a symbolic reference file.</remarks>
		public static readonly string SYMREF = "ref: ";

		/// <summary>Magic string denoting the header of a packed-refs file.</summary>
		/// <remarks>Magic string denoting the header of a packed-refs file.</remarks>
		public static readonly string PACKED_REFS_HEADER = "# pack-refs with:";

		/// <summary>If in the header, denotes the file has peeled data.</summary>
		/// <remarks>If in the header, denotes the file has peeled data.</remarks>
		public static readonly string PACKED_REFS_PEELED = " peeled";

		/// <summary>The names of the additional refs supported by this class</summary>
		private static readonly string[] additionalRefsNames = new string[] { Constants.MERGE_HEAD
			, Constants.FETCH_HEAD, Constants.ORIG_HEAD };

		private readonly FileRepository parent;

		private readonly FilePath gitDir;

		private readonly FilePath refsDir;

		private readonly FilePath logsDir;

		private readonly FilePath logsRefsDir;

		private readonly FilePath packedRefsFile;

		/// <summary>Immutable sorted list of loose references.</summary>
		/// <remarks>
		/// Immutable sorted list of loose references.
		/// <p>
		/// Symbolic references in this collection are stored unresolved, that is
		/// their target appears to be a new reference with no ObjectId. These are
		/// converted into resolved references during a get operation, ensuring the
		/// live value is always returned.
		/// </remarks>
		private readonly AtomicReference<RefList<RefDirectory.LooseRef>> looseRefs = new 
			AtomicReference<RefList<RefDirectory.LooseRef>>();

		/// <summary>Immutable sorted list of packed references.</summary>
		/// <remarks>Immutable sorted list of packed references.</remarks>
		private readonly AtomicReference<RefDirectory.PackedRefList> packedRefs = new AtomicReference
			<RefDirectory.PackedRefList>();

		/// <summary>Number of modifications made to this database.</summary>
		/// <remarks>
		/// Number of modifications made to this database.
		/// <p>
		/// This counter is incremented when a change is made, or detected from the
		/// filesystem during a read operation.
		/// </remarks>
		private readonly AtomicInteger modCnt = new AtomicInteger();

		/// <summary>
		/// Last
		/// <see cref="modCnt">modCnt</see>
		/// that we sent to listeners.
		/// <p>
		/// This value is compared to
		/// <see cref="modCnt">modCnt</see>
		/// , and a notification is sent to
		/// the listeners only when it differs.
		/// </summary>
		private readonly AtomicInteger lastNotifiedModCnt = new AtomicInteger();

		internal RefDirectory(FileRepository db)
		{
			//$NON-NLS-1$
			//$NON-NLS-1$
			//$NON-NLS-1$
			FS fs = db.FileSystem;
			parent = db;
			gitDir = db.Directory;
			refsDir = fs.Resolve(gitDir, Constants.R_REFS);
			logsDir = fs.Resolve(gitDir, Constants.LOGS);
			logsRefsDir = fs.Resolve(gitDir, Constants.LOGS + '/' + Constants.R_REFS);
			packedRefsFile = fs.Resolve(gitDir, Constants.PACKED_REFS);
			looseRefs.Set(RefList.EmptyList<RefDirectory.LooseRef>());
			packedRefs.Set(RefDirectory.PackedRefList.NO_PACKED_REFS);
		}

		internal virtual Repository GetRepository()
		{
			return parent;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Create()
		{
			FileUtils.Mkdir(refsDir);
			FileUtils.Mkdir(logsDir);
			FileUtils.Mkdir(logsRefsDir);
			FileUtils.Mkdir(new FilePath(refsDir, Sharpen.Runtime.Substring(Constants.R_HEADS
				, Constants.R_REFS.Length)));
			FileUtils.Mkdir(new FilePath(refsDir, Sharpen.Runtime.Substring(Constants.R_TAGS, 
				Constants.R_REFS.Length)));
			FileUtils.Mkdir(new FilePath(logsRefsDir, Sharpen.Runtime.Substring(Constants.R_HEADS
				, Constants.R_REFS.Length)));
		}

		public override void Close()
		{
		}

		// We have no resources to close.
		internal virtual void Rescan()
		{
			looseRefs.Set(RefList.EmptyList<RefDirectory.LooseRef>());
			packedRefs.Set(RefDirectory.PackedRefList.NO_PACKED_REFS);
		}

		public override void Refresh()
		{
			base.Refresh();
			Rescan();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IsNameConflicting(string name)
		{
			RefList<Ref> packed = GetPackedRefs();
			RefList<RefDirectory.LooseRef> loose = GetLooseRefs();
			// Cannot be nested within an existing reference.
			int lastSlash = name.LastIndexOf('/');
			while (0 < lastSlash)
			{
				string needle = Sharpen.Runtime.Substring(name, 0, lastSlash);
				if (loose.Contains(needle) || packed.Contains(needle))
				{
					return true;
				}
				lastSlash = name.LastIndexOf('/', lastSlash - 1);
			}
			// Cannot be the container of an existing reference.
			string prefix = name + '/';
			int idx;
			idx = -(packed.Find(prefix) + 1);
			if (idx < packed.Size() && packed.Get(idx).GetName().StartsWith(prefix))
			{
				return true;
			}
			idx = -(loose.Find(prefix) + 1);
			if (idx < loose.Size() && loose.Get(idx).GetName().StartsWith(prefix))
			{
				return true;
			}
			return false;
		}

		private RefList<RefDirectory.LooseRef> GetLooseRefs()
		{
			RefList<RefDirectory.LooseRef> oldLoose = looseRefs.Get();
			RefDirectory.LooseScanner scan = new RefDirectory.LooseScanner(this, oldLoose);
			scan.Scan(ALL);
			RefList<RefDirectory.LooseRef> loose;
			if (scan.newLoose != null)
			{
				loose = scan.newLoose.ToRefList();
				if (looseRefs.CompareAndSet(oldLoose, loose))
				{
					modCnt.IncrementAndGet();
				}
			}
			else
			{
				loose = oldLoose;
			}
			return loose;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Ref GetRef(string needle)
		{
			RefList<Ref> packed = GetPackedRefs();
			Ref @ref = null;
			foreach (string prefix in SEARCH_PATH)
			{
				@ref = ReadRef(prefix + needle, packed);
				if (@ref != null)
				{
					@ref = Resolve(@ref, 0, null, null, packed);
					break;
				}
			}
			FireRefsChanged();
			return @ref;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IDictionary<string, Ref> GetRefs(string prefix)
		{
			RefList<Ref> packed = GetPackedRefs();
			RefList<RefDirectory.LooseRef> oldLoose = looseRefs.Get();
			RefDirectory.LooseScanner scan = new RefDirectory.LooseScanner(this, oldLoose);
			scan.Scan(prefix);
			RefList<RefDirectory.LooseRef> loose;
			if (scan.newLoose != null)
			{
				loose = scan.newLoose.ToRefList();
				if (looseRefs.CompareAndSet(oldLoose, loose))
				{
					modCnt.IncrementAndGet();
				}
			}
			else
			{
				loose = oldLoose;
			}
			FireRefsChanged();
			RefListBuilder<Ref> symbolic = scan.symbolic;
			for (int idx = 0; idx < symbolic.Size(); )
			{
				Ref symbolicRef = symbolic.Get(idx);
				Ref resolvedRef = Resolve(symbolicRef, 0, prefix, loose, packed);
				if (resolvedRef != null && resolvedRef.GetObjectId() != null)
				{
					symbolic.Set(idx, resolvedRef);
					idx++;
				}
				else
				{
					// A broken symbolic reference, we have to drop it from the
					// collections the client is about to receive. Should be a
					// rare occurrence so pay a copy penalty.
					symbolic.Remove(idx);
					int toRemove = loose.Find(symbolicRef.GetName());
					if (0 <= toRemove)
					{
						loose = loose.Remove(toRemove);
					}
				}
			}
			return new RefMap(prefix, packed, Upcast(loose), symbolic.ToRefList());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IList<Ref> GetAdditionalRefs()
		{
			IList<Ref> ret = new List<Ref>();
			foreach (string name in additionalRefsNames)
			{
				Ref r = GetRef(name);
				if (r != null)
				{
					ret.AddItem(r);
				}
			}
			return ret;
		}

		private RefList<Ref> Upcast<_T0>(RefList<_T0> loose) where _T0:Ref
		{
			return RefList<Ref>.Copy<_T0>(loose);
		}

		private class LooseScanner
		{
			private readonly RefList<RefDirectory.LooseRef> curLoose;

			private int curIdx;

			internal readonly RefListBuilder<Ref> symbolic = new RefListBuilder<Ref>(4);

			internal RefListBuilder<RefDirectory.LooseRef> newLoose;

			internal LooseScanner(RefDirectory _enclosing, RefList<RefDirectory.LooseRef> curLoose
				)
			{
				this._enclosing = _enclosing;
				this.curLoose = curLoose;
			}

			internal virtual void Scan(string prefix)
			{
				if (RefDatabase.ALL.Equals(prefix))
				{
					this.ScanOne(Constants.HEAD);
					this.ScanTree(Constants.R_REFS, this._enclosing.refsDir);
					// If any entries remain, they are deleted, drop them.
					if (this.newLoose == null && this.curIdx < this.curLoose.Size())
					{
						this.newLoose = this.curLoose.Copy(this.curIdx);
					}
				}
				else
				{
					if (prefix.StartsWith(Constants.R_REFS) && prefix.EndsWith("/"))
					{
						this.curIdx = -(this.curLoose.Find(prefix) + 1);
						FilePath dir = new FilePath(this._enclosing.refsDir, Sharpen.Runtime.Substring(prefix
							, Constants.R_REFS.Length));
						this.ScanTree(prefix, dir);
						// Skip over entries still within the prefix; these have
						// been removed from the directory.
						while (this.curIdx < this.curLoose.Size())
						{
							if (!this.curLoose.Get(this.curIdx).GetName().StartsWith(prefix))
							{
								break;
							}
							if (this.newLoose == null)
							{
								this.newLoose = this.curLoose.Copy(this.curIdx);
							}
							this.curIdx++;
						}
						// Keep any entries outside of the prefix space, we
						// do not know anything about their status.
						if (this.newLoose != null)
						{
							while (this.curIdx < this.curLoose.Size())
							{
								this.newLoose.Add(this.curLoose.Get(this.curIdx++));
							}
						}
					}
				}
			}

			private bool ScanTree(string prefix, FilePath dir)
			{
				string[] entries = dir.List(LockFile.FILTER);
				if (entries == null)
				{
					// not a directory or an I/O error
					return false;
				}
				if (0 < entries.Length)
				{
					Arrays.Sort(entries);
					foreach (string name in entries)
					{
						FilePath e = new FilePath(dir, name);
						if (!this.ScanTree(prefix + name + '/', e))
						{
							this.ScanOne(prefix + name);
						}
					}
				}
				return true;
			}

			private void ScanOne(string name)
			{
				RefDirectory.LooseRef cur;
				if (this.curIdx < this.curLoose.Size())
				{
					do
					{
						cur = this.curLoose.Get(this.curIdx);
						int cmp = RefComparator.CompareTo(cur, name);
						if (cmp < 0)
						{
							// Reference is not loose anymore, its been deleted.
							// Skip the name in the new result list.
							if (this.newLoose == null)
							{
								this.newLoose = this.curLoose.Copy(this.curIdx);
							}
							this.curIdx++;
							cur = null;
							continue;
						}
						if (cmp > 0)
						{
							// Newly discovered loose reference.
							cur = null;
						}
						break;
					}
					while (this.curIdx < this.curLoose.Size());
				}
				else
				{
					cur = null;
				}
				// Newly discovered loose reference.
				RefDirectory.LooseRef n;
				try
				{
					n = this._enclosing.ScanRef(cur, name);
				}
				catch (IOException)
				{
					n = null;
				}
				if (n != null)
				{
					if (cur != n && this.newLoose == null)
					{
						this.newLoose = this.curLoose.Copy(this.curIdx);
					}
					if (this.newLoose != null)
					{
						this.newLoose.Add(n);
					}
					if (n.IsSymbolic())
					{
						this.symbolic.Add(n);
					}
				}
				else
				{
					if (cur != null)
					{
						// Tragically, this file is no longer a loose reference.
						// Kill our cached entry of it.
						if (this.newLoose == null)
						{
							this.newLoose = this.curLoose.Copy(this.curIdx);
						}
					}
				}
				if (cur != null)
				{
					this.curIdx++;
				}
			}

			private readonly RefDirectory _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Ref Peel(Ref @ref)
		{
			Ref leaf = @ref.GetLeaf();
			if (leaf.IsPeeled() || leaf.GetObjectId() == null)
			{
				return @ref;
			}
			ObjectIdRef newLeaf = DoPeel(leaf);
			// Try to remember this peeling in the cache, so we don't have to do
			// it again in the future, but only if the reference is unchanged.
			if (leaf.GetStorage().IsLoose())
			{
				RefList<RefDirectory.LooseRef> curList = looseRefs.Get();
				int idx = curList.Find(leaf.GetName());
				if (0 <= idx && curList.Get(idx) == leaf)
				{
					RefDirectory.LooseRef asPeeled = ((RefDirectory.LooseRef)leaf).Peel(newLeaf);
					RefList<RefDirectory.LooseRef> newList = curList.Set(idx, asPeeled);
					looseRefs.CompareAndSet(curList, newList);
				}
			}
			return Recreate(@ref, newLeaf);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private ObjectIdRef DoPeel(Ref leaf)
		{
			RevWalk rw = new RevWalk(GetRepository());
			try
			{
				RevObject obj = rw.ParseAny(leaf.GetObjectId());
				if (obj is RevTag)
				{
					return new ObjectIdRef.PeeledTag(leaf.GetStorage(), leaf.GetName(), leaf.GetObjectId
						(), rw.Peel(obj).Copy());
				}
				else
				{
					return new ObjectIdRef.PeeledNonTag(leaf.GetStorage(), leaf.GetName(), leaf.GetObjectId
						());
				}
			}
			finally
			{
				rw.Release();
			}
		}

		private static Ref Recreate(Ref old, ObjectIdRef leaf)
		{
			if (old.IsSymbolic())
			{
				Ref dst = Recreate(old.GetTarget(), leaf);
				return new SymbolicRef(old.GetName(), dst);
			}
			return leaf;
		}

		internal virtual void StoredSymbolicRef(RefDirectoryUpdate u, long modified, string
			 target)
		{
			PutLooseRef(NewSymbolicRef(modified, u.GetRef().GetName(), target));
			FireRefsChanged();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override RefUpdate NewUpdate(string name, bool detach)
		{
			bool detachingSymbolicRef = false;
			RefList<Ref> packed = GetPackedRefs();
			Ref @ref = ReadRef(name, packed);
			if (@ref != null)
			{
				@ref = Resolve(@ref, 0, null, null, packed);
			}
			if (@ref == null)
			{
				@ref = new ObjectIdRef.Unpeeled(RefStorage.NEW, name, null);
			}
			else
			{
				detachingSymbolicRef = detach && @ref.IsSymbolic();
				if (detachingSymbolicRef)
				{
					@ref = new ObjectIdRef.Unpeeled(RefStorage.LOOSE, name, @ref.GetObjectId());
				}
			}
			RefDirectoryUpdate refDirUpdate = new RefDirectoryUpdate(this, @ref);
			if (detachingSymbolicRef)
			{
				refDirUpdate.SetDetachingSymbolicRef();
			}
			return refDirUpdate;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override RefRename NewRename(string fromName, string toName)
		{
			RefDirectoryUpdate from = ((RefDirectoryUpdate)NewUpdate(fromName, false));
			RefDirectoryUpdate to = ((RefDirectoryUpdate)NewUpdate(toName, false));
			return new RefDirectoryRename(from, to);
		}

		internal virtual void Stored(RefDirectoryUpdate update, long modified)
		{
			ObjectId target = update.GetNewObjectId().Copy();
			Ref leaf = update.GetRef().GetLeaf();
			PutLooseRef(new RefDirectory.LooseUnpeeled(modified, leaf.GetName(), target));
		}

		private void PutLooseRef(RefDirectory.LooseRef @ref)
		{
			RefList<RefDirectory.LooseRef> cList;
			RefList<RefDirectory.LooseRef> nList;
			do
			{
				cList = looseRefs.Get();
				nList = cList.Put(@ref);
			}
			while (!looseRefs.CompareAndSet(cList, nList));
			modCnt.IncrementAndGet();
			FireRefsChanged();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Delete(RefDirectoryUpdate update)
		{
			Ref dst = update.GetRef().GetLeaf();
			string name = dst.GetName();
			// Write the packed-refs file using an atomic update. We might
			// wind up reading it twice, before and after the lock, to ensure
			// we don't miss an edit made externally.
			RefDirectory.PackedRefList packed = GetPackedRefs();
			if (packed.Contains(name))
			{
				LockFile lck = new LockFile(packedRefsFile, update.GetRepository().FileSystem);
				if (!lck.Lock())
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().cannotLockFile, packedRefsFile
						));
				}
				try
				{
					RefDirectory.PackedRefList cur = ReadPackedRefs();
					int idx = cur.Find(name);
					if (0 <= idx)
					{
						CommitPackedRefs(lck, cur.Remove(idx), packed);
					}
				}
				finally
				{
					lck.Unlock();
				}
			}
			RefList<RefDirectory.LooseRef> curLoose;
			RefList<RefDirectory.LooseRef> newLoose;
			do
			{
				curLoose = looseRefs.Get();
				int idx = curLoose.Find(name);
				if (idx < 0)
				{
					break;
				}
				newLoose = curLoose.Remove(idx);
			}
			while (!looseRefs.CompareAndSet(curLoose, newLoose));
			int levels = LevelsIn(name) - 2;
			Delete(LogFor(name), levels);
			if (dst.GetStorage().IsLoose())
			{
				update.Unlock();
				Delete(FileFor(name), levels);
			}
			modCnt.IncrementAndGet();
			FireRefsChanged();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Log(RefUpdate update, string msg, bool deref)
		{
			ObjectId oldId = update.GetOldObjectId();
			ObjectId newId = update.GetNewObjectId();
			Ref @ref = update.GetRef();
			PersonIdent ident = update.GetRefLogIdent();
			if (ident == null)
			{
				ident = new PersonIdent(parent);
			}
			else
			{
				ident = new PersonIdent(ident);
			}
			StringBuilder r = new StringBuilder();
			r.Append(ObjectId.ToString(oldId));
			r.Append(' ');
			r.Append(ObjectId.ToString(newId));
			r.Append(' ');
			r.Append(ident.ToExternalString());
			r.Append('\t');
			r.Append(msg);
			r.Append('\n');
			byte[] rec = Constants.Encode(r.ToString());
			if (deref && @ref.IsSymbolic())
			{
				Log(@ref.GetName(), rec);
				Log(@ref.GetLeaf().GetName(), rec);
			}
			else
			{
				Log(@ref.GetName(), rec);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Log(string refName, byte[] rec)
		{
			FilePath log = LogFor(refName);
			bool write;
			if (IsLogAllRefUpdates() && ShouldAutoCreateLog(refName))
			{
				write = true;
			}
			else
			{
				if (log.IsFile())
				{
					write = true;
				}
				else
				{
					write = false;
				}
			}
			if (write)
			{
				WriteConfig wc = GetRepository().GetConfig().Get(WriteConfig.KEY);
				FileOutputStream @out;
				try
				{
					@out = new FileOutputStream(log, true);
				}
				catch (FileNotFoundException err)
				{
					FilePath dir = log.GetParentFile();
					if (dir.Exists())
					{
						throw;
					}
					if (!dir.Mkdirs() && !dir.IsDirectory())
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().cannotCreateDirectory, 
							dir));
					}
					@out = new FileOutputStream(log, true);
				}
				try
				{
					if (wc.GetFSyncRefFiles())
					{
						FileChannel fc = @out.GetChannel();
						ByteBuffer buf = ByteBuffer.Wrap(rec);
						while (0 < buf.Remaining())
						{
							fc.Write(buf);
						}
						fc.Force(true);
					}
					else
					{
						@out.Write(rec);
					}
				}
				finally
				{
					@out.Close();
				}
			}
		}

		private bool IsLogAllRefUpdates()
		{
			return ((FileBasedConfig)parent.GetConfig()).Get(CoreConfig.KEY).IsLogAllRefUpdates
				();
		}

		private bool ShouldAutoCreateLog(string refName)
		{
			return refName.Equals(Constants.HEAD) || refName.StartsWith(Constants.R_HEADS) ||
				 refName.StartsWith(Constants.R_REMOTES);
		}

		//
		//
		/// <exception cref="System.IO.IOException"></exception>
		private Ref Resolve(Ref @ref, int depth, string prefix, RefList<RefDirectory.LooseRef
			> loose, RefList<Ref> packed)
		{
			if (@ref.IsSymbolic())
			{
				Ref dst = @ref.GetTarget();
				if (MAX_SYMBOLIC_REF_DEPTH <= depth)
				{
					return null;
				}
				// claim it doesn't exist
				// If the cached value can be assumed to be current due to a
				// recent scan of the loose directory, use it.
				if (loose != null && dst.GetName().StartsWith(prefix))
				{
					int idx;
					if (0 <= (idx = loose.Find(dst.GetName())))
					{
						dst = loose.Get(idx);
					}
					else
					{
						if (0 <= (idx = packed.Find(dst.GetName())))
						{
							dst = packed.Get(idx);
						}
						else
						{
							return @ref;
						}
					}
				}
				else
				{
					dst = ReadRef(dst.GetName(), packed);
					if (dst == null)
					{
						return @ref;
					}
				}
				dst = Resolve(dst, depth + 1, prefix, loose, packed);
				if (dst == null)
				{
					return null;
				}
				return new SymbolicRef(@ref.GetName(), dst);
			}
			return @ref;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefDirectory.PackedRefList GetPackedRefs()
		{
			RefDirectory.PackedRefList curList = packedRefs.Get();
			if (!curList.snapshot.IsModified(packedRefsFile))
			{
				return curList;
			}
			RefDirectory.PackedRefList newList = ReadPackedRefs();
			if (packedRefs.CompareAndSet(curList, newList))
			{
				modCnt.IncrementAndGet();
			}
			return newList;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefDirectory.PackedRefList ReadPackedRefs()
		{
			FileSnapshot snapshot = FileSnapshot.Save(packedRefsFile);
			BufferedReader br;
			try
			{
				br = new BufferedReader(new InputStreamReader(new FileInputStream(packedRefsFile)
					, Constants.CHARSET));
			}
			catch (FileNotFoundException)
			{
				// Ignore it and leave the new list empty.
				return RefDirectory.PackedRefList.NO_PACKED_REFS;
			}
			try
			{
				return new RefDirectory.PackedRefList(ParsePackedRefs(br), snapshot);
			}
			finally
			{
				br.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefList<Ref> ParsePackedRefs(BufferedReader br)
		{
			RefListBuilder<Ref> all = new RefListBuilder<Ref>();
			Ref last = null;
			bool peeled = false;
			bool needSort = false;
			string p;
			while ((p = br.ReadLine()) != null)
			{
				if (p[0] == '#')
				{
					if (p.StartsWith(PACKED_REFS_HEADER))
					{
						p = Sharpen.Runtime.Substring(p, PACKED_REFS_HEADER.Length);
						peeled = p.Contains(PACKED_REFS_PEELED);
					}
					continue;
				}
				if (p[0] == '^')
				{
					if (last == null)
					{
						throw new IOException(JGitText.Get().peeledLineBeforeRef);
					}
					ObjectId id = ObjectId.FromString(Sharpen.Runtime.Substring(p, 1));
					last = new ObjectIdRef.PeeledTag(RefStorage.PACKED, last.GetName(), last.GetObjectId
						(), id);
					all.Set(all.Size() - 1, last);
					continue;
				}
				int sp = p.IndexOf(' ');
				ObjectId id_1 = ObjectId.FromString(Sharpen.Runtime.Substring(p, 0, sp));
				string name = Copy(p, sp + 1, p.Length);
				ObjectIdRef cur;
				if (peeled)
				{
					cur = new ObjectIdRef.PeeledNonTag(RefStorage.PACKED, name, id_1);
				}
				else
				{
					cur = new ObjectIdRef.Unpeeled(RefStorage.PACKED, name, id_1);
				}
				if (last != null && RefComparator.CompareTo(last, cur) > 0)
				{
					needSort = true;
				}
				all.Add(cur);
				last = cur;
			}
			if (needSort)
			{
				all.Sort();
			}
			return all.ToRefList();
		}

		private static string Copy(string src, int off, int end)
		{
			// Don't use substring since it could leave a reference to the much
			// larger existing string. Force construction of a full new object.
			return new StringBuilder(end - off).AppendRange(src, off, end).ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CommitPackedRefs(LockFile lck, RefList<Ref> refs, RefDirectory.PackedRefList
			 oldPackedList)
		{
			new _RefWriter_782(this, lck, oldPackedList, refs, refs).WritePackedRefs();
		}

		private sealed class _RefWriter_782 : RefWriter
		{
			public _RefWriter_782(RefDirectory _enclosing, LockFile lck, RefDirectory.PackedRefList
				 oldPackedList, RefList<Ref> refs, RefList<Ref> baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.lck = lck;
				this.oldPackedList = oldPackedList;
				this.refs = refs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void WriteFile(string name, byte[] content)
			{
				lck.SetFSync(true);
				lck.SetNeedSnapshot(true);
				try
				{
					lck.Write(content);
				}
				catch (IOException ioe)
				{
					throw new ObjectWritingException(MessageFormat.Format(JGitText.Get().unableToWrite
						, name), ioe);
				}
				try
				{
					lck.WaitForStatChange();
				}
				catch (Exception)
				{
					lck.Unlock();
					throw new ObjectWritingException(MessageFormat.Format(JGitText.Get().interruptedWriting
						, name));
				}
				if (!lck.Commit())
				{
					throw new ObjectWritingException(MessageFormat.Format(JGitText.Get().unableToWrite
						, name));
				}
				this._enclosing.packedRefs.CompareAndSet(oldPackedList, new RefDirectory.PackedRefList
					(refs, lck.GetCommitSnapshot()));
			}

			private readonly RefDirectory _enclosing;

			private readonly LockFile lck;

			private readonly RefDirectory.PackedRefList oldPackedList;

			private readonly RefList<Ref> refs;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Ref ReadRef(string name, RefList<Ref> packed)
		{
			RefList<RefDirectory.LooseRef> curList = looseRefs.Get();
			int idx = curList.Find(name);
			if (0 <= idx)
			{
				RefDirectory.LooseRef o = curList.Get(idx);
				RefDirectory.LooseRef n = ScanRef(o, name);
				if (n == null)
				{
					if (looseRefs.CompareAndSet(curList, curList.Remove(idx)))
					{
						modCnt.IncrementAndGet();
					}
					return packed.Get(name);
				}
				if (o == n)
				{
					return n;
				}
				if (looseRefs.CompareAndSet(curList, curList.Set(idx, n)))
				{
					modCnt.IncrementAndGet();
				}
				return n;
			}
			RefDirectory.LooseRef n_1 = ScanRef(null, name);
			if (n_1 == null)
			{
				return packed.Get(name);
			}
			// check whether the found new ref is the an additional ref. These refs
			// should not go into looseRefs
			for (int i = 0; i < additionalRefsNames.Length; i++)
			{
				if (name.Equals(additionalRefsNames[i]))
				{
					return n_1;
				}
			}
			if (looseRefs.CompareAndSet(curList, curList.Add(idx, n_1)))
			{
				modCnt.IncrementAndGet();
			}
			return n_1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefDirectory.LooseRef ScanRef(RefDirectory.LooseRef @ref, string name)
		{
			FilePath path = FileFor(name);
			long modified = path.LastModified();
			if (@ref != null)
			{
				if (@ref.GetLastModified() == modified)
				{
					return @ref;
				}
				name = @ref.GetName();
			}
			else
			{
				if (modified == 0)
				{
					return null;
				}
			}
			int limit = 4096;
			byte[] buf;
			try
			{
				buf = IOUtil.ReadSome(path, limit);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			// doesn't exist; not a reference.
			int n = buf.Length;
			if (n == 0)
			{
				return null;
			}
			// empty file; not a reference.
			if (IsSymRef(buf, n))
			{
				if (n == limit)
				{
					return null;
				}
				// possibly truncated ref
				// trim trailing whitespace
				while (0 < n && char.IsWhiteSpace((char)buf[n - 1]))
				{
					n--;
				}
				if (n < 6)
				{
					string content = RawParseUtils.Decode(buf, 0, n);
					throw new IOException(MessageFormat.Format(JGitText.Get().notARef, name, content)
						);
				}
				string target = RawParseUtils.Decode(buf, 5, n);
				return NewSymbolicRef(modified, name, target);
			}
			if (n < Constants.OBJECT_ID_STRING_LENGTH)
			{
				return null;
			}
			// impossibly short object identifier; not a reference.
			ObjectId id;
			try
			{
				id = ObjectId.FromString(buf, 0);
			}
			catch (ArgumentException)
			{
				while (0 < n && char.IsWhiteSpace((char)buf[n - 1]))
				{
					n--;
				}
				string content = RawParseUtils.Decode(buf, 0, n);
				throw new IOException(MessageFormat.Format(JGitText.Get().notARef, name, content)
					);
			}
			return new RefDirectory.LooseUnpeeled(modified, name, id);
		}

		private static bool IsSymRef(byte[] buf, int n)
		{
			if (n < 6)
			{
				return false;
			}
			return buf[0] == 'r' && buf[1] == 'e' && buf[2] == 'f' && buf[3] == ':' && buf[4]
				 == ' ';
		}

		//
		//
		//
		//
		/// <summary>If the parent should fire listeners, fires them.</summary>
		/// <remarks>If the parent should fire listeners, fires them.</remarks>
		private void FireRefsChanged()
		{
			int last = lastNotifiedModCnt.Get();
			int curr = modCnt.Get();
			if (last != curr && lastNotifiedModCnt.CompareAndSet(last, curr) && last != 0)
			{
				parent.FireEvent(new RefsChangedEvent());
			}
		}

		/// <summary>Create a reference update to write a temporary reference.</summary>
		/// <remarks>Create a reference update to write a temporary reference.</remarks>
		/// <returns>an update for a new temporary reference.</returns>
		/// <exception cref="System.IO.IOException">a temporary name cannot be allocated.</exception>
		internal virtual RefDirectoryUpdate NewTemporaryUpdate()
		{
			FilePath tmp = FilePath.CreateTempFile("renamed_", "_ref", refsDir);
			string name = Constants.R_REFS + tmp.GetName();
			Ref @ref = new ObjectIdRef.Unpeeled(RefStorage.NEW, name, null);
			return new RefDirectoryUpdate(this, @ref);
		}

		/// <summary>Locate the file on disk for a single reference name.</summary>
		/// <remarks>Locate the file on disk for a single reference name.</remarks>
		/// <param name="name">
		/// name of the ref, relative to the Git repository top level
		/// directory (so typically starts with refs/).
		/// </param>
		/// <returns>the loose file location.</returns>
		internal virtual FilePath FileFor(string name)
		{
			if (name.StartsWith(Constants.R_REFS))
			{
				name = Sharpen.Runtime.Substring(name, Constants.R_REFS.Length);
				return new FilePath(refsDir, name);
			}
			return new FilePath(gitDir, name);
		}

		/// <summary>Locate the log file on disk for a single reference name.</summary>
		/// <remarks>Locate the log file on disk for a single reference name.</remarks>
		/// <param name="name">
		/// name of the ref, relative to the Git repository top level
		/// directory (so typically starts with refs/).
		/// </param>
		/// <returns>the log file location.</returns>
		internal virtual FilePath LogFor(string name)
		{
			if (name.StartsWith(Constants.R_REFS))
			{
				name = Sharpen.Runtime.Substring(name, Constants.R_REFS.Length);
				return new FilePath(logsRefsDir, name);
			}
			return new FilePath(logsDir, name);
		}

		internal static int LevelsIn(string name)
		{
			int count = 0;
			for (int p = name.IndexOf('/'); p >= 0; p = name.IndexOf('/', p + 1))
			{
				count++;
			}
			return count;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void Delete(FilePath file, int depth)
		{
			if (!file.Delete() && file.IsFile())
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().fileCannotBeDeleted, file
					));
			}
			FilePath dir = file.GetParentFile();
			for (int i = 0; i < depth; ++i)
			{
				if (!dir.Delete())
				{
					break;
				}
				// ignore problem here
				dir = dir.GetParentFile();
			}
		}

		private class PackedRefList : RefList<Ref>
		{
			internal static readonly RefDirectory.PackedRefList NO_PACKED_REFS = new RefDirectory.PackedRefList
				(RefList.EmptyList(), FileSnapshot.MISSING_FILE);

			internal readonly FileSnapshot snapshot;

			internal PackedRefList(RefList<Ref> src, FileSnapshot s) : base(src)
			{
				snapshot = s;
			}
		}

		private static RefDirectory.LooseSymbolicRef NewSymbolicRef(long lastModified, string
			 name, string target)
		{
			Ref dst = new ObjectIdRef.Unpeeled(RefStorage.NEW, target, null);
			return new RefDirectory.LooseSymbolicRef(lastModified, name, dst);
		}

		private interface LooseRef : Ref
		{
			long GetLastModified();

			RefDirectory.LooseRef Peel(ObjectIdRef newLeaf);
		}

		private sealed class LoosePeeledTag : ObjectIdRef.PeeledTag, RefDirectory.LooseRef
		{
			private readonly long lastModified;

			internal LoosePeeledTag(long mtime, string refName, ObjectId id, ObjectId p) : base
				(RefStorage.LOOSE, refName, id, p)
			{
				this.lastModified = mtime;
			}

			public long GetLastModified()
			{
				return lastModified;
			}

			public RefDirectory.LooseRef Peel(ObjectIdRef newLeaf)
			{
				return this;
			}
		}

		private sealed class LooseNonTag : ObjectIdRef.PeeledNonTag, RefDirectory.LooseRef
		{
			private readonly long lastModified;

			internal LooseNonTag(long mtime, string refName, ObjectId id) : base(RefStorage.LOOSE
				, refName, id)
			{
				this.lastModified = mtime;
			}

			public long GetLastModified()
			{
				return lastModified;
			}

			public RefDirectory.LooseRef Peel(ObjectIdRef newLeaf)
			{
				return this;
			}
		}

		private sealed class LooseUnpeeled : ObjectIdRef.Unpeeled, RefDirectory.LooseRef
		{
			private readonly long lastModified;

			internal LooseUnpeeled(long mtime, string refName, ObjectId id) : base(RefStorage
				.LOOSE, refName, id)
			{
				this.lastModified = mtime;
			}

			public long GetLastModified()
			{
				return lastModified;
			}

			public RefDirectory.LooseRef Peel(ObjectIdRef newLeaf)
			{
				if (newLeaf.GetPeeledObjectId() != null)
				{
					return new RefDirectory.LoosePeeledTag(lastModified, GetName(), GetObjectId(), newLeaf
						.GetPeeledObjectId());
				}
				else
				{
					return new RefDirectory.LooseNonTag(lastModified, GetName(), GetObjectId());
				}
			}
		}

		private sealed class LooseSymbolicRef : SymbolicRef, RefDirectory.LooseRef
		{
			private readonly long lastModified;

			internal LooseSymbolicRef(long mtime, string refName, Ref target) : base(refName, 
				target)
			{
				this.lastModified = mtime;
			}

			public long GetLastModified()
			{
				return lastModified;
			}

			public RefDirectory.LooseRef Peel(ObjectIdRef newLeaf)
			{
				// We should never try to peel the symbolic references.
				throw new NotSupportedException();
			}
		}
	}
}
