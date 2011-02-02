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
using NGit.Errors;
using NGit.Storage.File;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Traditional file system based
	/// <see cref="NGit.ObjectDatabase">NGit.ObjectDatabase</see>
	/// .
	/// <p>
	/// This is the classical object database representation for a Git repository,
	/// where objects are stored loose by hashing them into directories by their
	/// <see cref="NGit.ObjectId">NGit.ObjectId</see>
	/// , or are stored in compressed containers known as
	/// <see cref="PackFile">PackFile</see>
	/// s.
	/// <p>
	/// Optionally an object database can reference one or more alternates; other
	/// ObjectDatabase instances that are searched in addition to the current
	/// database.
	/// <p>
	/// Databases are divided into two halves: a half that is considered to be fast
	/// to search (the
	/// <code>PackFile</code>
	/// s), and a half that is considered to be slow
	/// to search (loose objects). When alternates are present the fast half is fully
	/// searched (recursively through all alternates) before the slow half is
	/// considered.
	/// </summary>
	public class ObjectDirectory : FileObjectDatabase
	{
		private static readonly ObjectDirectory.PackList NO_PACKS = new ObjectDirectory.PackList
			(FileSnapshot.DIRTY, new PackFile[0]);

		/// <summary>Maximum number of candidates offered as resolutions of abbreviation.</summary>
		/// <remarks>Maximum number of candidates offered as resolutions of abbreviation.</remarks>
		private const int RESOLVE_ABBREV_LIMIT = 256;

		private readonly Config config;

		private readonly FilePath objects;

		private readonly FilePath infoDirectory;

		private readonly FilePath packDirectory;

		private readonly FilePath alternatesFile;

		private readonly AtomicReference<ObjectDirectory.PackList> packList;

		private readonly FS fs;

		private readonly AtomicReference<FileObjectDatabase.AlternateHandle[]> alternates;

		private readonly UnpackedObjectCache unpackedObjectCache;

		/// <summary>Initialize a reference to an on-disk object directory.</summary>
		/// <remarks>Initialize a reference to an on-disk object directory.</remarks>
		/// <param name="cfg">configuration this directory consults for write settings.</param>
		/// <param name="dir">the location of the <code>objects</code> directory.</param>
		/// <param name="alternatePaths">a list of alternate object directories</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		/// <exception cref="System.IO.IOException">an alternate object cannot be opened.</exception>
		public ObjectDirectory(Config cfg, FilePath dir, FilePath[] alternatePaths, FS fs
			)
		{
			config = cfg;
			objects = dir;
			infoDirectory = new FilePath(objects, "info");
			packDirectory = new FilePath(objects, "pack");
			alternatesFile = new FilePath(infoDirectory, "alternates");
			packList = new AtomicReference<ObjectDirectory.PackList>(NO_PACKS);
			unpackedObjectCache = new UnpackedObjectCache();
			this.fs = fs;
			alternates = new AtomicReference<FileObjectDatabase.AlternateHandle[]>();
			if (alternatePaths != null)
			{
				FileObjectDatabase.AlternateHandle[] alt;
				alt = new FileObjectDatabase.AlternateHandle[alternatePaths.Length];
				for (int i = 0; i < alternatePaths.Length; i++)
				{
					alt[i] = OpenAlternate(alternatePaths[i]);
				}
				alternates.Set(alt);
			}
		}

		/// <returns>the location of the <code>objects</code> directory.</returns>
		internal sealed override FilePath GetDirectory()
		{
			return objects;
		}

		public override bool Exists()
		{
			return objects.Exists();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Create()
		{
			FileUtils.Mkdirs(objects);
			FileUtils.Mkdir(infoDirectory);
			FileUtils.Mkdir(packDirectory);
		}

		public override ObjectInserter NewInserter()
		{
			return new ObjectDirectoryInserter(this, config);
		}

		public override void Close()
		{
			unpackedObjectCache.Clear();
			ObjectDirectory.PackList packs = packList.Get();
			packList.Set(NO_PACKS);
			foreach (PackFile p in packs.packs)
			{
				p.Close();
			}
			// Fully close all loaded alternates and clear the alternate list.
			FileObjectDatabase.AlternateHandle[] alt = alternates.Get();
			if (alt != null)
			{
				alternates.Set(null);
				foreach (FileObjectDatabase.AlternateHandle od in alt)
				{
					od.Close();
				}
			}
		}

		/// <summary>Compute the location of a loose object file.</summary>
		/// <remarks>Compute the location of a loose object file.</remarks>
		/// <param name="objectId">identity of the loose object to map to the directory.</param>
		/// <returns>location of the object, if it were to exist as a loose object.</returns>
		internal override FilePath FileFor(AnyObjectId objectId)
		{
			return base.FileFor(objectId);
		}

		/// <returns>
		/// unmodifiable collection of all known pack files local to this
		/// directory. Most recent packs are presented first. Packs most
		/// likely to contain more recent objects appear before packs
		/// containing objects referenced by commits further back in the
		/// history of the repository.
		/// </returns>
		public virtual ICollection<PackFile> GetPacks()
		{
			ObjectDirectory.PackList list = packList.Get();
			if (list == NO_PACKS)
			{
				list = ScanPacks(list);
			}
			PackFile[] packs = list.packs;
			return Sharpen.Collections.UnmodifiableCollection(Arrays.AsList(packs));
		}

		/// <summary>Add a single existing pack to the list of available pack files.</summary>
		/// <remarks>Add a single existing pack to the list of available pack files.</remarks>
		/// <param name="pack">path of the pack file to open.</param>
		/// <param name="idx">path of the corresponding index file.</param>
		/// <returns>the pack that was opened and added to the database.</returns>
		/// <exception cref="System.IO.IOException">
		/// index file could not be opened, read, or is not recognized as
		/// a Git pack file index.
		/// </exception>
		internal override PackFile OpenPack(FilePath pack, FilePath idx)
		{
			string p = pack.GetName();
			string i = idx.GetName();
			if (p.Length != 50 || !p.StartsWith("pack-") || !p.EndsWith(".pack"))
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().notAValidPack, pack));
			}
			if (i.Length != 49 || !i.StartsWith("pack-") || !i.EndsWith(".idx"))
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().notAValidPack, idx));
			}
			if (!Sharpen.Runtime.Substring(p, 0, 45).Equals(Sharpen.Runtime.Substring(i, 0, 45
				)))
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().packDoesNotMatchIndex, 
					pack));
			}
			PackFile res = new PackFile(idx, pack);
			InsertPack(res);
			return res;
		}

		public override string ToString()
		{
			return "ObjectDirectory[" + GetDirectory() + "]";
		}

		internal override bool HasObject1(AnyObjectId objectId)
		{
			if (unpackedObjectCache.IsUnpacked(objectId))
			{
				return true;
			}
			foreach (PackFile p in packList.Get().packs)
			{
				try
				{
					if (p.HasObject(objectId))
					{
						return true;
					}
				}
				catch (IOException)
				{
					// The hasObject call should have only touched the index,
					// so any failure here indicates the index is unreadable
					// by this process, and the pack is likewise not readable.
					//
					RemovePack(p);
					continue;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId
			 id)
		{
			// Go through the packs once. If we didn't find any resolutions
			// scan for new packs and check once more.
			//
			int oldSize = matches.Count;
			ObjectDirectory.PackList pList = packList.Get();
			for (; ; )
			{
				foreach (PackFile p in pList.packs)
				{
					try
					{
						p.Resolve(matches, id, RESOLVE_ABBREV_LIMIT);
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
					if (matches.Count > RESOLVE_ABBREV_LIMIT)
					{
						return;
					}
				}
				if (matches.Count == oldSize)
				{
					ObjectDirectory.PackList nList = ScanPacks(pList);
					if (nList == pList || nList.packs.Length == 0)
					{
						break;
					}
					pList = nList;
					continue;
				}
				break;
			}
			string fanOut = Sharpen.Runtime.Substring(id.Name, 0, 2);
			string[] entries = new FilePath(GetDirectory(), fanOut).List();
			if (entries != null)
			{
				foreach (string e in entries)
				{
					if (e.Length != Constants.OBJECT_ID_STRING_LENGTH - 2)
					{
						continue;
					}
					try
					{
						ObjectId entId = ObjectId.FromString(fanOut + e);
						if (id.PrefixCompare(entId) == 0)
						{
							matches.AddItem(entId);
						}
					}
					catch (ArgumentException)
					{
						continue;
					}
					if (matches.Count > RESOLVE_ABBREV_LIMIT)
					{
						return;
					}
				}
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				alt.db.Resolve(matches, id);
				if (matches.Count > RESOLVE_ABBREV_LIMIT)
				{
					return;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectLoader OpenObject1(WindowCursor curs, AnyObjectId objectId
			)
		{
			if (unpackedObjectCache.IsUnpacked(objectId))
			{
				ObjectLoader ldr = OpenObject2(curs, objectId.Name, objectId);
				if (ldr != null)
				{
					return ldr;
				}
				else
				{
					unpackedObjectCache.Remove(objectId);
				}
			}
			ObjectDirectory.PackList pList = packList.Get();
			for (; ; )
			{
				foreach (PackFile p in pList.packs)
				{
					try
					{
						ObjectLoader ldr = p.Get(curs, objectId);
						if (ldr != null)
						{
							return ldr;
						}
					}
					catch (PackMismatchException)
					{
						// Pack was modified; refresh the entire pack list.
						//
						pList = ScanPacks(pList);
						goto SEARCH_continue;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
				}
				return null;
SEARCH_continue: ;
			}
SEARCH_break: ;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override long GetObjectSize1(WindowCursor curs, AnyObjectId objectId)
		{
			ObjectDirectory.PackList pList = packList.Get();
			for (; ; )
			{
				foreach (PackFile p in pList.packs)
				{
					try
					{
						long sz = p.GetObjectSize(curs, objectId);
						if (0 <= sz)
						{
							return sz;
						}
					}
					catch (PackMismatchException)
					{
						// Pack was modified; refresh the entire pack list.
						//
						pList = ScanPacks(pList);
						goto SEARCH_continue;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
				}
				return -1;
SEARCH_continue: ;
			}
SEARCH_break: ;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override long GetObjectSize2(WindowCursor curs, string objectName, AnyObjectId
			 objectId)
		{
			try
			{
				FilePath path = FileFor(objectName);
				FileInputStream @in = new FileInputStream(path);
				try
				{
					return UnpackedObject.GetSize(@in, objectId, curs);
				}
				finally
				{
					@in.Close();
				}
			}
			catch (FileNotFoundException)
			{
				return -1;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void SelectObjectRepresentation(PackWriter packer, ObjectToPack
			 otp, WindowCursor curs)
		{
			ObjectDirectory.PackList pList = packList.Get();
			for (; ; )
			{
				foreach (PackFile p in pList.packs)
				{
					try
					{
						LocalObjectRepresentation rep = p.Representation(curs, otp);
						if (rep != null)
						{
							packer.Select(otp, rep);
						}
					}
					catch (PackMismatchException)
					{
						// Pack was modified; refresh the entire pack list.
						//
						pList = ScanPacks(pList);
						goto SEARCH_continue;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
				}
				goto SEARCH_break;
SEARCH_continue: ;
			}
SEARCH_break: ;
			foreach (FileObjectDatabase.AlternateHandle h in MyAlternates())
			{
				h.db.SelectObjectRepresentation(packer, otp, curs);
			}
		}

		internal override bool HasObject2(string objectName)
		{
			return FileFor(objectName).Exists();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectLoader OpenObject2(WindowCursor curs, string objectName, 
			AnyObjectId objectId)
		{
			try
			{
				FilePath path = FileFor(objectName);
				FileInputStream @in = new FileInputStream(path);
				try
				{
					unpackedObjectCache.Add(objectId);
					return UnpackedObject.Open(@in, path, objectId, curs);
				}
				finally
				{
					@in.Close();
				}
			}
			catch (FileNotFoundException)
			{
				unpackedObjectCache.Remove(objectId);
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override FileObjectDatabase.InsertLooseObjectResult InsertUnpackedObject
			(FilePath tmp, ObjectId id, bool createDuplicate)
		{
			// If the object is already in the repository, remove temporary file.
			//
			if (unpackedObjectCache.IsUnpacked(id))
			{
				FileUtils.Delete(tmp);
				return FileObjectDatabase.InsertLooseObjectResult.EXISTS_LOOSE;
			}
			if (!createDuplicate && Has(id))
			{
				FileUtils.Delete(tmp);
				return FileObjectDatabase.InsertLooseObjectResult.EXISTS_PACKED;
			}
			FilePath dst = FileFor(id);
			if (dst.Exists())
			{
				// We want to be extra careful and avoid replacing an object
				// that already exists. We can't be sure renameTo() would
				// fail on all platforms if dst exists, so we check first.
				//
				FileUtils.Delete(tmp);
				return FileObjectDatabase.InsertLooseObjectResult.EXISTS_LOOSE;
			}
			if (tmp.RenameTo(dst))
			{
				dst.SetReadOnly();
				unpackedObjectCache.Add(id);
				return FileObjectDatabase.InsertLooseObjectResult.INSERTED;
			}
			// Maybe the directory doesn't exist yet as the object
			// directories are always lazily created. Note that we
			// try the rename first as the directory likely does exist.
			//
			FileUtils.Mkdir(dst.GetParentFile());
			if (tmp.RenameTo(dst))
			{
				dst.SetReadOnly();
				unpackedObjectCache.Add(id);
				return FileObjectDatabase.InsertLooseObjectResult.INSERTED;
			}
			if (!createDuplicate && Has(id))
			{
				FileUtils.Delete(tmp);
				return FileObjectDatabase.InsertLooseObjectResult.EXISTS_PACKED;
			}
			// The object failed to be renamed into its proper
			// location and it doesn't exist in the repository
			// either. We really don't know what went wrong, so
			// fail.
			//
			FileUtils.Delete(tmp);
			return FileObjectDatabase.InsertLooseObjectResult.FAILURE;
		}

		internal override bool TryAgain1()
		{
			ObjectDirectory.PackList old = packList.Get();
			if (old.snapshot.IsModified(packDirectory))
			{
				return old != ScanPacks(old);
			}
			return false;
		}

		internal override Config GetConfig()
		{
			return config;
		}

		internal override FS GetFS()
		{
			return fs;
		}

		private void InsertPack(PackFile pf)
		{
			ObjectDirectory.PackList o;
			ObjectDirectory.PackList n;
			do
			{
				o = packList.Get();
				// If the pack in question is already present in the list
				// (picked up by a concurrent thread that did a scan?) we
				// do not want to insert it a second time.
				//
				PackFile[] oldList = o.packs;
				string name = pf.GetPackFile().GetName();
				foreach (PackFile p in oldList)
				{
					if (PackFile.SORT.Compare(pf, p) < 0)
					{
						break;
					}
					if (name.Equals(p.GetPackFile().GetName()))
					{
						return;
					}
				}
				PackFile[] newList = new PackFile[1 + oldList.Length];
				newList[0] = pf;
				System.Array.Copy(oldList, 0, newList, 1, oldList.Length);
				n = new ObjectDirectory.PackList(o.snapshot, newList);
			}
			while (!packList.CompareAndSet(o, n));
		}

		private void RemovePack(PackFile deadPack)
		{
			ObjectDirectory.PackList o;
			ObjectDirectory.PackList n;
			do
			{
				o = packList.Get();
				PackFile[] oldList = o.packs;
				int j = IndexOf(oldList, deadPack);
				if (j < 0)
				{
					break;
				}
				PackFile[] newList = new PackFile[oldList.Length - 1];
				System.Array.Copy(oldList, 0, newList, 0, j);
				System.Array.Copy(oldList, j + 1, newList, j, newList.Length - j);
				n = new ObjectDirectory.PackList(o.snapshot, newList);
			}
			while (!packList.CompareAndSet(o, n));
			deadPack.Close();
		}

		private static int IndexOf(PackFile[] list, PackFile pack)
		{
			for (int i = 0; i < list.Length; i++)
			{
				if (list[i] == pack)
				{
					return i;
				}
			}
			return -1;
		}

		private ObjectDirectory.PackList ScanPacks(ObjectDirectory.PackList original)
		{
			lock (packList)
			{
				ObjectDirectory.PackList o;
				ObjectDirectory.PackList n;
				do
				{
					o = packList.Get();
					if (o != original)
					{
						// Another thread did the scan for us, while we
						// were blocked on the monitor above.
						//
						return o;
					}
					n = ScanPacksImpl(o);
					if (n == o)
					{
						return n;
					}
				}
				while (!packList.CompareAndSet(o, n));
				return n;
			}
		}

		private ObjectDirectory.PackList ScanPacksImpl(ObjectDirectory.PackList old)
		{
			IDictionary<string, PackFile> forReuse = ReuseMap(old);
			FileSnapshot snapshot = FileSnapshot.Save(packDirectory);
			ICollection<string> names = ListPackDirectory();
			IList<PackFile> list = new AList<PackFile>(names.Count >> 2);
			bool foundNew = false;
			foreach (string indexName in names)
			{
				// Must match "pack-[0-9a-f]{40}.idx" to be an index.
				//
				if (indexName.Length != 49 || !indexName.EndsWith(".idx"))
				{
					continue;
				}
				string @base = Sharpen.Runtime.Substring(indexName, 0, indexName.Length - 4);
				string packName = @base + ".pack";
				if (!names.Contains(packName))
				{
					// Sometimes C Git's HTTP fetch transport leaves a
					// .idx file behind and does not download the .pack.
					// We have to skip over such useless indexes.
					//
					continue;
				}
				PackFile oldPack = Sharpen.Collections.Remove(forReuse, packName);
				if (oldPack != null)
				{
					list.AddItem(oldPack);
					continue;
				}
				FilePath packFile = new FilePath(packDirectory, packName);
				FilePath idxFile = new FilePath(packDirectory, indexName);
				list.AddItem(new PackFile(idxFile, packFile));
				foundNew = true;
			}
			// If we did not discover any new files, the modification time was not
			// changed, and we did not remove any files, then the set of files is
			// the same as the set we were given. Instead of building a new object
			// return the same collection.
			//
			if (!foundNew && forReuse.IsEmpty() && snapshot.Equals(old.snapshot))
			{
				old.snapshot.SetClean(snapshot);
				return old;
			}
			foreach (PackFile p in forReuse.Values)
			{
				p.Close();
			}
			if (list.IsEmpty())
			{
				return new ObjectDirectory.PackList(snapshot, NO_PACKS.packs);
			}
			PackFile[] r = Sharpen.Collections.ToArray(list, new PackFile[list.Count]);
			Arrays.Sort(r, PackFile.SORT);
			return new ObjectDirectory.PackList(snapshot, r);
		}

		private static IDictionary<string, PackFile> ReuseMap(ObjectDirectory.PackList old
			)
		{
			IDictionary<string, PackFile> forReuse = new Dictionary<string, PackFile>();
			foreach (PackFile p in old.packs)
			{
				if (p.Invalid())
				{
					// The pack instance is corrupted, and cannot be safely used
					// again. Do not include it in our reuse map.
					//
					p.Close();
					continue;
				}
				PackFile prior = forReuse.Put(p.GetPackFile().GetName(), p);
				if (prior != null)
				{
					// This should never occur. It should be impossible for us
					// to have two pack files with the same name, as all of them
					// came out of the same directory. If it does, we promised to
					// close any PackFiles we did not reuse, so close the second,
					// readers are likely to be actively using the first.
					//
					forReuse.Put(prior.GetPackFile().GetName(), prior);
					p.Close();
				}
			}
			return forReuse;
		}

		private ICollection<string> ListPackDirectory()
		{
			string[] nameList = packDirectory.List();
			if (nameList == null)
			{
				return Sharpen.Collections.EmptySet<string>();
			}
			ICollection<string> nameSet = new HashSet<string>();
			foreach (string name in nameList)
			{
				if (name.StartsWith("pack-"))
				{
					nameSet.AddItem(name);
				}
			}
			return nameSet;
		}

		internal override FileObjectDatabase.AlternateHandle[] MyAlternates()
		{
			FileObjectDatabase.AlternateHandle[] alt = alternates.Get();
			if (alt == null)
			{
				lock (alternates)
				{
					alt = alternates.Get();
					if (alt == null)
					{
						try
						{
							alt = LoadAlternates();
						}
						catch (IOException)
						{
							alt = new FileObjectDatabase.AlternateHandle[0];
						}
						alternates.Set(alt);
					}
				}
			}
			return alt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FileObjectDatabase.AlternateHandle[] LoadAlternates()
		{
			IList<FileObjectDatabase.AlternateHandle> l = new AList<FileObjectDatabase.AlternateHandle
				>(4);
			BufferedReader br = Open(alternatesFile);
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					l.AddItem(OpenAlternate(line));
				}
			}
			finally
			{
				br.Close();
			}
			return Sharpen.Collections.ToArray(l, new FileObjectDatabase.AlternateHandle[l.Count
				]);
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		private static BufferedReader Open(FilePath f)
		{
			return new BufferedReader(new FileReader(f));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FileObjectDatabase.AlternateHandle OpenAlternate(string location)
		{
			FilePath objdir = fs.Resolve(objects, location);
			return OpenAlternate(objdir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FileObjectDatabase.AlternateHandle OpenAlternate(FilePath objdir)
		{
			FilePath parent = objdir.GetParentFile();
			if (RepositoryCache.FileKey.IsGitRepository(parent, fs))
			{
				RepositoryCache.FileKey key = RepositoryCache.FileKey.Exact(parent, fs);
				FileRepository db = (FileRepository)RepositoryCache.Open(key);
				return new FileObjectDatabase.AlternateRepository(db);
			}
			NGit.Storage.File.ObjectDirectory db_1 = new NGit.Storage.File.ObjectDirectory(config
				, objdir, null, fs);
			return new FileObjectDatabase.AlternateHandle(db_1);
		}

		private sealed class PackList
		{
			/// <summary>State just before reading the pack directory.</summary>
			/// <remarks>State just before reading the pack directory.</remarks>
			internal readonly FileSnapshot snapshot;

			/// <summary>
			/// All known packs, sorted by
			/// <see cref="PackFile.SORT">PackFile.SORT</see>
			/// .
			/// </summary>
			internal readonly PackFile[] packs;

			internal PackList(FileSnapshot monitor, PackFile[] packs)
			{
				this.snapshot = monitor;
				this.packs = packs;
			}
		}

		public override ObjectDatabase NewCachedDatabase()
		{
			return NewCachedFileObjectDatabase();
		}

		internal override FileObjectDatabase NewCachedFileObjectDatabase()
		{
			return new CachedObjectDirectory(this);
		}
	}
}
