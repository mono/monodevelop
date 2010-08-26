/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <me@lathund.dewire.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Roger C. Soares <rogersoares@intelinet.com.br>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using MiscUtil.Conversion;
using MiscUtil.IO;
using System.Text;

namespace GitSharp.Core
{
	/// <summary>
	/// A representation of the Git index.
	/// 
	/// The index points to the objects currently checked out or in the process of
	/// being prepared for committing or objects involved in an unfinished merge.
	/// 
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
	/// </summary>
	//[Obsolete("Use DirCache instead.")]
	public class GitIndex
	{
		/// <summary>
		/// Stage 0 represents merged entries.
		/// </summary>
		private const int STAGE_0 = 0;
		private static bool? filemode = null; // needed for testing.

		private readonly IDictionary<byte[], Entry> _entries;
		private readonly FileInfo _cacheFile;

		// Index is modified
		private bool _changed;
		private Header _header;
		private long _lastCacheTime;
		private bool _statDirty;

		/// <summary>
		/// Encoding that is used for encoding / decoding filenames only
		/// </summary>
		//public Encoding FilenameEncoding
		//{
		//   get;
		//   set;
		//}

		///	<summary>
		/// Construct a Git index representation.
		/// </summary>
		///	<param name="db"> </param>
		public GitIndex(Repository db)
		{
			//FilenameEncoding = Constants.CHARSET; //  defaults to UTF8 actually
			_entries = new SortedDictionary<byte[], Entry>(new ByteVectorComparer());
			Repository = db;
			_cacheFile = db.getIndexFile();
		}

		public Repository Repository { get; private set; }

		///	<returns>
		/// True if we have modified the index in memory since reading it from disk.
		/// </returns>
		public bool IsChanged
		{
			get { return _changed || _statDirty; }
		}

		///	<summary>
		/// Return the members of the index sorted by the unsigned byte
		///	values of the path names.
		///	
		///	Small beware: Unaccounted for are unmerged entries. You may want
		///	to abort if members with stage != 0 are found if you are doing
		///	any updating operations. All stages will be found after one another
		///	here later. Currently only one stage per name is returned.	
		///	</summary>
		///	<returns> 
		/// The index entries sorted 
		/// </returns>
		public IList<Entry> Members
		{
			get { return _entries.Values.ToList(); }
		}

		///	<summary>
		/// Reread index data from disk if the index file has been changed
		/// </summary>
		///	<exception cref="IOException"> </exception>
		public void RereadIfNecessary()
		{
            _cacheFile.Refresh();
			if (_cacheFile.Exists && _cacheFile.lastModified() != _lastCacheTime)
			{
				Read();
				Repository.fireIndexChanged();
			}
		}

		///	<summary>
		/// Add the content of a file to the index.
		///	</summary>
		///	<param name="wd"> workdir </param>
		///	<param name="f"> the file </param>
		///	<returns> a new or updated index entry for the path represented by f</returns>
		///	<exception cref="IOException"> </exception>
		public Entry add(FileSystemInfo wd, FileInfo f)
		{
			return add(wd, f, null);
		}

		///	<summary>
		/// Add the content of a file to the index.
		///	</summary>
		///	<param name="wd">workdir</param>
		///	<param name="f">the file</param>
		/// <param name="content">content of the file</param>
		///	<returns> a new or updated index entry for the path represented by f </returns>
		/// <exception cref="IOException"> </exception>
		public Entry add(FileSystemInfo wd, FileInfo f, byte[] content)
		{
			byte[] key = MakeKey(wd, f);
			Entry e;

			if (!_entries.TryGetValue(key, out e))
			{
				e = new Entry(Repository, key, f, STAGE_0, content);
				_entries[key] = e;
			}
			else
			{
				e.update(f);
			}

			return e;
		}

		/// <summary>
		/// Add the encoded filename and content of a file to the index.
		/// </summary>
		/// <param name="relative_filename">relative filename with respect to the working directory</param>
		/// <param name="content"></param>
		/// <returns></returns>
		public Entry add(byte[] relative_filename, byte[] content)
		{
			byte[] key = relative_filename;
			Entry e;

			if (!_entries.TryGetValue(key, out e))
			{
				e = new Entry(Repository, key, null, STAGE_0, content);
				_entries[key] = e;
			}
			else
			{
				e.update(null, content);
			}

			return e;
		}

