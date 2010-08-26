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

using System.IO;
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core
{
	/// <summary>
	/// A tree visitor for writing a directory tree to the git object database.
	/// Blob data is fetched from the files, not the cached blobs.
	/// </summary>
    public class WriteTree : TreeVisitorWithCurrentDirectory
    {
        private readonly ObjectWriter ow;

		///	<summary>
		/// Construct a WriteTree for a given directory
		///	</summary>
		///	<param name="sourceDirectory"> </param>
		///	<param name="db"> </param>
        public WriteTree(DirectoryInfo sourceDirectory, Repository db)
            : base(sourceDirectory)
        {
            ow = new ObjectWriter(db);
        }

        public override void VisitFile(FileTreeEntry f)
        {
            f.Id = ow.WriteBlob(PathUtil.CombineFilePath(GetCurrentDirectory(), f.Name));
        }

        public override void VisitSymlink(SymlinkTreeEntry s)
        {
            if (s.IsModified)
            {
                throw new SymlinksNotSupportedException("Symlink \""
                        + s.FullName
                        + "\" cannot be written as the link target"
                        + " cannot be read from within Java.");
            }
        }

		public override void EndVisitTree(Tree t)
		{
			base.EndVisitTree(t);
			t.Id = ow.WriteTree(t);
		}

		public override void VisitGitlink(GitLinkTreeEntry e)
		{
			if (e.IsModified)
			{
				throw new GitlinksNotSupportedException(e.FullName);
			}
		}
	}
}
