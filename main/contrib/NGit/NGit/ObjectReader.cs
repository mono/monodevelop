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
using NGit.Errors;
using NGit.Revwalk;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Reads an
	/// <see cref="ObjectDatabase">ObjectDatabase</see>
	/// for a single thread.
	/// <p>
	/// Readers that can support efficient reuse of pack encoded objects should also
	/// implement the companion interface
	/// <see cref="NGit.Storage.Pack.ObjectReuseAsIs">NGit.Storage.Pack.ObjectReuseAsIs</see>
	/// .
	/// </summary>
	public abstract class ObjectReader
	{
		/// <summary>Type hint indicating the caller doesn't know the type.</summary>
		/// <remarks>Type hint indicating the caller doesn't know the type.</remarks>
		public const int OBJ_ANY = -1;

		/// <summary>Construct a new reader from the same data.</summary>
		/// <remarks>
		/// Construct a new reader from the same data.
		/// <p>
		/// Applications can use this method to build a new reader from the same data
		/// source, but for an different thread.
		/// </remarks>
		/// <returns>a brand new reader, using the same data source.</returns>
		public abstract ObjectReader NewReader();

		/// <summary>Obtain a unique abbreviation (prefix) of an object SHA-1.</summary>
		/// <remarks>
		/// Obtain a unique abbreviation (prefix) of an object SHA-1.
		/// This method uses a reasonable default for the minimum length. Callers who
		/// don't care about the minimum length should prefer this method.
		/// The returned abbreviation would expand back to the argument ObjectId when
		/// passed to
		/// <see cref="Resolve(AbbreviatedObjectId)">Resolve(AbbreviatedObjectId)</see>
		/// , assuming no new objects
		/// are added to this repository between calls.
		/// </remarks>
		/// <param name="objectId">object identity that needs to be abbreviated.</param>
		/// <returns>SHA-1 abbreviation.</returns>
		/// <exception cref="System.IO.IOException">the object store cannot be read.</exception>
		public virtual AbbreviatedObjectId Abbreviate(AnyObjectId objectId)
		{
			return Abbreviate(objectId, 7);
		}

		/// <summary>Obtain a unique abbreviation (prefix) of an object SHA-1.</summary>
		/// <remarks>
		/// Obtain a unique abbreviation (prefix) of an object SHA-1.
		/// The returned abbreviation would expand back to the argument ObjectId when
		/// passed to
		/// <see cref="Resolve(AbbreviatedObjectId)">Resolve(AbbreviatedObjectId)</see>
		/// , assuming no new objects
		/// are added to this repository between calls.
		/// The default implementation of this method abbreviates the id to the
		/// minimum length, then resolves it to see if there are multiple results.
		/// When multiple results are found, the length is extended by 1 and resolve
		/// is tried again.
		/// </remarks>
		/// <param name="objectId">object identity that needs to be abbreviated.</param>
		/// <param name="len">
		/// minimum length of the abbreviated string. Must be in the range
		/// [2,
		/// <value>Constants#OBJECT_ID_STRING_LENGTH</value>
		/// ].
		/// </param>
		/// <returns>
		/// SHA-1 abbreviation. If no matching objects exist in the
		/// repository, the abbreviation will match the minimum length.
		/// </returns>
		/// <exception cref="System.IO.IOException">the object store cannot be read.</exception>
		public virtual AbbreviatedObjectId Abbreviate(AnyObjectId objectId, int len)
		{
			if (len == Constants.OBJECT_ID_STRING_LENGTH)
			{
				return AbbreviatedObjectId.FromObjectId(objectId);
			}
			AbbreviatedObjectId abbrev = objectId.Abbreviate(len);
			ICollection<ObjectId> matches = Resolve(abbrev);
			while (1 < matches.Count && len < Constants.OBJECT_ID_STRING_LENGTH)
			{
				abbrev = objectId.Abbreviate(++len);
				IList<ObjectId> n = new AList<ObjectId>(8);
				foreach (ObjectId candidate in matches)
				{
					if (abbrev.PrefixCompare(candidate) == 0)
					{
						n.AddItem(candidate);
					}
				}
				if (1 < n.Count)
				{
					matches = n;
				}
				else
				{
					matches = Resolve(abbrev);
				}
			}
			return abbrev;
		}

		/// <summary>Resolve an abbreviated ObjectId to its full form.</summary>
		/// <remarks>
		/// Resolve an abbreviated ObjectId to its full form.
		/// This method searches for an ObjectId that begins with the abbreviation,
		/// and returns at least some matching candidates.
		/// If the returned collection is empty, no objects start with this
		/// abbreviation. The abbreviation doesn't belong to this repository, or the
		/// repository lacks the necessary objects to complete it.
		/// If the collection contains exactly one member, the abbreviation is
		/// (currently) unique within this database. There is a reasonably high
		/// probability that the returned id is what was previously abbreviated.
		/// If the collection contains 2 or more members, the abbreviation is not
		/// unique. In this case the implementation is only required to return at
		/// least 2 candidates to signal the abbreviation has conflicts. User
		/// friendly implementations should return as many candidates as reasonably
		/// possible, as the caller may be able to disambiguate further based on
		/// context. However since databases can be very large (e.g. 10 million
		/// objects) returning 625,000 candidates for the abbreviation "0" is simply
		/// unreasonable, so implementors should draw the line at around 256 matches.
		/// </remarks>
		/// <param name="id">
		/// abbreviated id to resolve to a complete identity. The
		/// abbreviation must have a length of at least 2.
		/// </param>
		/// <returns>candidates that begin with the abbreviated identity.</returns>
		/// <exception cref="System.IO.IOException">the object store cannot be read.</exception>
		public abstract ICollection<ObjectId> Resolve(AbbreviatedObjectId id);

		/// <summary>Does the requested object exist in this database?</summary>
		/// <param name="objectId">identity of the object to test for existence of.</param>
		/// <returns>true if the specified object is stored in this database.</returns>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual bool Has(AnyObjectId objectId)
		{
			return Has(objectId, OBJ_ANY);
		}

		/// <summary>Does the requested object exist in this database?</summary>
		/// <param name="objectId">identity of the object to test for existence of.</param>
		/// <param name="typeHint">
		/// hint about the type of object being requested;
		/// <see cref="OBJ_ANY">OBJ_ANY</see>
		/// if the object type is not known, or does not
		/// matter to the caller.
		/// </param>
		/// <returns>true if the specified object is stored in this database.</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// typeHint was not OBJ_ANY, and the object's actual type does
		/// not match typeHint.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual bool Has(AnyObjectId objectId, int typeHint)
		{
			try
			{
				Open(objectId, typeHint);
				return true;
			}
			catch (MissingObjectException)
			{
				return false;
			}
		}

		/// <summary>Open an object from this database.</summary>
		/// <remarks>Open an object from this database.</remarks>
		/// <param name="objectId">identity of the object to open.</param>
		/// <returns>
		/// a
		/// <see cref="ObjectLoader">ObjectLoader</see>
		/// for accessing the object.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual ObjectLoader Open(AnyObjectId objectId)
		{
			return Open(objectId, OBJ_ANY);
		}

		/// <summary>Open an object from this database.</summary>
		/// <remarks>Open an object from this database.</remarks>
		/// <param name="objectId">identity of the object to open.</param>
		/// <param name="typeHint">
		/// hint about the type of object being requested;
		/// <see cref="OBJ_ANY">OBJ_ANY</see>
		/// if the object type is not known, or does not
		/// matter to the caller.
		/// </param>
		/// <returns>
		/// a
		/// <see cref="ObjectLoader">ObjectLoader</see>
		/// for accessing the object.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// typeHint was not OBJ_ANY, and the object's actual type does
		/// not match typeHint.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public abstract ObjectLoader Open(AnyObjectId objectId, int typeHint);

		/// <summary>Asynchronous object opening.</summary>
		/// <remarks>Asynchronous object opening.</remarks>
		/// <?></?>
		/// <param name="objectIds">
		/// objects to open from the object store. The supplied collection
		/// must not be modified until the queue has finished.
		/// </param>
		/// <param name="reportMissing">
		/// if true missing objects are reported by calling failure with a
		/// MissingObjectException. This may be more expensive for the
		/// implementation to guarantee. If false the implementation may
		/// choose to report MissingObjectException, or silently skip over
		/// the object with no warning.
		/// </param>
		/// <returns>queue to read the objects from.</returns>
		public virtual AsyncObjectLoaderQueue<T> Open<T>(Iterable<T> objectIds, bool reportMissing
			) where T:ObjectId
		{
			Iterator<T> idItr = objectIds.Iterator();
			return new _AsyncObjectLoaderQueue_272<T>(this, idItr);
		}

		private sealed class _AsyncObjectLoaderQueue_272<T> : AsyncObjectLoaderQueue<T> where T:ObjectId
		{
			public _AsyncObjectLoaderQueue_272(ObjectReader _enclosing, Iterator<T> idItr)
			{
				this._enclosing = _enclosing;
				this.idItr = idItr;
			}

			private T cur;

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public bool Next()
			{
				if (idItr.HasNext())
				{
					this.cur = idItr.Next();
					return true;
				}
				else
				{
					return false;
				}
			}

			public T GetCurrent()
			{
				return this.cur;
			}

			public ObjectId GetObjectId()
			{
				return this.cur;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public ObjectLoader Open()
			{
				return this._enclosing.Open(this.cur, ObjectReader.OBJ_ANY);
			}

			public bool Cancel(bool mayInterruptIfRunning)
			{
				return true;
			}

			public void Release()
			{
			}

			private readonly ObjectReader _enclosing;

			private readonly Iterator<T> idItr;
		}

		// Since we are sequential by default, we don't
		// have any state to clean up if we terminate early.
		/// <summary>Get only the size of an object.</summary>
		/// <remarks>
		/// Get only the size of an object.
		/// <p>
		/// The default implementation of this method opens an ObjectLoader.
		/// Databases are encouraged to override this if a faster access method is
		/// available to them.
		/// </remarks>
		/// <param name="objectId">identity of the object to open.</param>
		/// <param name="typeHint">
		/// hint about the type of object being requested;
		/// <see cref="OBJ_ANY">OBJ_ANY</see>
		/// if the object type is not known, or does not
		/// matter to the caller.
		/// </param>
		/// <returns>size of object in bytes.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// typeHint was not OBJ_ANY, and the object's actual type does
		/// not match typeHint.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual long GetObjectSize(AnyObjectId objectId, int typeHint)
		{
			return Open(objectId, typeHint).GetSize();
		}

		/// <summary>Asynchronous object size lookup.</summary>
		/// <remarks>Asynchronous object size lookup.</remarks>
		/// <?></?>
		/// <param name="objectIds">
		/// objects to get the size of from the object store. The supplied
		/// collection must not be modified until the queue has finished.
		/// </param>
		/// <param name="reportMissing">
		/// if true missing objects are reported by calling failure with a
		/// MissingObjectException. This may be more expensive for the
		/// implementation to guarantee. If false the implementation may
		/// choose to report MissingObjectException, or silently skip over
		/// the object with no warning.
		/// </param>
		/// <returns>queue to read object sizes from.</returns>
		public virtual AsyncObjectSizeQueue<T> GetObjectSize<T>(Iterable<T> objectIds, bool
			 reportMissing) where T:ObjectId
		{
			Iterator<T> idItr = objectIds.Iterator();
			return new _AsyncObjectSizeQueue_354<T>(this, idItr);
		}

		private sealed class _AsyncObjectSizeQueue_354 <T>: AsyncObjectSizeQueue<T> where T:ObjectId
		{
			public _AsyncObjectSizeQueue_354(ObjectReader _enclosing, Iterator<T> idItr)
			{
				this._enclosing = _enclosing;
				this.idItr = idItr;
			}

			private T cur;

			private long sz;

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public bool Next()
			{
				if (idItr.HasNext())
				{
					this.cur = idItr.Next();
					this.sz = this._enclosing.GetObjectSize(this.cur, ObjectReader.OBJ_ANY);
					return true;
				}
				else
				{
					return false;
				}
			}

			public T GetCurrent()
			{
				return this.cur;
			}

			public ObjectId GetObjectId()
			{
				return this.cur;
			}

			public long GetSize()
			{
				return this.sz;
			}

			public bool Cancel(bool mayInterruptIfRunning)
			{
				return true;
			}

			public void Release()
			{
			}

			private readonly ObjectReader _enclosing;

			private readonly Iterator<T> idItr;
		}

		// Since we are sequential by default, we don't
		// have any state to clean up if we terminate early.
		/// <summary>
		/// Advice from a
		/// <see cref="NGit.Revwalk.RevWalk">NGit.Revwalk.RevWalk</see>
		/// that a walk is starting from these roots.
		/// </summary>
		/// <param name="walk">the revision pool that is using this reader.</param>
		/// <param name="roots">
		/// starting points of the revision walk. The starting points have
		/// their headers parsed, but might be missing bodies.
		/// </param>
		/// <exception cref="System.IO.IOException">the reader cannot initialize itself to support the walk.
		/// 	</exception>
		public virtual void WalkAdviceBeginCommits(RevWalk walk, ICollection<RevCommit> roots
			)
		{
		}

		// Do nothing by default, most readers don't want or need advice.
		/// <summary>
		/// Advice from an
		/// <see cref="NGit.Revwalk.ObjectWalk">NGit.Revwalk.ObjectWalk</see>
		/// that trees will be traversed.
		/// </summary>
		/// <param name="ow">the object pool that is using this reader.</param>
		/// <param name="min">the first commit whose root tree will be read.</param>
		/// <param name="max">the last commit whose root tree will be read.</param>
		/// <exception cref="System.IO.IOException">the reader cannot initialize itself to support the walk.
		/// 	</exception>
		public virtual void WalkAdviceBeginTrees(ObjectWalk ow, RevCommit min, RevCommit 
			max)
		{
		}

		// Do nothing by default, most readers don't want or need advice.
		/// <summary>Advice from that a walk is over.</summary>
		/// <remarks>Advice from that a walk is over.</remarks>
		public virtual void WalkAdviceEnd()
		{
		}

		// Do nothing by default, most readers don't want or need advice.
		/// <summary>Release any resources used by this reader.</summary>
		/// <remarks>
		/// Release any resources used by this reader.
		/// <p>
		/// A reader that has been released can be used again, but may need to be
		/// released after the subsequent usage.
		/// </remarks>
		public virtual void Release()
		{
		}
		// Do nothing.
	}
}