		public bool Remove(string relative_file_path)
		{
			if (string.IsNullOrEmpty(relative_file_path))
				return false;
			var key = Core.Repository.GitInternalSlash(Constants.encode(relative_file_path));
			return _entries.Remove(key);
		}

		/// <summary>
		/// Remove a path from the index.
		/// </summary>
		/// <param name="wd"> workdir </param>
		/// <param name="f"> the file whose path shall be removed. </param>
		/// <returns> true if such a path was found (and thus removed) </returns>
		/// <exception cref="IOException">  </exception>
		public bool remove(FileSystemInfo wd, FileSystemInfo f)
		{
			byte[] key = MakeKey(wd, f);
			return _entries.Remove(key);
		}

		///	<summary>
		/// Read the cache file into memory.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		public void Read()
		{
			_changed = false;
			_statDirty = false;

			_cacheFile.Refresh();

			if (!_cacheFile.Exists)
			{
				_header = null;
				_entries.Clear();
				_lastCacheTime = 0;
				return;
			}

			using (var cache = new FileStream(_cacheFile.FullName, System.IO.FileMode.Open))
			using (var buffer = new EndianBinaryReader(new BigEndianBitConverter(), cache))
			{
				_header = new Header(buffer);
				_entries.Clear();

				for (int i = 0; i < _header.Entries; ++i)
				{
					var entry = new Entry(Repository, buffer);
					_entries[Constants.encode(entry.Name)] = entry;
				}

				_lastCacheTime = _cacheFile.lastModified();
			}
		}

		///	<summary>
		/// Write content of index to disk.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		public void write()
		{
			CheckWriteOk();
			var tmpIndex = new FileInfo(_cacheFile.FullName + ".tmp");
			var @lock = new FileInfo(_cacheFile.FullName + ".lock");

			try
			{
				using (@lock.Create())
				{
				}
			}
			catch (IOException)
			{
				throw new IOException("Index file is in use");
			}

			try
			{
				using (var fileOutputStream = new FileStream(tmpIndex.FullName, System.IO.FileMode.CreateNew))
				using (var writer = new EndianBinaryWriter(new BigEndianBitConverter(), fileOutputStream))
				{
					MessageDigest newMessageDigest = Constants.newMessageDigest();
					var ms = new MemoryStream();

					_header = new Header(_entries.Values as ICollection);
					_header.Write(ms);

					newMessageDigest.Update(ms.ToArray());
					writer.Write(ms.ToArray());

					foreach (Entry entry in _entries.Values)
					{
						ms = new MemoryStream();

						entry.Write(ms);
						newMessageDigest.Update(ms.ToArray());
						writer.Write(ms.ToArray());
					}

					ms = new MemoryStream();

					byte[] digestBuffer = newMessageDigest.Digest();
					ms.Write(digestBuffer, 0, digestBuffer.Length);
					writer.Write(ms.ToArray(), 0, digestBuffer.Length);
				}

				_cacheFile.Refresh();
				if (_cacheFile.Exists)
				{
					try
					{
						_cacheFile.Delete();
					}
					catch (IOException)
					{
						throw new IOException("Could not rename delete old index");
					}
				}

				if (!tmpIndex.RenameTo(_cacheFile.FullName))
				{
					throw new IOException("Could not rename temporary index file to index");
				}

				_changed = false;
				_statDirty = false;
			    _cacheFile.Refresh();
				_lastCacheTime = _cacheFile.lastModified();
				Repository.fireIndexChanged();
			}
			finally
			{
				try
				{
					@lock.Delete();
				}
				catch (IOException)
				{
					throw new IOException("Could not delete lock file. Should not happen");
				}

				try
				{
					tmpIndex = new FileInfo(_cacheFile.FullName + ".tmp");
					if (tmpIndex.Exists)
					{
						tmpIndex.Delete();
					}
				}
				catch (Exception)
				{
					throw new IOException("Could not delete temporary index file. Should not happen");
				}
			}
		}

