/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System.IO;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core
{
    /// <summary>
	/// Reader for a deltified object Stored in a pack file.
    /// </summary>
    public abstract class DeltaPackedObjectLoader : PackedObjectLoader
    {
        private const int ObjCommit = Constants.OBJ_COMMIT;
        private readonly int _deltaSize;

    	protected DeltaPackedObjectLoader(PackFile pr, long dataOffset, long objectOffset, int deltaSz)
            : base(pr, dataOffset, objectOffset)
        {
            Type = -1;
            _deltaSize = deltaSz;
        }

        public override void Materialize(WindowCursor curs)
        {
			if ( curs == null)
			{
				throw new System.ArgumentNullException("curs");
			}
			
            if (CachedBytes != null)
            {
                return;
            }

            if (Type != ObjCommit)
            {
                UnpackedObjectCache.Entry cache = PackFile.readCache(DataOffset);
                if (cache != null)
                {
                    curs.Release();
                    Type = cache.type;
                    Size = cache.data.Length;
                    CachedBytes = cache.data;
                    return;
                }
            }

            try
            {
                PackedObjectLoader baseLoader = GetBaseLoader(curs);
                baseLoader.Materialize(curs);
                CachedBytes = BinaryDelta.Apply(baseLoader.CachedBytes, PackFile.decompress(DataOffset, _deltaSize, curs));
                curs.Release();
                Type = baseLoader.Type;
                Size = CachedBytes.Length;
                if (Type != ObjCommit)
                {
                	PackFile.saveCache(DataOffset, CachedBytes, Type);
                }
            }
            catch (IOException dfe)
            {
				throw new CorruptObjectException("object at " + DataOffset + " in "
                    + PackFile.File.FullName + " has bad zlib stream", dfe);
            }
        }

    	public override long RawSize
    	{
    		get { return _deltaSize; }
    	}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="windowCursor">
		/// Temporary thread storage during data access.
		/// </param>
		/// <returns>
		/// The object loader for the base object
		/// </returns>
        public abstract PackedObjectLoader GetBaseLoader(WindowCursor windowCursor);
    }
}