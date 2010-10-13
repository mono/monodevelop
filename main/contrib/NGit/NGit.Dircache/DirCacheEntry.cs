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
using System.IO;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// A single file (or stage of a file) in a
	/// <see cref="DirCache">DirCache</see>
	/// .
	/// <p>
	/// An entry represents exactly one stage of a file. If a file path is unmerged
	/// then multiple DirCacheEntry instances may appear for the same path name.
	/// </summary>
	public class DirCacheEntry
	{
		private static readonly byte[] nullpad = new byte[8];

		/// <summary>The standard (fully merged) stage for an entry.</summary>
		/// <remarks>The standard (fully merged) stage for an entry.</remarks>
		public const int STAGE_0 = 0;

		/// <summary>The base tree revision for an entry.</summary>
		/// <remarks>The base tree revision for an entry.</remarks>
		public const int STAGE_1 = 1;

		/// <summary>The first tree revision (usually called "ours").</summary>
		/// <remarks>The first tree revision (usually called "ours").</remarks>
		public const int STAGE_2 = 2;

		/// <summary>The second tree revision (usually called "theirs").</summary>
		/// <remarks>The second tree revision (usually called "theirs").</remarks>
		public const int STAGE_3 = 3;

		private const int P_MTIME = 8;

		private const int P_MODE = 24;

		private const int P_SIZE = 36;

		private const int P_OBJECTID = 40;

		private const int P_FLAGS = 60;

		private const int P_FLAGS2 = 62;

		/// <summary>
		/// Mask applied to data in
		/// <see cref="P_FLAGS">P_FLAGS</see>
		/// to get the name length.
		/// </summary>
		private const int NAME_MASK = unchecked((int)(0xfff));

		private const int INTENT_TO_ADD = unchecked((int)(0x20000000));

		private const int SKIP_WORKTREE = unchecked((int)(0x40000000));

		private const int EXTENDED_FLAGS = (INTENT_TO_ADD | SKIP_WORKTREE);

		private const int INFO_LEN = 62;

		private const int INFO_LEN_EXTENDED = 64;

		private const int EXTENDED = unchecked((int)(0x40));

		private const int ASSUME_VALID = unchecked((int)(0x80));

		/// <summary>In-core flag signaling that the entry should be considered as modified.</summary>
		/// <remarks>In-core flag signaling that the entry should be considered as modified.</remarks>
		private const int UPDATE_NEEDED = unchecked((int)(0x1));

		/// <summary>(Possibly shared) header information storage.</summary>
		/// <remarks>(Possibly shared) header information storage.</remarks>
		private readonly byte[] info;

		/// <summary>
		/// First location within
		/// <see cref="info">info</see>
		/// where our header starts.
		/// </summary>
		private readonly int infoOffset;

		/// <summary>Our encoded path name, from the root of the repository.</summary>
		/// <remarks>Our encoded path name, from the root of the repository.</remarks>
		internal readonly byte[] path;

		/// <summary>Flags which are never stored to disk.</summary>
		/// <remarks>Flags which are never stored to disk.</remarks>
		private byte inCoreFlags;

		/// <exception cref="System.IO.IOException"></exception>
		internal DirCacheEntry(byte[] sharedInfo, MutableInteger infoAt, InputStream @in, 
			MessageDigest md)
		{
			// private static final int P_CTIME = 0;
			// private static final int P_CTIME_NSEC = 4;
			// private static final int P_MTIME_NSEC = 12;
			// private static final int P_DEV = 16;
			// private static final int P_INO = 20;
			// private static final int P_UID = 28;
			// private static final int P_GID = 32;
			info = sharedInfo;
			infoOffset = infoAt.value;
			IOUtil.ReadFully(@in, info, infoOffset, INFO_LEN);
			int len;
			if (IsExtended())
			{
				len = INFO_LEN_EXTENDED;
				IOUtil.ReadFully(@in, info, infoOffset + INFO_LEN, INFO_LEN_EXTENDED - INFO_LEN);
				if ((GetExtendedFlags() & ~EXTENDED_FLAGS) != 0)
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().DIRCUnrecognizedExtendedFlags
						, GetExtendedFlags().ToString()));
				}
			}
			else
			{
				len = INFO_LEN;
			}
			infoAt.value += len;
			md.Update(info, infoOffset, len);
			int pathLen = NB.DecodeUInt16(info, infoOffset + P_FLAGS) & NAME_MASK;
			int skipped = 0;
			if (pathLen < NAME_MASK)
			{
				path = new byte[pathLen];
				IOUtil.ReadFully(@in, path, 0, pathLen);
				md.Update(path, 0, pathLen);
			}
			else
			{
				ByteArrayOutputStream tmp = new ByteArrayOutputStream();
				{
					byte[] buf = new byte[NAME_MASK];
					IOUtil.ReadFully(@in, buf, 0, NAME_MASK);
					tmp.Write(buf);
				}
				for (; ; )
				{
					int c = @in.Read();
					if (c < 0)
					{
						throw new EOFException(JGitText.Get().shortReadOfBlock);
					}
					if (c == 0)
					{
						break;
					}
					tmp.Write(c);
				}
				path = tmp.ToByteArray();
				pathLen = path.Length;
				skipped = 1;
				// we already skipped 1 '\0' above to break the loop.
				md.Update(path, 0, pathLen);
				md.Update(unchecked((byte)0));
			}
			// Index records are padded out to the next 8 byte alignment
			// for historical reasons related to how C Git read the files.
			//
			int actLen = len + pathLen;
			int expLen = (actLen + 8) & ~7;
			int padLen = expLen - actLen - skipped;
			if (padLen > 0)
			{
				IOUtil.SkipFully(@in, padLen);
				md.Update(nullpad, 0, padLen);
			}
		}

		/// <summary>Create an empty entry at stage 0.</summary>
		/// <remarks>Create an empty entry at stage 0.</remarks>
		/// <param name="newPath">name of the cache entry.</param>
		/// <exception cref="System.ArgumentException">
		/// If the path starts or ends with "/", or contains "//" either
		/// "\0". These sequences are not permitted in a git tree object
		/// or DirCache file.
		/// </exception>
		public DirCacheEntry(string newPath) : this(Constants.Encode(newPath))
		{
		}

		/// <summary>Create an empty entry at the specified stage.</summary>
		/// <remarks>Create an empty entry at the specified stage.</remarks>
		/// <param name="newPath">name of the cache entry.</param>
		/// <param name="stage">the stage index of the new entry.</param>
		/// <exception cref="System.ArgumentException">
		/// If the path starts or ends with "/", or contains "//" either
		/// "\0". These sequences are not permitted in a git tree object
		/// or DirCache file.  Or if
		/// <code>stage</code>
		/// is outside of the
		/// range 0..3, inclusive.
		/// </exception>
		public DirCacheEntry(string newPath, int stage) : this(Constants.Encode(newPath), 
			stage)
		{
		}

		/// <summary>Create an empty entry at stage 0.</summary>
		/// <remarks>Create an empty entry at stage 0.</remarks>
		/// <param name="newPath">name of the cache entry, in the standard encoding.</param>
		/// <exception cref="System.ArgumentException">
		/// If the path starts or ends with "/", or contains "//" either
		/// "\0". These sequences are not permitted in a git tree object
		/// or DirCache file.
		/// </exception>
		public DirCacheEntry(byte[] newPath) : this(newPath, STAGE_0)
		{
		}

		/// <summary>Create an empty entry at the specified stage.</summary>
		/// <remarks>Create an empty entry at the specified stage.</remarks>
		/// <param name="newPath">name of the cache entry, in the standard encoding.</param>
		/// <param name="stage">the stage index of the new entry.</param>
		/// <exception cref="System.ArgumentException">
		/// If the path starts or ends with "/", or contains "//" either
		/// "\0". These sequences are not permitted in a git tree object
		/// or DirCache file.  Or if
		/// <code>stage</code>
		/// is outside of the
		/// range 0..3, inclusive.
		/// </exception>
		public DirCacheEntry(byte[] newPath, int stage)
		{
			if (!IsValidPath(newPath))
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidPath, ToString
					(newPath)));
			}
			if (stage < 0 || 3 < stage)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidStageForPath
					, stage, ToString(newPath)));
			}
			info = new byte[INFO_LEN];
			infoOffset = 0;
			path = newPath;
			int flags = ((stage & unchecked((int)(0x3))) << 12);
			if (path.Length < NAME_MASK)
			{
				flags |= path.Length;
			}
			else
			{
				flags |= NAME_MASK;
			}
			NB.EncodeInt16(info, infoOffset + P_FLAGS, flags);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Write(OutputStream os)
		{
			int len = IsExtended() ? INFO_LEN_EXTENDED : INFO_LEN;
			int pathLen = path.Length;
			os.Write(info, infoOffset, len);
			os.Write(path, 0, pathLen);
			// Index records are padded out to the next 8 byte alignment
			// for historical reasons related to how C Git read the files.
			//
			int actLen = len + pathLen;
			int expLen = (actLen + 8) & ~7;
			if (actLen != expLen)
			{
				os.Write(nullpad, 0, expLen - actLen);
			}
		}

		/// <summary>
		/// Is it possible for this entry to be accidentally assumed clean?
		/// <p>
		/// The "racy git" problem happens when a work file can be updated faster
		/// than the filesystem records file modification timestamps.
		/// </summary>
		/// <remarks>
		/// Is it possible for this entry to be accidentally assumed clean?
		/// <p>
		/// The "racy git" problem happens when a work file can be updated faster
		/// than the filesystem records file modification timestamps. It is possible
		/// for an application to edit a work file, update the index, then edit it
		/// again before the filesystem will give the work file a new modification
		/// timestamp. This method tests to see if file was written out at the same
		/// time as the index.
		/// </remarks>
		/// <param name="smudge_s">seconds component of the index's last modified time.</param>
		/// <param name="smudge_ns">nanoseconds component of the index's last modified time.</param>
		/// <returns>true if extra careful checks should be used.</returns>
		public bool MightBeRacilyClean(int smudge_s, int smudge_ns)
		{
			// If the index has a modification time then it came from disk
			// and was not generated from scratch in memory. In such cases
			// the entry is 'racily clean' if the entry's cached modification
			// time is equal to or later than the index modification time. In
			// such cases the work file is too close to the index to tell if
			// it is clean or not based on the modification time alone.
			//
			int @base = infoOffset + P_MTIME;
			int mtime = NB.DecodeInt32(info, @base);
			if (smudge_s == mtime)
			{
				return smudge_ns <= NB.DecodeInt32(info, @base + 4);
			}
			return false;
		}

		/// <summary>Force this entry to no longer match its working tree file.</summary>
		/// <remarks>
		/// Force this entry to no longer match its working tree file.
		/// <p>
		/// This avoids the "racy git" problem by making this index entry no longer
		/// match the file in the working directory. Later git will be forced to
		/// compare the file content to ensure the file matches the working tree.
		/// </remarks>
		public void SmudgeRacilyClean()
		{
			// To mark an entry racily clean we set its length to 0 (like native git
			// does). Entries which are not racily clean and have zero length can be
			// distinguished from racily clean entries by checking P_OBJECTID
			// against the SHA1 of empty content. When length is 0 and P_OBJECTID is
			// different from SHA1 of empty content we know the entry is marked
			// racily clean
			int @base = infoOffset + P_SIZE;
			Arrays.Fill(info, @base, @base + 4, unchecked((byte)0));
		}

		/// <summary>
		/// Check whether this entry has been smudged or not
		/// <p>
		/// If a blob has length 0 we know his id see
		/// <see cref="NGit.Constants.EMPTY_BLOB_ID">NGit.Constants.EMPTY_BLOB_ID</see>
		/// . If an entry
		/// has length 0 and an ID different from the one for empty blob we know this
		/// entry was smudged.
		/// </summary>
		/// <returns>
		/// <code>true</code> if the entry is smudged, <code>false</code>
		/// otherwise
		/// </returns>
		public bool IsSmudged()
		{
			int @base = infoOffset + P_OBJECTID;
			return (GetLength() == 0) && (Constants.EMPTY_BLOB_ID.CompareTo(info, @base) != 0
				);
		}

		internal byte[] IdBuffer()
		{
			return info;
		}

		internal int IdOffset()
		{
			return infoOffset + P_OBJECTID;
		}

		/// <summary>
		/// Is this entry always thought to be unmodified?
		/// <p>
		/// Most entries in the index do not have this flag set.
		/// </summary>
		/// <remarks>
		/// Is this entry always thought to be unmodified?
		/// <p>
		/// Most entries in the index do not have this flag set. Users may however
		/// set them on if the file system stat() costs are too high on this working
		/// directory, such as on NFS or SMB volumes.
		/// </remarks>
		/// <returns>true if we must assume the entry is unmodified.</returns>
		public virtual bool IsAssumeValid()
		{
			return (info[infoOffset + P_FLAGS] & ASSUME_VALID) != 0;
		}

		/// <summary>Set the assume valid flag for this entry,</summary>
		/// <param name="assume">
		/// true to ignore apparent modifications; false to look at last
		/// modified to detect file modifications.
		/// </param>
		public virtual void SetAssumeValid(bool assume)
		{
			if (assume)
			{
				info[infoOffset + P_FLAGS] |= ASSUME_VALID;
			}
			else
			{
				info[infoOffset + P_FLAGS] &= unchecked((byte)~ASSUME_VALID);
			}
		}

		/// <returns>true if this entry should be checked for changes</returns>
		public virtual bool IsUpdateNeeded()
		{
			return (inCoreFlags & UPDATE_NEEDED) != 0;
		}

		/// <summary>Set whether this entry must be checked for changes</summary>
		/// <param name="updateNeeded"></param>
		public virtual void SetUpdateNeeded(bool updateNeeded)
		{
			if (updateNeeded)
			{
				inCoreFlags |= UPDATE_NEEDED;
			}
			else
			{
				inCoreFlags &= unchecked((byte)~UPDATE_NEEDED);
			}
		}

		/// <summary>Get the stage of this entry.</summary>
		/// <remarks>
		/// Get the stage of this entry.
		/// <p>
		/// Entries have one of 4 possible stages: 0-3.
		/// </remarks>
		/// <returns>the stage of this entry.</returns>
		public virtual int GetStage()
		{
			return (info[infoOffset + P_FLAGS] >> 4) & unchecked((int)(0x3));
		}

		/// <summary>Returns whether this entry should be skipped from the working tree.</summary>
		/// <remarks>Returns whether this entry should be skipped from the working tree.</remarks>
		/// <returns>true if this entry should be skipepd.</returns>
		public virtual bool IsSkipWorkTree()
		{
			return (GetExtendedFlags() & SKIP_WORKTREE) != 0;
		}

		/// <summary>Returns whether this entry is intent to be added to the Index.</summary>
		/// <remarks>Returns whether this entry is intent to be added to the Index.</remarks>
		/// <returns>true if this entry is intent to add.</returns>
		public virtual bool IsIntentToAdd()
		{
			return (GetExtendedFlags() & INTENT_TO_ADD) != 0;
		}

		/// <summary>
		/// Obtain the raw
		/// <see cref="NGit.FileMode">NGit.FileMode</see>
		/// bits for this entry.
		/// </summary>
		/// <returns>mode bits for the entry.</returns>
		/// <seealso cref="NGit.FileMode.FromBits(int)">NGit.FileMode.FromBits(int)</seealso>
		public virtual int GetRawMode()
		{
			return NB.DecodeInt32(info, infoOffset + P_MODE);
		}

		/// <summary>
		/// Obtain the
		/// <see cref="NGit.FileMode">NGit.FileMode</see>
		/// for this entry.
		/// </summary>
		/// <returns>the file mode singleton for this entry.</returns>
		public virtual FileMode GetFileMode()
		{
			return FileMode.FromBits(GetRawMode());
		}

		/// <summary>Set the file mode for this entry.</summary>
		/// <remarks>Set the file mode for this entry.</remarks>
		/// <param name="mode">the new mode constant.</param>
		/// <exception cref="System.ArgumentException">
		/// If
		/// <code>mode</code>
		/// is
		/// <see cref="NGit.FileMode.MISSING">NGit.FileMode.MISSING</see>
		/// ,
		/// <see cref="NGit.FileMode.TREE">NGit.FileMode.TREE</see>
		/// , or any other type code not permitted
		/// in a tree object.
		/// </exception>
		public virtual void SetFileMode(FileMode mode)
		{
			switch (mode.GetBits() & FileMode.TYPE_MASK)
			{
				case FileMode.TYPE_MISSING:
				case FileMode.TYPE_TREE:
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidModeForPath
						, mode, GetPathString()));
				}
			}
			NB.EncodeInt32(info, infoOffset + P_MODE, mode.GetBits());
		}

		/// <summary>Get the cached last modification date of this file, in milliseconds.</summary>
		/// <remarks>
		/// Get the cached last modification date of this file, in milliseconds.
		/// <p>
		/// One of the indicators that the file has been modified by an application
		/// changing the working tree is if the last modification time for the file
		/// differs from the time stored in this entry.
		/// </remarks>
		/// <returns>
		/// last modification time of this file, in milliseconds since the
		/// Java epoch (midnight Jan 1, 1970 UTC).
		/// </returns>
		public virtual long GetLastModified()
		{
			return DecodeTS(P_MTIME);
		}

		/// <summary>Set the cached last modification date of this file, using milliseconds.</summary>
		/// <remarks>Set the cached last modification date of this file, using milliseconds.</remarks>
		/// <param name="when">new cached modification date of the file, in milliseconds.</param>
		public virtual void SetLastModified(long when)
		{
			EncodeTS(P_MTIME, when);
		}

		/// <summary>Get the cached size (in bytes) of this file.</summary>
		/// <remarks>
		/// Get the cached size (in bytes) of this file.
		/// <p>
		/// One of the indicators that the file has been modified by an application
		/// changing the working tree is if the size of the file (in bytes) differs
		/// from the size stored in this entry.
		/// <p>
		/// Note that this is the length of the file in the working directory, which
		/// may differ from the size of the decompressed blob if work tree filters
		/// are being used, such as LF<->CRLF conversion.
		/// </remarks>
		/// <returns>cached size of the working directory file, in bytes.</returns>
		public virtual int GetLength()
		{
			return NB.DecodeInt32(info, infoOffset + P_SIZE);
		}

		/// <summary>Set the cached size (in bytes) of this file.</summary>
		/// <remarks>Set the cached size (in bytes) of this file.</remarks>
		/// <param name="sz">new cached size of the file, as bytes.</param>
		public virtual void SetLength(int sz)
		{
			NB.EncodeInt32(info, infoOffset + P_SIZE, sz);
		}

		/// <summary>Set the cached size (in bytes) of this file.</summary>
		/// <remarks>Set the cached size (in bytes) of this file.</remarks>
		/// <param name="sz">new cached size of the file, as bytes.</param>
		/// <exception cref="System.ArgumentException">
		/// if the size exceeds the 2 GiB barrier imposed by current file
		/// format limitations.
		/// </exception>
		public virtual void SetLength(long sz)
		{
			if (int.MaxValue <= sz)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().sizeExceeds2GB, GetPathString
					(), sz));
			}
			SetLength((int)sz);
		}

		/// <summary>Obtain the ObjectId for the entry.</summary>
		/// <remarks>
		/// Obtain the ObjectId for the entry.
		/// <p>
		/// Using this method to compare ObjectId values between entries is
		/// inefficient as it causes memory allocation.
		/// </remarks>
		/// <returns>object identifier for the entry.</returns>
		public virtual ObjectId GetObjectId()
		{
			return ObjectId.FromRaw(IdBuffer(), IdOffset());
		}

		/// <summary>Set the ObjectId for the entry.</summary>
		/// <remarks>Set the ObjectId for the entry.</remarks>
		/// <param name="id">
		/// new object identifier for the entry. May be
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// to remove the current identifier.
		/// </param>
		public virtual void SetObjectId(AnyObjectId id)
		{
			id.CopyRawTo(IdBuffer(), IdOffset());
		}

		/// <summary>Set the ObjectId for the entry from the raw binary representation.</summary>
		/// <remarks>Set the ObjectId for the entry from the raw binary representation.</remarks>
		/// <param name="bs">
		/// the raw byte buffer to read from. At least 20 bytes after p
		/// must be available within this byte array.
		/// </param>
		/// <param name="p">position to read the first byte of data from.</param>
		public virtual void SetObjectIdFromRaw(byte[] bs, int p)
		{
			int n = Constants.OBJECT_ID_LENGTH;
			System.Array.Copy(bs, p, IdBuffer(), IdOffset(), n);
		}

		/// <summary>Get the entry's complete path.</summary>
		/// <remarks>
		/// Get the entry's complete path.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>
		/// complete path of the entry, from the root of the repository. If
		/// the entry is in a subtree there will be at least one '/' in the
		/// returned string.
		/// </returns>
		public virtual string GetPathString()
		{
			return ToString(path);
		}

		/// <summary>Copy the ObjectId and other meta fields from an existing entry.</summary>
		/// <remarks>
		/// Copy the ObjectId and other meta fields from an existing entry.
		/// <p>
		/// This method copies everything except the path from one entry to another,
		/// supporting renaming.
		/// </remarks>
		/// <param name="src">the entry to copy ObjectId and meta fields from.</param>
		public virtual void CopyMetaData(NGit.Dircache.DirCacheEntry src)
		{
			int pLen = NB.DecodeUInt16(info, infoOffset + P_FLAGS) & NAME_MASK;
			System.Array.Copy(src.info, src.infoOffset, info, infoOffset, INFO_LEN);
			NB.EncodeInt16(info, infoOffset + P_FLAGS, pLen | NB.DecodeUInt16(info, infoOffset
				 + P_FLAGS) & ~NAME_MASK);
		}

		/// <returns>true if the entry contains extended flags.</returns>
		internal virtual bool IsExtended()
		{
			return (info[infoOffset + P_FLAGS] & EXTENDED) != 0;
		}

		private long DecodeTS(int pIdx)
		{
			int @base = infoOffset + pIdx;
			int sec = NB.DecodeInt32(info, @base);
			int ms = NB.DecodeInt32(info, @base + 4) / 1000000;
			return 1000L * sec + ms;
		}

		private void EncodeTS(int pIdx, long when)
		{
			int @base = infoOffset + pIdx;
			NB.EncodeInt32(info, @base, (int)(when / 1000));
			NB.EncodeInt32(info, @base + 4, ((int)(when % 1000)) * 1000000);
		}

		private int GetExtendedFlags()
		{
			if (IsExtended())
			{
				return NB.DecodeUInt16(info, infoOffset + P_FLAGS2) << 16;
			}
			else
			{
				return 0;
			}
		}

		private static string ToString(byte[] path)
		{
			return Constants.CHARSET.Decode(ByteBuffer.Wrap(path)).ToString();
		}

		internal static bool IsValidPath(byte[] path)
		{
			if (path.Length == 0)
			{
				return false;
			}
			// empty path is not permitted.
			bool componentHasChars = false;
			foreach (byte c in path)
			{
				switch (c)
				{
					case 0:
					{
						return false;
					}

					case (byte)('/'):
					{
						// NUL is never allowed within the path.
						if (componentHasChars)
						{
							componentHasChars = false;
						}
						else
						{
							return false;
						}
						break;
					}

					default:
					{
						componentHasChars = true;
						break;
					}
				}
			}
			return componentHasChars;
		}

		internal static int GetMaximumInfoLength(bool extended)
		{
			return extended ? INFO_LEN_EXTENDED : INFO_LEN;
		}
	}
}
