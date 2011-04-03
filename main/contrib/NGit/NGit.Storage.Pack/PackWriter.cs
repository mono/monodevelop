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

		private readonly ObjectIdOwnerMap<ObjectToPack> objectsMap = new ObjectIdOwnerMap
			<ObjectToPack>();

		private IList<ObjectToPack> edgeObjects = new BlockList<ObjectToPack>();

		private IList<CachedPack> cachedPacks = new AList<CachedPack>(2);

		private ICollection<ObjectId> tagTargets = Sharpen.Collections.EmptySet<ObjectId>();

		private ICSharpCode.SharpZipLib.Zip.Compression.Deflater myDeflater;

		private readonly ObjectReader reader;

		/// <summary>
		/// <see cref="reader">reader</see>
		/// recast to the reuse interface, if it supports it.
		/// </summary>
		private readonly ObjectReuseAsIs reuseSupport;

		private readonly PackConfig config;

		private readonly PackWriter.Statistics stats;

		private PackWriter.Statistics.ObjectType typeStats;

		private IList<ObjectToPack> sortedByName;

		private byte[] packcsum;

		private bool deltaBaseAsOffset;

		private bool reuseDeltas;

		private bool reuseDeltaCommits;

		private bool reuseValidate;

		private bool thin;

		private bool useCachedPacks;

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
				objectsLists[Constants.OBJ_COMMIT] = new BlockList<ObjectToPack>();
				objectsLists[Constants.OBJ_TREE] = new BlockList<ObjectToPack>();
				objectsLists[Constants.OBJ_BLOB] = new BlockList<ObjectToPack>();
				objectsLists[Constants.OBJ_TAG] = new BlockList<ObjectToPack>();
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
			reuseValidate = true;
			// be paranoid by default
			stats = new PackWriter.Statistics();
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

		/// <summary>Check if the writer will reuse commits that are already stored as deltas.
		/// 	</summary>
		/// <remarks>Check if the writer will reuse commits that are already stored as deltas.
		/// 	</remarks>
		/// <returns>
		/// true if the writer would reuse commits stored as deltas, assuming
		/// delta reuse is already enabled.
		/// </returns>
		public virtual bool IsReuseDeltaCommits()
		{
			return reuseDeltaCommits;
		}

		/// <summary>Set the writer to reuse existing delta versions of commits.</summary>
		/// <remarks>Set the writer to reuse existing delta versions of commits.</remarks>
		/// <param name="reuse">
		/// if true, the writer will reuse any commits stored as deltas.
		/// By default the writer does not reuse delta commits.
		/// </param>
		public virtual void SetReuseDeltaCommits(bool reuse)
		{
			reuseDeltaCommits = reuse;
		}

		/// <summary>Check if the writer validates objects before copying them.</summary>
		/// <remarks>Check if the writer validates objects before copying them.</remarks>
		/// <returns>
		/// true if validation is enabled; false if the reader will handle
		/// object validation as a side-effect of it consuming the output.
		/// </returns>
		public virtual bool IsReuseValidatingObjects()
		{
			return reuseValidate;
		}

		/// <summary>Enable (or disable) object validation during packing.</summary>
		/// <remarks>Enable (or disable) object validation during packing.</remarks>
		/// <param name="validate">
		/// if true the pack writer will validate an object before it is
		/// put into the output. This additional validation work may be
		/// necessary to avoid propagating corruption from one local pack
		/// file to another local pack file.
		/// </param>
		public virtual void SetReuseValidatingObjects(bool validate)
		{
			reuseValidate = validate;
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

		/// <returns>true to reuse cached packs. If true index creation isn't available.</returns>
		public virtual bool IsUseCachedPacks()
		{
			return useCachedPacks;
		}

		/// <param name="useCached">
		/// if set to true and a cached pack is present, it will be
		/// appended onto the end of a thin-pack, reducing the amount of
		/// working set space and CPU used by PackWriter. Enabling this
		/// feature prevents PackWriter from creating an index for the
		/// newly created pack, so its only suitable for writing to a
		/// network client, where the client will make the index.
		/// </param>
		public virtual void SetUseCachedPacks(bool useCached)
		{
			useCachedPacks = useCached;
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

		/// <summary>Set the tag targets that should be hoisted earlier during packing.</summary>
		/// <remarks>
		/// Set the tag targets that should be hoisted earlier during packing.
		/// <p>
		/// Callers may put objects into this set before invoking any of the
		/// preparePack methods to influence where an annotated tag's target is
		/// stored within the resulting pack. Typically these will be clustered
		/// together, and hoisted earlier in the file even if they are ancient
		/// revisions, allowing readers to find tag targets with better locality.
		/// </remarks>
		/// <param name="objects">objects that annotated tags point at.</param>
		public virtual void SetTagTargets(ICollection<ObjectId> objects)
		{
			tagTargets = objects;
		}

		/// <summary>Returns objects number in a pack file that was created by this writer.</summary>
		/// <remarks>Returns objects number in a pack file that was created by this writer.</remarks>
		/// <returns>number of objects in pack.</returns>
		/// <exception cref="System.IO.IOException">a cached pack cannot supply its object count.
		/// 	</exception>
		public virtual long GetObjectCount()
		{
			if (stats.totalObjects == 0)
			{
				long objCnt = 0;
				foreach (IList<ObjectToPack> list in objectsLists)
				{
					objCnt += list.Count;
				}
				foreach (CachedPack pack in cachedPacks)
				{
					objCnt += pack.GetObjectCount();
				}
				return objCnt;
			}
			return stats.totalObjects;
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
		/// ; objects returned by iterator may be
		/// later reused by caller as object id and type are internally
		/// copied in each iteration.
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
		/// <param name="want">
		/// collection of objects to be marked as interesting (start
		/// points of graph traversal).
		/// </param>
		/// <param name="have">
		/// collection of objects to be marked as uninteresting (end
		/// points of graph traversal).
		/// </param>
		/// <exception cref="System.IO.IOException">when some I/O problem occur during reading objects.
		/// 	</exception>
		public virtual void PreparePack<_T0, _T1>(ProgressMonitor countingMonitor, ICollection
			<_T0> want, ICollection<_T1> have) where _T0:ObjectId where _T1:ObjectId
		{
			ObjectWalk ow = new ObjectWalk(reader);
			PreparePack(countingMonitor, ow, want, have);
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
		/// <param name="walk">ObjectWalk to perform enumeration.</param>
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
		public virtual void PreparePack<_T0, _T1>(ProgressMonitor countingMonitor, ObjectWalk
			 walk, ICollection<_T0> interestingObjects, ICollection<_T1> uninterestingObjects
			) where _T0:ObjectId where _T1:ObjectId
		{
			if (countingMonitor == null)
			{
				countingMonitor = NullProgressMonitor.INSTANCE;
			}
			FindObjectsToPack(countingMonitor, walk, interestingObjects, uninterestingObjects
				);
		}

		/// <summary>Determine if the pack file will contain the requested object.</summary>
		/// <remarks>Determine if the pack file will contain the requested object.</remarks>
		/// <param name="id">the object to test the existence of.</param>
		/// <returns>true if the object will appear in the output pack file.</returns>
		/// <exception cref="System.IO.IOException">a cached pack cannot be examined.</exception>
		public virtual bool WillInclude(ObjectId id)
		{
			ObjectToPack obj = objectsMap.Get(id);
			if (obj != null && !obj.IsEdge())
			{
				return true;
			}
			ICollection<ObjectId> toFind = Sharpen.Collections.Singleton(id.ToObjectId());
			foreach (CachedPack pack in cachedPacks)
			{
				if (pack.HasObject(toFind.AsIterable()).Contains(id))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Lookup the ObjectToPack object for a given ObjectId.</summary>
		/// <remarks>Lookup the ObjectToPack object for a given ObjectId.</remarks>
		/// <param name="id">the object to find in the pack.</param>
		/// <returns>the object we are packing, or null.</returns>
		public virtual ObjectToPack Get(AnyObjectId id)
		{
			ObjectToPack obj = objectsMap.Get(id);
			return obj != null && !obj.IsEdge() ? obj : null;
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
			if (!cachedPacks.IsEmpty())
			{
				throw new IOException(JGitText.Get().cachedPacksPreventsIndexCreation);
			}
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
				int cnt = 0;
				foreach (IList<ObjectToPack> list in objectsLists)
				{
					cnt += list.Count;
				}
				sortedByName = new BlockList<ObjectToPack>(cnt);
				foreach (IList<ObjectToPack> list_1 in objectsLists)
				{
					Sharpen.Collections.AddAll(sortedByName, list_1);
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
			long objCnt = GetObjectCount();
			stats.totalObjects = objCnt;
			writeMonitor.BeginTask(JGitText.Get().writingObjects, (int)objCnt);
			long writeStart = Runtime.CurrentTimeMillis();
			@out.WriteFileHeader(PACK_VERSION_GENERATED, objCnt);
			@out.Flush();
			WriteObjects(@out);
			if (!edgeObjects.IsEmpty() || !cachedPacks.IsEmpty())
			{
				foreach (PackWriter.Statistics.ObjectType typeStat in stats.objectTypes)
				{
					if (typeStat == null)
					{
						continue;
					}
					stats.thinPackBytes += typeStat.bytes;
				}
			}
			foreach (CachedPack pack in cachedPacks)
			{
				long deltaCnt = pack.GetDeltaCount();
				stats.reusedObjects += pack.GetObjectCount();
				stats.reusedDeltas += deltaCnt;
				stats.totalDeltas += deltaCnt;
				reuseSupport.CopyPackAsIs(@out, pack, reuseValidate);
			}
			WriteChecksum(@out);
			@out.Flush();
			stats.timeWriting = Runtime.CurrentTimeMillis() - writeStart;
			stats.totalBytes = @out.Length();
			stats.reusedPacks = Sharpen.Collections.UnmodifiableList(cachedPacks);
			foreach (PackWriter.Statistics.ObjectType typeStat_1 in stats.objectTypes)
			{
				if (typeStat_1 == null)
				{
					continue;
				}
				typeStat_1.cntDeltas += typeStat_1.reusedDeltas;
				stats.reusedObjects += typeStat_1.reusedObjects;
				stats.reusedDeltas += typeStat_1.reusedDeltas;
				stats.totalDeltas += typeStat_1.cntDeltas;
			}
			reader.Release();
			writeMonitor.EndTask();
		}

		/// <returns>
		/// description of what this PackWriter did in order to create the
		/// final pack stream. The object is only available to callers after
		/// <see cref="WritePack(NGit.ProgressMonitor, NGit.ProgressMonitor, Sharpen.OutputStream)
		/// 	">WritePack(NGit.ProgressMonitor, NGit.ProgressMonitor, Sharpen.OutputStream)</see>
		/// </returns>
		public virtual PackWriter.Statistics GetStatistics()
		{
			return stats;
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
			int cnt = 0;
			foreach (IList<ObjectToPack> list in objectsLists)
			{
				cnt += list.Count;
			}
			long start = Runtime.CurrentTimeMillis();
			monitor.BeginTask(JGitText.Get().searchForReuse, cnt);
			foreach (IList<ObjectToPack> list_1 in objectsLists)
			{
				reuseSupport.SelectObjectRepresentation(this, monitor, list_1.AsIterable());
			}
			monitor.EndTask();
			stats.timeSearchingForReuse = Runtime.CurrentTimeMillis() - start;
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
				[Constants.OBJ_BLOB].Count + edgeObjects.Count];
			int cnt = 0;
			cnt = FindObjectsNeedingDelta(list, cnt, Constants.OBJ_TREE);
			cnt = FindObjectsNeedingDelta(list, cnt, Constants.OBJ_BLOB);
			if (cnt == 0)
			{
				return;
			}
			int nonEdgeCnt = cnt;
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
			long sizingStart = Runtime.CurrentTimeMillis();
			monitor.BeginTask(JGitText.Get().searchForSizes, cnt);
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
							otp = objectsMap.Get(notFound.GetObjectId());
							if (otp != null && otp.IsEdge())
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
			stats.timeSearchingForSizes = Runtime.CurrentTimeMillis() - sizingStart;
			// Sort the objects by path hash so like files are near each other,
			// and then by size descending so that bigger files are first. This
			// applies "Linus' Law" which states that newer files tend to be the
			// bigger ones, because source files grow and hardly ever shrink.
			//
			Arrays.Sort(list, 0, cnt, new _IComparer_812());
			// Above we stored the objects we cannot delta onto the end.
			// Remove them from the list so we don't waste time on them.
			while (0 < cnt && list[cnt - 1].IsDoNotDelta())
			{
				if (!list[cnt - 1].IsEdge())
				{
					nonEdgeCnt--;
				}
				cnt--;
			}
			if (cnt == 0)
			{
				return;
			}
			long searchStart = Runtime.CurrentTimeMillis();
			monitor.BeginTask(JGitText.Get().compressingObjects, nonEdgeCnt);
			SearchForDeltas(monitor, list, cnt);
			monitor.EndTask();
			stats.deltaSearchNonEdgeObjects = nonEdgeCnt;
			stats.timeCompressing = Runtime.CurrentTimeMillis() - searchStart;
			for (int i = 0; i < cnt; i++)
			{
				if (!list[i].IsEdge() && list[i].IsDeltaRepresentation())
				{
					stats.deltasFound++;
				}
			}
		}

		private sealed class _IComparer_812 : IComparer<ObjectToPack>
		{
			public _IComparer_812()
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
				cmp = (a.IsEdge() ? 0 : 1) - (b.IsEdge() ? 0 : 1);
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
				if (otp.IsReuseAsIs())
				{
					// already reusing a representation
					continue;
				}
				if (otp.IsDoNotDelta())
				{
					// delta is disabled for this path
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
			ThreadSafeProgressMonitor pm = new ThreadSafeProgressMonitor(monitor);
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
			pm.StartWorkers(myTasks.Count);
			Executor executor = config.GetExecutor();
			IList<Exception> errors = Sharpen.Collections.SynchronizedList(new AList<Exception
				>());
			if (executor is ExecutorService)
			{
				// Caller supplied us a service, use it directly.
				//
				RunTasks((ExecutorService)executor, pm, myTasks, errors);
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
						RunTasks(pool, pm, myTasks, errors);
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
					foreach (DeltaTask task in myTasks)
					{
						executor.Execute(new _Runnable_962(task, errors));
					}
					try
					{
						pm.WaitForCompletion();
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

		private sealed class _Runnable_962 : Runnable
		{
			public _Runnable_962(DeltaTask task, IList<Exception> errors)
			{
				this.task = task;
				this.errors = errors;
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
			}

			private readonly DeltaTask task;

			private readonly IList<Exception> errors;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RunTasks(ExecutorService pool, ThreadSafeProgressMonitor pm, IList<DeltaTask
			> tasks, IList<Exception> errors)
		{
			IList<Future<object>> futures = new AList<Future<object>>(tasks.Count);
			foreach (DeltaTask task in tasks)
			{
				futures.AddItem(pool.Submit(task));
			}
			try
			{
				pm.WaitForCompletion();
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
			WriteObjects(@out, objectsLists[Constants.OBJ_COMMIT]);
			WriteObjects(@out, objectsLists[Constants.OBJ_TAG]);
			WriteObjects(@out, objectsLists[Constants.OBJ_TREE]);
			WriteObjects(@out, objectsLists[Constants.OBJ_BLOB]);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObjects(PackOutputStream @out, IList<ObjectToPack> list)
		{
			if (list.IsEmpty())
			{
				return;
			}
			typeStats = stats.objectTypes[list[0].GetType()];
			long beginOffset = @out.Length();
			if (reuseSupport != null)
			{
				reuseSupport.WriteObjects(@out, list);
			}
			else
			{
				foreach (ObjectToPack otp in list)
				{
					@out.WriteObject(otp);
				}
			}
			typeStats.bytes += @out.Length() - beginOffset;
			typeStats.cntObjects = list.Count;
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
					reuseSupport.CopyObjectAsIs(@out, otp, reuseValidate);
					@out.EndObject();
					otp.SetCRC(@out.GetCRC32());
					typeStats.reusedObjects++;
					if (otp.IsDeltaRepresentation())
					{
						typeStats.reusedDeltas++;
						typeStats.deltaBytes += @out.Length() - otp.GetOffset();
					}
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
			typeStats.cntDeltas++;
			typeStats.deltaBytes += @out.Length() - otp.GetOffset();
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
		private void FindObjectsToPack<_T0, _T1>(ProgressMonitor countingMonitor, ObjectWalk
			 walker, ICollection<_T0> want, ICollection<_T1> have) where _T0:ObjectId where 
			_T1:ObjectId
		{
			long countingStart = Runtime.CurrentTimeMillis();
			countingMonitor.BeginTask(JGitText.Get().countingObjects, ProgressMonitor.UNKNOWN
				);
			if (have == null)
			{
				have = Sharpen.Collections.EmptySet<_T1>();
			}
			stats.interestingObjects = Sharpen.Collections.UnmodifiableSet(new HashSet<ObjectId>(want.UpcastTo<_T0,ObjectId> ()));
			stats.uninterestingObjects = Sharpen.Collections.UnmodifiableSet(new HashSet<ObjectId>(have.UpcastTo<_T1,ObjectId> ()));
			IList<ObjectId> all = new AList<ObjectId>(want.Count + have.Count);
			Sharpen.Collections.AddAll(all, want);
			Sharpen.Collections.AddAll(all, have);
			IDictionary<ObjectId, CachedPack> tipToPack = new Dictionary<ObjectId, CachedPack
				>();
			RevFlag inCachedPack = walker.NewFlag("inCachedPack");
			RevFlag include = walker.NewFlag("include");
			RevFlag added = walker.NewFlag("added");
			RevFlagSet keepOnRestart = new RevFlagSet();
			keepOnRestart.AddItem(inCachedPack);
			walker.SetRetainBody(false);
			walker.Carry(include);
			int haveEst = have.Count;
			if (have.IsEmpty())
			{
				walker.Sort(RevSort.COMMIT_TIME_DESC);
				if (useCachedPacks && reuseSupport != null)
				{
					ICollection<ObjectId> need = new HashSet<ObjectId>(want.UpcastTo<_T0,ObjectId> ());
					IList<CachedPack> shortCircuit = new List<CachedPack>();
					foreach (CachedPack pack in reuseSupport.GetCachedPacks())
					{
						if (need.ContainsAll(pack.GetTips()))
						{
							need.RemoveAll(pack.GetTips());
							shortCircuit.AddItem(pack);
						}
						foreach (ObjectId id in pack.GetTips())
						{
							tipToPack.Put(id, pack);
							all.AddItem(id);
						}
					}
					if (need.IsEmpty() && !shortCircuit.IsEmpty())
					{
						Sharpen.Collections.AddAll(cachedPacks, shortCircuit);
						foreach (CachedPack pack_1 in shortCircuit)
						{
							countingMonitor.Update((int)pack_1.GetObjectCount());
						}
						countingMonitor.EndTask();
						stats.timeCounting = Runtime.CurrentTimeMillis() - countingStart;
						return;
					}
					haveEst += tipToPack.Count;
				}
			}
			else
			{
				walker.Sort(RevSort.TOPO);
				if (thin)
				{
					walker.Sort(RevSort.BOUNDARY, true);
				}
			}
			IList<RevObject> wantObjs = new AList<RevObject>(want.Count);
			IList<RevObject> haveObjs = new AList<RevObject>(haveEst);
			IList<RevTag> wantTags = new AList<RevTag>(want.Count);
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
						if (tipToPack.ContainsKey(o))
						{
							o.Add(inCachedPack);
						}
						if (have.Contains(o))
						{
							haveObjs.AddItem(o);
						}
						else
						{
							if (want.Contains(o))
							{
								o.Add(include);
								wantObjs.AddItem(o);
								if (o is RevTag)
								{
									wantTags.AddItem((RevTag)o);
								}
							}
						}
					}
					catch (MissingObjectException e)
					{
						if (ignoreMissingUninteresting && have.Contains(e.GetObjectId()))
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
			if (!wantTags.IsEmpty())
			{
				all = new AList<ObjectId>(wantTags.Count);
				foreach (RevTag tag in wantTags)
				{
					all.AddItem(tag.GetObject());
				}
				q = walker.ParseAny(all.AsIterable(), true);
				try
				{
					while (q.Next() != null)
					{
					}
				}
				finally
				{
					// Just need to pop the queue item to parse the object.
					q.Release();
				}
			}
			foreach (RevObject obj in wantObjs)
			{
				walker.MarkStart(obj);
			}
			foreach (RevObject obj_1 in haveObjs)
			{
				walker.MarkUninteresting(obj_1);
			}
			int typesToPrune = 0;
			int maxBases = config.GetDeltaSearchWindowSize();
			ICollection<RevTree> baseTrees = new HashSet<RevTree>();
			BlockList<RevCommit> commits = new BlockList<RevCommit>();
			RevCommit c;
			while ((c = walker.Next()) != null)
			{
				if (c.Has(inCachedPack))
				{
					CachedPack pack = tipToPack.Get(c);
					if (IncludesAllTips(pack, include, walker))
					{
						UseCachedPack(walker, keepOnRestart, wantObjs, haveObjs, pack);
						//
						commits = new BlockList<RevCommit>();
						countingMonitor.EndTask();
						countingMonitor.BeginTask(JGitText.Get().countingObjects, ProgressMonitor.UNKNOWN
							);
						continue;
					}
				}
				if (c.Has(RevFlag.UNINTERESTING))
				{
					if (baseTrees.Count <= maxBases)
					{
						baseTrees.AddItem(c.Tree);
					}
					continue;
				}
				commits.AddItem(c);
				countingMonitor.Update(1);
			}
			int commitCnt = 0;
			bool putTagTargets = false;
			foreach (RevCommit cmit in commits)
			{
				if (!cmit.Has(added))
				{
					cmit.Add(added);
					AddObject(cmit, 0);
					commitCnt++;
				}
				for (int i = 0; i < cmit.ParentCount; i++)
				{
					RevCommit p = cmit.GetParent(i);
					if (!p.Has(added) && !p.Has(RevFlag.UNINTERESTING))
					{
						p.Add(added);
						AddObject(p, 0);
						commitCnt++;
					}
				}
				if (!putTagTargets && 4096 < commitCnt)
				{
					foreach (ObjectId id in tagTargets)
					{
						RevObject obj_2 = walker.LookupOrNull(id);
						if (obj_2 is RevCommit && obj_2.Has(include) && !obj_2.Has(RevFlag.UNINTERESTING)
							 && !obj_2.Has(added))
						{
							obj_2.Add(added);
							AddObject(obj_2, 0);
						}
					}
					putTagTargets = true;
				}
			}
			commits = null;
			foreach (CachedPack p_1 in cachedPacks)
			{
				foreach (ObjectId d in p_1.HasObject(objectsLists[Constants.OBJ_COMMIT].AsIterable
					()))
				{
					if (baseTrees.Count <= maxBases)
					{
						baseTrees.AddItem(walker.LookupCommit(d).Tree);
					}
					objectsMap.Get(d).SetEdge();
					typesToPrune |= 1 << Constants.OBJ_COMMIT;
				}
			}
			BaseSearch bases = new BaseSearch(countingMonitor, baseTrees, objectsMap, edgeObjects
				, reader);
			//
			RevObject o_1;
			while ((o_1 = walker.NextObject()) != null)
			{
				if (o_1.Has(RevFlag.UNINTERESTING))
				{
					continue;
				}
				int pathHash = walker.GetPathHashCode();
				byte[] pathBuf = walker.GetPathBuffer();
				int pathLen = walker.GetPathLength();
				bases.AddBase(o_1.Type, pathBuf, pathLen, pathHash);
				AddObject(o_1, pathHash);
				countingMonitor.Update(1);
			}
			foreach (CachedPack p_2 in cachedPacks)
			{
				foreach (ObjectId d in p_2.HasObject(objectsLists[Constants.OBJ_TREE].AsIterable(
					)))
				{
					objectsMap.Get(d).SetEdge();
					typesToPrune |= 1 << Constants.OBJ_TREE;
				}
				foreach (ObjectId d_1 in p_2.HasObject(objectsLists[Constants.OBJ_BLOB].AsIterable
					()))
				{
					objectsMap.Get(d_1).SetEdge();
					typesToPrune |= 1 << Constants.OBJ_BLOB;
				}
				foreach (ObjectId d_2 in p_2.HasObject(objectsLists[Constants.OBJ_TAG].AsIterable
					()))
				{
					objectsMap.Get(d_2).SetEdge();
					typesToPrune |= 1 << Constants.OBJ_TAG;
				}
			}
			if (typesToPrune != 0)
			{
				PruneObjectList(typesToPrune, Constants.OBJ_COMMIT);
				PruneObjectList(typesToPrune, Constants.OBJ_TREE);
				PruneObjectList(typesToPrune, Constants.OBJ_BLOB);
				PruneObjectList(typesToPrune, Constants.OBJ_TAG);
			}
			foreach (CachedPack pack_2 in cachedPacks)
			{
				countingMonitor.Update((int)pack_2.GetObjectCount());
			}
			countingMonitor.EndTask();
			stats.timeCounting = Runtime.CurrentTimeMillis() - countingStart;
		}

		private void PruneObjectList(int typesToPrune, int typeCode)
		{
			if ((typesToPrune & (1 << typeCode)) == 0)
			{
				return;
			}
			IList<ObjectToPack> list = objectsLists[typeCode];
			int size = list.Count;
			int src = 0;
			int dst = 0;
			for (; src < size; src++)
			{
				ObjectToPack obj = list[src];
				if (obj.IsEdge())
				{
					continue;
				}
				if (dst != src)
				{
					list.Set(dst, obj);
				}
				dst++;
			}
			while (dst < list.Count)
			{
				list.Remove(list.Count - 1);
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void UseCachedPack(ObjectWalk walker, RevFlagSet keepOnRestart, IList<RevObject
			> wantObj, IList<RevObject> baseObj, CachedPack pack)
		{
			cachedPacks.AddItem(pack);
			foreach (ObjectId id in pack.GetTips())
			{
				baseObj.AddItem(walker.LookupOrNull(id));
			}
			SetThin(true);
			walker.ResetRetain(keepOnRestart);
			walker.Sort(RevSort.TOPO);
			walker.Sort(RevSort.BOUNDARY, true);
			foreach (RevObject id_1 in wantObj)
			{
				walker.MarkStart(id_1);
			}
			foreach (RevObject id_2 in baseObj)
			{
				walker.MarkUninteresting(id_2);
			}
		}

		private static bool IncludesAllTips(CachedPack pack, RevFlag include, ObjectWalk 
			walker)
		{
			foreach (ObjectId id in pack.GetTips())
			{
				if (!walker.LookupOrNull(id).Has(include))
				{
					return false;
				}
			}
			return true;
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
			ObjectToPack otp;
			if (reuseSupport != null)
			{
				otp = reuseSupport.NewObjectToPack(@object);
			}
			else
			{
				otp = new ObjectToPack(@object);
			}
			otp.SetPathHash(pathHashCode);
			try
			{
				objectsLists[@object.Type].AddItem(otp);
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
			objectsMap.Add(otp);
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
			if (nFmt == StoredObjectRepresentation.PACK_DELTA && reuseDeltas && ReuseDeltaFor
				(otp))
			{
				ObjectId baseId = next.GetDeltaBase();
				ObjectToPack ptr = objectsMap.Get(baseId);
				if (ptr != null && !ptr.IsEdge())
				{
					otp.SetDeltaBase(ptr);
					otp.SetReuseAsIs();
					otp.SetWeight(nWeight);
				}
				else
				{
					if (thin && ptr != null && ptr.IsEdge())
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

		private bool ReuseDeltaFor(ObjectToPack otp)
		{
			switch (otp.GetType())
			{
				case Constants.OBJ_COMMIT:
				{
					return reuseDeltaCommits;
				}

				case Constants.OBJ_TREE:
				{
					return true;
				}

				case Constants.OBJ_BLOB:
				{
					return true;
				}

				case Constants.OBJ_TAG:
				{
					return false;
				}

				default:
				{
					return true;
					break;
				}
			}
		}

		/// <summary>Summary of how PackWriter created the pack.</summary>
		/// <remarks>Summary of how PackWriter created the pack.</remarks>
		public class Statistics
		{
			/// <summary>Statistics about a single class of object.</summary>
			/// <remarks>Statistics about a single class of object.</remarks>
			public class ObjectType
			{
				internal long cntObjects;

				internal long cntDeltas;

				internal long reusedObjects;

				internal long reusedDeltas;

				internal long bytes;

				internal long deltaBytes;

				/// <returns>
				/// total number of objects output. This total includes the
				/// value of
				/// <see cref="GetDeltas()">GetDeltas()</see>
				/// .
				/// </returns>
				public virtual long GetObjects()
				{
					return cntObjects;
				}

				/// <returns>
				/// total number of deltas output. This may be lower than the
				/// actual number of deltas if a cached pack was reused.
				/// </returns>
				public virtual long GetDeltas()
				{
					return cntDeltas;
				}

				/// <returns>
				/// number of objects whose existing representation was
				/// reused in the output. This count includes
				/// <see cref="GetReusedDeltas()">GetReusedDeltas()</see>
				/// .
				/// </returns>
				public virtual long GetReusedObjects()
				{
					return reusedObjects;
				}

				/// <returns>
				/// number of deltas whose existing representation was reused
				/// in the output, as their base object was also output or
				/// was assumed present for a thin pack. This may be lower
				/// than the actual number of reused deltas if a cached pack
				/// was reused.
				/// </returns>
				public virtual long GetReusedDeltas()
				{
					return reusedDeltas;
				}

				/// <returns>
				/// total number of bytes written. This size includes the
				/// object headers as well as the compressed data. This size
				/// also includes all of
				/// <see cref="GetDeltaBytes()">GetDeltaBytes()</see>
				/// .
				/// </returns>
				public virtual long GetBytes()
				{
					return bytes;
				}

				/// <returns>
				/// number of delta bytes written. This size includes the
				/// object headers for the delta objects.
				/// </returns>
				public virtual long GetDeltaBytes()
				{
					return deltaBytes;
				}
			}

			internal ICollection<ObjectId> interestingObjects;

			internal ICollection<ObjectId> uninterestingObjects;

			internal ICollection<CachedPack> reusedPacks;

			internal int deltaSearchNonEdgeObjects;

			internal int deltasFound;

			internal long totalObjects;

			internal long totalDeltas;

			internal long reusedObjects;

			internal long reusedDeltas;

			internal long totalBytes;

			internal long thinPackBytes;

			internal long timeCounting;

			internal long timeSearchingForReuse;

			internal long timeSearchingForSizes;

			internal long timeCompressing;

			internal long timeWriting;

			internal PackWriter.Statistics.ObjectType[] objectTypes;

			/// <returns>
			/// unmodifiable collection of objects to be included in the
			/// pack. May be null if the pack was hand-crafted in a unit
			/// test.
			/// </returns>
			public virtual ICollection<ObjectId> GetInterestingObjects()
			{
				return interestingObjects;
			}

			/// <returns>
			/// unmodifiable collection of objects that should be excluded
			/// from the pack, as the peer that will receive the pack already
			/// has these objects.
			/// </returns>
			public virtual ICollection<ObjectId> GetUninterestingObjects()
			{
				return uninterestingObjects;
			}

			/// <returns>
			/// unmodifiable collection of the cached packs that were reused
			/// in the output, if any were selected for reuse.
			/// </returns>
			public virtual ICollection<CachedPack> GetReusedPacks()
			{
				return reusedPacks;
			}

			/// <returns>
			/// number of objects in the output pack that went through the
			/// delta search process in order to find a potential delta base.
			/// </returns>
			public virtual int GetDeltaSearchNonEdgeObjects()
			{
				return deltaSearchNonEdgeObjects;
			}

			/// <returns>
			/// number of objects in the output pack that went through delta
			/// base search and found a suitable base. This is a subset of
			/// <see cref="GetDeltaSearchNonEdgeObjects()">GetDeltaSearchNonEdgeObjects()</see>
			/// .
			/// </returns>
			public virtual int GetDeltasFound()
			{
				return deltasFound;
			}

			/// <returns>
			/// total number of objects output. This total includes the value
			/// of
			/// <see cref="GetTotalDeltas()">GetTotalDeltas()</see>
			/// .
			/// </returns>
			public virtual long GetTotalObjects()
			{
				return totalObjects;
			}

			/// <returns>
			/// total number of deltas output. This may be lower than the
			/// actual number of deltas if a cached pack was reused.
			/// </returns>
			public virtual long GetTotalDeltas()
			{
				return totalDeltas;
			}

			/// <returns>
			/// number of objects whose existing representation was reused in
			/// the output. This count includes
			/// <see cref="GetReusedDeltas()">GetReusedDeltas()</see>
			/// .
			/// </returns>
			public virtual long GetReusedObjects()
			{
				return reusedObjects;
			}

			/// <returns>
			/// number of deltas whose existing representation was reused in
			/// the output, as their base object was also output or was
			/// assumed present for a thin pack. This may be lower than the
			/// actual number of reused deltas if a cached pack was reused.
			/// </returns>
			public virtual long GetReusedDeltas()
			{
				return reusedDeltas;
			}

			/// <returns>
			/// total number of bytes written. This size includes the pack
			/// header, trailer, thin pack, and reused cached pack(s).
			/// </returns>
			public virtual long GetTotalBytes()
			{
				return totalBytes;
			}

			/// <returns>
			/// size of the thin pack in bytes, if a thin pack was generated.
			/// A thin pack is created when the client already has objects
			/// and some deltas are created against those objects, or if a
			/// cached pack is being used and some deltas will reference
			/// objects in the cached pack. This size does not include the
			/// pack header or trailer.
			/// </returns>
			public virtual long GetThinPackBytes()
			{
				return thinPackBytes;
			}

			/// <param name="typeCode">object type code, e.g. OBJ_COMMIT or OBJ_TREE.</param>
			/// <returns>information about this type of object in the pack.</returns>
			public virtual PackWriter.Statistics.ObjectType ByObjectType(int typeCode)
			{
				return objectTypes[typeCode];
			}

			/// <returns>
			/// time in milliseconds spent enumerating the objects that need
			/// to be included in the output. This time includes any restarts
			/// that occur when a cached pack is selected for reuse.
			/// </returns>
			public virtual long GetTimeCounting()
			{
				return timeCounting;
			}

			/// <returns>
			/// time in milliseconds spent matching existing representations
			/// against objects that will be transmitted, or that the client
			/// can be assumed to already have.
			/// </returns>
			public virtual long GetTimeSearchingForReuse()
			{
				return timeSearchingForReuse;
			}

			/// <returns>
			/// time in milliseconds spent finding the sizes of all objects
			/// that will enter the delta compression search window. The
			/// sizes need to be known to better match similar objects
			/// together and improve delta compression ratios.
			/// </returns>
			public virtual long GetTimeSearchingForSizes()
			{
				return timeSearchingForSizes;
			}

			/// <returns>
			/// time in milliseconds spent on delta compression. This is
			/// observed wall-clock time and does not accurately track CPU
			/// time used when multiple threads were used to perform the
			/// delta compression.
			/// </returns>
			public virtual long GetTimeCompressing()
			{
				return timeCompressing;
			}

			/// <returns>
			/// time in milliseconds spent writing the pack output, from
			/// start of header until end of trailer. The transfer speed can
			/// be approximated by dividing
			/// <see cref="GetTotalBytes()">GetTotalBytes()</see>
			/// by this
			/// value.
			/// </returns>
			public virtual long GetTimeWriting()
			{
				return timeWriting;
			}

			/// <returns>
			/// get the average output speed in terms of bytes-per-second.
			/// <code>getTotalBytes() / (getTimeWriting() / 1000.0)</code>
			/// .
			/// </returns>
			public virtual double GetTransferRate()
			{
				return GetTotalBytes() / (GetTimeWriting() / 1000.0);
			}

			/// <returns>formatted message string for display to clients.</returns>
			public virtual string GetMessage()
			{
				return MessageFormat.Format(JGitText.Get().packWriterStatistics, totalObjects, totalDeltas
					, reusedObjects, reusedDeltas);
			}

			public Statistics()
			{
				{
					objectTypes = new PackWriter.Statistics.ObjectType[5];
					objectTypes[Constants.OBJ_COMMIT] = new PackWriter.Statistics.ObjectType();
					objectTypes[Constants.OBJ_TREE] = new PackWriter.Statistics.ObjectType();
					objectTypes[Constants.OBJ_BLOB] = new PackWriter.Statistics.ObjectType();
					objectTypes[Constants.OBJ_TAG] = new PackWriter.Statistics.ObjectType();
				}
			}
			//
			//
		}
	}
}
