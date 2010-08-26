/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
	/// Visitor interface for traversing the index and two trees in parallel.
	/// <para />
	/// When merging we deal with up to two tree nodes and a base node. Then
	/// we figure out what to do.
	///<para />
	/// A File argument is supplied to allow us to check for modifications in
	/// a work tree or update the file.
	///</summary>
    public interface IndexTreeVisitor
    {
		///	<summary>
		/// Visit a blob, and corresponding tree and index entries.
		///	</summary>
		///	<param name="treeEntry"></param>
		///	<param name="indexEntry"></param>
		///	<param name="file"></param>
		///	<exception cref="IOException"></exception>
        void VisitEntry(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file);

		///	<summary>
		/// Visit a blob, and corresponding tree nodes and associated index entry.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <param name="auxEntry"></param>
		/// <param name="indexEntry"></param>
		/// <param name="file"></param>
		/// <exception cref="IOException"></exception>
        void VisitEntry(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry indexEntry, FileInfo file);

		///	<summary>
		/// Invoked after handling all child nodes of a tree, during a three way merge
		///	</summary>
		///	<param name="tree"></param>
		///	<param name="auxTree"></param>
		///	<param name="curDir"></param>
		///	<exception cref="IOException"></exception>
        void FinishVisitTree(Tree tree, Tree auxTree, string curDir);

		///	<summary>
		/// Invoked after handling all child nodes of a tree, during two way merge.
		///	</summary>
		///	<param name="tree"></param>
		///	<param name="i"></param>
		///	<param name="curDir"></param>
		///	<exception cref="IOException"></exception>
        void FinishVisitTree(Tree tree, int i, string curDir);
    }
}
