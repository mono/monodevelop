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

using System.Text;
using NGit;
using NGit.Blame;
using NGit.Diff;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Blame
{
	/// <summary>Collects line annotations for inspection by applications.</summary>
	/// <remarks>
	/// Collects line annotations for inspection by applications.
	/// <p>
	/// A result is usually updated incrementally as the BlameGenerator digs back
	/// further through history. Applications that want to lay annotations down text
	/// to the original source file in a viewer may find the BlameResult structure an
	/// easy way to acquire the information, at the expense of keeping tables in
	/// memory tracking every line of the result file.
	/// <p>
	/// This class is not thread-safe.
	/// <p>
	/// During blame processing there are two files involved:
	/// <ul>
	/// <li>result - The file whose lines are being examined. This is the revision
	/// the user is trying to view blame/annotation information alongside of.</li>
	/// <li>source - The file that was blamed with supplying one or more lines of
	/// data into result. The source may be a different file path (due to copy or
	/// rename). Source line numbers may differ from result line numbers due to lines
	/// being added/removed in intermediate revisions.</li>
	/// </ul>
	/// </remarks>
	public class BlameResult
	{
		/// <summary>Construct a new BlameResult for a generator.</summary>
		/// <remarks>Construct a new BlameResult for a generator.</remarks>
		/// <param name="gen">the generator the result will consume records from.</param>
		/// <returns>
		/// the new result object. null if the generator cannot find the path
		/// it starts from.
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public static NGit.Blame.BlameResult Create(BlameGenerator gen)
		{
			string path = gen.GetResultPath();
			RawText contents = gen.GetResultContents();
			if (contents == null)
			{
				gen.Release();
				return null;
			}
			return new NGit.Blame.BlameResult(gen, path, contents);
		}

		private readonly string resultPath;

		private readonly RevCommit[] sourceCommits;

		private readonly PersonIdent[] sourceAuthors;

		private readonly PersonIdent[] sourceCommitters;

		private readonly string[] sourcePaths;

		/// <summary>Warning: these are actually 1-based.</summary>
		/// <remarks>Warning: these are actually 1-based.</remarks>
		private readonly int[] sourceLines;

		private RawText resultContents;

		private BlameGenerator generator;

		private int lastLength;

		internal BlameResult(BlameGenerator bg, string path, RawText text)
		{
			generator = bg;
			resultPath = path;
			resultContents = text;
			int cnt = text.Size();
			sourceCommits = new RevCommit[cnt];
			sourceAuthors = new PersonIdent[cnt];
			sourceCommitters = new PersonIdent[cnt];
			sourceLines = new int[cnt];
			sourcePaths = new string[cnt];
		}

		/// <returns>path of the file this result annotates.</returns>
		public virtual string GetResultPath()
		{
			return resultPath;
		}

		/// <returns>contents of the result file, available for display.</returns>
		public virtual RawText GetResultContents()
		{
			return resultContents;
		}

		/// <summary>
		/// Throw away the
		/// <see cref="GetResultContents()">GetResultContents()</see>
		/// .
		/// </summary>
		public virtual void DiscardResultContents()
		{
			resultContents = null;
		}

		/// <summary>Check if the given result line has been annotated yet.</summary>
		/// <remarks>Check if the given result line has been annotated yet.</remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>true if the data has been annotated, false otherwise.</returns>
		public virtual bool HasSourceData(int idx)
		{
			return sourceLines[idx] != 0;
		}

		/// <summary>Check if the given result line has been annotated yet.</summary>
		/// <remarks>Check if the given result line has been annotated yet.</remarks>
		/// <param name="start">first index to examine.</param>
		/// <param name="end">last index to examine.</param>
		/// <returns>true if the data has been annotated, false otherwise.</returns>
		public virtual bool HasSourceData(int start, int end)
		{
			for (; start < end; start++)
			{
				if (sourceLines[start] == 0)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Get the commit that provided the specified line of the result.</summary>
		/// <remarks>
		/// Get the commit that provided the specified line of the result.
		/// <p>
		/// The source commit may be null if the line was blamed to an uncommitted
		/// revision, such as the working tree copy, or during a reverse blame if the
		/// line survives to the end revision (e.g. the branch tip).
		/// </remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>
		/// commit that provided line
		/// <code>idx</code>
		/// . May be null.
		/// </returns>
		public virtual RevCommit GetSourceCommit(int idx)
		{
			return sourceCommits[idx];
		}

		/// <summary>Get the author that provided the specified line of the result.</summary>
		/// <remarks>Get the author that provided the specified line of the result.</remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>
		/// author that provided line
		/// <code>idx</code>
		/// . May be null.
		/// </returns>
		public virtual PersonIdent GetSourceAuthor(int idx)
		{
			return sourceAuthors[idx];
		}

		/// <summary>Get the committer that provided the specified line of the result.</summary>
		/// <remarks>Get the committer that provided the specified line of the result.</remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>
		/// committer that provided line
		/// <code>idx</code>
		/// . May be null.
		/// </returns>
		public virtual PersonIdent GetSourceCommitter(int idx)
		{
			return sourceCommitters[idx];
		}

		/// <summary>Get the file path that provided the specified line of the result.</summary>
		/// <remarks>Get the file path that provided the specified line of the result.</remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>
		/// source file path that provided line
		/// <code>idx</code>
		/// .
		/// </returns>
		public virtual string GetSourcePath(int idx)
		{
			return sourcePaths[idx];
		}

		/// <summary>Get the corresponding line number in the source file.</summary>
		/// <remarks>Get the corresponding line number in the source file.</remarks>
		/// <param name="idx">line to read data of, 0 based.</param>
		/// <returns>matching line number in the source file.</returns>
		public virtual int GetSourceLine(int idx)
		{
			return sourceLines[idx] - 1;
		}

		/// <summary>Compute all pending information.</summary>
		/// <remarks>Compute all pending information.</remarks>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual void ComputeAll()
		{
			BlameGenerator gen = generator;
			if (gen == null)
			{
				return;
			}
			try
			{
				while (gen.Next())
				{
					LoadFrom(gen);
				}
			}
			finally
			{
				gen.Release();
				generator = null;
			}
		}

		/// <summary>Compute the next available segment and return the first index.</summary>
		/// <remarks>
		/// Compute the next available segment and return the first index.
		/// <p>
		/// Computes one segment and returns to the caller the first index that is
		/// available. After return the caller can also inspect
		/// <see cref="LastLength()">LastLength()</see>
		/// to determine how many lines of the result were computed.
		/// </remarks>
		/// <returns>index that is now available. -1 if no more are available.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual int ComputeNext()
		{
			BlameGenerator gen = generator;
			if (gen == null)
			{
				return -1;
			}
			if (gen.Next())
			{
				LoadFrom(gen);
				lastLength = gen.GetRegionLength();
				return gen.GetResultStart();
			}
			else
			{
				gen.Release();
				generator = null;
				return -1;
			}
		}

		/// <returns>
		/// length of the last segment found by
		/// <see cref="ComputeNext()">ComputeNext()</see>
		/// .
		/// </returns>
		public virtual int LastLength()
		{
			return lastLength;
		}

		/// <summary>Compute until the entire range has been populated.</summary>
		/// <remarks>Compute until the entire range has been populated.</remarks>
		/// <param name="start">first index to examine.</param>
		/// <param name="end">last index to examine.</param>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual void ComputeRange(int start, int end)
		{
			BlameGenerator gen = generator;
			if (gen == null)
			{
				return;
			}
			while (start < end)
			{
				if (HasSourceData(start, end))
				{
					return;
				}
				if (!gen.Next())
				{
					gen.Release();
					generator = null;
					return;
				}
				LoadFrom(gen);
				// If the result contains either end of our current range bounds,
				// update the bounds to avoid scanning that section during the
				// next loop iteration.
				int resLine = gen.GetResultStart();
				int resEnd = gen.GetResultEnd();
				if (resLine <= start && start < resEnd)
				{
					start = resEnd;
				}
				if (resLine <= end && end < resEnd)
				{
					end = resLine;
				}
			}
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("BlameResult: ");
			r.Append(GetResultPath());
			return r.ToString();
		}

		private void LoadFrom(BlameGenerator gen)
		{
			RevCommit srcCommit = gen.GetSourceCommit();
			PersonIdent srcAuthor = gen.GetSourceAuthor();
			PersonIdent srcCommitter = gen.GetSourceCommitter();
			string srcPath = gen.GetSourcePath();
			int srcLine = gen.GetSourceStart();
			int resLine = gen.GetResultStart();
			int resEnd = gen.GetResultEnd();
			for (; resLine < resEnd; resLine++)
			{
				// Reverse blame can generate multiple results for the same line.
				// Favor the first one selected, as this is the oldest and most
				// likely to be nearest to the inquiry made by the user.
				if (sourceLines[resLine] != 0)
				{
					continue;
				}
				sourceCommits[resLine] = srcCommit;
				sourceAuthors[resLine] = srcAuthor;
				sourceCommitters[resLine] = srcCommitter;
				sourcePaths[resLine] = srcPath;
				// Since sourceLines is 1-based to permit hasSourceData to use 0 to
				// mean the line has not been annotated yet, pre-increment instead
				// of the traditional post-increment when making the assignment.
				sourceLines[resLine] = ++srcLine;
			}
		}
	}
}
