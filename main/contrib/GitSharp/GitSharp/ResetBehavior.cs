/*
 * Copyright (C) 2009, Nulltoken <emeric.fermas@gmail.com>
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

namespace GitSharp
{

	/// <summary>
	/// Reset policies for Branch.Reset (see Branch)
	/// </summary>
	public enum ResetBehavior
	{

		/// <summary>
		/// Resets the index but not the working directory (i.e., the changed files are preserved but not marked for commit).
		/// </summary>
		Mixed,

		/// <summary>
		/// Does not touch the index nor the working directory at all, but requires them to be in a good order. This leaves all your changed files "Changes to be committed", as git-status would put it.
		/// </summary>
		Soft,

		/// <summary>
		/// Matches the working directory and index to that of the commit being reset to. Any changes to tracked files in the working directory since are lost.
		/// </summary>
		Hard,

		/// <summary>
		/// Resets the index to match the tree recorded by the named commit, and updates the files that are different between the named commit and the current commit in the working directory.
		/// </summary>
		Merge
	}
}