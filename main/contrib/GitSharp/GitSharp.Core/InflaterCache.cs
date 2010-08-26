/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Runtime.CompilerServices;

namespace GitSharp.Core
{

    public class InflaterCache
    {

        private static int SZ = 4;

        private static Inflater[] inflaterCache;

        private static int openInflaterCount;
		
		private static Object locker = new Object();

        private InflaterCache()
        {
            inflaterCache = new Inflater[SZ];
        }

        public static InflaterCache Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new InflaterCache();
                return m_instance;
            }
        }
        private static InflaterCache m_instance;

        /// <summary>
		/// Obtain an Inflater for decompression.
		/// <para />
		/// Inflaters obtained through this cache should be returned (if possible) by
		/// <see cref="release(Inflater)"/> to avoid garbage collection and reallocation.
        /// </summary>
		/// <returns>An available inflater. Never null.</returns>
        public Inflater get()
        {
            Inflater r = getImpl();
            return r ?? new Inflater(false);
        }

        private Inflater getImpl()
        {
			lock(locker)
			{
	            if (openInflaterCount > 0)
	            {
	                Inflater r = inflaterCache[--openInflaterCount];
	                inflaterCache[openInflaterCount] = null;
	                return r;
	            }
	            return null;
			}
        }

        /**
         * Release an inflater previously obtained from this cache.
         * 
         * @param i
         *            the inflater to return. May be null, in which case this method
         *            does nothing.
         */
        public void release(Inflater i)
        {
            if (i != null)
            {
                i.Reset();
                releaseImpl(i);
             }
        }

        private static bool releaseImpl(Inflater i)
        {
			lock(locker)
			{
	            if (openInflaterCount < SZ)
	            {
	                inflaterCache[openInflaterCount++] = i;
	                return false;
	            }
	            return true;
			}
        }

    }
}
