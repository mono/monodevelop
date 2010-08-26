/*
 * Copyright (C) 2009, Google Inc.
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
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
	/// Keeps track of a <see cref="PackFile"/> associated <code>.keep</code> file.
    /// </summary>
    public class PackLock
    {
        private readonly FileInfo _keepFile;

        /// <summary>
        /// Create a new lock for a pack file.
        /// </summary>
        /// <param name="packFile">
		/// Location of the <code>pack-*.pack</code> file.
        /// </param>
        public PackLock(FileInfo packFile)
        {
            string n = packFile.Name;
            string p = packFile.DirectoryName + Path.DirectorySeparatorChar + n.Slice(0, n.Length - 5) + ".keep";
            _keepFile = new FileInfo(p);
        }

        /// <summary>
        /// Create the <code>pack-*.keep</code> file, with the given message.
        /// </summary>
        /// <param name="msg">message to store in the file.</param>
        /// <returns>
        /// true if the keep file was successfully written; false otherwise.
        /// </returns>
        /// <exception cref="IOException">
		/// The keep file could not be written.
        /// </exception>
        public bool Lock(string msg)
        {
            if (msg == null) 
				return false;
			
            if (!msg.EndsWith("\n")) msg += "\n";
            using(LockFile lf = new LockFile(_keepFile))
			{
	            if (!lf.Lock()) 
					return false;
	            lf.Write(Constants.encode(msg));
	            return lf.Commit();
			}
        }

        /// <summary>
		/// Remove the <code>.keep</code> file that holds this pack in place.
        /// </summary>
        public void Unlock()
        {
            _keepFile.Delete();
        }
    }
}