		private void CheckWriteOk()
		{
			foreach (Entry e in _entries.Values)
			{
				if (e.Stage != STAGE_0)
				{
					throw new NotSupportedException(
						 "Cannot work with other stages than zero right now. Won't write corrupt index.");
				}
			}
		}

		private static bool FileCanExecute(FileSystemInfo f)
		{
			return FS.canExecute(f);
		}

		private static bool FileSetExecute(FileInfo f, bool @value)
		{
			return FS.setExecute(f, @value);
		}

		private static bool FileHasExecute()
		{
			return FS.supportsExecute();
		}

		private byte[] MakeKey(FileSystemInfo wd, FileSystemInfo f)
		{
			if (!string.IsNullOrEmpty(f.DirectoryName()) &&
				 wd.IsDirectory() && wd.Exists &&
				 !f.DirectoryName().StartsWith(wd.DirectoryName()))
			{
				throw new Exception("Path is not in working dir");
			}

			string relName = Repository.StripWorkDir(wd, f);
			return Core.Repository.GitInternalSlash(Constants.encode(relName));
		}

		private static bool ConfigFilemode(Repository repository)
		{
			// temporary til we can actually set parameters. We need to be able
			// to change this for testing.
			if (filemode != null)
			{
				return filemode.Value;
			}

			RepositoryConfig config = repository.Config;
			return config.getBoolean("core", null, "filemode", true);
		}

		///	<summary>
		/// Read a Tree recursively into the index
		/// </summary>
		/// <param name="t">The tree to read</param>
		///	<exception cref="IOException"></exception>
		public void ReadTree(Tree t)
		{
			_entries.Clear();
			ReadTree(string.Empty, t);
		}

		private void ReadTree(string prefix, Tree t)
		{
			TreeEntry[] members = t.Members;
			for (int i = 0; i < members.Length; ++i)
			{
				TreeEntry te = members[i];
				string name;
				if (prefix.Length > 0)
				{
					name = prefix + "/" + te.Name;
				}
				else
				{
					name = te.Name;
				}
				Tree tr = (te as Tree);
				if (tr != null)
				{
					ReadTree(name, tr);
				}
				else
				{
					var e = new Entry(Repository, te, 0);
					_entries[Constants.encode(name)] = e;
				}
			}
		}

		///	<summary>
		/// Add tree entry to index
		/// </summary>
		///	<param name="te"> tree entry </param>
		///	<returns> new or modified index entry </returns>
		///	<exception cref="IOException"> </exception>
		public Entry addEntry(TreeEntry te)
		{
			byte[] key = Constants.encode(te.FullName);
			var e = new Entry(Repository, te, 0);
			_entries[key] = e;
			return e;
		}

		///	<summary>
		/// Check out content of the content represented by the index
		///	</summary>
		///	<param name="workDir">workdir </param>
		///	<exception cref="IOException"> </exception>
		public void checkout(FileSystemInfo workDir)
		{
			foreach (Entry e in _entries.Values)
			{
				if (e.Stage != STAGE_0)
				{
					continue;
				}

				checkoutEntry(workDir, e);
			}
		}

		/// <summary>
		/// Check out content of the specified index entry
		/// </summary>
		/// <param name="workDir">workdir</param>
		/// <param name="e">index entry</param>
		/// <exception cref="IOException"></exception>
		public void checkoutEntry(FileSystemInfo workDir, Entry e)
		{
			ObjectLoader ol = Repository.OpenBlob(e.ObjectId);
			byte[] bytes = ol.Bytes;

			var file = new FileInfo(Path.Combine(workDir.DirectoryName(), e.Name));

			if (file.Exists)
			{
				file.Delete();
			}

			file.Directory.Mkdirs();

			using (var fs = new FileStream(file.FullName, System.IO.FileMode.CreateNew))
			{
				var ms = new MemoryStream(bytes);
				ms.WriteTo(fs);
			}

			if (ConfigFilemode(Repository) && FileHasExecute())
			{
				if (FileMode.ExecutableFile.Equals(e.Mode))
				{
					if (!FileCanExecute(file))
					{
						FileSetExecute(file, true);
					}
				}
				else if (FileCanExecute(file))
				{
					FileSetExecute(file, false);
				}
			}

			e.Mtime = file.lastModified() * 1000000L;
			e.Ctime = e.Mtime;
		}

