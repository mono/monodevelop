/*
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

using System;

namespace GitSharp.Core.RevWalk
{

	/**
	 * Produces commits for RevWalk to return to applications.
	 * <para />
	 * Implementations of this basic class provide the real work behind RevWalk.
	 * Conceptually a Generator is an iterator or a queue, it returns commits until
	 * there are no more relevant. Generators may be piped/stacked together to
	 * Create a more complex set of operations.
	 * 
	 * @see PendingGenerator
	 * @see StartGenerator
	 */
	public abstract class Generator
	{
		#region Enums

		[Flags]
		[Serializable]
		public enum GeneratorOutputType
		{
			None = 0,

			/// <summary>
			/// Commits are sorted by commit date and time, descending.
			/// </summary>
			SortCommitTimeDesc = 1 << 0,

			/// <summary>
			/// Output may have <see cref="RevWalk.REWRITE"/> marked on it.
			/// </summary>
			HasRewrite = 1 << 1,

			/// <summary>
			/// Output needs <see cref="RewriteGenerator"/>.
			/// </summary>
			NeedsRewrite = 1 << 2,

			/// <summary>
			/// Topological ordering is enforced (all children before parents).
			/// </summary>
			SortTopo = 1 << 3,

			/// <summary>
			/// Output may have <see cref="RevWalk.UNINTERESTING"/> marked on it.
			/// </summary>
			HasUninteresting = 1 << 4
		}

		#endregion

		/// <summary>
		/// Connect the supplied queue to this generator's own free list (if any).
		/// </summary>
		/// <param name="q">
		/// Another FIFO queue that wants to share our queue's free list.
		/// </param>
		public virtual void shareFreeList(BlockRevQueue q)
		{
			// Do nothing by default.
		}

		/// <summary>
		/// * Obtain flags describing the output behavior of this generator.
		/// </summary>
		public abstract GeneratorOutputType OutputType { get; }

		/// <summary>
		/// Return the next commit to the application, or the next generator.
		/// </summary>
		/// <returns>
		/// Next available commit; null if no more are to be returned.
		/// </returns>
		public abstract RevCommit next();
	}
}