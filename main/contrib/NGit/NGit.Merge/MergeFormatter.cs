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

using System.Collections.Generic;
using NGit.Diff;
using NGit.Merge;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>A class to convert merge results into a Git conformant textual presentation
	/// 	</summary>
	public class MergeFormatter
	{
		/// <summary>
		/// Formats the results of a merge of
		/// <see cref="NGit.Diff.RawText">NGit.Diff.RawText</see>
		/// objects in a Git
		/// conformant way. This method also assumes that the
		/// <see cref="NGit.Diff.RawText">NGit.Diff.RawText</see>
		/// objects
		/// being merged are line oriented files which use LF as delimiter. This
		/// method will also use LF to separate chunks and conflict metadata,
		/// therefore it fits only to texts that are LF-separated lines.
		/// </summary>
		/// <param name="out">the outputstream where to write the textual presentation</param>
		/// <param name="res">the merge result which should be presented</param>
		/// <param name="seqName">
		/// When a conflict is reported each conflicting range will get a
		/// name. This name is following the "<&lt;&lt;&lt;&lt;&lt;&lt; " or ">&gt;&gt;&gt;&gt;&gt;&gt; "
		/// conflict markers. The names for the sequences are given in
		/// this list
		/// </param>
		/// <param name="charsetName">
		/// the name of the characterSet used when writing conflict
		/// metadata
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void FormatMerge(OutputStream @out, MergeResult<RawText> res, IList
			<string> seqName, string charsetName)
		{
			string lastConflictingName = null;
			// is set to non-null whenever we are
			// in a conflict
			bool threeWayMerge = (res.GetSequences().Count == 3);
			foreach (MergeChunk chunk in res)
			{
				RawText seq = res.GetSequences()[chunk.GetSequenceIndex()];
				if (lastConflictingName != null && chunk.GetConflictState() != MergeChunk.ConflictState
					.NEXT_CONFLICTING_RANGE)
				{
					// found the end of an conflict
					@out.Write(Sharpen.Runtime.GetBytesForString((">>>>>>> " + lastConflictingName + 
						"\n"), charsetName));
					lastConflictingName = null;
				}
				if (chunk.GetConflictState() == MergeChunk.ConflictState.FIRST_CONFLICTING_RANGE)
				{
					// found the start of an conflict
					@out.Write(Sharpen.Runtime.GetBytesForString(("<<<<<<< " + seqName[chunk.GetSequenceIndex
						()] + "\n"), charsetName));
					lastConflictingName = seqName[chunk.GetSequenceIndex()];
				}
				else
				{
					if (chunk.GetConflictState() == MergeChunk.ConflictState.NEXT_CONFLICTING_RANGE)
					{
						// found another conflicting chunk
						lastConflictingName = seqName[chunk.GetSequenceIndex()];
						@out.Write(Sharpen.Runtime.GetBytesForString((threeWayMerge ? "=======\n" : "======= "
							 + lastConflictingName + "\n"), charsetName));
					}
				}
				// the lines with conflict-metadata are written. Now write the chunk
				for (int i = chunk.GetBegin(); i < chunk.GetEnd(); i++)
				{
					seq.WriteLine(@out, i);
					@out.Write('\n');
				}
			}
			// one possible leftover: if the merge result ended with a conflict we
			// have to close the last conflict here
			if (lastConflictingName != null)
			{
				@out.Write(Sharpen.Runtime.GetBytesForString((">>>>>>> " + lastConflictingName + 
					"\n"), charsetName));
			}
		}

		/// <summary>
		/// Formats the results of a merge of exactly two
		/// <see cref="NGit.Diff.RawText">NGit.Diff.RawText</see>
		/// objects in
		/// a Git conformant way. This convenience method accepts the names for the
		/// three sequences (base and the two merged sequences) as explicit
		/// parameters and doesn't require the caller to specify a List
		/// </summary>
		/// <param name="out">
		/// the
		/// <see cref="Sharpen.OutputStream">Sharpen.OutputStream</see>
		/// where to write the textual
		/// presentation
		/// </param>
		/// <param name="res">the merge result which should be presented</param>
		/// <param name="baseName">the name ranges from the base should get</param>
		/// <param name="oursName">the name ranges from ours should get</param>
		/// <param name="theirsName">the name ranges from theirs should get</param>
		/// <param name="charsetName">
		/// the name of the characterSet used when writing conflict
		/// metadata
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void FormatMerge(OutputStream @out, MergeResult<RawText> res, string baseName
			, string oursName, string theirsName, string charsetName)
		{
			IList<string> names = new AList<string>(3);
			names.AddItem(baseName);
			names.AddItem(oursName);
			names.AddItem(theirsName);
			FormatMerge(@out, res, names, charsetName);
		}
	}
}