		///	<summary>
		/// Construct and write tree out of index.
		///	</summary>
		///	<returns> SHA-1 of the constructed tree</returns>
		/// <exception cref="IOException"></exception>
		public ObjectId writeTree()
		{
			CheckWriteOk();
			var writer = new ObjectWriter(Repository);
			var current = new Tree(Repository);
			var trees = new Stack<Tree>();
			trees.Push(current);
			var prevName = new string[0];

			foreach (Entry e in _entries.Values)
			{
				if (e.Stage != STAGE_0)
				{
					continue;
				}

				string[] newName = SplitDirPath(e.Name);
				int c = LongestCommonPath(prevName, newName);
				while (c < trees.Count - 1)
				{
					current.Id = writer.WriteTree(current);
					trees.Pop();
					current = trees.Count == 0 ? null : trees.Peek();
				}

				while (trees.Count < newName.Length)
				{
					if (!current.ExistsTree(newName[trees.Count - 1]))
					{
						current = new Tree(current, Constants.encode(newName[trees.Count - 1]));
						current.Parent.AddEntry(current);
						trees.Push(current);
					}
					else
					{
						current = (Tree)current.findTreeMember(newName[trees.Count - 1]);
						trees.Push(current);
					}
				}

				var ne = new FileTreeEntry(current, e.ObjectId, Constants.encode(newName[newName.Length - 1]),
													(e.Mode & FileMode.ExecutableFile.Bits) == FileMode.ExecutableFile.Bits);
				current.AddEntry(ne);
			}

			while (trees.Count != 0)
			{
				current.Id = writer.WriteTree(current);
				trees.Pop();

				if (trees.Count != 0)
				{
					current = trees.Peek();
				}
			}

			return current.TreeId;
		}

		internal string[] SplitDirPath(string name)
		{
			// TODO : Maybe should we rely on a plain string.Split(). Seems to deliver the expected output.
			var tmp = new string[name.Length / 2 + 1];
			int p0 = -1;
			int p1;
			int c = 0;
			while ((p1 = name.IndexOf('/', p0 + 1)) != -1)
			{
				tmp[c++] = name.Slice(p0 + 1, p1);
				p0 = p1;
			}
			tmp[c++] = name.Substring(p0 + 1);
			var ret = new string[c];
			for (int i = 0; i < c; ++i)
			{
				ret[i] = tmp[i];
			}
			return ret;
		}

		internal int LongestCommonPath(string[] a, string[] b)
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
		/// Look up an entry with the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>Index entry for the path or null if not in index.</returns>
		public Entry GetEntry(string path)
		{
			byte[] val = Repository.GitInternalSlash(Constants.encode(path));
			return _entries.Where(e => e.Key.SequenceEqual(val)).FirstOrDefault().Value;
		}

		#region Nested Types

		#region Nested type: ByteVectorComparer

		private class ByteVectorComparer : IComparer<byte[]>
		{
			#region IComparer<byte[]> Members

			public int Compare(byte[] x, byte[] y)
			{
				for (int i = 0; i < x.Length && i < y.Length; ++i)
				{
					int c = x[i] - y[i];
					if (c != 0)
					{
						return c;
					}
				}

				if (x.Length < y.Length)
				{
					return -1;
				}

				if (x.Length > y.Length)
				{
					return 1;
				}

				return 0;
			}

			#endregion
		}

		#endregion

		#region Nested type: Entry

		/// <summary>
		/// An index entry
		/// </summary>
		public class Entry
		{
			private readonly int _dev;
			private readonly int _gid;
			private readonly int _ino;
			private readonly byte[] _name;
			private readonly int _uid;

			private short _flags;
			private int _size;

			internal Entry(Repository repository, byte[] key, FileInfo f, int stage)
				: this(repository, key, f, stage, null)
			{
			}

