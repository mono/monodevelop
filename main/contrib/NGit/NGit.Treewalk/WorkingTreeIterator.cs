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
using NGit.Diff;
using NGit.Dircache;
using NGit.Errors;
using NGit.Ignore;
using NGit.Submodule;
using NGit.Treewalk;
using NGit.Util;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>
	/// Walks a working directory tree as part of a
	/// <see cref="TreeWalk">TreeWalk</see>
	/// .
	/// <p>
	/// Most applications will want to use the standard implementation of this
	/// iterator,
	/// <see cref="FileTreeIterator">FileTreeIterator</see>
	/// , as that does all IO through the standard
	/// <code>java.io</code> package. Plugins for a Java based IDE may however wish
	/// to create their own implementations of this class to allow traversal of the
	/// IDE's project space, as well as benefit from any caching the IDE may have.
	/// </summary>
	/// <seealso cref="FileTreeIterator">FileTreeIterator</seealso>
	public abstract class WorkingTreeIterator : AbstractTreeIterator
	{
		/// <summary>
		/// An empty entry array, suitable for
		/// <see cref="Init(Entry[])">Init(Entry[])</see>
		/// .
		/// </summary>
		protected internal static readonly WorkingTreeIterator.Entry[] EOF = new WorkingTreeIterator.Entry
			[] {  };

		/// <summary>Size we perform file IO in if we have to read and hash a file.</summary>
		/// <remarks>Size we perform file IO in if we have to read and hash a file.</remarks>
		internal const int BUFFER_SIZE = 2048;

		/// <summary>
		/// Maximum size of files which may be read fully into memory for performance
		/// reasons.
		/// </summary>
		/// <remarks>
		/// Maximum size of files which may be read fully into memory for performance
		/// reasons.
		/// </remarks>
		private const long MAXIMUM_FILE_SIZE_TO_READ_FULLY = 65536;

		/// <summary>Inherited state of this iterator, describing working tree, etc.</summary>
		/// <remarks>Inherited state of this iterator, describing working tree, etc.</remarks>
		private readonly WorkingTreeIterator.IteratorState state;

		/// <summary>
		/// The
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// for the current entry.
		/// </summary>
		private byte[] contentId;

		/// <summary>
		/// Index within
		/// <see cref="entries">entries</see>
		/// that
		/// <see cref="contentId">contentId</see>
		/// came from.
		/// </summary>
		private int contentIdFromPtr;

		/// <summary>List of entries obtained from the subclass.</summary>
		/// <remarks>List of entries obtained from the subclass.</remarks>
		private WorkingTreeIterator.Entry[] entries;

		/// <summary>
		/// Total number of entries in
		/// <see cref="entries">entries</see>
		/// that are valid.
		/// </summary>
		private int entryCnt;

		/// <summary>
		/// Current position within
		/// <see cref="entries">entries</see>
		/// .
		/// </summary>
		private int ptr;

		/// <summary>If there is a .gitignore file present, the parsed rules from it.</summary>
		/// <remarks>If there is a .gitignore file present, the parsed rules from it.</remarks>
		private IgnoreNode ignoreNode;

		/// <summary>Repository that is the root level being iterated over</summary>
		protected internal Repository repository;

		/// <summary>Cached value of isEntryIgnored().</summary>
		/// <remarks>
		/// Cached value of isEntryIgnored(). 0 if not ignored, 1 if ignored, -1 if
		/// the value is not yet cached.
		/// </remarks>
		private int ignoreStatus = -1;

		/// <summary>Create a new iterator with no parent.</summary>
		/// <remarks>Create a new iterator with no parent.</remarks>
		/// <param name="options">working tree options to be used</param>
		protected internal WorkingTreeIterator(WorkingTreeOptions options) : base()
		{
			state = new WorkingTreeIterator.IteratorState(options);
		}

		/// <summary>Create a new iterator with no parent and a prefix.</summary>
		/// <remarks>
		/// Create a new iterator with no parent and a prefix.
		/// <p>
		/// The prefix path supplied is inserted in front of all paths generated by
		/// this iterator. It is intended to be used when an iterator is being
		/// created for a subsection of an overall repository and needs to be
		/// combined with other iterators that are created to run over the entire
		/// repository namespace.
		/// </remarks>
		/// <param name="prefix">
		/// position of this iterator in the repository tree. The value
		/// may be null or the empty string to indicate the prefix is the
		/// root of the repository. A trailing slash ('/') is
		/// automatically appended if the prefix does not end in '/'.
		/// </param>
		/// <param name="options">working tree options to be used</param>
		protected internal WorkingTreeIterator(string prefix, WorkingTreeOptions options)
			 : base(prefix)
		{
			state = new WorkingTreeIterator.IteratorState(options);
		}

		/// <summary>Create an iterator for a subtree of an existing iterator.</summary>
		/// <remarks>Create an iterator for a subtree of an existing iterator.</remarks>
		/// <param name="p">parent tree iterator.</param>
		protected internal WorkingTreeIterator(NGit.Treewalk.WorkingTreeIterator p) : base
			(p)
		{
			state = p.state;
		}

		/// <summary>Initialize this iterator for the root level of a repository.</summary>
		/// <remarks>
		/// Initialize this iterator for the root level of a repository.
		/// <p>
		/// This method should only be invoked after calling
		/// <see cref="Init(Entry[])">Init(Entry[])</see>
		/// ,
		/// and only for the root iterator.
		/// </remarks>
		/// <param name="repo">the repository.</param>
		protected internal virtual void InitRootIterator(Repository repo)
		{
			repository = repo;
			WorkingTreeIterator.Entry entry;
			if (ignoreNode is WorkingTreeIterator.PerDirectoryIgnoreNode)
			{
				entry = ((WorkingTreeIterator.PerDirectoryIgnoreNode)ignoreNode).entry;
			}
			else
			{
				entry = null;
			}
			ignoreNode = new WorkingTreeIterator.RootIgnoreNode(entry, repo);
		}

		/// <summary>
		/// Define the matching
		/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
		/// , to optimize ObjectIds.
		/// Once the DirCacheIterator has been set this iterator must only be
		/// advanced by the TreeWalk that is supplied, as it assumes that itself and
		/// the corresponding DirCacheIterator are positioned on the same file path
		/// whenever
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// is invoked.
		/// </summary>
		/// <param name="walk">the walk that will be advancing this iterator.</param>
		/// <param name="treeId">
		/// index of the matching
		/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
		/// .
		/// </param>
		public virtual void SetDirCacheIterator(TreeWalk walk, int treeId)
		{
			state.walk = walk;
			state.dirCacheTree = treeId;
		}

		public override bool HasId
		{
			get
			{
				if (contentIdFromPtr == ptr)
				{
					return true;
				}
				return (mode & FileMode.TYPE_MASK) == FileMode.TYPE_FILE;
			}
		}

		public override byte[] IdBuffer
		{
			get
			{
				if (contentIdFromPtr == ptr)
				{
					return contentId;
				}
				if (state.walk != null)
				{
					// If there is a matching DirCacheIterator, we can reuse
					// its idBuffer, but only if we appear to be clean against
					// the cached index information for the path.
					//
					DirCacheIterator i = state.walk.GetTree<DirCacheIterator>(state.dirCacheTree);
					if (i != null)
					{
						DirCacheEntry ent = i.GetDirCacheEntry();
						if (ent != null && CompareMetadata(ent) == WorkingTreeIterator.MetadataDiff.EQUAL)
						{
							return i.IdBuffer;
						}
					}
				}
				switch (mode & FileMode.TYPE_MASK)
				{
					case FileMode.TYPE_FILE:
					{
						contentIdFromPtr = ptr;
						return contentId = IdBufferBlob(entries[ptr]);
					}

					case FileMode.TYPE_SYMLINK:
					{
						// Java does not support symbolic links, so we should not
						// have reached this particular part of the walk code.
						//
						return zeroid;
					}

					case FileMode.TYPE_GITLINK:
					{
						contentIdFromPtr = ptr;
						return contentId = IdSubmodule(entries[ptr]);
					}
				}
				return zeroid;
			}
		}

		/// <summary>Get submodule id for given entry.</summary>
		/// <remarks>Get submodule id for given entry.</remarks>
		/// <param name="e"></param>
		/// <returns>non-null submodule id</returns>
		protected internal virtual byte[] IdSubmodule(WorkingTreeIterator.Entry e)
		{
			if (repository == null)
			{
				return zeroid;
			}
			FilePath directory;
			try
			{
				directory = repository.WorkTree;
			}
			catch (NoWorkTreeException)
			{
				return zeroid;
			}
			return IdSubmodule(directory, e);
		}

		/// <summary>
		/// Get submodule id using the repository at the location of the entry
		/// relative to the directory.
		/// </summary>
		/// <remarks>
		/// Get submodule id using the repository at the location of the entry
		/// relative to the directory.
		/// </remarks>
		/// <param name="directory"></param>
		/// <param name="e"></param>
		/// <returns>non-null submodule id</returns>
		protected internal virtual byte[] IdSubmodule(FilePath directory, WorkingTreeIterator.Entry
			 e)
		{
			Repository submoduleRepo;
			try
			{
				submoduleRepo = SubmoduleWalk.GetSubmoduleRepository(directory, e.GetName());
			}
			catch (IOException)
			{
				return zeroid;
			}
			if (submoduleRepo == null)
			{
				return zeroid;
			}
			ObjectId head;
			try
			{
				head = submoduleRepo.Resolve(Constants.HEAD);
			}
			catch (IOException)
			{
				return zeroid;
			}
			finally
			{
				submoduleRepo.Close();
			}
			if (head == null)
			{
				return zeroid;
			}
			byte[] id = new byte[Constants.OBJECT_ID_LENGTH];
			head.CopyRawTo(id, 0);
			return id;
		}

		private static readonly byte[] digits = new byte[] { (byte)('0'), (byte)('1'), (byte
			)('2'), (byte)('3'), (byte)('4'), (byte)('5'), (byte)('6'), (byte)('7'), (byte)(
			'8'), (byte)('9') };

		private static readonly byte[] hblob = Constants.EncodedTypeString(Constants.OBJ_BLOB
			);

		private byte[] IdBufferBlob(WorkingTreeIterator.Entry e)
		{
			try
			{
				InputStream @is = e.OpenInputStream();
				if (@is == null)
				{
					return zeroid;
				}
				try
				{
					state.InitializeDigestAndReadBuffer();
					long len = e.GetLength();
					if (!MightNeedCleaning())
					{
						return ComputeHash(@is, len);
					}
					if (len <= MAXIMUM_FILE_SIZE_TO_READ_FULLY)
					{
						ByteBuffer rawbuf = IOUtil.ReadWholeStream(@is, (int)len);
						byte[] raw = ((byte[])rawbuf.Array());
						int n = rawbuf.Limit();
						if (!IsBinary(raw, n))
						{
							rawbuf = FilterClean(raw, n);
							raw = ((byte[])rawbuf.Array());
							n = rawbuf.Limit();
						}
						return ComputeHash(new ByteArrayInputStream(raw, 0, n), n);
					}
					if (IsBinary(e))
					{
						return ComputeHash(@is, len);
					}
					long canonLen;
					InputStream lenIs = FilterClean(e.OpenInputStream());
					try
					{
						canonLen = ComputeLength(lenIs);
					}
					finally
					{
						SafeClose(lenIs);
					}
					return ComputeHash(FilterClean(@is), canonLen);
				}
				finally
				{
					SafeClose(@is);
				}
			}
			catch (IOException)
			{
				// Can't read the file? Don't report the failure either.
				return zeroid;
			}
		}

		private static void SafeClose(InputStream @in)
		{
			try
			{
				@in.Close();
			}
			catch (IOException)
			{
			}
		}

		// Suppress any error related to closing an input
		// stream. We don't care, we should not have any
		// outstanding data to flush or anything like that.
		private bool MightNeedCleaning()
		{
			switch (GetOptions().GetAutoCRLF())
			{
				case CoreConfig.AutoCRLF.FALSE:
				default:
				{
					return false;
					break;
				}

				case CoreConfig.AutoCRLF.TRUE:
				{
					return true;
				}
			}
		}

		private bool IsBinary(byte[] content, int sz)
		{
			return RawText.IsBinary(content, sz);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool IsBinary(WorkingTreeIterator.Entry entry)
		{
			InputStream @in = entry.OpenInputStream();
			try
			{
				return RawText.IsBinary(@in);
			}
			finally
			{
				SafeClose(@in);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ByteBuffer FilterClean(byte[] src, int n)
		{
			InputStream @in = new ByteArrayInputStream(src);
			try
			{
				return IOUtil.ReadWholeStream(FilterClean(@in), n);
			}
			finally
			{
				SafeClose(@in);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private InputStream FilterClean(InputStream @in)
		{
			return new EolCanonicalizingInputStream(@in);
		}

		/// <summary>Returns the working tree options used by this iterator.</summary>
		/// <remarks>Returns the working tree options used by this iterator.</remarks>
		/// <returns>working tree options</returns>
		public virtual WorkingTreeOptions GetOptions()
		{
			return state.options;
		}

		public override int IdOffset
		{
			get
			{
				return 0;
			}
		}

		public override void Reset()
		{
			if (!First)
			{
				ptr = 0;
				if (!Eof)
				{
					ParseEntry();
				}
			}
		}

		public override bool First
		{
			get
			{
				return ptr == 0;
			}
		}

		public override bool Eof
		{
			get
			{
				return ptr == entryCnt;
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		public override void Next(int delta)
		{
			ptr += delta;
			if (!Eof)
			{
				ParseEntry();
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		public override void Back(int delta)
		{
			ptr -= delta;
			ParseEntry();
		}

		private void ParseEntry()
		{
			ignoreStatus = -1;
			WorkingTreeIterator.Entry e = entries[ptr];
			mode = e.GetMode().GetBits();
			int nameLen = e.encodedNameLen;
			EnsurePathCapacity(pathOffset + nameLen, pathOffset);
			System.Array.Copy(e.encodedName, 0, path, pathOffset, nameLen);
			pathLen = pathOffset + nameLen;
		}

		/// <summary>Get the byte length of this entry.</summary>
		/// <remarks>Get the byte length of this entry.</remarks>
		/// <returns>size of this file, in bytes.</returns>
		public virtual long GetEntryLength()
		{
			return Current().GetLength();
		}

		/// <summary>Get the last modified time of this entry.</summary>
		/// <remarks>Get the last modified time of this entry.</remarks>
		/// <returns>
		/// last modified time of this file, in milliseconds since the epoch
		/// (Jan 1, 1970 UTC).
		/// </returns>
		public virtual long GetEntryLastModified()
		{
			return Current().GetLastModified();
		}

		/// <summary>Obtain an input stream to read the file content.</summary>
		/// <remarks>
		/// Obtain an input stream to read the file content.
		/// <p>
		/// Efficient implementations are not required. The caller will usually
		/// obtain the stream only once per entry, if at all.
		/// <p>
		/// The input stream should not use buffering if the implementation can avoid
		/// it. The caller will buffer as necessary to perform efficient block IO
		/// operations.
		/// <p>
		/// The caller will close the stream once complete.
		/// </remarks>
		/// <returns>a stream to read from the file.</returns>
		/// <exception cref="System.IO.IOException">the file could not be opened for reading.
		/// 	</exception>
		public virtual InputStream OpenEntryStream()
		{
			InputStream rawis = Current().OpenInputStream();
			InputStream @is;
			if (GetOptions().GetAutoCRLF() != CoreConfig.AutoCRLF.FALSE)
			{
				@is = new EolCanonicalizingInputStream(rawis);
			}
			else
			{
				@is = rawis;
			}
			return @is;
		}

		/// <summary>Determine if the current entry path is ignored by an ignore rule.</summary>
		/// <remarks>Determine if the current entry path is ignored by an ignore rule.</remarks>
		/// <returns>true if the entry was ignored by an ignore rule file.</returns>
		/// <exception cref="System.IO.IOException">a relevant ignore rule file exists but cannot be read.
		/// 	</exception>
		public virtual bool IsEntryIgnored()
		{
			return IsEntryIgnored(pathLen);
		}

		/// <summary>Determine if the entry path is ignored by an ignore rule.</summary>
		/// <remarks>Determine if the entry path is ignored by an ignore rule.</remarks>
		/// <param name="pLen">the length of the path in the path buffer.</param>
		/// <returns>true if the entry is ignored by an ignore rule.</returns>
		/// <exception cref="System.IO.IOException">a relevant ignore rule file exists but cannot be read.
		/// 	</exception>
		protected internal virtual bool IsEntryIgnored(int pLen)
		{
			if (pLen == pathLen) {
				if (ignoreStatus == -1)
					ignoreStatus = IsEntryIgnoredInternal (pLen) ? 1 : 0;
				return ignoreStatus == 1;
			}
			return IsEntryIgnoredInternal (pLen);
		}

		bool IsEntryIgnoredInternal (int pLen)
		{
			IgnoreNode rules = GetIgnoreNode();
			if (rules != null)
			{
				// The ignore code wants path to start with a '/' if possible.
				// If we have the '/' in our path buffer because we are inside
				// a subdirectory include it in the range we convert to string.
				//
				int pOff = pathOffset;
				if (0 < pOff)
				{
					pOff--;
				}
				string p = TreeWalk.PathOf(path, pOff, pLen);
				switch (rules.IsIgnored(p, FileMode.TREE.Equals(mode)))
				{
					case IgnoreNode.MatchResult.IGNORED:
					{
						return true;
					}

					case IgnoreNode.MatchResult.NOT_IGNORED:
					{
						return false;
					}

					case IgnoreNode.MatchResult.CHECK_PARENT:
					{
						break;
					}
				}
			}
			if (parent is NGit.Treewalk.WorkingTreeIterator)
			{
				return ((NGit.Treewalk.WorkingTreeIterator)parent).IsEntryIgnored(pLen);
			}
			return false;
		}
		
		/// <exception cref="System.IO.IOException"></exception>
		private IgnoreNode GetIgnoreNode()
		{
			if (ignoreNode is WorkingTreeIterator.PerDirectoryIgnoreNode)
			{
				ignoreNode = ((WorkingTreeIterator.PerDirectoryIgnoreNode)ignoreNode).Load();
			}
			return ignoreNode;
		}

		private sealed class _IComparer_573 : IComparer<WorkingTreeIterator.Entry>
		{
			public _IComparer_573()
			{
			}

			public int Compare(WorkingTreeIterator.Entry o1, WorkingTreeIterator.Entry o2)
			{
				byte[] a = o1.encodedName;
				byte[] b = o2.encodedName;
				int aLen = o1.encodedNameLen;
				int bLen = o2.encodedNameLen;
				int cPos;
				for (cPos = 0; cPos < aLen && cPos < bLen; cPos++)
				{
					int cmp = (a[cPos] & unchecked((int)(0xff))) - (b[cPos] & unchecked((int)(0xff)));
					if (cmp != 0)
					{
						return cmp;
					}
				}
				if (cPos < aLen)
				{
					return (a[cPos] & unchecked((int)(0xff))) - NGit.Treewalk.WorkingTreeIterator.LastPathChar
						(o2);
				}
				if (cPos < bLen)
				{
					return NGit.Treewalk.WorkingTreeIterator.LastPathChar(o1) - (b[cPos] & unchecked(
						(int)(0xff)));
				}
				return NGit.Treewalk.WorkingTreeIterator.LastPathChar(o1) - NGit.Treewalk.WorkingTreeIterator
					.LastPathChar(o2);
			}
		}

		private static readonly IComparer<WorkingTreeIterator.Entry> ENTRY_CMP = new _IComparer_573
			();

		internal static int LastPathChar(WorkingTreeIterator.Entry e)
		{
			return e.GetMode() == FileMode.TREE ? '/' : '\0';
		}

		/// <summary>Constructor helper.</summary>
		/// <remarks>Constructor helper.</remarks>
		/// <param name="list">
		/// files in the subtree of the work tree this iterator operates
		/// on
		/// </param>
		protected internal virtual void Init(WorkingTreeIterator.Entry[] list)
		{
			// Filter out nulls, . and .. as these are not valid tree entries,
			// also cache the encoded forms of the path names for efficient use
			// later on during sorting and iteration.
			//
			entries = list;
			int i;
			int o;
			CharsetEncoder nameEncoder = state.nameEncoder;
			for (i = 0, o = 0; i < entries.Length; i++)
			{
				WorkingTreeIterator.Entry e = entries[i];
				if (e == null)
				{
					continue;
				}
				string name = e.GetName();
				if (".".Equals(name) || "..".Equals(name))
				{
					continue;
				}
				if (Constants.DOT_GIT.Equals(name))
				{
					continue;
				}
				if (Constants.DOT_GIT_IGNORE.Equals(name))
				{
					ignoreNode = new WorkingTreeIterator.PerDirectoryIgnoreNode(e);
				}
				if (i != o)
				{
					entries[o] = e;
				}
				e.EncodeName(nameEncoder);
				o++;
			}
			entryCnt = o;
			Arrays.Sort(entries, 0, entryCnt, ENTRY_CMP);
			contentIdFromPtr = -1;
			ptr = 0;
			if (!Eof)
			{
				ParseEntry();
			}
		}

		/// <summary>Obtain the current entry from this iterator.</summary>
		/// <remarks>Obtain the current entry from this iterator.</remarks>
		/// <returns>the currently selected entry.</returns>
		protected internal virtual WorkingTreeIterator.Entry Current()
		{
			return entries[ptr];
		}

		/// <summary>
		/// The result of a metadata-comparison between the current entry and a
		/// <see cref="DirCacheEntry">DirCacheEntry</see>
		/// </summary>
		public enum MetadataDiff
		{
			EQUAL,
			DIFFER_BY_METADATA,
			SMUDGED,
			DIFFER_BY_TIMESTAMP
		}

		/// <summary>
		/// Compare the metadata (mode, length, modification-timestamp) of the
		/// current entry and a
		/// <see cref="NGit.Dircache.DirCacheEntry">NGit.Dircache.DirCacheEntry</see>
		/// </summary>
		/// <param name="entry">
		/// the
		/// <see cref="NGit.Dircache.DirCacheEntry">NGit.Dircache.DirCacheEntry</see>
		/// to compare with
		/// </param>
		/// <returns>
		/// a
		/// <see cref="MetadataDiff">MetadataDiff</see>
		/// which tells whether and how the entries
		/// metadata differ
		/// </returns>
		public virtual WorkingTreeIterator.MetadataDiff CompareMetadata(DirCacheEntry entry
			)
		{
			if (entry.IsAssumeValid)
			{
				return WorkingTreeIterator.MetadataDiff.EQUAL;
			}
			if (entry.IsUpdateNeeded)
			{
				return WorkingTreeIterator.MetadataDiff.DIFFER_BY_METADATA;
			}
			if (!entry.IsSmudged && (GetEntryLength() != entry.Length))
			{
				return WorkingTreeIterator.MetadataDiff.DIFFER_BY_METADATA;
			}
			// Determine difference in mode-bits of file and index-entry. In the
			// bitwise presentation of modeDiff we'll have a '1' when the two modes
			// differ at this position.
			int modeDiff = EntryRawMode ^ entry.RawMode;
			// Do not rely on filemode differences in case of symbolic links
			if (modeDiff != 0 && !FileMode.SYMLINK.Equals(entry.RawMode))
			{
				// Ignore the executable file bits if WorkingTreeOptions tell me to
				// do so. Ignoring is done by setting the bits representing a
				// EXECUTABLE_FILE to '0' in modeDiff
				if (!state.options.IsFileMode())
				{
					modeDiff &= ~FileMode.EXECUTABLE_FILE.GetBits();
				}
				if (modeDiff != 0)
				{
					// Report a modification if the modes still (after potentially
					// ignoring EXECUTABLE_FILE bits) differ
					return WorkingTreeIterator.MetadataDiff.DIFFER_BY_METADATA;
				}
			}
			// Git under windows only stores seconds so we round the timestamp
			// Java gives us if it looks like the timestamp in index is seconds
			// only. Otherwise we compare the timestamp at millisecond precision.
			long cacheLastModified = entry.LastModified;
			long fileLastModified = GetEntryLastModified();
			if (cacheLastModified % 1000 == 0 || fileLastModified % 1000 == 0)
			{
				cacheLastModified = cacheLastModified - cacheLastModified % 1000;
				fileLastModified = fileLastModified - fileLastModified % 1000;
			}
			if (fileLastModified != cacheLastModified)
			{
				return WorkingTreeIterator.MetadataDiff.DIFFER_BY_TIMESTAMP;
			}
			else
			{
				if (!entry.IsSmudged)
				{
					// The file is clean when you look at timestamps.
					return WorkingTreeIterator.MetadataDiff.EQUAL;
				}
				else
				{
					return WorkingTreeIterator.MetadataDiff.SMUDGED;
				}
			}
		}

		/// <summary>
		/// Checks whether this entry differs from a given entry from the
		/// <see cref="NGit.Dircache.DirCache">NGit.Dircache.DirCache</see>
		/// .
		/// File status information is used and if status is same we consider the
		/// file identical to the state in the working directory. Native git uses
		/// more stat fields than we have accessible in Java.
		/// </summary>
		/// <param name="entry">the entry from the dircache we want to compare against</param>
		/// <param name="forceContentCheck">
		/// True if the actual file content should be checked if
		/// modification time differs.
		/// </param>
		/// <returns>true if content is most likely different.</returns>
		public virtual bool IsModified(DirCacheEntry entry, bool forceContentCheck)
		{
			WorkingTreeIterator.MetadataDiff diff = CompareMetadata(entry);
			switch (diff)
			{
				case WorkingTreeIterator.MetadataDiff.DIFFER_BY_TIMESTAMP:
				{
					if (forceContentCheck)
					{
						// But we are told to look at content even though timestamps
						// tell us about modification
						return ContentCheck(entry);
					}
					else
					{
						// We are told to assume a modification if timestamps differs
						return true;
					}
					goto case WorkingTreeIterator.MetadataDiff.SMUDGED;
				}

				case WorkingTreeIterator.MetadataDiff.SMUDGED:
				{
					// The file is clean by timestamps but the entry was smudged.
					// Lets do a content check
					return ContentCheck(entry);
				}

				case WorkingTreeIterator.MetadataDiff.EQUAL:
				{
					return false;
				}

				case WorkingTreeIterator.MetadataDiff.DIFFER_BY_METADATA:
				{
					return true;
				}

				default:
				{
					throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().unexpectedCompareResult
						, diff.ToString()));
				}
			}
		}

		/// <summary>
		/// Get the file mode to use for the current entry when it is to be updated
		/// in the index.
		/// </summary>
		/// <remarks>
		/// Get the file mode to use for the current entry when it is to be updated
		/// in the index.
		/// </remarks>
		/// <param name="indexIter">
		/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
		/// positioned at the same entry as this
		/// iterator or null if no
		/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
		/// is available
		/// at this iterator's current entry
		/// </param>
		/// <returns>index file mode</returns>
		public virtual FileMode GetIndexFileMode(DirCacheIterator indexIter)
		{
			FileMode wtMode = EntryFileMode;
			if (indexIter == null)
			{
				return wtMode;
			}
			if (GetOptions().IsFileMode())
			{
				return wtMode;
			}
			FileMode iMode = indexIter.EntryFileMode;
			if (FileMode.REGULAR_FILE == wtMode && FileMode.EXECUTABLE_FILE == iMode)
			{
				return iMode;
			}
			if (FileMode.EXECUTABLE_FILE == wtMode && FileMode.REGULAR_FILE == iMode)
			{
				return iMode;
			}
			return wtMode;
		}

		/// <summary>Compares the entries content with the content in the filesystem.</summary>
		/// <remarks>
		/// Compares the entries content with the content in the filesystem.
		/// Unsmudges the entry when it is detected that it is clean.
		/// </remarks>
		/// <param name="entry">the entry to be checked</param>
		/// <returns>
		/// <code>true</code> if the content matches, <code>false</code>
		/// otherwise
		/// </returns>
		private bool ContentCheck(DirCacheEntry entry)
		{
			if (EntryObjectId.Equals(entry.GetObjectId()))
			{
				// Content has not changed
				// We know the entry can't be racily clean because it's still clean.
				// Therefore we unsmudge the entry!
				// If by any chance we now unsmudge although we are still in the
				// same time-slot as the last modification to the index file the
				// next index write operation will smudge again.
				// Caution: we are unsmudging just by setting the length of the
				// in-memory entry object. It's the callers task to detect that we
				// have modified the entry and to persist the modified index.
				entry.SetLength((int)GetEntryLength());
				return false;
			}
			else
			{
				// Content differs: that's a real change!
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private long ComputeLength(InputStream @in)
		{
			// Since we only care about the length, use skip. The stream
			// may be able to more efficiently wade through its data.
			//
			long length = 0;
			for (; ; )
			{
				long n = @in.Skip(1 << 20);
				if (n <= 0)
				{
					break;
				}
				length += n;
			}
			return length;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private byte[] ComputeHash(InputStream @in, long length)
		{
			MessageDigest contentDigest = state.contentDigest;
			byte[] contentReadBuffer = state.contentReadBuffer;
			contentDigest.Reset();
			contentDigest.Update(hblob);
			contentDigest.Update(unchecked((byte)' '));
			long sz = length;
			if (sz == 0)
			{
				contentDigest.Update(unchecked((byte)'0'));
			}
			else
			{
				int bufn = contentReadBuffer.Length;
				int p = bufn;
				do
				{
					contentReadBuffer[--p] = digits[(int)(sz % 10)];
					sz /= 10;
				}
				while (sz > 0);
				contentDigest.Update(contentReadBuffer, p, bufn - p);
			}
			contentDigest.Update(unchecked((byte)0));
			for (; ; )
			{
				int r = @in.Read(contentReadBuffer);
				if (r <= 0)
				{
					break;
				}
				contentDigest.Update(contentReadBuffer, 0, r);
				sz += r;
			}
			if (sz != length)
			{
				return zeroid;
			}
			return contentDigest.Digest();
		}

		/// <summary>A single entry within a working directory tree.</summary>
		/// <remarks>A single entry within a working directory tree.</remarks>
		public abstract class Entry
		{
			internal byte[] encodedName;

			internal int encodedNameLen;

			internal virtual void EncodeName(CharsetEncoder enc)
			{
				ByteBuffer b;
				try
				{
					b = enc.Encode(CharBuffer.Wrap(GetName()));
				}
				catch (CharacterCodingException)
				{
					// This should so never happen.
					throw new RuntimeException(MessageFormat.Format(JGitText.Get().unencodeableFile, 
						GetName()));
				}
				encodedNameLen = b.Limit();
				if (b.HasArray() && b.ArrayOffset() == 0)
				{
					encodedName = ((byte[])b.Array());
				}
				else
				{
					b.Get(encodedName = new byte[encodedNameLen]);
				}
			}

			public override string ToString()
			{
				return GetMode().ToString() + " " + GetName();
			}

			/// <summary>Get the type of this entry.</summary>
			/// <remarks>
			/// Get the type of this entry.
			/// <p>
			/// <b>Note: Efficient implementation required.</b>
			/// <p>
			/// The implementation of this method must be efficient. If a subclass
			/// needs to compute the value they should cache the reference within an
			/// instance member instead.
			/// </remarks>
			/// <returns>
			/// a file mode constant from
			/// <see cref="NGit.FileMode">NGit.FileMode</see>
			/// .
			/// </returns>
			public abstract FileMode GetMode();

			/// <summary>Get the byte length of this entry.</summary>
			/// <remarks>
			/// Get the byte length of this entry.
			/// <p>
			/// <b>Note: Efficient implementation required.</b>
			/// <p>
			/// The implementation of this method must be efficient. If a subclass
			/// needs to compute the value they should cache the reference within an
			/// instance member instead.
			/// </remarks>
			/// <returns>size of this file, in bytes.</returns>
			public abstract long GetLength();

			/// <summary>Get the last modified time of this entry.</summary>
			/// <remarks>
			/// Get the last modified time of this entry.
			/// <p>
			/// <b>Note: Efficient implementation required.</b>
			/// <p>
			/// The implementation of this method must be efficient. If a subclass
			/// needs to compute the value they should cache the reference within an
			/// instance member instead.
			/// </remarks>
			/// <returns>time since the epoch (in ms) of the last change.</returns>
			public abstract long GetLastModified();

			/// <summary>Get the name of this entry within its directory.</summary>
			/// <remarks>
			/// Get the name of this entry within its directory.
			/// <p>
			/// Efficient implementations are not required. The caller will obtain
			/// the name only once and cache it once obtained.
			/// </remarks>
			/// <returns>name of the entry.</returns>
			public abstract string GetName();

			/// <summary>Obtain an input stream to read the file content.</summary>
			/// <remarks>
			/// Obtain an input stream to read the file content.
			/// <p>
			/// Efficient implementations are not required. The caller will usually
			/// obtain the stream only once per entry, if at all.
			/// <p>
			/// The input stream should not use buffering if the implementation can
			/// avoid it. The caller will buffer as necessary to perform efficient
			/// block IO operations.
			/// <p>
			/// The caller will close the stream once complete.
			/// </remarks>
			/// <returns>a stream to read from the file.</returns>
			/// <exception cref="System.IO.IOException">the file could not be opened for reading.
			/// 	</exception>
			public abstract InputStream OpenInputStream();
		}

		/// <summary>Magic type indicating we know rules exist, but they aren't loaded.</summary>
		/// <remarks>Magic type indicating we know rules exist, but they aren't loaded.</remarks>
		private class PerDirectoryIgnoreNode : IgnoreNode
		{
			internal readonly WorkingTreeIterator.Entry entry;

			internal PerDirectoryIgnoreNode(WorkingTreeIterator.Entry entry) : base(Sharpen.Collections
				.EmptyList<IgnoreRule>())
			{
				this.entry = entry;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual IgnoreNode Load()
			{
				IgnoreNode r = new IgnoreNode();
				InputStream @in = entry.OpenInputStream();
				try
				{
					r.Parse(@in);
				}
				finally
				{
					@in.Close();
				}
				return r.GetRules().IsEmpty() ? null : r;
			}
		}

		/// <summary>Magic type indicating there may be rules for the top level.</summary>
		/// <remarks>Magic type indicating there may be rules for the top level.</remarks>
		private class RootIgnoreNode : WorkingTreeIterator.PerDirectoryIgnoreNode
		{
			internal readonly Repository repository;

			internal RootIgnoreNode(WorkingTreeIterator.Entry entry, Repository repository) : 
				base(entry)
			{
				this.repository = repository;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override IgnoreNode Load()
			{
				IgnoreNode r;
				if (entry != null)
				{
					r = base.Load();
					if (r == null)
					{
						r = new IgnoreNode();
					}
				}
				else
				{
					r = new IgnoreNode();
				}
				FS fs = repository.FileSystem;
				string path = repository.GetConfig().Get(CoreConfig.KEY).GetExcludesFile();
				if (path != null)
				{
					FilePath excludesfile;
					if (path.StartsWith("~/"))
					{
						excludesfile = fs.Resolve(fs.UserHome(), Sharpen.Runtime.Substring(path, 2));
					}
					else
					{
						excludesfile = fs.Resolve(null, path);
					}
					LoadRulesFromFile(r, excludesfile);
				}
				FilePath exclude = fs.Resolve(repository.Directory, "info/exclude");
				LoadRulesFromFile(r, exclude);
				return r.GetRules().IsEmpty() ? null : r;
			}

			/// <exception cref="System.IO.FileNotFoundException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			private void LoadRulesFromFile(IgnoreNode r, FilePath exclude)
			{
				if (exclude.Exists())
				{
					FileInputStream @in = new FileInputStream(exclude);
					try
					{
						r.Parse(@in);
					}
					finally
					{
						@in.Close();
					}
				}
			}
		}

		private sealed class IteratorState
		{
			/// <summary>Options used to process the working tree.</summary>
			/// <remarks>Options used to process the working tree.</remarks>
			internal readonly WorkingTreeOptions options;

			/// <summary>File name character encoder.</summary>
			/// <remarks>File name character encoder.</remarks>
			internal readonly CharsetEncoder nameEncoder;

			/// <summary>
			/// Digest computer for
			/// <see cref="WorkingTreeIterator.contentId">WorkingTreeIterator.contentId</see>
			/// computations.
			/// </summary>
			internal MessageDigest contentDigest;

			/// <summary>
			/// Buffer used to perform
			/// <see cref="WorkingTreeIterator.contentId">WorkingTreeIterator.contentId</see>
			/// computations.
			/// </summary>
			internal byte[] contentReadBuffer;

			/// <summary>TreeWalk with a (supposedly) matching DirCacheIterator.</summary>
			/// <remarks>TreeWalk with a (supposedly) matching DirCacheIterator.</remarks>
			internal TreeWalk walk;

			/// <summary>
			/// Position of the matching
			/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
			/// .
			/// </summary>
			internal int dirCacheTree;

			internal IteratorState(WorkingTreeOptions options)
			{
				this.options = options;
				this.nameEncoder = Constants.CHARSET.NewEncoder();
			}

			internal void InitializeDigestAndReadBuffer()
			{
				if (contentDigest == null)
				{
					contentDigest = Constants.NewMessageDigest();
					contentReadBuffer = new byte[BUFFER_SIZE];
				}
			}
		}
	}
}
