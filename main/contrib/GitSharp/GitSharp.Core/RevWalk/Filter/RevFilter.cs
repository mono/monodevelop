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
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Selects interesting revisions during walking.
	/// <para />
	/// This is an abstract interface. Applications may implement a subclass, or use
	/// one of the predefined implementations already available within this package.
	/// Filters may be chained together using <see cref="AndRevFilter"/> and
	/// <see cref="OrRevFilter"/> to create complex boolean expressions.
	/// <para />
	/// Applications should install the filter on a RevWalk by
	/// <seealso cref="RevWalk.setRevFilter(RevFilter)"/> prior to starting traversal.
	/// <para />
	/// Unless specifically noted otherwise a RevFilter implementation is not thread
	/// safe and may not be shared by different RevWalk instances at the same time.
	/// This restriction allows RevFilter implementations to cache state within their
	/// instances during <seealso cref="include(RevWalk, RevCommit)"/> if it is beneficial to
	/// their implementation. Deep clones created by <seealso cref="Clone()"/> may be used to
	/// construct a thread-safe copy of an existing filter.
	/// <para />
	/// <b>Message filters:</b>
	/// <ul>
	/// <li>Author name/email: <seealso cref="AuthorRevFilter"/></li>
	/// <li>Committer name/email: <seealso cref="CommitterRevFilter"/></li>
	/// <li>Message body: <seealso cref="MessageRevFilter"/></li>
	/// </ul>
	/// <para />
	/// <b>Merge filters:</b>
	/// <ul>
	/// <li>Skip all merges: <seealso cref="NO_MERGES"/>.</li>
	/// </ul>
	/// <para />
	/// <b>Boolean modifiers:</b>
	/// <ul>
	/// <li>AND: <seealso cref="AndRevFilter"/></li>
	/// <li>OR: <seealso cref="OrRevFilter"/></li>
	/// <li>NOT: <seealso cref="NotRevFilter"/></li>
	/// </ul>
	/// </summary>
	public abstract class RevFilter
	{
		/// <summary>
		/// Default filter that always returns true (thread safe).
		/// </summary>
		public static readonly RevFilter ALL = new RevFilterAll();

		/// <summary>
		/// Default filter that always returns false (thread safe).
		/// </summary>
		public static readonly RevFilter NONE = new RevFilterNone();

		/// <summary>
		/// Excludes commits with more than one parent (thread safe).
		/// </summary>
		public static readonly RevFilter NO_MERGES = new RevFilterNoMerges();

		/// <summary>
		/// Selects only merge bases of the starting points (thread safe).
		/// <para />
		/// This is a special case filter that cannot be combined with any other
		/// filter. Its include method always throws an exception as context
		/// information beyond the arguments is necessary to determine if the
		/// supplied commit is a merge base.
		/// </summary>
		public static readonly RevFilter MERGE_BASE = new RevFilterMergeBase();

		/// <summary>
		/// Create a new filter that does the opposite of this filter.
		/// </summary>
		/// <returns>
		/// A new filter that includes commits this filter rejects.
		/// </returns>
		public virtual RevFilter negate()
		{
			return NotRevFilter.create(this);
		}

		/// <summary>
		/// Determine if the supplied commit should be included in results.
		/// </summary>
		/// <param name="walker">
		/// The active walker this filter is being invoked from within.
		/// </param>
		/// <param name="cmit">
		/// The commit currently being tested. The commit has been parsed
		/// and its body is available for inspection.
		/// </param>
		/// <returns>
		/// true to include this commit in the results; false to have this
		/// commit be omitted entirely from the results.
		/// </returns>
		/// <exception cref="StopWalkException">
		/// The filter knows for certain that no additional commits can
		/// ever match, and the current commit doesn't match either. The
		/// walk is halted and no more results are provided.
		/// </exception>
		/// <exception cref="MissingObjectException">
		/// An object the filter needs to consult to determine its answer
		/// does not exist in the Git repository the Walker is operating
		/// on. Filtering this commit is impossible without the object.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// An object the filter needed to consult was not of the
		/// expected object type. This usually indicates a corrupt
		/// repository, as an object link is referencing the wrong type.
		/// </exception>
		/// <exception cref="Exception">
		/// A loose object or pack file could not be Read to obtain data
		/// necessary for the filter to make its decision.
		/// </exception>
		public abstract bool include(RevWalk walker, RevCommit cmit);

		/// <summary>
		/// Clone this revision filter, including its parameters.
		/// <para />
		/// This is a deep Clone. If this filter embeds objects or other filters it
		/// must also Clone those, to ensure the instances do not share mutable data.
		/// </summary>
		/// <returns>
		/// Another copy of this filter, suitable for another thread.
		/// </returns>
		public abstract RevFilter Clone();

		public override string ToString()
		{
			string n = GetType().Name;
			int lastDot = n.LastIndexOf('.');
			if (lastDot >= 0)
			{
				n = n.Substring(lastDot + 1);
			}
			return n.Replace('$', '.');
		}

		#region Nested Types

		private class RevFilterAll : RevFilter
		{
			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return true;
			}

			public override RevFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "ALL";
			}
		}

		/// <summary>
		/// Default filter that always returns false (thread safe).
		/// </summary>
		private class RevFilterNone : RevFilter
		{
			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return false;
			}

			public override RevFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "NONE";
			}
		}

		/// <summary>
		/// Excludes commits with more than one parent (thread safe).
		/// </summary>
		private class RevFilterNoMerges : RevFilter
		{
			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return cmit.ParentCount < 2;
			}

			public override RevFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "NO_MERGES";
			}
		}

		///	<summary>
		/// Selects only merge bases of the starting points (thread safe).
		///	<para />
		///	This is a special case filter that cannot be combined with any other
		///	filter. Its include method always throws an exception as context
		///	information beyond the arguments is necessary to determine if the
		///	supplied commit is a merge base. </summary>
		private class RevFilterMergeBase : RevFilter
		{
			public override bool include(RevWalk walker, RevCommit cmit)
			{
				throw new InvalidOperationException("Cannot be combined.");
			}

			public override RevFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "MERGE_BASE";
			}
		}

		#endregion
	}
}