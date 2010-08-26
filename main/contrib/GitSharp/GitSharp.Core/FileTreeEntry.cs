/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System.Text;

namespace GitSharp.Core
{
    public class FileTreeEntry : TreeEntry 
    {
        public FileTreeEntry(Tree parent, ObjectId id, byte[] nameUTF8, bool execute)
            : base(parent,id, nameUTF8)
        {
            this.SetExecutable(execute);
        }

        private FileMode _mode;
        public override FileMode Mode
        {
            get { return _mode ; }
        }

        public override void Accept(TreeVisitor tv, int flags)
        {
            if ((MODIFIED_ONLY & flags) == MODIFIED_ONLY && !IsModified)
                return;

            tv.VisitFile(this);
        }

        public bool IsExecutable
        {
            get
            {
                return this.Mode == FileMode.ExecutableFile;
            }
        }

        public void SetExecutable(bool execute)
        {
            _mode = execute ? FileMode.ExecutableFile : FileMode.RegularFile;
        }

        public ObjectLoader OpenReader()
        {
            return this.Repository.OpenBlob(this.Id);
        }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append(ObjectId.ToString(this.Id));
            r.Append(' ');
            r.Append(this.IsExecutable ? 'X' : 'F');
            r.Append(' ');
            r.Append(this.FullName);
            return r.ToString();
        }
    }
}
