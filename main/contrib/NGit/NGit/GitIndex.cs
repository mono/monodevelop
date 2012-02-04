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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Events;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>A representation of the Git index.</summary>
	/// <remarks>
	/// A representation of the Git index.
	/// The index points to the objects currently checked out or in the process of
	/// being prepared for committing or objects involved in an unfinished merge.
	/// The abstract format is:<br/> path stage flags statdata SHA-1
	/// <ul>
	/// <li>Path is the relative path in the workdir</li>
	/// <li>stage is 0 (normally), but when
	/// merging 1 is the common ancestor version, 2 is 'our' version and 3 is 'their'
	/// version. A fully resolved merge only contains stage 0.</li>
	/// <li>flags is the object type and information of validity</li>
	/// <li>statdata is the size of this object and some other file system specifics,
	/// some of it ignored by JGit</li>
	/// <li>SHA-1 represents the content of the references object</li>
	/// </ul>
	/// An index can also contain a tree cache which we ignore for now. We drop the
	/// tree cache when writing the index.
	/// </remarks>
	[System.ObsoleteAttribute(@"Use NGit.Dircache.DirCache instead.")]
	public class GitIndex
	{
		/// <summary>Stage 0 represents merged entries.</summary>
		/// <remarks>Stage 0 represents merged entries.</remarks>
		public const int STAGE_0 = 0;

		private RandomAccessFile cache;

		private FilePath cacheFile;

		private bool changed;

		private bool statDirty;

		private GitIndex.Header header;

		private long lastCacheTime;

		private readonly Repository db;

		private sealed class _IComparer_122 : IComparer<byte[]>
		{
			public _IComparer_122()
			{
			}

			// Index is modified
			// Stat information updated
			public int Compare(byte[] o1, byte[] o2)
			{
				for (int i = 0; i < o1.Length && i < o2.Length; ++i)
				{
					int c = (o1[i] & unchecked((int)(0xff))) - (o2[i] & unchecked((int)(0xff)));
					if (c != 0)
					{
						return c;
					}
				}
				if (o1.Length < o2.Length)
				{
					return -1;
				}
				else
				{
					if (o1.Length > o2.Length)
					{
						return 1;
					}
				}
				return 0;
			}
		}

		private IDictionary<byte[], GitIndex.Entry> entries = new SortedDictionary<byte[]
			, GitIndex.Entry>(new _IComparer_122());

		/// <summary>Construct a Git index representation.</summary>
		/// <remarks>Construct a Git index representation.</remarks>
		/// <param name="db"></param>
		public GitIndex(Repository db)
		{
			this.db = db;
			this.cacheFile = db.GetIndexFile();
		}

		/// <returns>true if we have modified the index in memory since reading it from disk</returns>
		public virtual bool IsChanged()
		{
			return changed || statDirty;
		}

		/// <summary>Reread index data from disk if the index file has been changed</summary>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void RereadIfNecessary()
		{
			if (cacheFile.Exists() && cacheFile.LastModified() != lastCacheTime)
			{
				Read();
				db.FireEvent(new IndexChangedEvent());
			}
		}

		/// <summary>Add the content of a file to the index.</summary>
		/// <remarks>Add the content of a file to the index.</remarks>
		/// <param name="wd">workdir</param>
		/// <param name="f">the file</param>
		/// <returns>a new or updated index entry for the path represented by f</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual GitIndex.Entry Add(FilePath wd, FilePath f)
		{
			byte[] key = MakeKey(wd, f);
			GitIndex.Entry e = entries.Get(key);
			if (e == null)
			{
				e = new GitIndex.Entry(this, key, f, 0);
				entries.Put(key, e);
			}
			else
			{
				e.Update(f);
			}
			return e;
		}

		/// <summary>Add the content of a file to the index.</summary>
		/// <remarks>Add the content of a file to the index.</remarks>
		/// <param name="wd">workdir</param>
		/// <param name="f">the file</param>
		/// <param name="content">content of the file</param>
		/// <returns>a new or updated index entry for the path represented by f</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual GitIndex.Entry Add(FilePath wd, FilePath f, byte[] content)
		{
			byte[] key = MakeKey(wd, f);
			GitIndex.Entry e = entries.Get(key);
			if (e == null)
			{
				e = new GitIndex.Entry(this, key, f, 0, content);
				entries.Put(key, e);
			}
			else
			{
				e.Update(f, content);
			}
			return e;
		}

		/// <summary>Remove a path from the index.</summary>
		/// <remarks>Remove a path from the index.</remarks>
		/// <param name="wd">workdir</param>
		/// <param name="f">the file whose path shall be removed.</param>
		/// <returns>true if such a path was found (and thus removed)</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool Remove(FilePath wd, FilePath f)
		{
			byte[] key = MakeKey(wd, f);
			return Sharpen.Collections.Remove(entries, key) != null;
		}

		/// <summary>Read the cache file into memory.</summary>
		/// <remarks>Read the cache file into memory.</remarks>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Read()
		{
			changed = false;
			statDirty = false;
			if (!cacheFile.Exists())
			{
				header = null;
				entries.Clear();
				lastCacheTime = 0;
				return;
			}
			cache = new RandomAccessFile(cacheFile, "r");
			try
			{
				FileChannel channel = cache.GetChannel();
				ByteBuffer buffer = ByteBuffer.AllocateDirect((int)cacheFile.Length());
				buffer.Order(ByteOrder.BIG_ENDIAN);
				int j = channel.Read(buffer);
				if (j != buffer.Capacity())
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().couldNotReadIndexInOneGo
						, j, buffer.Capacity()));
				}
				buffer.Flip();
				header = new GitIndex.Header(buffer);
				entries.Clear();
				for (int i = 0; i < header.entries; ++i)
				{
					GitIndex.Entry entry = new GitIndex.Entry(this, buffer);
					GitIndex.Entry existing = entries.Get(entry.name);
					entries.Put(entry.name, entry);
					if (existing != null)
					{
						entry.stages |= existing.stages;
					}
				}
				lastCacheTime = cacheFile.LastModified();
			}
			finally
			{
				cache.Close();
			}
		}

		/// <summary>Write content of index to disk.</summary>
		/// <remarks>Write content of index to disk.</remarks>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Write()
		{
			CheckWriteOk();
			FilePath tmpIndex = new FilePath(cacheFile.GetAbsoluteFile() + ".tmp");
			FilePath Lock = new FilePath(cacheFile.GetAbsoluteFile() + ".lock");
			if (!Lock.CreateNewFile())
			{
				throw new IOException(JGitText.Get().indexFileIsInUse);
			}
			try
			{
				FileOutputStream fileOutputStream = new FileOutputStream(tmpIndex);
				FileChannel fc = fileOutputStream.GetChannel();
				ByteBuffer buf = ByteBuffer.Allocate(4096);
				MessageDigest newMessageDigest = Constants.NewMessageDigest();
				header = new GitIndex.Header(entries);
				header.Write(buf);
				buf.Flip();
				newMessageDigest.Update(((byte[])buf.Array()), buf.ArrayOffset(), buf.Limit());
				fc.Write(buf);
				buf.Flip();
				buf.Clear();
				for (Iterator i = entries.Values.Iterator(); i.HasNext(); )
				{
					GitIndex.Entry e = (GitIndex.Entry)i.Next();
					e.Write(buf);
					buf.Flip();
					newMessageDigest.Update(((byte[])buf.Array()), buf.ArrayOffset(), buf.Limit());
					fc.Write(buf);
					buf.Flip();
					buf.Clear();
				}
				buf.Put(newMessageDigest.Digest());
				buf.Flip();
				fc.Write(buf);
				fc.Close();
				fileOutputStream.Close();
				if (cacheFile.Exists())
				{
					if (db.FileSystem.RetryFailedLockFileCommit())
					{
						// file deletion fails on windows if another
						// thread is reading the file concurrently
						// So let's try 10 times...
						bool deleted = false;
						for (int i_1 = 0; i_1 < 10; i_1++)
						{
							if (cacheFile.Delete())
							{
								deleted = true;
								break;
							}
							try
							{
								Sharpen.Thread.Sleep(100);
							}
							catch (Exception)
							{
							}
						}
						// ignore
						if (!deleted)
						{
							throw new IOException(JGitText.Get().couldNotRenameDeleteOldIndex);
						}
					}
					else
					{
						if (!cacheFile.Delete())
						{
							throw new IOException(JGitText.Get().couldNotRenameDeleteOldIndex);
						}
					}
				}
				if (!tmpIndex.RenameTo(cacheFile))
				{
					throw new IOException(JGitText.Get().couldNotRenameTemporaryIndexFileToIndex);
				}
				changed = false;
				statDirty = false;
				lastCacheTime = cacheFile.LastModified();
				db.FireEvent(new IndexChangedEvent());
			}
			finally
			{
				if (!Lock.Delete())
				{
					throw new IOException(JGitText.Get().couldNotDeleteLockFileShouldNotHappen);
				}
				if (tmpIndex.Exists() && !tmpIndex.Delete())
				{
					throw new IOException(JGitText.Get().couldNotDeleteTemporaryIndexFileShouldNotHappen
						);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckWriteOk()
		{
			for (Iterator i = entries.Values.Iterator(); i.HasNext(); )
			{
				GitIndex.Entry e = (GitIndex.Entry)i.Next();
				if (e.GetStage() != 0)
				{
					throw new NGit.Errors.NotSupportedException(JGitText.Get().cannotWorkWithOtherStagesThanZeroRightNow
						);
				}
			}
		}

		private bool File_canExecute(FilePath f)
		{
			return db.FileSystem.CanExecute(f);
		}

		private bool File_setExecute(FilePath f, bool value)
		{
			return db.FileSystem.SetExecute(f, value);
		}

		private bool File_hasExecute()
		{
			return db.FileSystem.SupportsExecute();
		}

		internal static byte[] MakeKey(FilePath wd, FilePath f)
		{
			if (!f.GetPath().StartsWith(wd.GetPath()))
			{
				throw new Error(JGitText.Get().pathIsNotInWorkingDir);
			}
			string relName = Repository.StripWorkDir(wd, f);
			return Constants.Encode(relName);
		}

		internal bool filemode;

		private bool Config_filemode()
		{
			// temporary til we can actually set parameters. We need to be able
			// to change this for testing.
			if (filemode != null)
			{
				return filemode;
			}
			Config config = db.GetConfig();
			filemode = Sharpen.Extensions.ValueOf(config.GetBoolean("core", null, "filemode", 
				true));
			return filemode;
		}

		/// <summary>An index entry</summary>
		[System.ObsoleteAttribute(@"Use NGit.Dircache.DirCacheEntry .")]
		public class Entry
		{
			internal long ctime;

			internal long mtime;

			private int dev;

			private int ino;

			internal int mode;

			private int uid;

			private int gid;

			private int size;

			internal ObjectId sha1;

			private short flags;

			internal byte[] name;

			internal int stages;

			/// <exception cref="System.IO.IOException"></exception>
			internal Entry(GitIndex _enclosing, byte[] key, FilePath f, int stage)
			{
				this._enclosing = _enclosing;
				this.ctime = f.LastModified() * 1000000L;
				this.mtime = this.ctime;
				// we use same here
				this.dev = -1;
				this.ino = -1;
				if (this._enclosing.Config_filemode() && this._enclosing.File_canExecute(f))
				{
					this.mode = FileMode.EXECUTABLE_FILE.GetBits();
				}
				else
				{
					this.mode = FileMode.REGULAR_FILE.GetBits();
				}
				this.uid = -1;
				this.gid = -1;
				this.size = (int)f.Length();
				ObjectInserter inserter = this._enclosing.db.NewObjectInserter();
				try
				{
					InputStream @in = new FileInputStream(f);
					try
					{
						this.sha1 = inserter.Insert(Constants.OBJ_BLOB, f.Length(), @in);
					}
					finally
					{
						@in.Close();
					}
					inserter.Flush();
				}
				finally
				{
					inserter.Release();
				}
				this.name = key;
				this.flags = (short)((stage << 12) | this.name.Length);
				// TODO: fix flags
				this.stages = (1 >> this.GetStage());
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal Entry(GitIndex _enclosing, byte[] key, FilePath f, int stage, byte[] newContent
				)
			{
				this._enclosing = _enclosing;
				this.ctime = f.LastModified() * 1000000L;
				this.mtime = this.ctime;
				// we use same here
				this.dev = -1;
				this.ino = -1;
				if (this._enclosing.Config_filemode() && this._enclosing.File_canExecute(f))
				{
					this.mode = FileMode.EXECUTABLE_FILE.GetBits();
				}
				else
				{
					this.mode = FileMode.REGULAR_FILE.GetBits();
				}
				this.uid = -1;
				this.gid = -1;
				this.size = newContent.Length;
				ObjectInserter inserter = this._enclosing.db.NewObjectInserter();
				try
				{
					InputStream @in = new FileInputStream(f);
					try
					{
						this.sha1 = inserter.Insert(Constants.OBJ_BLOB, newContent);
					}
					finally
					{
						@in.Close();
					}
					inserter.Flush();
				}
				finally
				{
					inserter.Release();
				}
				this.name = key;
				this.flags = (short)((stage << 12) | this.name.Length);
				// TODO: fix flags
				this.stages = (1 >> this.GetStage());
			}

			internal Entry(GitIndex _enclosing, TreeEntry f, int stage)
			{
				this._enclosing = _enclosing;
				this.ctime = -1;
				// hmm
				this.mtime = -1;
				this.dev = -1;
				this.ino = -1;
				this.mode = f.GetMode().GetBits();
				this.uid = -1;
				this.gid = -1;
				try
				{
					this.size = (int)this._enclosing.db.Open(f.GetId(), Constants.OBJ_BLOB).GetSize();
				}
				catch (IOException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					this.size = -1;
				}
				this.sha1 = f.GetId();
				this.name = Constants.Encode(f.GetFullName());
				this.flags = (short)((stage << 12) | this.name.Length);
				// TODO: fix flags
				this.stages = (1 >> this.GetStage());
			}

			internal Entry(GitIndex _enclosing, ByteBuffer b)
			{
				this._enclosing = _enclosing;
				int startposition = b.Position();
				this.ctime = b.GetInt() * 1000000000L + (b.GetInt() % 1000000000L);
				this.mtime = b.GetInt() * 1000000000L + (b.GetInt() % 1000000000L);
				this.dev = b.GetInt();
				this.ino = b.GetInt();
				this.mode = b.GetInt();
				this.uid = b.GetInt();
				this.gid = b.GetInt();
				this.size = b.GetInt();
				byte[] sha1bytes = new byte[Constants.OBJECT_ID_LENGTH];
				b.Get(sha1bytes);
				this.sha1 = ObjectId.FromRaw(sha1bytes);
				this.flags = b.GetShort();
				this.stages = (1 << this.GetStage());
				this.name = new byte[this.flags & unchecked((int)(0xFFF))];
				b.Get(this.name);
				b.Position(startposition + ((8 + 8 + 4 + 4 + 4 + 4 + 4 + 4 + 20 + 2 + this.name.Length
					 + 8) & ~7));
			}

			/// <summary>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </summary>
			/// <remarks>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </remarks>
			/// <param name="f">file in work dir</param>
			/// <returns>true if a change occurred</returns>
			/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
			public virtual bool Update(FilePath f)
			{
				long lm = f.LastModified() * 1000000L;
				bool modified = this.mtime != lm;
				this.mtime = lm;
				if (this.size != f.Length())
				{
					modified = true;
				}
				if (this._enclosing.Config_filemode())
				{
					if (this._enclosing.File_canExecute(f) != FileMode.EXECUTABLE_FILE.Equals(this.mode
						))
					{
						this.mode = FileMode.EXECUTABLE_FILE.GetBits();
						modified = true;
					}
				}
				if (modified)
				{
					this.size = (int)f.Length();
					ObjectInserter oi = this._enclosing.db.NewObjectInserter();
					try
					{
						InputStream @in = new FileInputStream(f);
						try
						{
							ObjectId newsha1 = oi.Insert(Constants.OBJ_BLOB, f.Length(), @in);
							oi.Flush();
							if (!newsha1.Equals(this.sha1))
							{
								modified = true;
							}
							this.sha1 = newsha1;
						}
						finally
						{
							@in.Close();
						}
					}
					finally
					{
						oi.Release();
					}
				}
				return modified;
			}

			/// <summary>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </summary>
			/// <remarks>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </remarks>
			/// <param name="f">file in work dir</param>
			/// <param name="newContent">the new content of the file</param>
			/// <returns>true if a change occurred</returns>
			/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
			public virtual bool Update(FilePath f, byte[] newContent)
			{
				bool modified = false;
				this.size = newContent.Length;
				ObjectInserter oi = this._enclosing.db.NewObjectInserter();
				try
				{
					ObjectId newsha1 = oi.Insert(Constants.OBJ_BLOB, newContent);
					oi.Flush();
					if (!newsha1.Equals(this.sha1))
					{
						modified = true;
					}
					this.sha1 = newsha1;
				}
				finally
				{
					oi.Release();
				}
				return modified;
			}

			internal virtual void Write(ByteBuffer buf)
			{
				int startposition = buf.Position();
				buf.PutInt((int)(this.ctime / 1000000000L));
				buf.PutInt((int)(this.ctime % 1000000000L));
				buf.PutInt((int)(this.mtime / 1000000000L));
				buf.PutInt((int)(this.mtime % 1000000000L));
				buf.PutInt(this.dev);
				buf.PutInt(this.ino);
				buf.PutInt(this.mode);
				buf.PutInt(this.uid);
				buf.PutInt(this.gid);
				buf.PutInt(this.size);
				this.sha1.CopyRawTo(buf);
				buf.PutShort(this.flags);
				buf.Put(this.name);
				int end = startposition + ((8 + 8 + 4 + 4 + 4 + 4 + 4 + 4 + 20 + 2 + this.name.Length
					 + 8) & ~7);
				int remain = end - buf.Position();
				while (remain-- > 0)
				{
					buf.Put(unchecked((byte)0));
				}
			}

			/// <summary>
			/// Check if an entry's content is different from the cache,
			/// File status information is used and status is same we
			/// consider the file identical to the state in the working
			/// directory.
			/// </summary>
			/// <remarks>
			/// Check if an entry's content is different from the cache,
			/// File status information is used and status is same we
			/// consider the file identical to the state in the working
			/// directory. Native git uses more stat fields than we
			/// have accessible in Java.
			/// </remarks>
			/// <param name="wd">working directory to compare content with</param>
			/// <returns>true if content is most likely different.</returns>
			public virtual bool IsModified(FilePath wd)
			{
				return this.IsModified(wd, false);
			}

			/// <summary>
			/// Check if an entry's content is different from the cache,
			/// File status information is used and status is same we
			/// consider the file identical to the state in the working
			/// directory.
			/// </summary>
			/// <remarks>
			/// Check if an entry's content is different from the cache,
			/// File status information is used and status is same we
			/// consider the file identical to the state in the working
			/// directory. Native git uses more stat fields than we
			/// have accessible in Java.
			/// </remarks>
			/// <param name="wd">working directory to compare content with</param>
			/// <param name="forceContentCheck">
			/// True if the actual file content
			/// should be checked if modification time differs.
			/// </param>
			/// <returns>true if content is most likely different.</returns>
			public virtual bool IsModified(FilePath wd, bool forceContentCheck)
			{
				if (this.IsAssumedValid())
				{
					return false;
				}
				if (this.IsUpdateNeeded())
				{
					return true;
				}
				FilePath file = this.GetFile(wd);
				long length = file.Length();
				if (length == 0)
				{
					if (!file.Exists())
					{
						return true;
					}
				}
				if (length != this.size)
				{
					return true;
				}
				// JDK1.6 has file.canExecute
				// if (file.canExecute() != FileMode.EXECUTABLE_FILE.equals(mode))
				// return true;
				int exebits = FileMode.EXECUTABLE_FILE.GetBits() ^ FileMode.REGULAR_FILE.GetBits(
					);
				if (this._enclosing.Config_filemode() && FileMode.EXECUTABLE_FILE.Equals(this.mode
					))
				{
					if (!this._enclosing.File_canExecute(file) && this._enclosing.File_hasExecute())
					{
						return true;
					}
				}
				else
				{
					if (FileMode.REGULAR_FILE.Equals(this.mode & ~exebits))
					{
						if (!file.IsFile())
						{
							return true;
						}
						if (this._enclosing.Config_filemode() && this._enclosing.File_canExecute(file) &&
							 this._enclosing.File_hasExecute())
						{
							return true;
						}
					}
					else
					{
						if (FileMode.SYMLINK.Equals(this.mode))
						{
							return true;
						}
						else
						{
							if (FileMode.TREE.Equals(this.mode))
							{
								if (!file.IsDirectory())
								{
									return true;
								}
							}
							else
							{
								System.Console.Out.WriteLine(MessageFormat.Format(JGitText.Get().doesNotHandleMode
									, this.mode, file));
								return true;
							}
						}
					}
				}
				// Git under windows only stores seconds so we round the timestamp
				// Java gives us if it looks like the timestamp in index is seconds
				// only. Otherwise we compare the timestamp at millisecond prevision.
				long javamtime = this.mtime / 1000000L;
				long lastm = file.LastModified();
				if (javamtime % 1000 == 0)
				{
					lastm = lastm - lastm % 1000;
				}
				if (lastm != javamtime)
				{
					if (!forceContentCheck)
					{
						return true;
					}
					try
					{
						InputStream @is = new FileInputStream(file);
						try
						{
							ObjectId newId = new ObjectInserter.Formatter().IdFor(Constants.OBJ_BLOB, file.Length
								(), @is);
							return !newId.Equals(this.sha1);
						}
						catch (IOException e)
						{
							Sharpen.Runtime.PrintStackTrace(e);
						}
						finally
						{
							try
							{
								@is.Close();
							}
							catch (IOException e)
							{
								// can't happen, but if it does we ignore it
								Sharpen.Runtime.PrintStackTrace(e);
							}
						}
					}
					catch (FileNotFoundException e)
					{
						// should not happen because we already checked this
						Sharpen.Runtime.PrintStackTrace(e);
						throw new Error(e);
					}
				}
				return false;
			}

			/// <summary>Returns the stages in which the entry's file is recorded in the index.</summary>
			/// <remarks>
			/// Returns the stages in which the entry's file is recorded in the index.
			/// The stages are bit-encoded: bit N is set if the file is present
			/// in stage N. In particular, the N-th bit will be set if this entry
			/// itself is in stage N (see getStage()).
			/// </remarks>
			/// <returns>flags denoting stages</returns>
			/// <seealso cref="GetStage()">GetStage()</seealso>
			public virtual int GetStages()
			{
				return this.stages;
			}

			// for testing
			internal virtual void ForceRecheck()
			{
				this.mtime = -1;
			}

			private FilePath GetFile(FilePath wd)
			{
				return new FilePath(wd, this.GetName());
			}

			public override string ToString()
			{
				return this.GetName() + "/SHA-1(" + this.sha1.Name + ")/M:" + Sharpen.Extensions.CreateDate
					(this.ctime / 1000000L) + "/C:" + Sharpen.Extensions.CreateDate(this.mtime / 1000000L
					) + "/d" + this.dev + "/i" + this.ino + "/m" + Sharpen.Extensions.ToString(this.
					mode, 8) + "/u" + this.uid + "/g" + this.gid + "/s" + this.size + "/f" + this.flags
					 + "/@" + this.GetStage();
			}

			/// <returns>path name for this entry</returns>
			public virtual string GetName()
			{
				return RawParseUtils.Decode(this.name);
			}

			/// <returns>path name for this entry as byte array, hopefully UTF-8 encoded</returns>
			public virtual byte[] GetNameUTF8()
			{
				return this.name;
			}

			/// <returns>SHA-1 of the entry managed by this index</returns>
			public virtual ObjectId GetObjectId()
			{
				return this.sha1;
			}

			/// <returns>the stage this entry is in</returns>
			public virtual int GetStage()
			{
				return (this.flags & unchecked((int)(0x3000))) >> 12;
			}

			/// <returns>size of disk object</returns>
			public virtual int GetSize()
			{
				return this.size;
			}

			/// <returns>true if this entry shall be assumed valid</returns>
			public virtual bool IsAssumedValid()
			{
				return (this.flags & unchecked((int)(0x8000))) != 0;
			}

			/// <returns>true if this entry should be checked for changes</returns>
			public virtual bool IsUpdateNeeded()
			{
				return (this.flags & unchecked((int)(0x4000))) != 0;
			}

			/// <summary>Set whether to always assume this entry valid</summary>
			/// <param name="assumeValid">true to ignore changes</param>
			public virtual void SetAssumeValid(bool assumeValid)
			{
				if (assumeValid)
				{
					this.flags |= unchecked((short)(0x8000));
				}
				else
				{
					this.flags &= ~unchecked((short)(0x8000));
				}
			}

			/// <summary>Set whether this entry must be checked</summary>
			/// <param name="updateNeeded"></param>
			public virtual void SetUpdateNeeded(bool updateNeeded)
			{
				if (updateNeeded)
				{
					this.flags |= unchecked((int)(0x4000));
				}
				else
				{
					this.flags &= ~unchecked((int)(0x4000));
				}
			}

			/// <summary>Return raw file mode bits.</summary>
			/// <remarks>
			/// Return raw file mode bits. See
			/// <see cref="FileMode">FileMode</see>
			/// </remarks>
			/// <returns>file mode bits</returns>
			public virtual int GetModeBits()
			{
				return this.mode;
			}

			private readonly GitIndex _enclosing;
		}

		internal class Header
		{
			private int signature;

			private int version;

			internal int entries;

			/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
			internal Header(ByteBuffer map)
			{
				Read(map);
			}

			/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
			private void Read(ByteBuffer buf)
			{
				signature = buf.GetInt();
				version = buf.GetInt();
				entries = buf.GetInt();
				if (signature != unchecked((int)(0x44495243)))
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().indexSignatureIsInvalid
						, signature));
				}
				if (version != 2)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().unknownIndexVersionOrCorruptIndex
						, version));
				}
			}

			internal virtual void Write(ByteBuffer buf)
			{
				buf.Order(ByteOrder.BIG_ENDIAN);
				buf.PutInt(signature);
				buf.PutInt(version);
				buf.PutInt(entries);
			}

			internal Header(IDictionary<byte[],GitIndex.Entry> entryset)
			{
				signature = unchecked((int)(0x44495243));
				version = 2;
				entries = entryset.Count;
			}
		}

		/// <summary>Read a Tree recursively into the index</summary>
		/// <param name="t">The tree to read</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void ReadTree(Tree t)
		{
			entries.Clear();
			ReadTree(string.Empty, t);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void ReadTree(string prefix, Tree t)
		{
			TreeEntry[] members = t.Members();
			for (int i = 0; i < members.Length; ++i)
			{
				TreeEntry te = members[i];
				string name;
				if (prefix.Length > 0)
				{
					name = prefix + "/" + te.GetName();
				}
				else
				{
					name = te.GetName();
				}
				if (te is Tree)
				{
					ReadTree(name, (Tree)te);
				}
				else
				{
					GitIndex.Entry e = new GitIndex.Entry(this, te, 0);
					entries.Put(Constants.Encode(name), e);
				}
			}
		}

		/// <summary>Add tree entry to index</summary>
		/// <param name="te">tree entry</param>
		/// <returns>new or modified index entry</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual GitIndex.Entry AddEntry(TreeEntry te)
		{
			byte[] key = Constants.Encode(te.GetFullName());
			GitIndex.Entry e = new GitIndex.Entry(this, te, 0);
			entries.Put(key, e);
			return e;
		}

		/// <summary>Check out content of the content represented by the index</summary>
		/// <param name="wd">workdir</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Checkout(FilePath wd)
		{
			foreach (GitIndex.Entry e in entries.Values)
			{
				if (e.GetStage() != 0)
				{
					continue;
				}
				CheckoutEntry(wd, e);
			}
		}

		/// <summary>Check out content of the specified index entry</summary>
		/// <param name="wd">workdir</param>
		/// <param name="e">index entry</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void CheckoutEntry(FilePath wd, GitIndex.Entry e)
		{
			ObjectLoader ol = db.Open(e.sha1, Constants.OBJ_BLOB);
			FilePath file = new FilePath(wd, e.GetName());
			file.Delete();
			FileUtils.Mkdirs(file.GetParentFile(), true);
			FileOutputStream dst = new FileOutputStream(file);
			try
			{
				ol.CopyTo(dst);
			}
			finally
			{
				dst.Close();
			}
			if (Config_filemode() && File_hasExecute())
			{
				if (FileMode.EXECUTABLE_FILE.Equals(e.mode))
				{
					if (!File_canExecute(file))
					{
						File_setExecute(file, true);
					}
				}
				else
				{
					if (File_canExecute(file))
					{
						File_setExecute(file, false);
					}
				}
			}
			e.mtime = file.LastModified() * 1000000L;
			e.ctime = e.mtime;
		}

		/// <summary>Construct and write tree out of index.</summary>
		/// <remarks>Construct and write tree out of index.</remarks>
		/// <returns>SHA-1 of the constructed tree</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual ObjectId WriteTree()
		{
			CheckWriteOk();
			ObjectInserter inserter = db.NewObjectInserter();
			try
			{
				Tree current = new Tree(db);
				Stack<Tree> trees = new Stack<Tree>();
				trees.Push(current);
				string[] prevName = new string[0];
				foreach (GitIndex.Entry e in entries.Values)
				{
					if (e.GetStage() != 0)
					{
						continue;
					}
					string[] newName = SplitDirPath(e.GetName());
					int c = LongestCommonPath(prevName, newName);
					while (c < trees.Count - 1)
					{
						current.SetId(inserter.Insert(Constants.OBJ_TREE, current.Format()));
						trees.Pop();
						current = trees.IsEmpty() ? null : (Tree)trees.Peek();
					}
					while (trees.Count < newName.Length)
					{
						if (!current.ExistsTree(newName[trees.Count - 1]))
						{
							current = new Tree(current, Constants.Encode(newName[trees.Count - 1]));
							current.GetParent().AddEntry(current);
							trees.Push(current);
						}
						else
						{
							current = (Tree)current.FindTreeMember(newName[trees.Count - 1]);
							trees.Push(current);
						}
					}
					FileTreeEntry ne = new FileTreeEntry(current, e.sha1, Constants.Encode(newName[newName
						.Length - 1]), (e.mode & FileMode.EXECUTABLE_FILE.GetBits()) == FileMode.EXECUTABLE_FILE
						.GetBits());
					current.AddEntry(ne);
				}
				while (!trees.IsEmpty())
				{
					current.SetId(inserter.Insert(Constants.OBJ_TREE, current.Format()));
					trees.Pop();
					if (!trees.IsEmpty())
					{
						current = trees.Peek();
					}
				}
				inserter.Flush();
				return current.GetId();
			}
			finally
			{
				inserter.Release();
			}
		}

		internal virtual string[] SplitDirPath(string name)
		{
			string[] tmp = new string[name.Length / 2 + 1];
			int p0 = -1;
			int p1;
			int c = 0;
			while ((p1 = name.IndexOf('/', p0 + 1)) != -1)
			{
				tmp[c++] = Sharpen.Runtime.Substring(name, p0 + 1, p1);
				p0 = p1;
			}
			tmp[c++] = Sharpen.Runtime.Substring(name, p0 + 1);
			string[] ret = new string[c];
			for (int i = 0; i < c; ++i)
			{
				ret[i] = tmp[i];
			}
			return ret;
		}

		internal virtual int LongestCommonPath(string[] a, string[] b)
		{
			int i;
			for (i = 0; i < a.Length && i < b.Length; ++i)
			{
				if (!a[i].Equals(b[i]))
				{
					return i;
				}
			}
			return i;
		}

		/// <summary>
		/// Return the members of the index sorted by the unsigned byte
		/// values of the path names.
		/// </summary>
		/// <remarks>
		/// Return the members of the index sorted by the unsigned byte
		/// values of the path names.
		/// Small beware: Unaccounted for are unmerged entries. You may want
		/// to abort if members with stage != 0 are found if you are doing
		/// any updating operations. All stages will be found after one another
		/// here later. Currently only one stage per name is returned.
		/// </remarks>
		/// <returns>The index entries sorted</returns>
		public virtual GitIndex.Entry[] GetMembers()
		{
			return Sharpen.Collections.ToArray(entries.Values, new GitIndex.Entry[entries.Count
				]);
		}

		/// <summary>Look up an entry with the specified path.</summary>
		/// <remarks>Look up an entry with the specified path.</remarks>
		/// <param name="path"></param>
		/// <returns>index entry for the path or null if not in index.</returns>
		/// <exception cref="Sharpen.UnsupportedEncodingException">Sharpen.UnsupportedEncodingException
		/// 	</exception>
		public virtual GitIndex.Entry GetEntry(string path)
		{
			return entries.Get(Repository.GitInternalSlash(Constants.Encode(path)));
		}

		/// <returns>The repository holding this index.</returns>
		public virtual Repository GetRepository()
		{
			return db;
		}
	}
}