			internal Entry(Repository repository, byte[] key, FileInfo f, int stage, byte[] newContent)
				: this(repository)
			{
				if (newContent == null && f == null)
					throw new ArgumentException("either file or newContent must be non-null");

				Ctime = f.lastModified() * 1000000L;
			    Mtime = Ctime; // we use same here
				_dev = -1;
				_ino = -1;

				if (ConfigFilemode(Repository) && FileCanExecute(f))
				{
					Mode = FileMode.ExecutableFile.Bits;
				}
				else
				{
					Mode = FileMode.RegularFile.Bits;
				}

				_uid = -1;
				_gid = -1;
				_size = ((newContent == null || newContent.Length == 0) && f != null) ? (int)(f).Length : newContent.Length;
				var writer = new ObjectWriter(Repository);
				if ((newContent == null || newContent.Length == 0) && f != null)
				{
					ObjectId = writer.WriteBlob(f);
				}
				else
				{
					ObjectId = writer.WriteBlob(newContent);
				}
				_name = key;
				_flags = (short)((stage << 12) | _name.Length); // TODO: fix _flags
			}

			internal Entry(Repository repository, TreeEntry f, int stage)
				: this(repository)
			{
				Ctime = -1; // hmm
				Mtime = -1;
				_dev = -1;
				_ino = -1;
				Mode = f.Mode.Bits;
				_uid = -1;
				_gid = -1;
				try
				{
					_size = (int)Repository.OpenBlob(f.Id).Size;
				}
				catch (IOException e)
				{
					e.printStackTrace();
					_size = -1;
				}
				ObjectId = f.Id;
				_name = Constants.encode(f.FullName);
				_flags = (short)((stage << 12) | _name.Length); // TODO: fix _flags
			}

			internal Entry(Repository repository, EndianBinaryReader b)
				: this(repository)
			{
				long startposition = b.BaseStream.Position;
				Ctime = b.ReadInt32() * 1000000000L + (b.ReadInt32() % 1000000000L);
				Mtime = b.ReadInt32() * 1000000000L + (b.ReadInt32() % 1000000000L);
				_dev = b.ReadInt32();
				_ino = b.ReadInt32();
				Mode = b.ReadInt32();
				_uid = b.ReadInt32();
				_gid = b.ReadInt32();
				_size = b.ReadInt32();
				byte[] sha1Bytes = b.ReadBytes(Constants.OBJECT_ID_LENGTH);
				ObjectId = ObjectId.FromRaw(sha1Bytes);
				_flags = b.ReadInt16();
				_name = b.ReadBytes(_flags & 0xFFF);
				b.BaseStream.Position = startposition +
												((8 + 8 + 4 + 4 + 4 + 4 + 4 + 4 + 20 + 2 + _name.Length + 8) & ~7);
			}

			private Entry(Repository repository)
			{
				Repository = repository;
				ConfigFileMode = ConfigFilemode(repository);
			}

			public ObjectId ObjectId { get; private set; }
			public Repository Repository { get; private set; }
			public bool ConfigFileMode { get; private set; }
			public int Mode { get; private set; }

			public long Ctime { get; set; }
			public long Mtime { get; set; }

			/// <returns> path name for this entry </returns>
			public string Name
			{
				get { return RawParseUtils.decode(_name); }
			}

			///	<returns> path name for this entry as byte array, hopefully UTF-8 encoded </returns>
			public byte[] NameUTF8
			{
				get { return _name; }
			}

			///	<returns> the stage this entry is in </returns>
			public int Stage
			{
				get { return (_flags & 0x3000) >> 12; }
			}

			///	<returns> size of disk object </returns>
			public int Size
			{
				get { return _size; }
			}

			///	<summary>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </summary>
			/// <param name="f">file in work dir</param>
			/// <returns> true if a change occurred </returns>
			/// <exception cref="IOException"></exception>
			public bool update(FileInfo f)
			{
				long lm = f.lastModified() * 1000000L;
				bool modified = Mtime != lm;
				Mtime = lm;

				if (_size != f.Length)
				{
					modified = true;
				}

				if (ConfigFileMode)
				{
					if (FileCanExecute(f) != FileMode.ExecutableFile.Equals(Mode))
					{
						Mode = FileMode.ExecutableFile.Bits;
						modified = true;
					}
				}

				if (modified)
				{
					_size = (int)f.Length;
					var writer = new ObjectWriter(Repository);
					ObjectId newsha1 = ObjectId = writer.WriteBlob(f);

					modified = !newsha1.Equals(ObjectId);
					ObjectId = newsha1;
				}

				return modified;
			}

