/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Charles O'Farrell <charleso@charleso.org>
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
using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// Writes out refs to the <see cref="Constants.INFO_REFS"/> and
    /// <see cref="Constants.PACKED_REFS"/> files.
    /// 
    /// This class is abstract as the writing of the files must be handled by the
    /// caller. This is because it is used by transport classes as well.
    /// </summary>
    public abstract class RefWriter
    {

        private readonly IEnumerable<Ref> refs;

        /// <param name="refs">
        /// the complete set of references. This should have been computed
        /// by applying updates to the advertised refs already discovered.
        /// </param>
        protected RefWriter(IEnumerable<Ref> refs)
        {
            this.refs = RefComparator.Sort(refs);
        }

        /// <param name="refs">
        /// the complete set of references. This should have been computed
        /// by applying updates to the advertised refs already discovered.
        /// </param>
        protected RefWriter(IDictionary<String, Ref> refs)
        {
            if (refs is RefMap)
                this.refs = refs.Values;
            else
                this.refs = RefComparator.Sort(refs.Values);
        }

        protected RefWriter(RefList<Ref> list)
        {
            refs = list.asList();
        }

        /// <summary>
        /// Rebuild the <see cref="Constants.INFO_REFS"/>.
        /// <para />
        /// This method rebuilds the contents of the <see cref="Constants.INFO_REFS"/> file
        /// to match the passed list of references.
        /// </summary>
        public void writeInfoRefs()
        {
            var w = new StringBuilder();
            var tmp = new char[Constants.OBJECT_ID_STRING_LENGTH];
            foreach (Ref r in refs)
            {
                if (Constants.HEAD.Equals(r.Name))
                {
                    // Historically HEAD has never been published through
                    // the INFO_REFS file. This is a mistake, but its the
                    // way things are.
                    //
                    continue;
                }

                r.ObjectId.CopyTo(tmp, w);
                w.Append('\t');
                w.Append(r.Name);
                w.Append('\n');

                if (r.PeeledObjectId != null)
                {
                    r.PeeledObjectId.CopyTo(tmp, w);
                    w.Append('\t');
                    w.Append(r.Name);
                    w.Append("^{}\n");
                }
            }
            writeFile(Constants.INFO_REFS, Constants.encode(w.ToString()));
        }

        /// <summary>
        /// Rebuild the <see cref="Constants.PACKED_REFS"/> file.
        /// <para />
        /// This method rebuilds the contents of the <see cref="Constants.PACKED_REFS"/>
        /// file to match the passed list of references, including only those refs
        /// that have a storage type of <see cref="Storage.Packed"/>.
        /// </summary>
        public void writePackedRefs()
        {
            bool peeled = false;

            foreach (Ref r in refs)
            {
                if (r.StorageFormat.IsPacked && r.IsPeeled)
                {
                    peeled = true;
                    break;
                }
            }

            var w = new StringBuilder();
            if (peeled)
            {
                w.Append(RefDirectory.PACKED_REFS_HEADER);
                if (peeled)
                    w.Append(RefDirectory.PACKED_REFS_PEELED);
                w.Append('\n');
            }

            var tmp = new char[Constants.OBJECT_ID_STRING_LENGTH];
            foreach (Ref r in refs)
            {
                if (r.StorageFormat != Storage.Packed)
                    continue;

                r.ObjectId.CopyTo(tmp, w);
                w.Append(' ');
                w.Append(r.Name);
                w.Append('\n');

                if (r.PeeledObjectId != null)
                {
                    w.Append('^');
                    r.PeeledObjectId.CopyTo(tmp, w);
                    w.Append('\n');
                }
            }
            writeFile(Constants.PACKED_REFS, Constants.encode(w.ToString()));
        }

        /// <summary>
        /// Handles actual writing of ref files to the git repository, which may
        /// differ slightly depending on the destination and transport.
        /// </summary>
        /// <param name="file">path to ref file.</param>
        /// <param name="content">byte content of file to be written.</param>
        protected abstract void writeFile(String file, byte[] content);
    }
}
