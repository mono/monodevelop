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
using NGit;
using NGit.Storage.File;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// The cached instance of an
	/// <see cref="ObjectDirectory">ObjectDirectory</see>
	/// .
	/// <p>
	/// This class caches the list of loose objects in memory, so the file system is
	/// not queried with stat calls.
	/// </summary>
	internal class CachedObjectDirectory : FileObjectDatabase
	{
		/// <summary>
		/// The set that contains unpacked objects identifiers, it is created when
		/// the cached instance is created.
		/// </summary>
		/// <remarks>
		/// The set that contains unpacked objects identifiers, it is created when
		/// the cached instance is created.
		/// </remarks>
		private readonly ObjectIdSubclassMap<ObjectId> unpackedObjects = new ObjectIdSubclassMap
			<ObjectId>();

		private readonly ObjectDirectory wrapped;

		private FileObjectDatabase.AlternateHandle[] alts;

		/// <summary>The constructor</summary>
		/// <param name="wrapped">the wrapped database</param>
		internal CachedObjectDirectory(ObjectDirectory wrapped)
		{
			this.wrapped = wrapped;
			FilePath objects = wrapped.GetDirectory();
			string[] fanout = objects.List();
			if (fanout == null)
			{
				fanout = new string[0];
			}
			foreach (string d in fanout)
			{
				if (d.Length != 2)
				{
					continue;
				}
				string[] entries = new FilePath(objects, d).List();
				if (entries == null)
				{
					continue;
				}
				foreach (string e in entries)
				{
					if (e.Length != Constants.OBJECT_ID_STRING_LENGTH - 2)
					{
						continue;
					}
					try
					{
						unpackedObjects.Add(ObjectId.FromString(d + e));
					}
					catch (ArgumentException)
					{
					}
				}
			}
		}

		// ignoring the file that does not represent loose object
		public override void Close()
		{
		}

		// Don't close anything.
		public override ObjectDatabase NewCachedDatabase()
		{
			return this;
		}

		internal override FileObjectDatabase NewCachedFileObjectDatabase()
		{
			return this;
		}

		internal override FilePath GetDirectory()
		{
			return wrapped.GetDirectory();
		}

		internal override Config GetConfig()
		{
			return wrapped.GetConfig();
		}

		internal override FS GetFS()
		{
			return wrapped.GetFS();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ICollection<CachedPack> GetCachedPacks()
		{
			return wrapped.GetCachedPacks();
		}

		internal override FileObjectDatabase.AlternateHandle[] MyAlternates()
		{
			if (alts == null)
			{
				FileObjectDatabase.AlternateHandle[] src = wrapped.MyAlternates();
				alts = new FileObjectDatabase.AlternateHandle[src.Length];
				for (int i = 0; i < alts.Length; i++)
				{
					FileObjectDatabase s = src[i].db;
					alts[i] = new FileObjectDatabase.AlternateHandle(s.NewCachedFileObjectDatabase());
				}
			}
			return alts;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId
			 id)
		{
			// In theory we could accelerate the loose object scan using our
			// unpackedObjects map, but its not worth the huge code complexity.
			// Scanning a single loose directory is fast enough, and this is
			// unlikely to be called anyway.
			//
			wrapped.Resolve(matches, id);
		}

		internal override bool TryAgain1()
		{
			return wrapped.TryAgain1();
		}

		public override bool Has(AnyObjectId objectId)
		{
			return HasObjectImpl1(objectId);
		}

		internal override bool HasObject1(AnyObjectId objectId)
		{
			return unpackedObjects.Contains(objectId) || wrapped.HasObject1(objectId);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectLoader OpenObject(WindowCursor curs, AnyObjectId objectId
			)
		{
			return OpenObjectImpl1(curs, objectId);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectLoader OpenObject1(WindowCursor curs, AnyObjectId objectId
			)
		{
			if (unpackedObjects.Contains(objectId))
			{
				return wrapped.OpenObject2(curs, objectId.Name, objectId);
			}
			return wrapped.OpenObject1(curs, objectId);
		}

		internal override bool HasObject2(string objectId)
		{
			return unpackedObjects.Contains(ObjectId.FromString(objectId));
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectLoader OpenObject2(WindowCursor curs, string objectName, 
			AnyObjectId objectId)
		{
			if (unpackedObjects.Contains(objectId))
			{
				return wrapped.OpenObject2(curs, objectName, objectId);
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override long GetObjectSize1(WindowCursor curs, AnyObjectId objectId)
		{
			if (unpackedObjects.Contains(objectId))
			{
				return wrapped.GetObjectSize2(curs, objectId.Name, objectId);
			}
			return wrapped.GetObjectSize1(curs, objectId);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override long GetObjectSize2(WindowCursor curs, string objectName, AnyObjectId
			 objectId)
		{
			if (unpackedObjects.Contains(objectId))
			{
				return wrapped.GetObjectSize2(curs, objectName, objectId);
			}
			return -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override FileObjectDatabase.InsertLooseObjectResult InsertUnpackedObject
			(FilePath tmp, ObjectId objectId, bool createDuplicate)
		{
			FileObjectDatabase.InsertLooseObjectResult result = wrapped.InsertUnpackedObject(
				tmp, objectId, createDuplicate);
			switch (result)
			{
				case FileObjectDatabase.InsertLooseObjectResult.INSERTED:
				case FileObjectDatabase.InsertLooseObjectResult.EXISTS_LOOSE:
				{
					unpackedObjects.AddIfAbsent(objectId);
					break;
				}

				case FileObjectDatabase.InsertLooseObjectResult.EXISTS_PACKED:
				case FileObjectDatabase.InsertLooseObjectResult.FAILURE:
				{
					break;
				}
			}
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override PackFile OpenPack(FilePath pack, FilePath idx)
		{
			return wrapped.OpenPack(pack, idx);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void SelectObjectRepresentation(PackWriter packer, ObjectToPack
			 otp, WindowCursor curs)
		{
			wrapped.SelectObjectRepresentation(packer, otp, curs);
		}
	}
}
