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

using NGit;
using NGit.Revplot;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revplot
{
	/// <summary>A commit reference to a commit in the DAG.</summary>
	/// <remarks>A commit reference to a commit in the DAG.</remarks>
	/// <?></?>
	/// <seealso cref="PlotCommitList{L}">PlotCommitList&lt;L&gt;</seealso>
	[System.Serializable]
	public class PlotCommit<L> : RevCommit, PlotCommit where L:PlotLane
	{
		internal static readonly NGit.Revplot.PlotCommit[] NO_CHILDREN = new NGit.Revplot.PlotCommit
			[] {  };

		internal static readonly PlotLane[] NO_LANES = new PlotLane[] {  };

		internal PlotLane[] passingLanes;

		internal PlotLane lane;

		internal NGit.Revplot.PlotCommit[] children;

		internal Ref[] refs;

		/// <summary>Create a new commit.</summary>
		/// <remarks>Create a new commit.</remarks>
		/// <param name="id">the identity of this commit.</param>
		protected internal PlotCommit(AnyObjectId id) : base(id)
		{
			passingLanes = NO_LANES;
			children = NO_CHILDREN;
		}

		internal virtual void AddPassingLane(PlotLane c)
		{
			int cnt = passingLanes.Length;
			if (cnt == 0)
			{
				passingLanes = new PlotLane[] { c };
			}
			else
			{
				if (cnt == 1)
				{
					passingLanes = new PlotLane[] { passingLanes[0], c };
				}
				else
				{
					PlotLane[] n = new PlotLane[cnt + 1];
					System.Array.Copy(passingLanes, 0, n, 0, cnt);
					n[cnt] = c;
					passingLanes = n;
				}
			}
		}

		internal virtual void AddChild(NGit.Revplot.PlotCommit c)
		{
			int cnt = children.Length;
			if (cnt == 0)
			{
				children = new NGit.Revplot.PlotCommit[] { c };
			}
			else
			{
				if (cnt == 1)
				{
					children = new NGit.Revplot.PlotCommit[] { children[0], c };
				}
				else
				{
					NGit.Revplot.PlotCommit[] n = new NGit.Revplot.PlotCommit[cnt + 1];
					System.Array.Copy(children, 0, n, 0, cnt);
					n[cnt] = c;
					children = n;
				}
			}
		}

		/// <summary>Get the number of child commits listed in this commit.</summary>
		/// <remarks>Get the number of child commits listed in this commit.</remarks>
		/// <returns>number of children; always a positive value but can be 0.</returns>
		public int GetChildCount()
		{
			return children.Length;
		}

		/// <summary>Get the nth child from this commit's child list.</summary>
		/// <remarks>Get the nth child from this commit's child list.</remarks>
		/// <param name="nth">
		/// child index to obtain. Must be in the range 0 through
		/// <see cref="PlotCommit{L}.GetChildCount()">PlotCommit&lt;L&gt;.GetChildCount()</see>
		/// -1.
		/// </param>
		/// <returns>the specified child.</returns>
		/// <exception cref="System.IndexOutOfRangeException">an invalid child index was specified.
		/// 	</exception>
		public NGit.Revplot.PlotCommit GetChild(int nth)
		{
			return children[nth];
		}

		/// <summary>Determine if the given commit is a child (descendant) of this commit.</summary>
		/// <remarks>Determine if the given commit is a child (descendant) of this commit.</remarks>
		/// <param name="c">the commit to test.</param>
		/// <returns>true if the given commit built on top of this commit.</returns>
		public bool IsChild(NGit.Revplot.PlotCommit c)
		{
			foreach (NGit.Revplot.PlotCommit a in children)
			{
				if (a == c)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Obtain the lane this commit has been plotted into.</summary>
		/// <remarks>Obtain the lane this commit has been plotted into.</remarks>
		/// <returns>the assigned lane for this commit.</returns>
		public L GetLane()
		{
			return (L)lane;
		}

		public override void Reset()
		{
			passingLanes = NO_LANES;
			children = NO_CHILDREN;
			lane = null;
			base.Reset();
		}
		
		void PlotCommit.AddChild(PlotCommit c)
		{
			this.AddChild(c);
		}
		
		void PlotCommit.AddPassingLane(PlotLane c)
		{
			this.AddPassingLane(c);
		}
		
		PlotLane PlotCommit.GetLane()
		{
			return GetLane ();
		}
		
		int PlotCommit.ParentCount {
			get {
				return base.ParentCount;
			}
		}
		
		PlotLane PlotCommit.lane
		{
			get
			{
				return this.lane;
			}
			set
			{
				this.lane = value;
			}
		}
		
		Ref[] PlotCommit.refs
		{
			get
			{
				return this.refs;
			}
			set
			{
				this.refs = value;
			}
		}
	}

	public interface PlotCommit
	{
		// Methods
		void AddChild(PlotCommit c);
		void AddPassingLane(PlotLane c);
		int ParentCount { get; }
		PlotLane GetLane();
		
		// Properties
		PlotLane lane { get; set; }
		Ref[] refs { get; set; }
	}
}
