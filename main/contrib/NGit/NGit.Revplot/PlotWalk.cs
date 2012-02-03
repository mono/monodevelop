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
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Revplot;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revplot
{
	/// <summary>Specialized RevWalk for visualization of a commit graph.</summary>
	/// <remarks>Specialized RevWalk for visualization of a commit graph.</remarks>
	public class PlotWalk : RevWalk
	{
		private IDictionary<AnyObjectId, ICollection<Ref>> reverseRefMap;

		public override void Dispose()
		{
			base.Dispose();
			reverseRefMap.Clear();
		}

		/// <summary>Create a new revision walker for a given repository.</summary>
		/// <remarks>Create a new revision walker for a given repository.</remarks>
		/// <param name="repo">the repository the walker will obtain data from.</param>
		public PlotWalk(Repository repo) : base(repo)
		{
			base.Sort(RevSort.TOPO, true);
			reverseRefMap = repo.GetAllRefsByPeeledObjectId();
		}

		/// <summary>Add additional refs to the walk</summary>
		/// <param name="refs">additional refs</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void AddAdditionalRefs(Iterable<Ref> refs)
		{
			foreach (Ref @ref in refs)
			{
				ICollection<Ref> set = reverseRefMap.Get(@ref.GetObjectId());
				if (set == null)
				{
					set = Sharpen.Collections.Singleton(@ref);
				}
				else
				{
					set = new HashSet<Ref>(set);
					set.AddItem(@ref);
				}
				reverseRefMap.Put(@ref.GetObjectId(), set);
			}
		}

		public override void Sort(RevSort s, bool use)
		{
			if (s == RevSort.TOPO && !use)
			{
				throw new ArgumentException(JGitText.Get().topologicalSortRequired);
			}
			base.Sort(s, use);
		}

		protected internal override RevCommit CreateCommit(AnyObjectId id)
		{
			return new PlotCommit<PlotLane>(id);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override RevCommit Next()
		{
			RevCommit pc = base.Next();
			PlotCommit commit = (PlotCommit)pc;
			if (pc != null)
			{
				commit.refs = GetRefs(pc);
			}
			return pc;
		}

		private Ref[] GetRefs(AnyObjectId commitId)
		{
			ICollection<Ref> list = reverseRefMap.Get(commitId);
			if (list == null)
			{
				return PlotCommit<PlotLane>.NO_REFS;
			}
			else
			{
				Ref[] tags = Sharpen.Collections.ToArray(list, new Ref[list.Count]);
				Arrays.Sort(tags, new PlotWalk.PlotRefComparator(this));
				return tags;
			}
		}

		internal class PlotRefComparator : IComparer<Ref>
		{
			public virtual int Compare(Ref o1, Ref o2)
			{
				try
				{
					RevObject obj1 = this._enclosing.ParseAny(o1.GetObjectId());
					RevObject obj2 = this._enclosing.ParseAny(o2.GetObjectId());
					long t1 = this.Timeof(obj1);
					long t2 = this.Timeof(obj2);
					if (t1 > t2)
					{
						return -1;
					}
					if (t1 < t2)
					{
						return 1;
					}
				}
				catch (IOException)
				{
				}
				// ignore
				int cmp = this.Kind(o1) - this.Kind(o2);
				if (cmp == 0)
				{
					cmp = Sharpen.Runtime.CompareOrdinal(o1.GetName(), o2.GetName());
				}
				return cmp;
			}

			internal virtual long Timeof(RevObject o)
			{
				if (o is RevCommit)
				{
					return ((RevCommit)o).CommitTime;
				}
				if (o is RevTag)
				{
					RevTag tag = (RevTag)o;
					PersonIdent who = tag.GetTaggerIdent();
					return who != null ? who.GetWhen().GetTime() : 0;
				}
				return 0;
			}

			internal virtual int Kind(Ref r)
			{
				if (r.GetName().StartsWith(Constants.R_TAGS))
				{
					return 0;
				}
				if (r.GetName().StartsWith(Constants.R_HEADS))
				{
					return 1;
				}
				if (r.GetName().StartsWith(Constants.R_REMOTES))
				{
					return 2;
				}
				return 3;
			}

			internal PlotRefComparator(PlotWalk _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PlotWalk _enclosing;
		}
	}
}
