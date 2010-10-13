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
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// <p>
	/// PackWriter class is responsible for generating pack files from specified set
	/// of objects from repository.
	/// </summary>
	/// <remarks>
	/// <p>
	/// PackWriter class is responsible for generating pack files from specified set
	/// of objects from repository. This implementation produce pack files in format
	/// version 2.
	/// </p>
	/// <p>
	/// Source of objects may be specified in two ways:
	/// <ul>
	/// <li>(usually) by providing sets of interesting and uninteresting objects in
	/// repository - all interesting objects and their ancestors except uninteresting
	/// objects and their ancestors will be included in pack, or</li>
	/// <li>by providing iterator of
	/// <see cref="NGit.Revwalk.RevObject">NGit.Revwalk.RevObject</see>
	/// specifying exact list and
	/// order of objects in pack</li>
	/// </ul>
	/// Typical usage consists of creating instance intended for some pack,
	/// configuring options, preparing the list of objects by calling
	/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
	/// 	</see>
	/// or
	/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
	/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
	/// 	</see>
	/// , and finally
	/// producing the stream with
	/// <see cref="WritePack(NGit.ProgressMonitor, NGit.ProgressMonitor, Sharpen.OutputStream)
	/// 	">WritePack(NGit.ProgressMonitor, NGit.ProgressMonitor, Sharpen.OutputStream)</see>
	/// .
	/// </p>
	/// <p>
	/// Class provide set of configurable options and
	/// <see cref="NGit.ProgressMonitor">NGit.ProgressMonitor</see>
	/// support, as operations may take a long time for big repositories. Deltas
	/// searching algorithm is <b>NOT IMPLEMENTED</b> yet - this implementation
	/// relies only on deltas and objects reuse.
	/// </p>
	/// <p>
	/// This class is not thread safe, it is intended to be used in one thread, with
	/// one instance per created pack. Subsequent calls to writePack result in
	/// undefined behavior.
	/// </p>
	/// </remarks>
	public class PackWriter
	{
		private const int PACK_VERSION_GENERATED = 2;

		private readonly IList<ObjectToPack>[] objectsLists = new IList<ObjectToPack>[Constants.OBJ_TAG
			 + 1];

		private readonly ObjectIdSubclassMap<ObjectToPack> objectsMap = new ObjectIdSubclassMap
			<ObjectToPack>();

		private readonly ObjectIdSubclassMap<ObjectToPack> edgeObjects = new ObjectIdSubclassMap
			<ObjectToPack>();

		private ICSharpCode.SharpZipLib.Zip.Compression.Deflater myDeflater;

		private readonly ObjectReader reader;

		/// <summary>
		/// <see cref="reader">reader</see>
		/// recast to the reuse interface, if it supports it.
		/// </summary>
		private readonly ObjectReuseAsIs reuseSupport;

		private readonly PackConfig config;

		private IList<ObjectToPack> sortedByName;

		private byte[] packcsum;

		private bool deltaBaseAsOffset;

		private bool reuseDeltas;

		private bool thin;

		private bool ignoreMissingUninteresting = true;

		/// <summary>Create writer for specified repository.</summary>
		/// <remarks>
		/// Create writer for specified repository.
		/// <p>
		/// Objects for packing are specified in
		/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
		/// 	</see>
		/// or
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="repo">repository where objects are stored.</param>
		public PackWriter(Repository repo) : this(repo, repo.NewObjectReader())
		{
		}

		/// <summary>Create a writer to load objects from the specified reader.</summary>
		/// <remarks>
		/// Create a writer to load objects from the specified reader.
		/// <p>
		/// Objects for packing are specified in
		/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
		/// 	</see>
		/// or
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="reader">reader to read from the repository with.</param>
		public PackWriter(ObjectReader reader) : this(new PackConfig(), reader)
		{
		}

		/// <summary>Create writer for specified repository.</summary>
		/// <remarks>
		/// Create writer for specified repository.
		/// <p>
		/// Objects for packing are specified in
		/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
		/// 	</see>
		/// or
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="repo">repository where objects are stored.</param>
		/// <param name="reader">reader to read from the repository with.</param>
		public PackWriter(Repository repo, ObjectReader reader) : this(new PackConfig(repo
			), reader)
		{
		}

		/// <summary>Create writer with a specified configuration.</summary>
		/// <remarks>
		/// Create writer with a specified configuration.
		/// <p>
		/// Objects for packing are specified in
		/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
		/// 	</see>
		/// or
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="config">configuration for the pack writer.</param>
		/// <param name="reader">reader to read from the repository with.</param>
		public PackWriter(PackConfig config, ObjectReader reader)
		{
			{
				objectsLists[0] = Sharpen.Collections.EmptyList<ObjectToPack>();
				objectsLists[Constants.OBJ_COMMIT] = new AList<ObjectToPack>();
				objectsLists[Constants.OBJ_TREE] = new AList<ObjectToPack>();
				objectsLists[Constants.OBJ_BLOB] = new AList<ObjectToPack>();
				objectsLists[Constants.OBJ_TAG] = new AList<ObjectToPack>();
			}
			// edge objects for thin packs
			this.config = config;
			this.reader = reader;
			if (reader is ObjectReuseAsIs)
			{
				reuseSupport = ((ObjectReuseAsIs)reader);
			}
			else
			{
				reuseSupport = null;
			}
			deltaBaseAsOffset = config.IsDeltaBaseAsOffset();
			reuseDeltas = config.IsReuseDeltas();
		}

		/// <summary>
		/// Check whether writer can store delta base as an offset (new style
		/// reducing pack size) or should store it as an object id (legacy style,
		/// compatible with old readers).
		/// </summary>
		/// <remarks>
		/// Check whether writer can store delta base as an offset (new style
		/// reducing pack size) or should store it as an object id (legacy style,
		/// compatible with old readers).
		/// Default setting:
		/// <value>PackConfig#DEFAULT_DELTA_BASE_AS_OFFSET</value>
		/// </remarks>
		/// <returns>
		/// true if delta base is stored as an offset; false if it is stored
		/// as an object id.
		/// </returns>
		public virtual bool IsDeltaBaseAsOffset()
		{
			return deltaBaseAsOffset;
		}

		/// <summary>Set writer delta base format.</summary>
		/// <remarks>
		/// Set writer delta base format. Delta base can be written as an offset in a
		/// pack file (new approach reducing file size) or as an object id (legacy
		/// approach, compatible with old readers).
		/// Default setting:
		/// <value>PackConfig#DEFAULT_DELTA_BASE_AS_OFFSET</value>
		/// </remarks>
		/// <param name="deltaBaseAsOffset">
		/// boolean indicating whether delta base can be stored as an
		/// offset.
		/// </param>
		public virtual void SetDeltaBaseAsOffset(bool deltaBaseAsOffset)
		{
			this.deltaBaseAsOffset = deltaBaseAsOffset;
		}

		/// <returns>true if this writer is producing a thin pack.</returns>
		public virtual bool IsThin()
		{
			return thin;
		}

		/// <param name="packthin">
		/// a boolean indicating whether writer may pack objects with
		/// delta base object not within set of objects to pack, but
		/// belonging to party repository (uninteresting/boundary) as
		/// determined by set; this kind of pack is used only for
		/// transport; true - to produce thin pack, false - otherwise.
		/// </param>
		public virtual void SetThin(bool packthin)
		{
			thin = packthin;
		}

		/// <returns>
		/// true to ignore objects that are uninteresting and also not found
		/// on local disk; false to throw a
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// out of
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// if an
		/// uninteresting object is not in the source repository. By default,
		/// true, permitting gracefully ignoring of uninteresting objects.
		/// </returns>
		public virtual bool IsIgnoreMissingUninteresting()
		{
			return ignoreMissingUninteresting;
		}

		/// <param name="ignore">
		/// true if writer should ignore non existing uninteresting
		/// objects during construction set of objects to pack; false
		/// otherwise - non existing uninteresting objects may cause
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// </param>
		public virtual void SetIgnoreMissingUninteresting(bool ignore)
		{
			ignoreMissingUninteresting = ignore;
		}

		/// <summary>Returns objects number in a pack file that was created by this writer.</summary>
		/// <remarks>Returns objects number in a pack file that was created by this writer.</remarks>
		/// <returns>number of objects in pack.</returns>
		public virtual int GetObjectsNumber()
		{
			return objectsMap.Size();
		}

		/// <summary>Prepare the list of objects to be written to the pack stream.</summary>
		/// <remarks>
		/// Prepare the list of objects to be written to the pack stream.
		/// <p>
		/// Iterator <b>exactly</b> determines which objects are included in a pack
		/// and order they appear in pack (except that objects order by type is not
		/// needed at input). This order should conform general rules of ordering
		/// objects in git - by recency and path (type and delta-base first is
		/// internally secured) and responsibility for guaranteeing this order is on
		/// a caller side. Iterator must return each id of object to write exactly
		/// once.
		/// </p>
		/// <p>
		/// When iterator returns object that has
		/// <see cref="NGit.Revwalk.RevFlag.UNINTERESTING">NGit.Revwalk.RevFlag.UNINTERESTING
		/// 	</see>
		/// flag,
		/// this object won't be included in an output pack. Instead, it is recorded
		/// as edge-object (known to remote repository) for thin-pack. In such a case
		/// writer may pack objects with delta base object not within set of objects
		/// to pack, but belonging to party repository - those marked with
		/// <see cref="NGit.Revwalk.RevFlag.UNINTERESTING">NGit.Revwalk.RevFlag.UNINTERESTING
		/// 	</see>
		/// flag. This type of pack is used only for
		/// transport.
		/// </p>
		/// </remarks>
		/// <param name="objectsSource">
		/// iterator of object to store in a pack; order of objects within
		/// each type is important, ordering by type is not needed;
		/// allowed types for objects are
		/// <see cref="NGit.Constants.OBJ_COMMIT">NGit.Constants.OBJ_COMMIT</see>
		/// ,
		/// <see cref="NGit.Constants.OBJ_TREE">NGit.Constants.OBJ_TREE</see>
		/// ,
		/// <see cref="NGit.Constants.OBJ_BLOB">NGit.Constants.OBJ_BLOB</see>
		/// and
		/// <see cref="NGit.Constants.OBJ_TAG">NGit.Constants.OBJ_TAG</see>
		/// ; objects returned by iterator may
		/// be later reused by caller as object id and type are internally
		/// copied in each iteration; if object returned by iterator has
		/// <see cref="NGit.Revwalk.RevFlag.UNINTERESTING">NGit.Revwalk.RevFlag.UNINTERESTING
		/// 	</see>
		/// flag set, it won't be included
		/// in a pack, but is considered as edge-object for thin-pack.
		/// </param>
		/// <exception cref="System.IO.IOException">when some I/O problem occur during reading objects.
		/// 	</exception>
		public virtual void PreparePack(Iterator<RevObject> objectsSource)
		{
			while (objectsSource.HasNext())
			{
				AddObject(objectsSource.Next());
			}
		}

		/// <summary>Prepare the list of objects to be written to the pack stream.</summary>
		/// <remarks>
		/// Prepare the list of objects to be written to the pack stream.
		/// <p>
		/// Basing on these 2 sets, another set of objects to put in a pack file is
		/// created: this set consists of all objects reachable (ancestors) from
		/// interesting objects, except uninteresting objects and their ancestors.
		/// This method uses class
		/// <see cref="NGit.Revwalk.ObjectWalk">NGit.Revwalk.ObjectWalk</see>
		/// extensively to find out that
		/// appropriate set of output objects and their optimal order in output pack.
		/// Order is consistent with general git in-pack rules: sort by object type,
		/// recency, path and delta-base first.
		/// </p>
		/// </remarks>
		/// <param name="countingMonitor">progress during object enumeration.</param>
		/// <param name="interestingObjects">
		/// collection of objects to be marked as interesting (start
		/// points of graph traversal).
		/// </param>
		/// <param name="uninterestingObjects">
		/// collection of objects to be marked as uninteresting (end
		/// points of graph traversal).
		/// </param>
		/// <exception cref="System.IO.IOException">when some I/O problem occur during reading objects.
		/// 	</exception>
		public virtual void PreparePack<_T0, _T1>(ProgressMonitor countingMonitor, ICollection
			<_T0> interestingObjects, ICollection<_T1> uninterestingObjects) where _T0:ObjectId
			 where _T1:ObjectId
		{
			if (countingMonitor == null)
			{
				countingMonitor = NullProgressMonitor.INSTANCE;
			}
			ObjectWalk walker = SetUpWalker(interestingObjects, uninterestingObjects);
			FindObjectsToPack(countingMonitor, walker);
		}

		/// <summary>Determine if the pack file will contain the requested object.</summary>
		/// <remarks>Determine if the pack file will contain the requested object.</remarks>
		/// <param name="id">the object to test the existence of.</param>
		/// <returns>true if the object will appear in the output pack file.</returns>
		public virtual bool WillInclude(AnyObjectId id)
		{
			return Get(id) != null;
		}

		/// <summary>Lookup the ObjectToPack object for a given ObjectId.</summary>
		/// <remarks>Lookup the ObjectToPack object for a given ObjectId.</remarks>
		/// <param name="id">the object to find in the pack.</param>
		/// <returns>the object we are packing, or null.</returns>
		public virtual ObjectToPack Get(AnyObjectId id)
		{
			return objectsMap.Get(id);
		}

		/// <summary>
		/// Computes SHA-1 of lexicographically sorted objects ids written in this
		/// pack, as used to name a pack file in repository.
		/// </summary>
		/// <remarks>
		/// Computes SHA-1 of lexicographically sorted objects ids written in this
		/// pack, as used to name a pack file in repository.
		/// </remarks>
		/// <returns>ObjectId representing SHA-1 name of a pack that was created.</returns>
		public virtual ObjectId ComputeName()
		{
			byte[] buf = new byte[Constants.OBJECT_ID_LENGTH];
			MessageDigest md = Constants.NewMessageDigest();
			foreach (ObjectToPack otp in SortByName())
			{
				otp.CopyRawTo(buf, 0);
				md.Update(buf, 0, Constants.OBJECT_ID_LENGTH);
			}
			return ObjectId.FromRaw(md.Digest());
		}

		/// <summary>Create an index file to match the pack file just written.</summary>
		/// <remarks>
		/// Create an index file to match the pack file just written.
		/// <p>
		/// This method can only be invoked after
		/// <see cref="PreparePack(Sharpen.Iterator{E})">PreparePack(Sharpen.Iterator&lt;E&gt;)
		/// 	</see>
		/// or
		/// <see cref="PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">PreparePack(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// has been
		/// invoked and completed successfully. Writing a corresponding index is an
		/// optional feature that not all pack users may require.
		/// </remarks>
		/// <param name="indexStream">
		/// output for the index data. Caller is responsible for closing
		/// this stream.
		/// </param>
		/// <exception cref="System.IO.IOException">the index data could not be written to the supplied stream.
		/// 	</exception>
		public virtual void WriteIndex(OutputStream indexStream)
		{
			IList<ObjectToPack> list = SortByName();
			PackIndexWriter iw;
			int indexVersion = config.GetIndexVersion();
			if (indexVersion <= 0)
			{
				iw = PackIndexWriter.CreateOldestPossible(indexStream, list);
			}
			else
			{
				iw = PackIndexWriter.CreateVersion(indexStream, indexVersion);
			}
			iw.Write(list, packcsum);
		}

		private IList<ObjectToPack> SortByName()
		{
			if (sortedByName == null)
			{
				sortedByName = new AList<ObjectToPack>(objectsMap.Size());
				foreach (IList<ObjectToPack> list in objectsLists)
				{
					foreach (ObjectToPack otp in list)
					{
						sortedByName.AddItem(otp);
					}
				}
				sortedByName.Sort();
			}
			return sortedByName;
		}

		/// <summary>Write the prepared pack to the supplied stream.</summary>
		/// <remarks>
		/// Write the prepared pack to the supplied stream.
		/// <p>
		/// At first, this method collects and sorts objects to pack, then deltas
		/// search is performed if set up accordingly, finally pack stream is
		/// written.
		/// </p>
		/// <p>
		/// All reused objects data checksum (Adler32/CRC32) is computed and
		/// validated against existing checksum.
		/// </p>
		/// </remarks>
		/// <param name="compressMonitor">progress monitor to report object compression work.
		/// 	</param>
		/// <param name="writeMonitor">progress monitor to report the number of objects written.
		/// 	</param>
		/// <param name="packStream">
		/// output stream of pack data. The stream should be buffered by
		/// the caller. The caller is responsible for closing the stream.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// an error occurred reading a local object's data to include in
		/// the pack, or writing compressed object data to the output
		/// stream.
		/// </exception>
		public virtual void WritePack(ProgressMonitor compressMonitor, ProgressMonitor writeMonitor
			, OutputStream packStream)
		{
			if (compressMonitor == null)
			{
				compressMonitor = NullProgressMonitor.INSTANCE;
			}
			if (writeMonitor == null)
			{
				writeMonitor = NullProgressMonitor.INSTANCE;
			}
			if ((reuseDeltas || config.IsReuseObjects()) && reuseSupport != null)
			{
				SearchForReuse(compressMonitor);
			}
			if (config.IsDeltaCompress())
			{
				SearchForDeltas(compressMonitor);
			}
			PackOutputStream @out = new PackOutputStream(writeMonitor, packStream, this);
			int objCnt = GetObjectsNumber();
			writeMonitor.BeginTask(JGitText.Get().writingObjects, objCnt);
			@out.WriteFileHeader(PACK_VERSION_GENERATED, objCnt);
			@out.Flush();
			WriteObjects(@out);
			WriteChecksum(@out);
			reader.Release();
			writeMonitor.EndTask();
		}

		/// <summary>Release all resources used by this writer.</summary>
		/// <remarks>Release all resources used by this writer.</remarks>
		public virtual void Release()
		{
			reader.Release();
			if (myDeflater != null)
			{
				myDeflater.Finish();
				myDeflater = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SearchForReuse(ProgressMonitor monitor)
		{
			monitor.BeginTask(JGitText.Get().searchForReuse, GetObjectsNumber());
			foreach (IList<ObjectToPack> list in objectsLists)
			{
				reuseSupport.SelectObjectRepresentation(this, monitor, list.AsIterable());
			}
			monitor.EndTask();
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void SearchForDeltas(ProgressMonitor monitor)
		{
			// Commits and annotated tags tend to have too many differences to
			// really benefit from delta compression. Consequently just don't
			// bother examining those types here.
			//
			ObjectToPack[] list = new ObjectToPack[objectsLists[Constants.OBJ_TREE].Count + objectsLists
				[Constants.OBJ_BLOB].Count + edgeObjects.Size()];
			int cnt = 0;
			cnt = FindObjectsNeedingDelta(list, cnt, Constants.OBJ_TREE);
			cnt = FindObjectsNeedingDelta(list, cnt, Constants.OBJ_BLOB);
			if (cnt == 0)
			{
				return;
			}
			// Queue up any edge objects that we might delta against.  We won't
			// be sending these as we assume the other side has them, but we need
			// them in the search phase below.
			//
			foreach (ObjectToPack eo in edgeObjects)
			{
				eo.SetWeight(0);
				list[cnt++] = eo;
			}
			// Compute the sizes of the objects so we can do a proper sort.
			// We let the reader skip missing objects if it chooses. For
			// some readers this can be a huge win. We detect missing objects
			// by having set the weights above to 0 and allowing the delta
			// search code to discover the missing object and skip over it, or
			// abort with an exception if we actually had to have it.
			//
			monitor.BeginTask(JGitText.Get().compressingObjects, cnt);
			AsyncObjectSizeQueue<ObjectToPack> sizeQueue = reader.GetObjectSize(Arrays.AsList
				<ObjectToPack>(list).SubList(0, cnt).AsIterable(), false);
			try
			{
				long limit = config.GetBigFileThreshold();
				for (; ; )
				{
					monitor.Update(1);
					try
					{
						if (!sizeQueue.Next())
						{
							break;
						}
					}
					catch (MissingObjectException notFound)
					{
						if (ignoreMissingUninteresting)
						{
							ObjectToPack otp = sizeQueue.GetCurrent();
							if (otp != null && otp.IsEdge())
							{
								otp.SetDoNotDelta(true);
								continue;
							}
							otp = edgeObjects.Get(notFound.GetObjectId());
							if (otp != null)
							{
								otp.SetDoNotDelta(true);
								continue;
							}
						}
						throw;
					}
					ObjectToPack otp_1 = sizeQueue.GetCurrent();
					if (otp_1 == null)
					{
						otp_1 = objectsMap.Get(sizeQueue.GetObjectId());
						if (otp_1 == null)
						{
							otp_1 = edgeObjects.Get(sizeQueue.GetObjectId());
						}
					}
					long sz = sizeQueue.GetSize();
					if (limit <= sz || int.MaxValue <= sz)
					{
						otp_1.SetDoNotDelta(true);
					}
					else
					{
						// too big, avoid costly files
						if (sz <= DeltaIndex.BLKSZ)
						{
							otp_1.SetDoNotDelta(true);
						}
						else
						{
							// too small, won't work
							otp_1.SetWeight((int)sz);
						}
					}
				}
			}
			finally
			{
				sizeQueue.Release();
			}
			monitor.EndTask();
			// Sort the objects by path hash so like files are near each other,
			// and then by size descending so that bigger files are first. This
			// applies "Linus' Law" which states that newer files tend to be the
			// bigger ones, because source files grow and hardly ever shrink.
			//
			Arrays.Sort(list, 0, cnt, new _IComparer_615());
			// Above we stored the objects we cannot delta onto the end.
			// Remove them from the list so we don't waste time on them.
			while (0 < cnt && list[cnt - 1].IsDoNotDelta())
			{
				cnt--;
			}
			if (cnt == 0)
			{
				return;
			}
			monitor.BeginTask(JGitText.Get().compressingObjects, cnt);
			SearchForDeltas(monitor, list, cnt);
			monitor.EndTask();
		}

		private sealed class _IComparer_615 : IComparer<ObjectToPack>
		{
			public _IComparer_615()
			{
			}

			public int Compare(ObjectToPack a, ObjectToPack b)
			{
				int cmp = (a.IsDoNotDelta() ? 1 : 0) - (b.IsDoNotDelta() ? 1 : 0);
				if (cmp != 0)
				{
					return cmp;
				}
				cmp = a.GetType() - b.GetType();
				if (cmp != 0)
				{
					return cmp;
				}
				cmp = ((int)(((uint)a.GetPathHash()) >> 1)) - ((int)(((uint)b.GetPathHash()) >> 1
					));
				if (cmp != 0)
				{
					return cmp;
				}
				cmp = (a.GetPathHash() & 1) - (b.GetPathHash() & 1);
				if (cmp != 0)
				{
					return cmp;
				}
				return b.GetWeight() - a.GetWeight();
			}
		}

		private int FindObjectsNeedingDelta(ObjectToPack[] list, int cnt, int type)
		{
			foreach (ObjectToPack otp in objectsLists[type])
			{
				if (otp.IsDoNotDelta())
				{
					// delta is disabled for this path
					continue;
				}
				if (otp.IsDeltaRepresentation())
				{
					// already reusing a delta
					continue;
				}
				otp.SetWeight(0);
				list[cnt++] = otp;
			}
			return cnt;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void SearchForDeltas(ProgressMonitor monitor, ObjectToPack[] list, int cnt
			)
		{
			int threads = config.GetThreads();
			if (threads == 0)
			{
				threads = Runtime.GetRuntime().AvailableProcessors();
			}
			if (threads <= 1 || cnt <= 2 * config.GetDeltaSearchWindowSize())
			{
				DeltaCache dc = new DeltaCache(config);
				DeltaWindow dw = new DeltaWindow(config, dc, reader);
				dw.Search(monitor, list, 0, cnt);
				return;
			}
			DeltaCache dc_1 = new ThreadSafeDeltaCache(config);
			ProgressMonitor pm = new ThreadSafeProgressMonitor(monitor);
			// Guess at the size of batch we want. Because we don't really
			// have a way for a thread to steal work from another thread if
			// it ends early, we over partition slightly so the work units
			// are a bit smaller.
			//
			int estSize = cnt / (threads * 2);
			if (estSize < 2 * config.GetDeltaSearchWindowSize())
			{
				estSize = 2 * config.GetDeltaSearchWindowSize();
			}
			IList<DeltaTask> myTasks = new AList<DeltaTask>(threads * 2);
			for (int i = 0; i < cnt; )
			{
				int start = i;
				int batchSize;
				if (cnt - i < estSize)
				{
					// If we don't have enough to fill the remaining block,
					// schedule what is left over as a single block.
					//
					batchSize = cnt - i;
				}
				else
				{
					// Try to split the block at the end of a path.
					//
					int end = start + estSize;
					while (end < cnt)
					{
						ObjectToPack a = list[end - 1];
						ObjectToPack b = list[end];
						if (a.GetPathHash() == b.GetPathHash())
						{
							end++;
						}
						else
						{
							break;
						}
					}
					batchSize = end - start;
				}
				i += batchSize;
				myTasks.AddItem(new DeltaTask(config, reader, dc_1, pm, batchSize, start, list));
			}
			Executor executor = config.GetExecutor();
			IList<Exception> errors = Sharpen.Collections.SynchronizedList(new AList<Exception
				>());
			if (executor is ExecutorService)
			{
				// Caller supplied us a service, use it directly.
				//
				RunTasks((ExecutorService)executor, myTasks, errors);
			}
			else
			{
				if (executor == null)
				{
					// Caller didn't give us a way to run the tasks, spawn up a
					// temporary thread pool and make sure it tears down cleanly.
					//
					ExecutorService pool = Executors.NewFixedThreadPool(threads);
					try
					{
						RunTasks(pool, myTasks, errors);
					}
					finally
					{
						pool.Shutdown();
						for (; ; )
						{
							try
							{
								if (pool.AwaitTermination(60, TimeUnit.SECONDS))
								{
									break;
								}
							}
							catch (Exception)
							{
								throw new IOException(JGitText.Get().packingCancelledDuringObjectsWriting);
							}
						}
					}
				}
				else
				{
					// The caller gave us an executor, but it might not do
					// asynchronous execution.  Wrap everything and hope it
					// can schedule these for us.
					//
					CountDownLatch done = new CountDownLatch(myTasks.Count);
					foreach (DeltaTask task in myTasks)
					{
						executor.Execute(new _Runnable_751(task, errors, done));
					}
					try
					{
						done.Await();
					}
					catch (Exception)
					{
						// We can't abort the other tasks as we have no handle.
						// Cross our fingers and just break out anyway.
						//
						throw new IOException(JGitText.Get().packingCancelledDuringObjectsWriting);
					}
				}
			}
			// If any task threw an error, try to report it back as
			// though we weren't using a threaded search algorithm.
			//
			if (!errors.IsEmpty())
			{
				Exception err = errors[0];
				if (err is Error)
				{
					throw (Error)err;
				}
				if (err is RuntimeException)
				{
					throw (RuntimeException)err;
				}
				if (err is IOException)
				{
					throw (IOException)err;
				}
				IOException fail = new IOException(err.Message);
				Sharpen.Extensions.InitCause(fail, err);
				throw fail;
			}
		}

		private sealed class _Runnable_751 : Runnable
		{
			public _Runnable_751(DeltaTask task, IList<Exception> errors, CountDownLatch done
				)
			{
				this.task = task;
				this.errors = errors;
				this.done = done;
			}

			public void Run()
			{
				try
				{
					task.Call();
				}
				catch (Exception failure)
				{
					errors.AddItem(failure);
				}
				finally
				{
					done.CountDown();
				}
			}

			private readonly DeltaTask task;

			private readonly IList<Exception> errors;

			private readonly CountDownLatch done;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RunTasks(ExecutorService pool, IList<DeltaTask> tasks, IList<Exception
			> errors)
		{
			IList<Future<object>> futures = new AList<Future<object>>(tasks.Count);
			foreach (DeltaTask task in tasks)
			{
				futures.AddItem(pool.Submit(task));
			}
			try
			{
				foreach (Future<object> f in futures)
				{
					try
					{
						f.Get();
					}
					catch (ExecutionException failed)
					{
						errors.AddItem(failed.InnerException);
					}
				}
			}
			catch (Exception)
			{
				foreach (Future<object> f in futures)
				{
					f.Cancel(true);
				}
				throw new IOException(JGitText.Get().packingCancelledDuringObjectsWriting);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObjects(PackOutputStream @out)
		{
			if (reuseSupport != null)
			{
				foreach (IList<ObjectToPack> list in objectsLists)
				{
					reuseSupport.WriteObjects(@out, list.AsIterable());
				}
			}
			else
			{
				foreach (IList<ObjectToPack> list in objectsLists)
				{
					foreach (ObjectToPack otp in list)
					{
						@out.WriteObject(otp);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void WriteObject(PackOutputStream @out, ObjectToPack otp)
		{
			if (otp.IsWritten())
			{
				return;
			}
			// We shouldn't be here.
			otp.MarkWantWrite();
			if (otp.IsDeltaRepresentation())
			{
				WriteBaseFirst(@out, otp);
			}
			@out.ResetCRC32();
			otp.SetOffset(@out.Length());
			while (otp.IsReuseAsIs())
			{
				try
				{
					reuseSupport.CopyObjectAsIs(@out, otp);
					@out.EndObject();
					otp.SetCRC(@out.GetCRC32());
					return;
				}
				catch (StoredObjectRepresentationNotAvailableException gone)
				{
					if (otp.GetOffset() == @out.Length())
					{
						RedoSearchForReuse(otp);
						continue;
					}
					else
					{
						// Object writing already started, we cannot recover.
						//
						CorruptObjectException coe;
						coe = new CorruptObjectException(otp, string.Empty);
						Sharpen.Extensions.InitCause(coe, gone);
						throw coe;
					}
				}
			}
			// If we reached here, reuse wasn't possible.
			//
			if (otp.IsDeltaRepresentation())
			{
				WriteDeltaObjectDeflate(@out, otp);
			}
			else
			{
				WriteWholeObjectDeflate(@out, otp);
			}
			@out.EndObject();
			otp.SetCRC(@out.GetCRC32());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteBaseFirst(PackOutputStream @out, ObjectToPack otp)
		{
			ObjectToPack baseInPack = otp.GetDeltaBase();
			if (baseInPack != null)
			{
				if (!baseInPack.IsWritten())
				{
					if (baseInPack.WantWrite())
					{
						// There is a cycle. Our caller is trying to write the
						// object we want as a base, and called us. Turn off
						// delta reuse so we can find another form.
						//
						reuseDeltas = false;
						RedoSearchForReuse(otp);
						reuseDeltas = true;
					}
					else
					{
						WriteObject(@out, baseInPack);
					}
				}
			}
			else
			{
				if (!thin)
				{
					// This should never occur, the base isn't in the pack and
					// the pack isn't allowed to reference base outside objects.
					// Write the object as a whole form, even if that is slow.
					//
					otp.ClearDeltaBase();
					otp.ClearReuseAsIs();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		private void RedoSearchForReuse(ObjectToPack otp)
		{
			otp.ClearDeltaBase();
			otp.ClearReuseAsIs();
			reuseSupport.SelectObjectRepresentation(this, NullProgressMonitor.INSTANCE, Sharpen.Collections
				.Singleton(otp).AsIterable());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteWholeObjectDeflate(PackOutputStream @out, ObjectToPack otp)
		{
			ICSharpCode.SharpZipLib.Zip.Compression.Deflater deflater = Deflater();
			ObjectLoader ldr = reader.Open(otp, otp.GetType());
			@out.WriteHeader(otp, ldr.GetSize());
			deflater.Reset();
			DeflaterOutputStream dst = new DeflaterOutputStream(@out, deflater);
			ldr.CopyTo(dst);
			dst.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteDeltaObjectDeflate(PackOutputStream @out, ObjectToPack otp)
		{
			DeltaCache.Ref @ref = otp.PopCachedDelta();
			if (@ref != null)
			{
				byte[] zbuf = @ref.Get();
				if (zbuf != null)
				{
					@out.WriteHeader(otp, otp.GetCachedSize());
					@out.Write(zbuf);
					return;
				}
			}
			TemporaryBuffer.Heap delta = Delta(otp);
			@out.WriteHeader(otp, delta.Length());
			ICSharpCode.SharpZipLib.Zip.Compression.Deflater deflater = Deflater();
			deflater.Reset();
			DeflaterOutputStream dst = new DeflaterOutputStream(@out, deflater);
			delta.WriteTo(dst, null);
			dst.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TemporaryBuffer.Heap Delta(ObjectToPack otp)
		{
			DeltaIndex index = new DeltaIndex(Buffer(otp.GetDeltaBaseId()));
			byte[] res = Buffer(otp);
			// We never would have proposed this pair if the delta would be
			// larger than the unpacked version of the object. So using it
			// as our buffer limit is valid: we will never reach it.
			//
			TemporaryBuffer.Heap delta = new TemporaryBuffer.Heap(res.Length);
			index.Encode(delta, res);
			return delta;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private byte[] Buffer(AnyObjectId objId)
		{
			return Buffer(config, reader, objId);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static byte[] Buffer(PackConfig config, ObjectReader or, AnyObjectId objId
			)
		{
			// PackWriter should have already pruned objects that
			// are above the big file threshold, so our chances of
			// the object being below it are very good. We really
			// shouldn't be here, unless the implementation is odd.
			return or.Open(objId).GetCachedBytes(config.GetBigFileThreshold());
		}

		private ICSharpCode.SharpZipLib.Zip.Compression.Deflater Deflater()
		{
			if (myDeflater == null)
			{
				myDeflater = new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(config.GetCompressionLevel
					());
			}
			return myDeflater;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteChecksum(PackOutputStream @out)
		{
			packcsum = @out.GetDigest();
			@out.Write(packcsum);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		private ObjectWalk SetUpWalker<_T0, _T1>(ICollection<_T0> interestingObjects, ICollection
			<_T1> uninterestingObjects) where _T0:ObjectId where _T1:ObjectId
		{
			IList<ObjectId> all = new AList<ObjectId>(interestingObjects.Count);
			foreach (ObjectId id in interestingObjects)
			{
				all.AddItem(id.Copy());
			}
			ICollection<ObjectId> not;
			if (uninterestingObjects != null && !uninterestingObjects.IsEmpty())
			{
				not = new HashSet<ObjectId>();
				foreach (ObjectId id_1 in uninterestingObjects)
				{
					not.AddItem(id_1.Copy());
				}
				Sharpen.Collections.AddAll(all, not);
			}
			else
			{
				not = Sharpen.Collections.EmptySet<ObjectId>();
			}
			ObjectWalk walker = new ObjectWalk(reader);
			walker.SetRetainBody(false);
			walker.Sort(RevSort.TOPO);
			if (thin && !not.IsEmpty())
			{
				walker.Sort(RevSort.BOUNDARY, true);
			}
			AsyncRevObjectQueue q = walker.ParseAny(all.AsIterable(), true);
			try
			{
				for (; ; )
				{
					try
					{
						RevObject o = q.Next();
						if (o == null)
						{
							break;
						}
						if (not.Contains(o.Copy()))
						{
							walker.MarkUninteresting(o);
						}
						else
						{
							walker.MarkStart(o);
						}
					}
					catch (MissingObjectException e)
					{
						if (ignoreMissingUninteresting && not.Contains(e.GetObjectId()))
						{
							continue;
						}
						throw;
					}
				}
			}
			finally
			{
				q.Release();
			}
			return walker;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void FindObjectsToPack(ProgressMonitor countingMonitor, ObjectWalk walker
			)
		{
			countingMonitor.BeginTask(JGitText.Get().countingObjects, ProgressMonitor.UNKNOWN
				);
			RevObject o;
			while ((o = walker.Next()) != null)
			{
				AddObject(o, 0);
				countingMonitor.Update(1);
			}
			while ((o = walker.NextObject()) != null)
			{
				AddObject(o, walker.GetPathHashCode());
				countingMonitor.Update(1);
			}
			countingMonitor.EndTask();
		}

		/// <summary>Include one object to the output file.</summary>
		/// <remarks>
		/// Include one object to the output file.
		/// <p>
		/// Objects are written in the order they are added. If the same object is
		/// added twice, it may be written twice, creating a larger than necessary
		/// file.
		/// </remarks>
		/// <param name="object">the object to add.</param>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the object is an unsupported type.
		/// 	</exception>
		public virtual void AddObject(RevObject @object)
		{
			AddObject(@object, 0);
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		private void AddObject(RevObject @object, int pathHashCode)
		{
			if (@object.Has(RevFlag.UNINTERESTING))
			{
				switch (@object.Type)
				{
					case Constants.OBJ_TREE:
					case Constants.OBJ_BLOB:
					{
						ObjectToPack otp = new ObjectToPack(@object);
						otp.SetPathHash(pathHashCode);
						otp.SetEdge();
						edgeObjects.Add(otp);
						thin = true;
						break;
					}
				}
				return;
			}
			ObjectToPack otp_1;
			if (reuseSupport != null)
			{
				otp_1 = reuseSupport.NewObjectToPack(@object);
			}
			else
			{
				otp_1 = new ObjectToPack(@object);
			}
			otp_1.SetPathHash(pathHashCode);
			try
			{
				objectsLists[@object.Type].AddItem(otp_1);
			}
			catch (IndexOutOfRangeException)
			{
				throw new IncorrectObjectTypeException(@object, JGitText.Get().incorrectObjectType_COMMITnorTREEnorBLOBnorTAG
					);
			}
			catch (NotSupportedException)
			{
				// index pointing to "dummy" empty list
				throw new IncorrectObjectTypeException(@object, JGitText.Get().incorrectObjectType_COMMITnorTREEnorBLOBnorTAG
					);
			}
			objectsMap.Add(otp_1);
		}

		/// <summary>Select an object representation for this writer.</summary>
		/// <remarks>
		/// Select an object representation for this writer.
		/// <p>
		/// An
		/// <see cref="NGit.ObjectReader">NGit.ObjectReader</see>
		/// implementation should invoke this method once for
		/// each representation available for an object, to allow the writer to find
		/// the most suitable one for the output.
		/// </remarks>
		/// <param name="otp">the object being packed.</param>
		/// <param name="next">the next available representation from the repository.</param>
		public virtual void Select(ObjectToPack otp, StoredObjectRepresentation next)
		{
			int nFmt = next.GetFormat();
			int nWeight;
			if (otp.IsReuseAsIs())
			{
				// We've already chosen to reuse a packed form, if next
				// cannot beat that break out early.
				//
				if (StoredObjectRepresentation.PACK_WHOLE < nFmt)
				{
					return;
				}
				else
				{
					// next isn't packed
					if (StoredObjectRepresentation.PACK_DELTA < nFmt && otp.IsDeltaRepresentation())
					{
						return;
					}
				}
				// next isn't a delta, but we are
				nWeight = next.GetWeight();
				if (otp.GetWeight() <= nWeight)
				{
					return;
				}
			}
			else
			{
				// next would be bigger
				nWeight = next.GetWeight();
			}
			if (nFmt == StoredObjectRepresentation.PACK_DELTA && reuseDeltas)
			{
				ObjectId baseId = next.GetDeltaBase();
				ObjectToPack ptr = objectsMap.Get(baseId);
				if (ptr != null)
				{
					otp.SetDeltaBase(ptr);
					otp.SetReuseAsIs();
					otp.SetWeight(nWeight);
				}
				else
				{
					if (thin && edgeObjects.Contains(baseId))
					{
						otp.SetDeltaBase(baseId);
						otp.SetReuseAsIs();
						otp.SetWeight(nWeight);
					}
					else
					{
						otp.ClearDeltaBase();
						otp.ClearReuseAsIs();
					}
				}
			}
			else
			{
				if (nFmt == StoredObjectRepresentation.PACK_WHOLE && config.IsReuseObjects())
				{
					otp.ClearDeltaBase();
					otp.SetReuseAsIs();
					otp.SetWeight(nWeight);
				}
				else
				{
					otp.ClearDeltaBase();
					otp.ClearReuseAsIs();
				}
			}
			otp.Select(next);
		}
	}
}
