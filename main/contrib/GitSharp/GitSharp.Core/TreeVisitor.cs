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

namespace GitSharp.Core
{
	/// <summary>
	/// A TreeVisitor is invoked depth first for every node in a tree and is expected
	/// to perform different actions.
	/// </summary>
    public interface TreeVisitor
    {
		///	<summary>
		/// Visit to a tree node before child nodes are visited.
		///	</summary>
		///	<param name="t">Tree</param>
		///	<exception cref="IOException"></exception>
        void StartVisitTree(Tree t);

		///	<summary>
		/// Visit to a tree node. after child nodes have been visited.
		///	</summary>
		///	<param name="t"> Tree </param>
		///	<exception cref="IOException"> </exception>
        void EndVisitTree(Tree t);

		///	<summary>
		/// Visit to a blob.
		///	</summary>
		///	<param name="f">Blob</param>
		///	<exception cref="IOException"></exception>
        void VisitFile(FileTreeEntry f);

		///	<summary>
		/// Visit to a symlink.
		///	</summary>
		///	<param name="s">Symlink entry.</param>
		///	<exception cref="IOException"></exception>
        void VisitSymlink(SymlinkTreeEntry s);

		///	<summary>
		/// Visit to a gitlink.
		///	</summary>
		///	<param name="e">Gitlink entry.</param>
		///	<exception cref="IOException"></exception>
        void VisitGitlink(GitLinkTreeEntry e);
    }
}
