/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using NGit;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Selects interesting revisions during walking.</summary>
	/// <remarks>
	/// Selects interesting revisions during walking.
	/// <p>
	/// This is an abstract interface. Applications may implement a subclass, or use
	/// one of the predefined implementations already available within this package.
	/// Filters may be chained together using <code>AndRevFilter</code> and
	/// <code>OrRevFilter</code> to create complex boolean expressions.
	/// <p>
	/// Applications should install the filter on a RevWalk by
	/// <see cref="NGit.Revwalk.RevWalk.SetRevFilter(RevFilter)">NGit.Revwalk.RevWalk.SetRevFilter(RevFilter)
	/// 	</see>
	/// prior to starting traversal.
	/// <p>
	/// Unless specifically noted otherwise a RevFilter implementation is not thread
	/// safe and may not be shared by different RevWalk instances at the same time.
	/// This restriction allows RevFilter implementations to cache state within their
	/// instances during
	/// <see cref="Include(NGit.Revwalk.RevWalk, NGit.Revwalk.RevCommit)">Include(NGit.Revwalk.RevWalk, NGit.Revwalk.RevCommit)
	/// 	</see>
	/// if it is beneficial to
	/// their implementation. Deep clones created by
	/// <see cref="Clone()">Clone()</see>
	/// may be used to
	/// construct a thread-safe copy of an existing filter.
	/// <p>
	/// <b>Message filters:</b>
	/// <ul>
	/// <li>Author name/email:
	/// <see cref="AuthorRevFilter">AuthorRevFilter</see>
	/// </li>
	/// <li>Committer name/email:
	/// <see cref="CommitterRevFilter">CommitterRevFilter</see>
	/// </li>
	/// <li>Message body:
	/// <see cref="MessageRevFilter">MessageRevFilter</see>
	/// </li>
	/// </ul>
	/// <p>
	/// <b>Merge filters:</b>
	/// <ul>
	/// <li>Skip all merges:
	/// <see cref="NO_MERGES">NO_MERGES</see>
	/// .</li>
	/// </ul>
	/// <p>
	/// <b>Boolean modifiers:</b>
	/// <ul>
	/// <li>AND:
	/// <see cref="AndRevFilter">AndRevFilter</see>
	/// </li>
	/// <li>OR:
	/// <see cref="OrRevFilter">OrRevFilter</see>
	/// </li>
	/// <li>NOT:
	/// <see cref="NotRevFilter">NotRevFilter</see>
	/// </li>
	/// </ul>
	/// </remarks>
	public abstract class RevFilter
	{
		private sealed class _RevFilter_97 : RevFilter
		{
			public _RevFilter_97()
			{
			}

			public override bool Include(RevWalk walker, RevCommit c)
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

		/// <summary>Default filter that always returns true (thread safe).</summary>
		/// <remarks>Default filter that always returns true (thread safe).</remarks>
		public static readonly RevFilter ALL = new _RevFilter_97();

		private sealed class _RevFilter_115 : RevFilter
		{
			public _RevFilter_115()
			{
			}

			public override bool Include(RevWalk walker, RevCommit c)
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

		/// <summary>Default filter that always returns false (thread safe).</summary>
		/// <remarks>Default filter that always returns false (thread safe).</remarks>
		public static readonly RevFilter NONE = new _RevFilter_115();

		private sealed class _RevFilter_133 : RevFilter
		{
			public _RevFilter_133()
			{
			}

			public override bool Include(RevWalk walker, RevCommit c)
			{
				return c.ParentCount < 2;
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

		/// <summary>Excludes commits with more than one parent (thread safe).</summary>
		/// <remarks>Excludes commits with more than one parent (thread safe).</remarks>
		public static readonly RevFilter NO_MERGES = new _RevFilter_133();

		private sealed class _RevFilter_158 : RevFilter
		{
			public _RevFilter_158()
			{
			}

			public override bool Include(RevWalk walker, RevCommit c)
			{
				throw new NotSupportedException(JGitText.Get().cannotBeCombined);
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

		/// <summary>Selects only merge bases of the starting points (thread safe).</summary>
		/// <remarks>
		/// Selects only merge bases of the starting points (thread safe).
		/// <p>
		/// This is a special case filter that cannot be combined with any other
		/// filter. Its include method always throws an exception as context
		/// information beyond the arguments is necessary to determine if the
		/// supplied commit is a merge base.
		/// </remarks>
		public static readonly RevFilter MERGE_BASE = new _RevFilter_158();

		/// <summary>Create a new filter that does the opposite of this filter.</summary>
		/// <remarks>Create a new filter that does the opposite of this filter.</remarks>
		/// <returns>a new filter that includes commits this filter rejects.</returns>
		public virtual RevFilter Negate()
		{
			return NotRevFilter.Create(this);
		}

		/// <summary>Determine if the supplied commit should be included in results.</summary>
		/// <remarks>Determine if the supplied commit should be included in results.</remarks>
		/// <param name="walker">the active walker this filter is being invoked from within.</param>
		/// <param name="cmit">
		/// the commit currently being tested. The commit has been parsed
		/// and its body is available for inspection.
		/// </param>
		/// <returns>
		/// true to include this commit in the results; false to have this
		/// commit be omitted entirely from the results.
		/// </returns>
		/// <exception cref="NGit.Errors.StopWalkException">
		/// the filter knows for certain that no additional commits can
		/// ever match, and the current commit doesn't match either. The
		/// walk is halted and no more results are provided.
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// an object the filter needs to consult to determine its answer
		/// does not exist in the Git repository the walker is operating
		/// on. Filtering this commit is impossible without the object.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// an object the filter needed to consult was not of the
		/// expected object type. This usually indicates a corrupt
		/// repository, as an object link is referencing the wrong type.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// a loose object or pack file could not be read to obtain data
		/// necessary for the filter to make its decision.
		/// </exception>
		public abstract bool Include(RevWalk walker, RevCommit cmit);

		/// <summary>Clone this revision filter, including its parameters.</summary>
		/// <remarks>
		/// Clone this revision filter, including its parameters.
		/// <p>
		/// This is a deep clone. If this filter embeds objects or other filters it
		/// must also clone those, to ensure the instances do not share mutable data.
		/// </remarks>
		/// <returns>another copy of this filter, suitable for another thread.</returns>
		public abstract RevFilter Clone();

		public override string ToString()
		{
			string n = GetType().FullName;
			int lastDot = n.LastIndexOf('.');
			if (lastDot >= 0)
			{
				n = Sharpen.Runtime.Substring(n, lastDot + 1);
			}
			return n.Replace('$', '.');
		}
	}
}
