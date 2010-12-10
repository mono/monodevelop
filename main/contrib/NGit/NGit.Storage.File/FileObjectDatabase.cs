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

using System.Collections.Generic;
using NGit;
using NGit.Storage.File;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	public abstract class FileObjectDatabase : ObjectDatabase
	{
		internal enum InsertLooseObjectResult
		{
			INSERTED,
			EXISTS_PACKED,
			EXISTS_LOOSE,
			FAILURE
		}

		public override ObjectReader NewReader()
		{
			return new WindowCursor(this);
		}

		public override ObjectInserter NewInserter()
		{
			return new ObjectDirectoryInserter(this, GetConfig());
		}

		/// <summary>
		/// Does the requested object exist in this database?
		/// <p>
		/// Alternates (if present) are searched automatically.
		/// </summary>
		/// <remarks>
		/// Does the requested object exist in this database?
		/// <p>
		/// Alternates (if present) are searched automatically.
		/// </remarks>
		/// <param name="objectId">identity of the object to test for existence of.</param>
		/// <returns>
		/// true if the specified object is stored in this database, or any
		/// of the alternate databases.
		/// </returns>
		public override bool Has(AnyObjectId objectId)
		{
			return HasObjectImpl1(objectId) || HasObjectImpl2(objectId.Name);
		}

		/// <summary>Compute the location of a loose object file.</summary>
		/// <remarks>Compute the location of a loose object file.</remarks>
		/// <param name="objectId">identity of the loose object to map to the directory.</param>
		/// <returns>location of the object, if it were to exist as a loose object.</returns>
		internal virtual FilePath FileFor(AnyObjectId objectId)
		{
			return FileFor(objectId.Name);
		}

		internal virtual FilePath FileFor(string objectName)
		{
			string d = Sharpen.Runtime.Substring(objectName, 0, 2);
			string f = Sharpen.Runtime.Substring(objectName, 2);
			return new FilePath(new FilePath(GetDirectory(), d), f);
		}

		internal bool HasObjectImpl1(AnyObjectId objectId)
		{
			if (HasObject1(objectId))
			{
				return true;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				if (alt.db.HasObjectImpl1(objectId))
				{
					return true;
				}
			}
			return TryAgain1() && HasObject1(objectId);
		}

		internal bool HasObjectImpl2(string objectId)
		{
			if (HasObject2(objectId))
			{
				return true;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				if (alt.db.HasObjectImpl2(objectId))
				{
					return true;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId
			 id);

		internal abstract Config GetConfig();

		/// <summary>Open an object from this database.</summary>
		/// <remarks>
		/// Open an object from this database.
		/// <p>
		/// Alternates (if present) are searched automatically.
		/// </remarks>
		/// <param name="curs">temporary working space associated with the calling thread.</param>
		/// <param name="objectId">identity of the object to open.</param>
		/// <returns>
		/// a
		/// <see cref="NGit.ObjectLoader">NGit.ObjectLoader</see>
		/// for accessing the data of the named
		/// object, or null if the object does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal virtual ObjectLoader OpenObject(WindowCursor curs, AnyObjectId objectId)
		{
			ObjectLoader ldr;
			ldr = OpenObjectImpl1(curs, objectId);
			if (ldr != null)
			{
				return ldr;
			}
			ldr = OpenObjectImpl2(curs, objectId.Name, objectId);
			if (ldr != null)
			{
				return ldr;
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal ObjectLoader OpenObjectImpl1(WindowCursor curs, AnyObjectId objectId)
		{
			ObjectLoader ldr;
			ldr = OpenObject1(curs, objectId);
			if (ldr != null)
			{
				return ldr;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				ldr = alt.db.OpenObjectImpl1(curs, objectId);
				if (ldr != null)
				{
					return ldr;
				}
			}
			if (TryAgain1())
			{
				ldr = OpenObject1(curs, objectId);
				if (ldr != null)
				{
					return ldr;
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal ObjectLoader OpenObjectImpl2(WindowCursor curs, string objectName, AnyObjectId
			 objectId)
		{
			ObjectLoader ldr;
			ldr = OpenObject2(curs, objectName, objectId);
			if (ldr != null)
			{
				return ldr;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				ldr = alt.db.OpenObjectImpl2(curs, objectName, objectId);
				if (ldr != null)
				{
					return ldr;
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual long GetObjectSize(WindowCursor curs, AnyObjectId objectId)
		{
			long sz = GetObjectSizeImpl1(curs, objectId);
			if (0 <= sz)
			{
				return sz;
			}
			return GetObjectSizeImpl2(curs, objectId.Name, objectId);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal long GetObjectSizeImpl1(WindowCursor curs, AnyObjectId objectId)
		{
			long sz;
			sz = GetObjectSize1(curs, objectId);
			if (0 <= sz)
			{
				return sz;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				sz = alt.db.GetObjectSizeImpl1(curs, objectId);
				if (0 <= sz)
				{
					return sz;
				}
			}
			if (TryAgain1())
			{
				sz = GetObjectSize1(curs, objectId);
				if (0 <= sz)
				{
					return sz;
				}
			}
			return -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal long GetObjectSizeImpl2(WindowCursor curs, string objectName, AnyObjectId
			 objectId)
		{
			long sz;
			sz = GetObjectSize2(curs, objectName, objectId);
			if (0 <= sz)
			{
				return sz;
			}
			foreach (FileObjectDatabase.AlternateHandle alt in MyAlternates())
			{
				sz = alt.db.GetObjectSizeImpl2(curs, objectName, objectId);
				if (0 <= sz)
				{
					return sz;
				}
			}
			return -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract void SelectObjectRepresentation(PackWriter packer, ObjectToPack
			 otp, WindowCursor curs);

		internal abstract FilePath GetDirectory();

		internal abstract FileObjectDatabase.AlternateHandle[] MyAlternates();

		internal abstract bool TryAgain1();

		internal abstract bool HasObject1(AnyObjectId objectId);

		internal abstract bool HasObject2(string objectId);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract ObjectLoader OpenObject1(WindowCursor curs, AnyObjectId objectId
			);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract ObjectLoader OpenObject2(WindowCursor curs, string objectName, 
			AnyObjectId objectId);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract long GetObjectSize1(WindowCursor curs, AnyObjectId objectId);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract long GetObjectSize2(WindowCursor curs, string objectName, AnyObjectId
			 objectId);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract FileObjectDatabase.InsertLooseObjectResult InsertUnpackedObject
			(FilePath tmp, ObjectId id, bool createDuplicate);

		internal abstract FileObjectDatabase NewCachedFileObjectDatabase();

		internal class AlternateHandle
		{
			internal readonly FileObjectDatabase db;

			internal AlternateHandle(FileObjectDatabase db)
			{
				this.db = db;
			}

			internal virtual void Close()
			{
				db.Close();
			}
		}

		internal class AlternateRepository : FileObjectDatabase.AlternateHandle
		{
			internal readonly FileRepository repository;

			internal AlternateRepository(FileRepository r) : base(((ObjectDirectory)r.ObjectDatabase
				))
			{
				repository = r;
			}

			internal override void Close()
			{
				repository.Close();
			}
		}
	}
}
