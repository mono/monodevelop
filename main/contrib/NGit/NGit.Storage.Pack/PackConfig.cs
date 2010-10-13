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

using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Configuration used by a
	/// <see cref="PackWriter">PackWriter</see>
	/// when constructing the stream.
	/// A configuration may be modified once created, but should not be modified
	/// while it is being used by a PackWriter. If a configuration is not modified it
	/// is safe to share the same configuration instance between multiple concurrent
	/// threads executing different PackWriters.
	/// </summary>
	public class PackConfig
	{
		/// <summary>
		/// Default value of deltas reuse option:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetReuseDeltas(bool)">SetReuseDeltas(bool)</seealso>
		public const bool DEFAULT_REUSE_DELTAS = true;

		/// <summary>
		/// Default value of objects reuse option:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetReuseObjects(bool)">SetReuseObjects(bool)</seealso>
		public const bool DEFAULT_REUSE_OBJECTS = true;

		/// <summary>
		/// Default value of delta compress option:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetDeltaCompress(bool)">SetDeltaCompress(bool)</seealso>
		public const bool DEFAULT_DELTA_COMPRESS = true;

		/// <summary>
		/// Default value of delta base as offset option:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetDeltaBaseAsOffset(bool)">SetDeltaBaseAsOffset(bool)</seealso>
		public const bool DEFAULT_DELTA_BASE_AS_OFFSET = false;

		/// <summary>
		/// Default value of maximum delta chain depth:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetMaxDeltaDepth(int)">SetMaxDeltaDepth(int)</seealso>
		public const int DEFAULT_MAX_DELTA_DEPTH = 50;

		/// <summary>
		/// Default window size during packing:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetDeltaSearchWindowSize(int)">SetDeltaSearchWindowSize(int)</seealso>
		public const int DEFAULT_DELTA_SEARCH_WINDOW_SIZE = 10;

		/// <summary>
		/// Default big file threshold:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetBigFileThreshold(int)">SetBigFileThreshold(int)</seealso>
		public const int DEFAULT_BIG_FILE_THRESHOLD = 50 * 1024 * 1024;

		/// <summary>
		/// Default delta cache size:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetDeltaCacheSize(long)">SetDeltaCacheSize(long)</seealso>
		public const long DEFAULT_DELTA_CACHE_SIZE = 50 * 1024 * 1024;

		/// <summary>
		/// Default delta cache limit:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetDeltaCacheLimit(int)">SetDeltaCacheLimit(int)</seealso>
		public const int DEFAULT_DELTA_CACHE_LIMIT = 100;

		/// <summary>
		/// Default index version:
		/// <value></value>
		/// </summary>
		/// <seealso cref="SetIndexVersion(int)">SetIndexVersion(int)</seealso>
		public const int DEFAULT_INDEX_VERSION = 2;

		private int compressionLevel = Deflater.DEFAULT_COMPRESSION;

		private bool reuseDeltas = DEFAULT_REUSE_DELTAS;

		private bool reuseObjects = DEFAULT_REUSE_OBJECTS;

		private bool deltaBaseAsOffset = DEFAULT_DELTA_BASE_AS_OFFSET;

		private bool deltaCompress = DEFAULT_DELTA_COMPRESS;

		private int maxDeltaDepth = DEFAULT_MAX_DELTA_DEPTH;

		private int deltaSearchWindowSize = DEFAULT_DELTA_SEARCH_WINDOW_SIZE;

		private long deltaSearchMemoryLimit;

		private long deltaCacheSize = DEFAULT_DELTA_CACHE_SIZE;

		private int deltaCacheLimit = DEFAULT_DELTA_CACHE_LIMIT;

		private int bigFileThreshold = DEFAULT_BIG_FILE_THRESHOLD;

		private int threads;

		private Executor executor;

		private int indexVersion = DEFAULT_INDEX_VERSION;

		/// <summary>Create a default configuration.</summary>
		/// <remarks>Create a default configuration.</remarks>
		public PackConfig()
		{
		}

		/// <summary>Create a configuration honoring the repository's settings.</summary>
		/// <remarks>Create a configuration honoring the repository's settings.</remarks>
		/// <param name="db">
		/// the repository to read settings from. The repository is not
		/// retained by the new configuration, instead its settings are
		/// copied during the constructor.
		/// </param>
		public PackConfig(Repository db)
		{
			// Fields are initialized to defaults.
			FromConfig(db.GetConfig());
		}

		/// <summary>
		/// Create a configuration honoring settings in a
		/// <see cref="NGit.Config">NGit.Config</see>
		/// .
		/// </summary>
		/// <param name="cfg">
		/// the source to read settings from. The source is not retained
		/// by the new configuration, instead its settings are copied
		/// during the constructor.
		/// </param>
		public PackConfig(Config cfg)
		{
			FromConfig(cfg);
		}

		/// <summary>Check whether to reuse deltas existing in repository.</summary>
		/// <remarks>
		/// Check whether to reuse deltas existing in repository.
		/// Default setting:
		/// <value>#DEFAULT_REUSE_DELTAS</value>
		/// </remarks>
		/// <returns>true if object is configured to reuse deltas; false otherwise.</returns>
		public virtual bool IsReuseDeltas()
		{
			return reuseDeltas;
		}

		/// <summary>Set reuse deltas configuration option for the writer.</summary>
		/// <remarks>
		/// Set reuse deltas configuration option for the writer.
		/// When enabled, writer will search for delta representation of object in
		/// repository and use it if possible. Normally, only deltas with base to
		/// another object existing in set of objects to pack will be used. The
		/// exception however is thin-packs where the base object may exist on the
		/// other side.
		/// When raw delta data is directly copied from a pack file, its checksum is
		/// computed to verify the data is not corrupt.
		/// Default setting:
		/// <value>#DEFAULT_REUSE_DELTAS</value>
		/// </remarks>
		/// <param name="reuseDeltas">boolean indicating whether or not try to reuse deltas.</param>
		public virtual void SetReuseDeltas(bool reuseDeltas)
		{
			this.reuseDeltas = reuseDeltas;
		}

		/// <summary>Checks whether to reuse existing objects representation in repository.</summary>
		/// <remarks>
		/// Checks whether to reuse existing objects representation in repository.
		/// Default setting:
		/// <value>#DEFAULT_REUSE_OBJECTS</value>
		/// </remarks>
		/// <returns>
		/// true if writer is configured to reuse objects representation from
		/// pack; false otherwise.
		/// </returns>
		public virtual bool IsReuseObjects()
		{
			return reuseObjects;
		}

		/// <summary>Set reuse objects configuration option for the writer.</summary>
		/// <remarks>
		/// Set reuse objects configuration option for the writer.
		/// If enabled, writer searches for compressed representation in a pack file.
		/// If possible, compressed data is directly copied from such a pack file.
		/// Data checksum is verified.
		/// Default setting:
		/// <value>#DEFAULT_REUSE_OBJECTS</value>
		/// </remarks>
		/// <param name="reuseObjects">
		/// boolean indicating whether or not writer should reuse existing
		/// objects representation.
		/// </param>
		public virtual void SetReuseObjects(bool reuseObjects)
		{
			this.reuseObjects = reuseObjects;
		}

		/// <summary>True if writer can use offsets to point to a delta base.</summary>
		/// <remarks>
		/// True if writer can use offsets to point to a delta base.
		/// If true the writer may choose to use an offset to point to a delta base
		/// in the same pack, this is a newer style of reference that saves space.
		/// False if the writer has to use the older (and more compatible style) of
		/// storing the full ObjectId of the delta base.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_BASE_AS_OFFSET</value>
		/// </remarks>
		/// <returns>
		/// true if delta base is stored as an offset; false if it is stored
		/// as an ObjectId.
		/// </returns>
		public virtual bool IsDeltaBaseAsOffset()
		{
			return deltaBaseAsOffset;
		}

		/// <summary>Set writer delta base format.</summary>
		/// <remarks>
		/// Set writer delta base format.
		/// Delta base can be written as an offset in a pack file (new approach
		/// reducing file size) or as an object id (legacy approach, compatible with
		/// old readers).
		/// Default setting:
		/// <value>#DEFAULT_DELTA_BASE_AS_OFFSET</value>
		/// </remarks>
		/// <param name="deltaBaseAsOffset">
		/// boolean indicating whether delta base can be stored as an
		/// offset.
		/// </param>
		public virtual void SetDeltaBaseAsOffset(bool deltaBaseAsOffset)
		{
			this.deltaBaseAsOffset = deltaBaseAsOffset;
		}

		/// <summary>Check whether the writer will create new deltas on the fly.</summary>
		/// <remarks>
		/// Check whether the writer will create new deltas on the fly.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_COMPRESS</value>
		/// </remarks>
		/// <returns>
		/// true if the writer will create a new delta when either
		/// <see cref="IsReuseDeltas()">IsReuseDeltas()</see>
		/// is false, or no suitable delta is
		/// available for reuse.
		/// </returns>
		public virtual bool IsDeltaCompress()
		{
			return deltaCompress;
		}

		/// <summary>Set whether or not the writer will create new deltas on the fly.</summary>
		/// <remarks>
		/// Set whether or not the writer will create new deltas on the fly.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_COMPRESS</value>
		/// </remarks>
		/// <param name="deltaCompress">
		/// true to create deltas when
		/// <see cref="IsReuseDeltas()">IsReuseDeltas()</see>
		/// is false,
		/// or when a suitable delta isn't available for reuse. Set to
		/// false to write whole objects instead.
		/// </param>
		public virtual void SetDeltaCompress(bool deltaCompress)
		{
			this.deltaCompress = deltaCompress;
		}

		/// <summary>Get maximum depth of delta chain set up for the writer.</summary>
		/// <remarks>
		/// Get maximum depth of delta chain set up for the writer.
		/// Generated chains are not longer than this value.
		/// Default setting:
		/// <value>#DEFAULT_MAX_DELTA_DEPTH</value>
		/// </remarks>
		/// <returns>maximum delta chain depth.</returns>
		public virtual int GetMaxDeltaDepth()
		{
			return maxDeltaDepth;
		}

		/// <summary>Set up maximum depth of delta chain for the writer.</summary>
		/// <remarks>
		/// Set up maximum depth of delta chain for the writer.
		/// Generated chains are not longer than this value. Too low value causes low
		/// compression level, while too big makes unpacking (reading) longer.
		/// Default setting:
		/// <value>#DEFAULT_MAX_DELTA_DEPTH</value>
		/// </remarks>
		/// <param name="maxDeltaDepth">maximum delta chain depth.</param>
		public virtual void SetMaxDeltaDepth(int maxDeltaDepth)
		{
			this.maxDeltaDepth = maxDeltaDepth;
		}

		/// <summary>Get the number of objects to try when looking for a delta base.</summary>
		/// <remarks>
		/// Get the number of objects to try when looking for a delta base.
		/// This limit is per thread, if 4 threads are used the actual memory used
		/// will be 4 times this value.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_SEARCH_WINDOW_SIZE</value>
		/// </remarks>
		/// <returns>the object count to be searched.</returns>
		public virtual int GetDeltaSearchWindowSize()
		{
			return deltaSearchWindowSize;
		}

		/// <summary>Set the number of objects considered when searching for a delta base.</summary>
		/// <remarks>
		/// Set the number of objects considered when searching for a delta base.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_SEARCH_WINDOW_SIZE</value>
		/// </remarks>
		/// <param name="objectCount">number of objects to search at once. Must be at least 2.
		/// 	</param>
		public virtual void SetDeltaSearchWindowSize(int objectCount)
		{
			if (objectCount <= 2)
			{
				SetDeltaCompress(false);
			}
			else
			{
				deltaSearchWindowSize = objectCount;
			}
		}

		/// <summary>Get maximum number of bytes to put into the delta search window.</summary>
		/// <remarks>
		/// Get maximum number of bytes to put into the delta search window.
		/// Default setting is 0, for an unlimited amount of memory usage. Actual
		/// memory used is the lower limit of either this setting, or the sum of
		/// space used by at most
		/// <see cref="GetDeltaSearchWindowSize()">GetDeltaSearchWindowSize()</see>
		/// objects.
		/// This limit is per thread, if 4 threads are used the actual memory limit
		/// will be 4 times this value.
		/// </remarks>
		/// <returns>the memory limit.</returns>
		public virtual long GetDeltaSearchMemoryLimit()
		{
			return deltaSearchMemoryLimit;
		}

		/// <summary>Set the maximum number of bytes to put into the delta search window.</summary>
		/// <remarks>
		/// Set the maximum number of bytes to put into the delta search window.
		/// Default setting is 0, for an unlimited amount of memory usage. If the
		/// memory limit is reached before
		/// <see cref="GetDeltaSearchWindowSize()">GetDeltaSearchWindowSize()</see>
		/// the
		/// window size is temporarily lowered.
		/// </remarks>
		/// <param name="memoryLimit">Maximum number of bytes to load at once, 0 for unlimited.
		/// 	</param>
		public virtual void SetDeltaSearchMemoryLimit(long memoryLimit)
		{
			deltaSearchMemoryLimit = memoryLimit;
		}

		/// <summary>Get the size of the in-memory delta cache.</summary>
		/// <remarks>
		/// Get the size of the in-memory delta cache.
		/// This limit is for the entire writer, even if multiple threads are used.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_CACHE_SIZE</value>
		/// </remarks>
		/// <returns>
		/// maximum number of bytes worth of delta data to cache in memory.
		/// If 0 the cache is infinite in size (up to the JVM heap limit
		/// anyway). A very tiny size such as 1 indicates the cache is
		/// effectively disabled.
		/// </returns>
		public virtual long GetDeltaCacheSize()
		{
			return deltaCacheSize;
		}

		/// <summary>Set the maximum number of bytes of delta data to cache.</summary>
		/// <remarks>
		/// Set the maximum number of bytes of delta data to cache.
		/// During delta search, up to this many bytes worth of small or hard to
		/// compute deltas will be stored in memory. This cache speeds up writing by
		/// allowing the cached entry to simply be dumped to the output stream.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_CACHE_SIZE</value>
		/// </remarks>
		/// <param name="size">
		/// number of bytes to cache. Set to 0 to enable an infinite
		/// cache, set to 1 (an impossible size for any delta) to disable
		/// the cache.
		/// </param>
		public virtual void SetDeltaCacheSize(long size)
		{
			deltaCacheSize = size;
		}

		/// <summary>Maximum size in bytes of a delta to cache.</summary>
		/// <remarks>
		/// Maximum size in bytes of a delta to cache.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_CACHE_LIMIT</value>
		/// </remarks>
		/// <returns>maximum size (in bytes) of a delta that should be cached.</returns>
		public virtual int GetDeltaCacheLimit()
		{
			return deltaCacheLimit;
		}

		/// <summary>Set the maximum size of a delta that should be cached.</summary>
		/// <remarks>
		/// Set the maximum size of a delta that should be cached.
		/// During delta search, any delta smaller than this size will be cached, up
		/// to the
		/// <see cref="GetDeltaCacheSize()">GetDeltaCacheSize()</see>
		/// maximum limit. This speeds up writing
		/// by allowing these cached deltas to be output as-is.
		/// Default setting:
		/// <value>#DEFAULT_DELTA_CACHE_LIMIT</value>
		/// </remarks>
		/// <param name="size">maximum size (in bytes) of a delta to be cached.</param>
		public virtual void SetDeltaCacheLimit(int size)
		{
			deltaCacheLimit = size;
		}

		/// <summary>Get the maximum file size that will be delta compressed.</summary>
		/// <remarks>
		/// Get the maximum file size that will be delta compressed.
		/// Files bigger than this setting will not be delta compressed, as they are
		/// more than likely already highly compressed binary data files that do not
		/// delta compress well, such as MPEG videos.
		/// Default setting:
		/// <value>#DEFAULT_BIG_FILE_THRESHOLD</value>
		/// </remarks>
		/// <returns>the configured big file threshold.</returns>
		public virtual int GetBigFileThreshold()
		{
			return bigFileThreshold;
		}

		/// <summary>Set the maximum file size that should be considered for deltas.</summary>
		/// <remarks>
		/// Set the maximum file size that should be considered for deltas.
		/// Default setting:
		/// <value>#DEFAULT_BIG_FILE_THRESHOLD</value>
		/// </remarks>
		/// <param name="bigFileThreshold">the limit, in bytes.</param>
		public virtual void SetBigFileThreshold(int bigFileThreshold)
		{
			this.bigFileThreshold = bigFileThreshold;
		}

		/// <summary>Get the compression level applied to objects in the pack.</summary>
		/// <remarks>
		/// Get the compression level applied to objects in the pack.
		/// Default setting:
		/// <value>java.util.zip.Deflater#DEFAULT_COMPRESSION</value>
		/// </remarks>
		/// <returns>
		/// current compression level, see
		/// <see cref="ICSharpCode.SharpZipLib.Zip.Compression.Deflater">ICSharpCode.SharpZipLib.Zip.Compression.Deflater
		/// 	</see>
		/// .
		/// </returns>
		public virtual int GetCompressionLevel()
		{
			return compressionLevel;
		}

		/// <summary>Set the compression level applied to objects in the pack.</summary>
		/// <remarks>
		/// Set the compression level applied to objects in the pack.
		/// Default setting:
		/// <value>java.util.zip.Deflater#DEFAULT_COMPRESSION</value>
		/// </remarks>
		/// <param name="level">
		/// compression level, must be a valid level recognized by the
		/// <see cref="ICSharpCode.SharpZipLib.Zip.Compression.Deflater">ICSharpCode.SharpZipLib.Zip.Compression.Deflater
		/// 	</see>
		/// class.
		/// </param>
		public virtual void SetCompressionLevel(int level)
		{
			compressionLevel = level;
		}

		/// <summary>Get the number of threads used during delta compression.</summary>
		/// <remarks>
		/// Get the number of threads used during delta compression.
		/// Default setting: 0 (auto-detect processors)
		/// </remarks>
		/// <returns>
		/// number of threads used for delta compression. 0 will auto-detect
		/// the threads to the number of available processors.
		/// </returns>
		public virtual int GetThreads()
		{
			return threads;
		}

		/// <summary>Set the number of threads to use for delta compression.</summary>
		/// <remarks>
		/// Set the number of threads to use for delta compression.
		/// During delta compression, if there are enough objects to be considered
		/// the writer will start up concurrent threads and allow them to compress
		/// different sections of the repository concurrently.
		/// An application thread pool can be set by
		/// <see cref="SetExecutor(Sharpen.Executor)">SetExecutor(Sharpen.Executor)</see>
		/// .
		/// If not set a temporary pool will be created by the writer, and torn down
		/// automatically when compression is over.
		/// Default setting: 0 (auto-detect processors)
		/// </remarks>
		/// <param name="threads">
		/// number of threads to use. If &lt;= 0 the number of available
		/// processors for this JVM is used.
		/// </param>
		public virtual void SetThreads(int threads)
		{
			this.threads = threads;
		}

		/// <returns>the preferred thread pool to execute delta search on.</returns>
		public virtual Executor GetExecutor()
		{
			return executor;
		}

		/// <summary>Set the executor to use when using threads.</summary>
		/// <remarks>
		/// Set the executor to use when using threads.
		/// During delta compression if the executor is non-null jobs will be queued
		/// up on it to perform delta compression in parallel. Aside from setting the
		/// executor, the caller must set
		/// <see cref="SetThreads(int)">SetThreads(int)</see>
		/// to enable threaded
		/// delta search.
		/// </remarks>
		/// <param name="executor">
		/// executor to use for threads. Set to null to create a temporary
		/// executor just for the writer.
		/// </param>
		public virtual void SetExecutor(Executor executor)
		{
			this.executor = executor;
		}

		/// <summary>Get the pack index file format version this instance creates.</summary>
		/// <remarks>
		/// Get the pack index file format version this instance creates.
		/// Default setting:
		/// <value>#DEFAULT_INDEX_VERSION</value>
		/// </remarks>
		/// <returns>
		/// the index version, the special version 0 designates the oldest
		/// (most compatible) format available for the objects.
		/// </returns>
		/// <seealso cref="NGit.Storage.File.PackIndexWriter">NGit.Storage.File.PackIndexWriter
		/// 	</seealso>
		public virtual int GetIndexVersion()
		{
			return indexVersion;
		}

		/// <summary>Set the pack index file format version this instance will create.</summary>
		/// <remarks>
		/// Set the pack index file format version this instance will create.
		/// Default setting:
		/// <value>#DEFAULT_INDEX_VERSION</value>
		/// </remarks>
		/// <param name="version">
		/// the version to write. The special version 0 designates the
		/// oldest (most compatible) format available for the objects.
		/// </param>
		/// <seealso cref="NGit.Storage.File.PackIndexWriter">NGit.Storage.File.PackIndexWriter
		/// 	</seealso>
		public virtual void SetIndexVersion(int version)
		{
			indexVersion = version;
		}

		/// <summary>Update properties by setting fields from the configuration.</summary>
		/// <remarks>
		/// Update properties by setting fields from the configuration.
		/// If a property's corresponding variable is not defined in the supplied
		/// configuration, then it is left unmodified.
		/// </remarks>
		/// <param name="rc">configuration to read properties from.</param>
		public virtual void FromConfig(Config rc)
		{
			SetMaxDeltaDepth(rc.GetInt("pack", "depth", GetMaxDeltaDepth()));
			SetDeltaSearchWindowSize(rc.GetInt("pack", "window", GetDeltaSearchWindowSize()));
			SetDeltaSearchMemoryLimit(rc.GetLong("pack", "windowmemory", GetDeltaSearchMemoryLimit
				()));
			SetDeltaCacheSize(rc.GetLong("pack", "deltacachesize", GetDeltaCacheSize()));
			SetDeltaCacheLimit(rc.GetInt("pack", "deltacachelimit", GetDeltaCacheLimit()));
			SetCompressionLevel(rc.GetInt("pack", "compression", rc.GetInt("core", "compression"
				, GetCompressionLevel())));
			SetIndexVersion(rc.GetInt("pack", "indexversion", GetIndexVersion()));
			SetBigFileThreshold(rc.GetInt("core", "bigfilethreshold", GetBigFileThreshold()));
			SetThreads(rc.GetInt("pack", "threads", GetThreads()));
			// These variables aren't standardized
			//
			SetReuseDeltas(rc.GetBoolean("pack", "reusedeltas", IsReuseDeltas()));
			SetReuseObjects(rc.GetBoolean("pack", "reuseobjects", IsReuseObjects()));
			SetDeltaCompress(rc.GetBoolean("pack", "deltacompression", IsDeltaCompress()));
		}
	}
}
