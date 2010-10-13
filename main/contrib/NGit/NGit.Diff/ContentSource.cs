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
using NGit.Diff;
using NGit.Errors;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>
	/// Supplies the content of a file for
	/// <see cref="DiffFormatter">DiffFormatter</see>
	/// .
	/// A content source is not thread-safe. Sources may contain state, including
	/// information about the last ObjectLoader they returned. Callers must be
	/// careful to ensure there is no more than one ObjectLoader pending on any
	/// source, at any time.
	/// </summary>
	public abstract class ContentSource
	{
		/// <summary>Construct a content source for an ObjectReader.</summary>
		/// <remarks>Construct a content source for an ObjectReader.</remarks>
		/// <param name="reader">the reader to obtain blobs from.</param>
		/// <returns>a source wrapping the reader.</returns>
		public static ContentSource Create(ObjectReader reader)
		{
			return new ContentSource.ObjectReaderSource(reader);
		}

		/// <summary>Construct a content source for a working directory.</summary>
		/// <remarks>
		/// Construct a content source for a working directory.
		/// If the iterator is a
		/// <see cref="NGit.Treewalk.FileTreeIterator">NGit.Treewalk.FileTreeIterator</see>
		/// an optimized version is
		/// used that doesn't require seeking through a TreeWalk.
		/// </remarks>
		/// <param name="iterator">the iterator to obtain source files through.</param>
		/// <returns>a content source wrapping the iterator.</returns>
		public static ContentSource Create(WorkingTreeIterator iterator)
		{
			if (iterator is FileTreeIterator)
			{
				FileTreeIterator i = (FileTreeIterator)iterator;
				return new ContentSource.FileSource(i.GetDirectory());
			}
			return new ContentSource.WorkingTreeSource(iterator);
		}

		/// <summary>Determine the size of the object.</summary>
		/// <remarks>Determine the size of the object.</remarks>
		/// <param name="path">the path of the file, relative to the root of the repository.</param>
		/// <param name="id">blob id of the file, if known.</param>
		/// <returns>the size in bytes.</returns>
		/// <exception cref="System.IO.IOException">the file cannot be accessed.</exception>
		public abstract long Size(string path, ObjectId id);

		/// <summary>Open the object.</summary>
		/// <remarks>Open the object.</remarks>
		/// <param name="path">the path of the file, relative to the root of the repository.</param>
		/// <param name="id">blob id of the file, if known.</param>
		/// <returns>
		/// a loader that can supply the content of the file. The loader must
		/// be used before another loader can be obtained from this same
		/// source.
		/// </returns>
		/// <exception cref="System.IO.IOException">the file cannot be accessed.</exception>
		public abstract ObjectLoader Open(string path, ObjectId id);

		private class ObjectReaderSource : ContentSource
		{
			private readonly ObjectReader reader;

			internal ObjectReaderSource(ObjectReader reader)
			{
				this.reader = reader;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Size(string path, ObjectId id)
			{
				return reader.GetObjectSize(id, Constants.OBJ_BLOB);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override ObjectLoader Open(string path, ObjectId id)
			{
				return reader.Open(id, Constants.OBJ_BLOB);
			}
		}

		private class WorkingTreeSource : ContentSource
		{
			private readonly TreeWalk tw;

			private readonly WorkingTreeIterator iterator;

			private string current;

			private WorkingTreeIterator ptr;

			internal WorkingTreeSource(WorkingTreeIterator iterator)
			{
				this.tw = new TreeWalk((ObjectReader)null);
				this.iterator = iterator;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Size(string path, ObjectId id)
			{
				Seek(path);
				return ptr.GetEntryLength();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override ObjectLoader Open(string path, ObjectId id)
			{
				Seek(path);
				return new _ObjectLoader_173(this);
			}

			private sealed class _ObjectLoader_173 : ObjectLoader
			{
				public _ObjectLoader_173(WorkingTreeSource _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public override long GetSize()
				{
					return this._enclosing.ptr.GetEntryLength();
				}

				public override int GetType()
				{
					return this._enclosing.ptr.GetEntryFileMode().GetObjectType();
				}

				/// <exception cref="NGit.Errors.MissingObjectException"></exception>
				/// <exception cref="System.IO.IOException"></exception>
				public override ObjectStream OpenStream()
				{
					InputStream @in = this._enclosing.ptr.OpenEntryStream();
					@in = new BufferedInputStream(@in);
					return new ObjectStream.Filter(this.GetType(), this.GetSize(), @in);
				}

				public override bool IsLarge()
				{
					return true;
				}

				/// <exception cref="NGit.Errors.LargeObjectException"></exception>
				public override byte[] GetCachedBytes()
				{
					throw new LargeObjectException();
				}

				private readonly WorkingTreeSource _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void Seek(string path)
			{
				if (!path.Equals(current))
				{
					iterator.Reset();
					tw.Reset();
					tw.AddTree(iterator);
					tw.Filter = PathFilter.Create(path);
					current = path;
					if (!tw.Next())
					{
						throw new FileNotFoundException(path);
					}
					ptr = tw.GetTree<WorkingTreeIterator>(0);
					if (ptr == null)
					{
						throw new FileNotFoundException(path);
					}
				}
			}
		}

		private class FileSource : ContentSource
		{
			private readonly FilePath root;

			internal FileSource(FilePath root)
			{
				this.root = root;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Size(string path, ObjectId id)
			{
				return new FilePath(root, path).Length();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override ObjectLoader Open(string path, ObjectId id)
			{
				FilePath p = new FilePath(root, path);
				if (!p.IsFile())
				{
					throw new FileNotFoundException(path);
				}
				return new _ObjectLoader_237(p);
			}

			private sealed class _ObjectLoader_237 : ObjectLoader
			{
				public _ObjectLoader_237(FilePath p)
				{
					this.p = p;
				}

				public override long GetSize()
				{
					return p.Length();
				}

				public override int GetType()
				{
					return Constants.OBJ_BLOB;
				}

				/// <exception cref="NGit.Errors.MissingObjectException"></exception>
				/// <exception cref="System.IO.IOException"></exception>
				public override ObjectStream OpenStream()
				{
					FileInputStream @in = new FileInputStream(p);
					long sz = @in.GetChannel().Size();
					int type = this.GetType();
					BufferedInputStream b = new BufferedInputStream(@in);
					return new ObjectStream.Filter(type, sz, b);
				}

				public override bool IsLarge()
				{
					return true;
				}

				/// <exception cref="NGit.Errors.LargeObjectException"></exception>
				public override byte[] GetCachedBytes()
				{
					throw new LargeObjectException();
				}

				private readonly FilePath p;
			}
		}

		/// <summary>A pair of sources to access the old and new sides of a DiffEntry.</summary>
		/// <remarks>A pair of sources to access the old and new sides of a DiffEntry.</remarks>
		public sealed class Pair
		{
			private readonly ContentSource oldSource;

			private readonly ContentSource newSource;

			/// <summary>Construct a pair of sources.</summary>
			/// <remarks>Construct a pair of sources.</remarks>
			/// <param name="oldSource">source to read the old side of a DiffEntry.</param>
			/// <param name="newSource">source to read the new side of a DiffEntry.</param>
			public Pair(ContentSource oldSource, ContentSource newSource)
			{
				this.oldSource = oldSource;
				this.newSource = newSource;
			}

			/// <summary>Determine the size of the object.</summary>
			/// <remarks>Determine the size of the object.</remarks>
			/// <param name="side">which side of the entry to read (OLD or NEW).</param>
			/// <param name="ent">the entry to examine.</param>
			/// <returns>the size in bytes.</returns>
			/// <exception cref="System.IO.IOException">the file cannot be accessed.</exception>
			public long Size(DiffEntry.Side side, DiffEntry ent)
			{
				switch (side)
				{
					case DiffEntry.Side.OLD:
					{
						return oldSource.Size(ent.oldPath, ent.oldId.ToObjectId());
					}

					case DiffEntry.Side.NEW:
					{
						return newSource.Size(ent.newPath, ent.newId.ToObjectId());
					}

					default:
					{
						throw new ArgumentException();
					}
				}
			}

			/// <summary>Open the object.</summary>
			/// <remarks>Open the object.</remarks>
			/// <param name="side">which side of the entry to read (OLD or NEW).</param>
			/// <param name="ent">the entry to examine.</param>
			/// <returns>
			/// a loader that can supply the content of the file. The loader
			/// must be used before another loader can be obtained from this
			/// same source.
			/// </returns>
			/// <exception cref="System.IO.IOException">the file cannot be accessed.</exception>
			public ObjectLoader Open(DiffEntry.Side side, DiffEntry ent)
			{
				switch (side)
				{
					case DiffEntry.Side.OLD:
					{
						return oldSource.Open(ent.oldPath, ent.oldId.ToObjectId());
					}

					case DiffEntry.Side.NEW:
					{
						return newSource.Open(ent.newPath, ent.newId.ToObjectId());
					}

					default:
					{
						throw new ArgumentException();
					}
				}
			}
		}
	}
}