			///	<summary>
			/// Update this index entry with stat and SHA-1 information if it looks
			/// like the file has been modified in the workdir.
			/// </summary>
			/// <param name="f">file in work dir</param>
			/// <param name="newContent">the new content of the file </param>
			/// <returns> true if a change occurred </returns>
			/// <exception cref="IOException"></exception>
			public bool update(FileInfo f, byte[] newContent) //[henon] TODO: remove parameter f as it is useless
			{
				bool modified = false;
				_size = newContent.Length;
				var writer = new ObjectWriter(Repository);
				ObjectId newsha1 = ObjectId = writer.WriteBlob(newContent);

				if (!newsha1.Equals(ObjectId))
				{
					modified = true;
				}

				ObjectId = newsha1;
				return modified;
			}

			internal void Write(MemoryStream buf)
			{
				var t = new BigEndianBitConverter();

				long startposition = buf.Position;

				var tmpBuffer = t.GetBytes((int)(Ctime / 1000000000L));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((int)(Ctime % 1000000000L));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((int)(Mtime / 1000000000L));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((int)(Mtime % 1000000000L));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((_dev));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((_ino));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((Mode));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((_uid));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((_gid));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes((_size));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				ObjectId.copyRawTo(buf);

				tmpBuffer = t.GetBytes((_flags));
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				buf.Write(_name, 0, _name.Length);

				long end = startposition
						  + ((8 + 8 + 4 + 4 + 4 + 4 + 4 + 4 + 20 + 2 + _name.Length + 8) & ~7);
				long remain = end - buf.Position;
				while (remain-- > 0)
				{
					buf.WriteByte(0);
				}
			}

			///	<summary>
			/// Check if an entry's content is different from the cache, 
			/// 
			/// File status information is used and status is same we
			///	consider the file identical to the state in the working
			/// directory. Native git uses more stat fields than we
			/// have accessible in Java.
			/// </summary>
			/// <param name="wd"> working directory to compare content with </param>
			/// <returns> true if content is most likely different. </returns>	 
			public bool IsModified(DirectoryInfo wd)
			{
				return IsModified(wd, false);
			}

			///	<summary>
			/// Check if an entry's content is different from the cache, 
			/// 
			/// File status information is used and status is same we
			///	consider the file identical to the state in the working
			/// directory. Native git uses more stat fields than we
			/// have accessible in Java.
			/// </summary>
			/// <param name="wd"> working directory to compare content with </param>
			/// <param name="forceContentCheck"> 
			/// True if the actual file content should be checked if modification time differs.
			/// </param>
			/// <returns> true if content is most likely different. </returns>
			public bool IsModified(DirectoryInfo wd, bool forceContentCheck)
			{
				if (isAssumedValid())
				{
					return false;
				}

				if (isUpdateNeeded())
				{
					return true;
				}

				FileInfo file = getFile(wd);
				if (!file.Exists)
				{
					return true;
				}

				// JDK1.6 has file.canExecute
				// if (file.canExecute() != FileMode.EXECUTABLE_FILE.equals(mode))
				// return true;
				int exebits = FileMode.ExecutableFile.Bits ^ FileMode.RegularFile.Bits;

				if (ConfigFileMode && FileMode.ExecutableFile.Equals(Mode))
				{
					if (!FileCanExecute(file) && FileHasExecute())
						return true;
				}
				else
				{
					if (FileMode.RegularFile.Equals(Mode & ~exebits))
					{
						if (!File.Exists(file.FullName) || ConfigFileMode && FileCanExecute(file) && FileHasExecute())
						{
							return true;
						}
					}
					else
					{
						if (FileMode.Symlink.Equals(Mode))
						{
							return true;
						}

						if (FileMode.Tree.Equals(Mode))
						{
							if (!Directory.Exists(file.FullName))
							{
								return true;
							}
						}
						else
						{
							Console.WriteLine("Does not handle mode " + Mode + " (" + file + ")");
							return true;
						}
					}
				}

				if (file.Length != _size)
				{
					return true;
				}

				// Git under windows only stores seconds so we round the timestamp
				// Java gives us if it looks like the timestamp in index is seconds
				// only. Otherwise we compare the timestamp at millisecond prevision.
				long javamtime = Mtime / 1000000L;
				long lastm = file.lastModified();

				if (javamtime % 1000 == 0)
				{
					lastm = lastm - lastm % 1000;
				}
				if (lastm != javamtime)
				{
                    if (!forceContentCheck)
					return true;

					try
					{
						using (Stream @is = new FileStream(file.FullName, System.IO.FileMode.Open, FileAccess.Read))
						{
							try
							{
								var objectWriter = new ObjectWriter(Repository);
								ObjectId newId = objectWriter.ComputeBlobSha1(file.Length, @is);
								bool ret = !newId.Equals(ObjectId);
								return ret;
							}
							catch (IOException e)
							{
								e.printStackTrace();
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
									e.printStackTrace();
								}
							}
						}
					}
					catch (FileNotFoundException e)
					{
						// should not happen because we already checked this
						e.printStackTrace();
						throw;
					}
                }
				return false;
			}

