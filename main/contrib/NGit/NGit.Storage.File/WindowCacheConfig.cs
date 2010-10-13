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
using NGit;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Configuration parameters for
	/// <see cref="WindowCache">WindowCache</see>
	/// .
	/// </summary>
	public class WindowCacheConfig
	{
		/// <summary>1024 (number of bytes in one kibibyte/kilobyte)</summary>
		public const int KB = 1024;

		/// <summary>
		/// 1024
		/// <see cref="KB">KB</see>
		/// (number of bytes in one mebibyte/megabyte)
		/// </summary>
		public const int MB = 1024 * KB;

		private int packedGitOpenFiles;

		private long packedGitLimit;

		private int packedGitWindowSize;

		private bool packedGitMMAP;

		private int deltaBaseCacheLimit;

		private int streamFileThreshold;

		/// <summary>Create a default configuration.</summary>
		/// <remarks>Create a default configuration.</remarks>
		public WindowCacheConfig()
		{
			packedGitOpenFiles = 128;
			packedGitLimit = 10 * MB;
			packedGitWindowSize = 8 * KB;
			packedGitMMAP = false;
			deltaBaseCacheLimit = 10 * MB;
			streamFileThreshold = PackConfig.DEFAULT_BIG_FILE_THRESHOLD;
		}

		/// <returns>
		/// maximum number of streams to open at a time. Open packs count
		/// against the process limits. <b>Default is 128.</b>
		/// </returns>
		public virtual int GetPackedGitOpenFiles()
		{
			return packedGitOpenFiles;
		}

		/// <param name="fdLimit">
		/// maximum number of streams to open at a time. Open packs count
		/// against the process limits
		/// </param>
		public virtual void SetPackedGitOpenFiles(int fdLimit)
		{
			packedGitOpenFiles = fdLimit;
		}

		/// <returns>
		/// maximum number bytes of heap memory to dedicate to caching pack
		/// file data. <b>Default is 10 MB.</b>
		/// </returns>
		public virtual long GetPackedGitLimit()
		{
			return packedGitLimit;
		}

		/// <param name="newLimit">
		/// maximum number bytes of heap memory to dedicate to caching
		/// pack file data.
		/// </param>
		public virtual void SetPackedGitLimit(long newLimit)
		{
			packedGitLimit = newLimit;
		}

		/// <returns>
		/// size in bytes of a single window mapped or read in from the pack
		/// file. <b>Default is 8 KB.</b>
		/// </returns>
		public virtual int GetPackedGitWindowSize()
		{
			return packedGitWindowSize;
		}

		/// <param name="newSize">size in bytes of a single window read in from the pack file.
		/// 	</param>
		public virtual void SetPackedGitWindowSize(int newSize)
		{
			packedGitWindowSize = newSize;
		}

		/// <returns>
		/// true enables use of Java NIO virtual memory mapping for windows;
		/// false reads entire window into a byte[] with standard read calls.
		/// <b>Default false.</b>
		/// </returns>
		public virtual bool IsPackedGitMMAP()
		{
			return packedGitMMAP;
		}

		/// <param name="usemmap">
		/// true enables use of Java NIO virtual memory mapping for
		/// windows; false reads entire window into a byte[] with standard
		/// read calls.
		/// </param>
		public virtual void SetPackedGitMMAP(bool usemmap)
		{
			packedGitMMAP = usemmap;
		}

		/// <returns>
		/// maximum number of bytes to cache in
		/// <see cref="DeltaBaseCache">DeltaBaseCache</see>
		/// for inflated, recently accessed objects, without delta chains.
		/// <b>Default 10 MB.</b>
		/// </returns>
		public virtual int GetDeltaBaseCacheLimit()
		{
			return deltaBaseCacheLimit;
		}

		/// <param name="newLimit">
		/// maximum number of bytes to cache in
		/// <see cref="DeltaBaseCache">DeltaBaseCache</see>
		/// for inflated, recently accessed
		/// objects, without delta chains.
		/// </param>
		public virtual void SetDeltaBaseCacheLimit(int newLimit)
		{
			deltaBaseCacheLimit = newLimit;
		}

		/// <returns>the size threshold beyond which objects must be streamed.</returns>
		public virtual int GetStreamFileThreshold()
		{
			return streamFileThreshold;
		}

		/// <param name="newLimit">
		/// new byte limit for objects that must be streamed. Objects
		/// smaller than this size can be obtained as a contiguous byte
		/// array, while objects bigger than this size require using an
		/// <see cref="NGit.ObjectStream">NGit.ObjectStream</see>
		/// .
		/// </param>
		public virtual void SetStreamFileThreshold(int newLimit)
		{
			streamFileThreshold = newLimit;
		}

		/// <summary>Update properties by setting fields from the configuration.</summary>
		/// <remarks>
		/// Update properties by setting fields from the configuration.
		/// <p>
		/// If a property is not defined in the configuration, then it is left
		/// unmodified.
		/// </remarks>
		/// <param name="rc">configuration to read properties from.</param>
		public virtual void FromConfig(Config rc)
		{
			SetPackedGitOpenFiles(rc.GetInt("core", null, "packedgitopenfiles", GetPackedGitOpenFiles
				()));
			SetPackedGitLimit(rc.GetLong("core", null, "packedgitlimit", GetPackedGitLimit())
				);
			SetPackedGitWindowSize(rc.GetInt("core", null, "packedgitwindowsize", GetPackedGitWindowSize
				()));
			SetPackedGitMMAP(rc.GetBoolean("core", null, "packedgitmmap", IsPackedGitMMAP()));
			SetDeltaBaseCacheLimit(rc.GetInt("core", null, "deltabasecachelimit", GetDeltaBaseCacheLimit
				()));
			long maxMem = Runtime.GetRuntime().MaxMemory();
			long sft = rc.GetLong("core", null, "streamfilethreshold", GetStreamFileThreshold
				());
			sft = Math.Min(sft, maxMem / 4);
			// don't use more than 1/4 of the heap
			sft = Math.Min(sft, int.MaxValue);
			// cannot exceed array length
			SetStreamFileThreshold((int)sft);
		}
	}
}