			// for testing
			internal void forceRecheck()
			{
				Mtime = -1;
			}

			private FileInfo getFile(DirectoryInfo wd)
			{
				return new FileInfo(Path.Combine(wd.FullName, Name));
			}

			public override string ToString()
			{
				return Name + "/SHA-1(" +
						 ObjectId.Name + ")/M:" +
                         (Ctime / 1000000L).MillisToUtcDateTime() + "/C:" +
                         (Mtime / 1000000L).MillisToUtcDateTime() + "/d" +
						 _dev +
						 "/i" + _ino +
						 "/m" + Convert.ToString(Mode, 8) +
						 "/u" + _uid +
						 "/g" + _gid +
						 "/s" + _size +
						 "/f" + _flags +
						 "/@" + Stage;
			}

			///	<returns> true if this entry shall be assumed valid </returns>
			public bool isAssumedValid()
			{
				return (_flags & 0x8000) != 0;
			}

			///	<returns> true if this entry should be checked for changes </returns>
			public bool isUpdateNeeded()
			{
				return (_flags & 0x4000) != 0;
			}

			/// <summary>
			/// Set whether to always assume this entry valid
			/// </summary>
			/// <param name="assumeValid"> true to ignore changes </param>
			public void setAssumeValid(bool assumeValid)
			{
				if (assumeValid)
				{
					_flags = Convert.ToInt16(Convert.ToInt32(_flags) | 0x8000);
				}
				else
				{
					_flags = Convert.ToInt16(Convert.ToInt32(_flags) & ~0x8000);
				}
			}

			///	<summary>
			/// Set whether this entry must be checked
			/// </summary>
			/// <param name="updateNeeded"> </param>
			public void setUpdateNeeded(bool updateNeeded)
			{
				if (updateNeeded)
				{
					_flags |= 0x4000;
				}
				else
				{
					_flags &= ~0x4000;
				}
			}

			/// <summary>
			/// Return raw file mode bits. See <seealso cref="FileMode"/>
			/// </summary>
			///	<returns> file mode bits </returns>
			public int getModeBits()
			{
				return Mode;
			}
		}

		#endregion

		#region Nested type: Header

		private class Header
		{
			private int _signature;
			private int _version;

			internal Header(EndianBinaryReader map)
			{
				Read(map);
			}

			internal Header(ICollection entryset)
			{
				_signature = 0x44495243;
				_version = 2;
				Entries = entryset.Count;
			}

			internal int Entries { get; private set; }

			private void Read(EndianBinaryReader buf)
			{
				_signature = buf.ReadInt32();
				_version = buf.ReadInt32();
				Entries = buf.ReadInt32();

				if (_signature != 0x44495243)
				{
					throw new CorruptObjectException("Index signature is invalid: " + _signature);
				}

				if (_version != 2)
				{
					throw new CorruptObjectException("Unknown index version (or corrupt index):" + _version);
				}
			}

			internal void Write(Stream buf)
			{
				var t = new BigEndianBitConverter();
				var tmpBuffer = t.GetBytes(_signature);
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes(_version);
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);

				tmpBuffer = t.GetBytes(Entries);
				buf.Write(tmpBuffer, 0, tmpBuffer.Length);
			}
		}

		#endregion

		#endregion
	}
}