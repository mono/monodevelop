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
using NGit.Diff;
using NGit.Errors;
using NGit.Patch;
using NGit.Revwalk;
using NGit.Storage.Pack;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>Format a Git style patch script.</summary>
	/// <remarks>Format a Git style patch script.</remarks>
	public class DiffFormatter
	{
		private const int DEFAULT_BINARY_FILE_THRESHOLD = PackConfig.DEFAULT_BIG_FILE_THRESHOLD;

		private static readonly byte[] noNewLine = Constants.EncodeASCII("\\ No newline at end of file\n"
			);

		/// <summary>Magic return content indicating it is empty or no content present.</summary>
		/// <remarks>Magic return content indicating it is empty or no content present.</remarks>
		private static readonly byte[] EMPTY = new byte[] {  };

		/// <summary>Magic return indicating the content is binary.</summary>
		/// <remarks>Magic return indicating the content is binary.</remarks>
		private static readonly byte[] BINARY = new byte[] {  };

		private readonly OutputStream @out;

		private Repository db;

		private ObjectReader reader;

		private int context = 3;

		private int abbreviationLength = 7;

		private DiffAlgorithm diffAlgorithm = MyersDiff<Sequence>.INSTANCE;

		private RawTextComparator comparator = RawTextComparator.DEFAULT;

		private int binaryFileThreshold = DEFAULT_BINARY_FILE_THRESHOLD;

		private string oldPrefix = "a/";

		private string newPrefix = "b/";

		private TreeFilter pathFilter = TreeFilter.ALL;

		private RenameDetector renameDetector;

		private ProgressMonitor progressMonitor;

		private ContentSource.Pair source;

		/// <summary>Create a new formatter with a default level of context.</summary>
		/// <remarks>Create a new formatter with a default level of context.</remarks>
		/// <param name="out">
		/// the stream the formatter will write line data to. This stream
		/// should have buffering arranged by the caller, as many small
		/// writes are performed to it.
		/// </param>
		public DiffFormatter(OutputStream @out)
		{
			this.@out = @out;
		}

		/// <returns>the stream we are outputting data to.</returns>
		protected internal virtual OutputStream GetOutputStream()
		{
			return @out;
		}

		/// <summary>Set the repository the formatter can load object contents from.</summary>
		/// <remarks>
		/// Set the repository the formatter can load object contents from.
		/// Once a repository has been set, the formatter must be released to ensure
		/// the internal ObjectReader is able to release its resources.
		/// </remarks>
		/// <param name="repository">source repository holding referenced objects.</param>
		public virtual void SetRepository(Repository repository)
		{
			if (reader != null)
			{
				reader.Release();
			}
			db = repository;
			reader = db.NewObjectReader();
			ContentSource cs = ContentSource.Create(reader);
			source = new ContentSource.Pair(cs, cs);
			DiffConfig dc = db.GetConfig().Get(DiffConfig.KEY);
			if (dc.IsNoPrefix())
			{
				SetOldPrefix(string.Empty);
				SetNewPrefix(string.Empty);
			}
			SetDetectRenames(dc.IsRenameDetectionEnabled());
		}

		/// <summary>Change the number of lines of context to display.</summary>
		/// <remarks>Change the number of lines of context to display.</remarks>
		/// <param name="lineCount">
		/// number of lines of context to see before the first
		/// modification and after the last modification within a hunk of
		/// the modified file.
		/// </param>
		public virtual void SetContext(int lineCount)
		{
			if (lineCount < 0)
			{
				throw new ArgumentException(JGitText.Get().contextMustBeNonNegative);
			}
			context = lineCount;
		}

		/// <summary>Change the number of digits to show in an ObjectId.</summary>
		/// <remarks>Change the number of digits to show in an ObjectId.</remarks>
		/// <param name="count">number of digits to show in an ObjectId.</param>
		public virtual void SetAbbreviationLength(int count)
		{
			if (count < 0)
			{
				throw new ArgumentException(JGitText.Get().abbreviationLengthMustBeNonNegative);
			}
			abbreviationLength = count;
		}

		/// <summary>Set the algorithm that constructs difference output.</summary>
		/// <remarks>Set the algorithm that constructs difference output.</remarks>
		/// <param name="alg">the algorithm to produce text file differences.</param>
		/// <seealso cref="MyersDiff{S}.INSTANCE">MyersDiff&lt;S&gt;.INSTANCE</seealso>
		public virtual void SetDiffAlgorithm(DiffAlgorithm alg)
		{
			diffAlgorithm = alg;
		}

		/// <summary>Set the line equivalence function for text file differences.</summary>
		/// <remarks>Set the line equivalence function for text file differences.</remarks>
		/// <param name="cmp">
		/// The equivalence function used to determine if two lines of
		/// text are identical. The function can be changed to ignore
		/// various types of whitespace.
		/// </param>
		/// <seealso cref="RawTextComparator.DEFAULT">RawTextComparator.DEFAULT</seealso>
		/// <seealso cref="RawTextComparator.WS_IGNORE_ALL">RawTextComparator.WS_IGNORE_ALL</seealso>
		/// <seealso cref="RawTextComparator.WS_IGNORE_CHANGE">RawTextComparator.WS_IGNORE_CHANGE
		/// 	</seealso>
		/// <seealso cref="RawTextComparator.WS_IGNORE_LEADING">RawTextComparator.WS_IGNORE_LEADING
		/// 	</seealso>
		/// <seealso cref="RawTextComparator.WS_IGNORE_TRAILING">RawTextComparator.WS_IGNORE_TRAILING
		/// 	</seealso>
		public virtual void SetDiffComparator(RawTextComparator cmp)
		{
			comparator = cmp;
		}

		/// <summary>Set maximum file size for text files.</summary>
		/// <remarks>
		/// Set maximum file size for text files.
		/// Files larger than this size will be treated as though they are binary and
		/// not text. Default is
		/// <value>#DEFAULT_BINARY_FILE_THRESHOLD</value>
		/// .
		/// </remarks>
		/// <param name="threshold">
		/// the limit, in bytes. Files larger than this size will be
		/// assumed to be binary, even if they aren't.
		/// </param>
		public virtual void SetBinaryFileThreshold(int threshold)
		{
			this.binaryFileThreshold = threshold;
		}

		/// <summary>Set the prefix applied in front of old file paths.</summary>
		/// <remarks>Set the prefix applied in front of old file paths.</remarks>
		/// <param name="prefix">
		/// the prefix in front of old paths. Typically this is the
		/// standard string
		/// <code>"a/"</code>
		/// , but may be any prefix desired by
		/// the caller. Must not be null. Use the empty string to have no
		/// prefix at all.
		/// </param>
		public virtual void SetOldPrefix(string prefix)
		{
			oldPrefix = prefix;
		}

		/// <summary>Set the prefix applied in front of new file paths.</summary>
		/// <remarks>Set the prefix applied in front of new file paths.</remarks>
		/// <param name="prefix">
		/// the prefix in front of new paths. Typically this is the
		/// standard string
		/// <code>"b/"</code>
		/// , but may be any prefix desired by
		/// the caller. Must not be null. Use the empty string to have no
		/// prefix at all.
		/// </param>
		public virtual void SetNewPrefix(string prefix)
		{
			newPrefix = prefix;
		}

		/// <returns>true if rename detection is enabled.</returns>
		public virtual bool IsDetectRenames()
		{
			return renameDetector != null;
		}

		/// <summary>Enable or disable rename detection.</summary>
		/// <remarks>
		/// Enable or disable rename detection.
		/// Before enabling rename detection the repository must be set with
		/// <see cref="SetRepository(NGit.Repository)">SetRepository(NGit.Repository)</see>
		/// . Once enabled the detector can be
		/// configured away from its defaults by obtaining the instance directly from
		/// <see cref="GetRenameDetector()">GetRenameDetector()</see>
		/// and invoking configuration.
		/// </remarks>
		/// <param name="on">if rename detection should be enabled.</param>
		public virtual void SetDetectRenames(bool on)
		{
			if (on && renameDetector == null)
			{
				AssertHaveRepository();
				renameDetector = new RenameDetector(db);
			}
			else
			{
				if (!on)
				{
					renameDetector = null;
				}
			}
		}

		/// <returns>the rename detector if rename detection is enabled.</returns>
		public virtual RenameDetector GetRenameDetector()
		{
			return renameDetector;
		}

		/// <summary>Set the progress monitor for long running rename detection.</summary>
		/// <remarks>Set the progress monitor for long running rename detection.</remarks>
		/// <param name="pm">progress monitor to receive rename detection status through.</param>
		public virtual void SetProgressMonitor(ProgressMonitor pm)
		{
			progressMonitor = pm;
		}

		/// <summary>Set the filter to produce only specific paths.</summary>
		/// <remarks>
		/// Set the filter to produce only specific paths.
		/// If the filter is an instance of
		/// <see cref="NGit.Revwalk.FollowFilter">NGit.Revwalk.FollowFilter</see>
		/// , the filter path
		/// will be updated during successive scan or format invocations. The updated
		/// path can be obtained from
		/// <see cref="GetPathFilter()">GetPathFilter()</see>
		/// .
		/// </remarks>
		/// <param name="filter">the tree filter to apply.</param>
		public virtual void SetPathFilter(TreeFilter filter)
		{
			pathFilter = filter != null ? filter : TreeFilter.ALL;
		}

		/// <returns>the current path filter.</returns>
		public virtual TreeFilter GetPathFilter()
		{
			return pathFilter;
		}

		/// <summary>Flush the underlying output stream of this formatter.</summary>
		/// <remarks>Flush the underlying output stream of this formatter.</remarks>
		/// <exception cref="System.IO.IOException">the stream's own flush method threw an exception.
		/// 	</exception>
		public virtual void Flush()
		{
			@out.Flush();
		}

		/// <summary>Release the internal ObjectReader state.</summary>
		/// <remarks>Release the internal ObjectReader state.</remarks>
		public virtual void Release()
		{
			if (reader != null)
			{
				reader.Release();
			}
		}

		/// <summary>Determine the differences between two trees.</summary>
		/// <remarks>
		/// Determine the differences between two trees.
		/// No output is created, instead only the file paths that are different are
		/// returned. Callers may choose to format these paths themselves, or convert
		/// them into
		/// <see cref="NGit.Patch.FileHeader">NGit.Patch.FileHeader</see>
		/// instances with a complete edit list by
		/// calling
		/// <see cref="ToFileHeader(DiffEntry)">ToFileHeader(DiffEntry)</see>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <returns>the paths that are different.</returns>
		/// <exception cref="System.IO.IOException">trees cannot be read or file contents cannot be read.
		/// 	</exception>
		public virtual IList<DiffEntry> Scan(AnyObjectId a, AnyObjectId b)
		{
			AssertHaveRepository();
			RevWalk rw = new RevWalk(reader);
			return Scan(rw.ParseTree(a), rw.ParseTree(b));
		}

		/// <summary>Determine the differences between two trees.</summary>
		/// <remarks>
		/// Determine the differences between two trees.
		/// No output is created, instead only the file paths that are different are
		/// returned. Callers may choose to format these paths themselves, or convert
		/// them into
		/// <see cref="NGit.Patch.FileHeader">NGit.Patch.FileHeader</see>
		/// instances with a complete edit list by
		/// calling
		/// <see cref="ToFileHeader(DiffEntry)">ToFileHeader(DiffEntry)</see>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <returns>the paths that are different.</returns>
		/// <exception cref="System.IO.IOException">trees cannot be read or file contents cannot be read.
		/// 	</exception>
		public virtual IList<DiffEntry> Scan(RevTree a, RevTree b)
		{
			AssertHaveRepository();
			CanonicalTreeParser aParser = new CanonicalTreeParser();
			CanonicalTreeParser bParser = new CanonicalTreeParser();
			aParser.Reset(reader, a);
			bParser.Reset(reader, b);
			return Scan(aParser, bParser);
		}

		/// <summary>Determine the differences between two trees.</summary>
		/// <remarks>
		/// Determine the differences between two trees.
		/// No output is created, instead only the file paths that are different are
		/// returned. Callers may choose to format these paths themselves, or convert
		/// them into
		/// <see cref="NGit.Patch.FileHeader">NGit.Patch.FileHeader</see>
		/// instances with a complete edit list by
		/// calling
		/// <see cref="ToFileHeader(DiffEntry)">ToFileHeader(DiffEntry)</see>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <returns>the paths that are different.</returns>
		/// <exception cref="System.IO.IOException">trees cannot be read or file contents cannot be read.
		/// 	</exception>
		public virtual IList<DiffEntry> Scan(AbstractTreeIterator a, AbstractTreeIterator
			 b)
		{
			AssertHaveRepository();
			TreeWalk walk = new TreeWalk(reader);
			walk.Reset();
			walk.AddTree(a);
			walk.AddTree(b);
			walk.Recursive = true;
			TreeFilter filter = pathFilter;
			if (a is WorkingTreeIterator)
			{
				filter = AndTreeFilter.Create(filter, new NotIgnoredFilter(0));
			}
			if (b is WorkingTreeIterator)
			{
				filter = AndTreeFilter.Create(filter, new NotIgnoredFilter(1));
			}
			if (!(pathFilter is FollowFilter))
			{
				filter = AndTreeFilter.Create(filter, TreeFilter.ANY_DIFF);
			}
			walk.Filter = filter;
			source = new ContentSource.Pair(Source(a), Source(b));
			IList<DiffEntry> files = DiffEntry.Scan(walk);
			if (pathFilter is FollowFilter && IsAdd(files))
			{
				// The file we are following was added here, find where it
				// came from so we can properly show the rename or copy,
				// then continue digging backwards.
				//
				a.Reset();
				b.Reset();
				walk.Reset();
				walk.AddTree(a);
				walk.AddTree(b);
				filter = TreeFilter.ANY_DIFF;
				if (a is WorkingTreeIterator)
				{
					filter = AndTreeFilter.Create(new NotIgnoredFilter(0), filter);
				}
				if (b is WorkingTreeIterator)
				{
					filter = AndTreeFilter.Create(new NotIgnoredFilter(1), filter);
				}
				walk.Filter = filter;
				if (renameDetector == null)
				{
					SetDetectRenames(true);
				}
				files = UpdateFollowFilter(DetectRenames(DiffEntry.Scan(walk)));
			}
			else
			{
				if (renameDetector != null)
				{
					files = DetectRenames(files);
				}
			}
			return files;
		}

		private ContentSource Source(AbstractTreeIterator iterator)
		{
			if (iterator is WorkingTreeIterator)
			{
				return ContentSource.Create((WorkingTreeIterator)iterator);
			}
			return ContentSource.Create(reader);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IList<DiffEntry> DetectRenames(IList<DiffEntry> files)
		{
			renameDetector.Reset();
			renameDetector.AddAll(files);
			return renameDetector.Compute(reader, progressMonitor);
		}

		private bool IsAdd(IList<DiffEntry> files)
		{
			string oldPath = ((FollowFilter)pathFilter).GetPath();
			foreach (DiffEntry ent in files)
			{
				if (ent.GetChangeType() == DiffEntry.ChangeType.ADD && ent.GetNewPath().Equals(oldPath
					))
				{
					return true;
				}
			}
			return false;
		}

		private IList<DiffEntry> UpdateFollowFilter(IList<DiffEntry> files)
		{
			string oldPath = ((FollowFilter)pathFilter).GetPath();
			foreach (DiffEntry ent in files)
			{
				if (IsRename(ent) && ent.GetNewPath().Equals(oldPath))
				{
					pathFilter = FollowFilter.Create(ent.GetOldPath());
					return Sharpen.Collections.SingletonList(ent);
				}
			}
			return Sharpen.Collections.EmptyList<DiffEntry>();
		}

		private static bool IsRename(DiffEntry ent)
		{
			return ent.GetChangeType() == DiffEntry.ChangeType.RENAME || ent.GetChangeType() 
				== DiffEntry.ChangeType.COPY;
		}

		/// <summary>Format the differences between two trees.</summary>
		/// <remarks>
		/// Format the differences between two trees.
		/// The patch is expressed as instructions to modify
		/// <code>a</code>
		/// to make it
		/// <code>b</code>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <exception cref="System.IO.IOException">
		/// trees cannot be read, file contents cannot be read, or the
		/// patch cannot be output.
		/// </exception>
		public virtual void Format(AnyObjectId a, AnyObjectId b)
		{
			Format(Scan(a, b));
		}

		/// <summary>Format the differences between two trees.</summary>
		/// <remarks>
		/// Format the differences between two trees.
		/// The patch is expressed as instructions to modify
		/// <code>a</code>
		/// to make it
		/// <code>b</code>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <exception cref="System.IO.IOException">
		/// trees cannot be read, file contents cannot be read, or the
		/// patch cannot be output.
		/// </exception>
		public virtual void Format(RevTree a, RevTree b)
		{
			Format(Scan(a, b));
		}

		/// <summary>Format the differences between two trees.</summary>
		/// <remarks>
		/// Format the differences between two trees.
		/// The patch is expressed as instructions to modify
		/// <code>a</code>
		/// to make it
		/// <code>b</code>
		/// .
		/// </remarks>
		/// <param name="a">the old (or previous) side.</param>
		/// <param name="b">the new (or updated) side.</param>
		/// <exception cref="System.IO.IOException">
		/// trees cannot be read, file contents cannot be read, or the
		/// patch cannot be output.
		/// </exception>
		public virtual void Format(AbstractTreeIterator a, AbstractTreeIterator b)
		{
			Format(Scan(a, b));
		}

		/// <summary>Format a patch script from a list of difference entries.</summary>
		/// <remarks>Format a patch script from a list of difference entries.</remarks>
		/// <param name="entries">entries describing the affected files.</param>
		/// <exception cref="System.IO.IOException">
		/// a file's content cannot be read, or the output stream cannot
		/// be written to.
		/// </exception>
		public virtual void Format<_T0>(IList<_T0> entries) where _T0:DiffEntry
		{
			foreach (DiffEntry ent in entries)
			{
				Format(ent);
			}
		}

		/// <summary>Format a patch script for one file entry.</summary>
		/// <remarks>Format a patch script for one file entry.</remarks>
		/// <param name="ent">the entry to be formatted.</param>
		/// <exception cref="System.IO.IOException">
		/// a file's content cannot be read, or the output stream cannot
		/// be written to.
		/// </exception>
		public virtual void Format(DiffEntry ent)
		{
			DiffFormatter.FormatResult res = CreateFormatResult(ent);
			Format(res.header, res.a, res.b);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteGitLinkDiffText(OutputStream o, DiffEntry ent)
		{
			if (ent.GetOldMode() == FileMode.GITLINK)
			{
				o.Write(Constants.EncodeASCII("-Subproject commit " + ent.GetOldId().Name + "\n")
					);
			}
			if (ent.GetNewMode() == FileMode.GITLINK)
			{
				o.Write(Constants.EncodeASCII("+Subproject commit " + ent.GetNewId().Name + "\n")
					);
			}
		}

		private string Format(AbbreviatedObjectId id)
		{
			if (id.IsComplete && db != null)
			{
				try
				{
					id = reader.Abbreviate(id.ToObjectId(), abbreviationLength);
				}
				catch (IOException)
				{
				}
			}
			// Ignore this. We'll report the full identity.
			return id.Name;
		}

		private static string QuotePath(string name)
		{
			return QuotedString.GIT_PATH.Quote(name);
		}

		/// <summary>Format a patch script, reusing a previously parsed FileHeader.</summary>
		/// <remarks>
		/// Format a patch script, reusing a previously parsed FileHeader.
		/// <p>
		/// This formatter is primarily useful for editing an existing patch script
		/// to increase or reduce the number of lines of context within the script.
		/// All header lines are reused as-is from the supplied FileHeader.
		/// </remarks>
		/// <param name="head">existing file header containing the header lines to copy.</param>
		/// <param name="a">
		/// text source for the pre-image version of the content. This
		/// must match the content of
		/// <see cref="DiffEntry.GetOldId()">DiffEntry.GetOldId()</see>
		/// .
		/// </param>
		/// <param name="b">
		/// text source for the post-image version of the content. This
		/// must match the content of
		/// <see cref="DiffEntry.GetNewId()">DiffEntry.GetNewId()</see>
		/// .
		/// </param>
		/// <exception cref="System.IO.IOException">writing to the supplied stream failed.</exception>
		public virtual void Format(FileHeader head, RawText a, RawText b)
		{
			// Reuse the existing FileHeader as-is by blindly copying its
			// header lines, but avoiding its hunks. Instead we recreate
			// the hunks from the text instances we have been supplied.
			//
			int start = head.GetStartOffset();
			int end = head.GetEndOffset();
			if (!head.GetHunks().IsEmpty())
			{
				end = head.GetHunks()[0].GetStartOffset();
			}
			@out.Write(head.GetBuffer(), start, end - start);
			if (head.GetPatchType() == FileHeader.PatchType.UNIFIED)
			{
				Format(head.ToEditList(), a, b);
			}
		}

		/// <summary>Formats a list of edits in unified diff format</summary>
		/// <param name="edits">some differences which have been calculated between A and B</param>
		/// <param name="a">the text A which was compared</param>
		/// <param name="b">the text B which was compared</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Format(EditList edits, RawText a, RawText b)
		{
			for (int curIdx = 0; curIdx < edits.Count; )
			{
				Edit curEdit = edits[curIdx];
				int endIdx = FindCombinedEnd(edits, curIdx);
				Edit endEdit = edits[endIdx];
				int aCur = Math.Max(0, curEdit.GetBeginA() - context);
				int bCur = Math.Max(0, curEdit.GetBeginB() - context);
				int aEnd = Math.Min(a.Size(), endEdit.GetEndA() + context);
				int bEnd = Math.Min(b.Size(), endEdit.GetEndB() + context);
				WriteHunkHeader(aCur, aEnd, bCur, bEnd);
				while (aCur < aEnd || bCur < bEnd)
				{
					if (aCur < curEdit.GetBeginA() || endIdx + 1 < curIdx)
					{
						WriteContextLine(a, aCur);
						if (IsEndOfLineMissing(a, aCur))
						{
							@out.Write(noNewLine);
						}
						aCur++;
						bCur++;
					}
					else
					{
						if (aCur < curEdit.GetEndA())
						{
							WriteRemovedLine(a, aCur);
							if (IsEndOfLineMissing(a, aCur))
							{
								@out.Write(noNewLine);
							}
							aCur++;
						}
						else
						{
							if (bCur < curEdit.GetEndB())
							{
								WriteAddedLine(b, bCur);
								if (IsEndOfLineMissing(b, bCur))
								{
									@out.Write(noNewLine);
								}
								bCur++;
							}
						}
					}
					if (End(curEdit, aCur, bCur) && ++curIdx < edits.Count)
					{
						curEdit = edits[curIdx];
					}
				}
			}
		}

		/// <summary>Output a line of context (unmodified line).</summary>
		/// <remarks>Output a line of context (unmodified line).</remarks>
		/// <param name="text">RawText for accessing raw data</param>
		/// <param name="line">the line number within text</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual void WriteContextLine(RawText text, int line)
		{
			WriteLine(' ', text, line);
		}

		private bool IsEndOfLineMissing(RawText text, int line)
		{
			return line + 1 == text.Size() && text.IsMissingNewlineAtEnd();
		}

		/// <summary>Output an added line.</summary>
		/// <remarks>Output an added line.</remarks>
		/// <param name="text">RawText for accessing raw data</param>
		/// <param name="line">the line number within text</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual void WriteAddedLine(RawText text, int line)
		{
			WriteLine('+', text, line);
		}

		/// <summary>Output a removed line</summary>
		/// <param name="text">RawText for accessing raw data</param>
		/// <param name="line">the line number within text</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual void WriteRemovedLine(RawText text, int line)
		{
			WriteLine('-', text, line);
		}

		/// <summary>Output a hunk header</summary>
		/// <param name="aStartLine">within first source</param>
		/// <param name="aEndLine">within first source</param>
		/// <param name="bStartLine">within second source</param>
		/// <param name="bEndLine">within second source</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual void WriteHunkHeader(int aStartLine, int aEndLine, int
			 bStartLine, int bEndLine)
		{
			@out.Write('@');
			@out.Write('@');
			WriteRange('-', aStartLine + 1, aEndLine - aStartLine);
			WriteRange('+', bStartLine + 1, bEndLine - bStartLine);
			@out.Write(' ');
			@out.Write('@');
			@out.Write('@');
			@out.Write('\n');
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteRange(char prefix, int begin, int cnt)
		{
			@out.Write(' ');
			@out.Write(prefix);
			switch (cnt)
			{
				case 0:
				{
					// If the range is empty, its beginning number must be the
					// line just before the range, or 0 if the range is at the
					// start of the file stream. Here, begin is always 1 based,
					// so an empty file would produce "0,0".
					//
					@out.Write(Constants.EncodeASCII(begin - 1));
					@out.Write(',');
					@out.Write('0');
					break;
				}

				case 1:
				{
					// If the range is exactly one line, produce only the number.
					//
					@out.Write(Constants.EncodeASCII(begin));
					break;
				}

				default:
				{
					@out.Write(Constants.EncodeASCII(begin));
					@out.Write(',');
					@out.Write(Constants.EncodeASCII(cnt));
					break;
					break;
				}
			}
		}

		/// <summary>Write a standard patch script line.</summary>
		/// <remarks>Write a standard patch script line.</remarks>
		/// <param name="prefix">prefix before the line, typically '-', '+', ' '.</param>
		/// <param name="text">the text object to obtain the line from.</param>
		/// <param name="cur">line number to output.</param>
		/// <exception cref="System.IO.IOException">the stream threw an exception while writing to it.
		/// 	</exception>
		protected internal virtual void WriteLine(char prefix, RawText text, int cur)
		{
			@out.Write(prefix);
			text.WriteLine(@out, cur);
			@out.Write('\n');
		}

		/// <summary>
		/// Creates a
		/// <see cref="NGit.Patch.FileHeader">NGit.Patch.FileHeader</see>
		/// representing the given
		/// <see cref="DiffEntry">DiffEntry</see>
		/// <p>
		/// This method does not use the OutputStream associated with this
		/// DiffFormatter instance. It is therefore safe to instantiate this
		/// DiffFormatter instance with a
		/// <see cref="NGit.Util.IO.DisabledOutputStream">NGit.Util.IO.DisabledOutputStream</see>
		/// if this method
		/// is the only one that will be used.
		/// </summary>
		/// <param name="ent">the DiffEntry to create the FileHeader for</param>
		/// <returns>
		/// a FileHeader representing the DiffEntry. The FileHeader's buffer
		/// will contain only the header of the diff output. It will also
		/// contain one
		/// <see cref="NGit.Patch.HunkHeader">NGit.Patch.HunkHeader</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the stream threw an exception while writing to it, or one of
		/// the blobs referenced by the DiffEntry could not be read.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">one of the blobs referenced by the DiffEntry is corrupt.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">one of the blobs referenced by the DiffEntry is missing.
		/// 	</exception>
		public virtual FileHeader ToFileHeader(DiffEntry ent)
		{
			return CreateFormatResult(ent).header;
		}

		private class FormatResult
		{
			internal FileHeader header;

			internal RawText a;

			internal RawText b;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		private DiffFormatter.FormatResult CreateFormatResult(DiffEntry ent)
		{
			DiffFormatter.FormatResult res = new DiffFormatter.FormatResult();
			ByteArrayOutputStream buf = new ByteArrayOutputStream();
			EditList editList;
			FileHeader.PatchType type;
			FormatHeader(buf, ent);
			if (ent.GetOldMode() == FileMode.GITLINK || ent.GetNewMode() == FileMode.GITLINK)
			{
				FormatOldNewPaths(buf, ent);
				WriteGitLinkDiffText(buf, ent);
				editList = new EditList();
				type = FileHeader.PatchType.UNIFIED;
			}
			else
			{
				AssertHaveRepository();
				byte[] aRaw = Open(DiffEntry.Side.OLD, ent);
				byte[] bRaw = Open(DiffEntry.Side.NEW, ent);
				if (aRaw == BINARY || bRaw == BINARY || RawText.IsBinary(aRaw) || RawText.IsBinary
					(bRaw))
				{
					//
					FormatOldNewPaths(buf, ent);
					buf.Write(Constants.EncodeASCII("Binary files differ\n"));
					editList = new EditList();
					type = FileHeader.PatchType.BINARY;
				}
				else
				{
					res.a = new RawText(aRaw);
					res.b = new RawText(bRaw);
					editList = Diff(res.a, res.b);
					type = FileHeader.PatchType.UNIFIED;
					switch (ent.GetChangeType())
					{
						case DiffEntry.ChangeType.RENAME:
						case DiffEntry.ChangeType.COPY:
						{
							if (!editList.IsEmpty())
							{
								FormatOldNewPaths(buf, ent);
							}
							break;
						}

						default:
						{
							FormatOldNewPaths(buf, ent);
							break;
							break;
						}
					}
				}
			}
			res.header = new FileHeader(buf.ToByteArray(), editList, type);
			return res;
		}

		private EditList Diff(RawText a, RawText b)
		{
			return diffAlgorithm.Diff(comparator, a, b);
		}

		private void AssertHaveRepository()
		{
			if (db == null)
			{
				throw new InvalidOperationException(JGitText.Get().repositoryIsRequired);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private byte[] Open(DiffEntry.Side side, DiffEntry entry)
		{
			if (entry.GetMode(side) == FileMode.MISSING)
			{
				return EMPTY;
			}
			if (entry.GetMode(side).GetObjectType() != Constants.OBJ_BLOB)
			{
				return EMPTY;
			}
			if (IsBinary(entry.GetPath(side)))
			{
				return BINARY;
			}
			AbbreviatedObjectId id = entry.GetId(side);
			if (!id.IsComplete)
			{
				ICollection<ObjectId> ids = reader.Resolve(id);
				if (ids.Count == 1)
				{
					id = AbbreviatedObjectId.FromObjectId(ids.Iterator().Next());
					switch (side)
					{
						case DiffEntry.Side.OLD:
						{
							entry.oldId = id;
							break;
						}

						case DiffEntry.Side.NEW:
						{
							entry.newId = id;
							break;
						}
					}
				}
				else
				{
					if (ids.Count == 0)
					{
						throw new MissingObjectException(id, Constants.OBJ_BLOB);
					}
					else
					{
						throw new AmbiguousObjectException(id, ids);
					}
				}
			}
			try
			{
				ObjectLoader ldr = source.Open(side, entry);
				return ldr.GetBytes(binaryFileThreshold);
			}
			catch (LargeObjectException.ExceedsLimit)
			{
				return BINARY;
			}
			catch (LargeObjectException.ExceedsByteArrayLimit)
			{
				return BINARY;
			}
			catch (LargeObjectException.OutOfMemory)
			{
				return BINARY;
			}
			catch (LargeObjectException tooBig)
			{
				tooBig.SetObjectId(id.ToObjectId());
				throw;
			}
		}

		private bool IsBinary(string path)
		{
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FormatHeader(ByteArrayOutputStream o, DiffEntry ent)
		{
			DiffEntry.ChangeType type = ent.GetChangeType();
			string oldp = ent.GetOldPath();
			string newp = ent.GetNewPath();
			FileMode oldMode = ent.GetOldMode();
			FileMode newMode = ent.GetNewMode();
			o.Write(Constants.EncodeASCII("diff --git "));
			o.Write(Constants.Encode(QuotePath(oldPrefix + (type == DiffEntry.ChangeType.ADD ? 
				newp : oldp))));
			o.Write(' ');
			o.Write(Constants.Encode(QuotePath(newPrefix + (type == DiffEntry.ChangeType.DELETE
				 ? oldp : newp))));
			o.Write('\n');
			switch (type)
			{
				case DiffEntry.ChangeType.ADD:
				{
					o.Write(Constants.EncodeASCII("new file mode "));
					newMode.CopyTo(o);
					o.Write('\n');
					break;
				}

				case DiffEntry.ChangeType.DELETE:
				{
					o.Write(Constants.EncodeASCII("deleted file mode "));
					oldMode.CopyTo(o);
					o.Write('\n');
					break;
				}

				case DiffEntry.ChangeType.RENAME:
				{
					o.Write(Constants.EncodeASCII("similarity index " + ent.GetScore() + "%"));
					o.Write('\n');
					o.Write(Constants.Encode("rename from " + QuotePath(oldp)));
					o.Write('\n');
					o.Write(Constants.Encode("rename to " + QuotePath(newp)));
					o.Write('\n');
					break;
				}

				case DiffEntry.ChangeType.COPY:
				{
					o.Write(Constants.EncodeASCII("similarity index " + ent.GetScore() + "%"));
					o.Write('\n');
					o.Write(Constants.Encode("copy from " + QuotePath(oldp)));
					o.Write('\n');
					o.Write(Constants.Encode("copy to " + QuotePath(newp)));
					o.Write('\n');
					if (!oldMode.Equals(newMode))
					{
						o.Write(Constants.EncodeASCII("new file mode "));
						newMode.CopyTo(o);
						o.Write('\n');
					}
					break;
				}

				case DiffEntry.ChangeType.MODIFY:
				{
					if (0 < ent.GetScore())
					{
						o.Write(Constants.EncodeASCII("dissimilarity index " + (100 - ent.GetScore()) + "%"
							));
						o.Write('\n');
					}
					break;
				}
			}
			if ((type == DiffEntry.ChangeType.MODIFY || type == DiffEntry.ChangeType.RENAME) 
				&& !oldMode.Equals(newMode))
			{
				o.Write(Constants.EncodeASCII("old mode "));
				oldMode.CopyTo(o);
				o.Write('\n');
				o.Write(Constants.EncodeASCII("new mode "));
				newMode.CopyTo(o);
				o.Write('\n');
			}
			if (!ent.GetOldId().Equals(ent.GetNewId()))
			{
				o.Write(Constants.EncodeASCII("index " + Format(ent.GetOldId()) + ".." + Format(ent
					.GetNewId())));
				//
				//
				//
				if (oldMode.Equals(newMode))
				{
					o.Write(' ');
					newMode.CopyTo(o);
				}
				o.Write('\n');
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FormatOldNewPaths(ByteArrayOutputStream o, DiffEntry ent)
		{
			string oldp;
			string newp;
			switch (ent.GetChangeType())
			{
				case DiffEntry.ChangeType.ADD:
				{
					oldp = DiffEntry.DEV_NULL;
					newp = QuotePath(newPrefix + ent.GetNewPath());
					break;
				}

				case DiffEntry.ChangeType.DELETE:
				{
					oldp = QuotePath(oldPrefix + ent.GetOldPath());
					newp = DiffEntry.DEV_NULL;
					break;
				}

				default:
				{
					oldp = QuotePath(oldPrefix + ent.GetOldPath());
					newp = QuotePath(newPrefix + ent.GetNewPath());
					break;
					break;
				}
			}
			o.Write(Constants.Encode("--- " + oldp + "\n"));
			o.Write(Constants.Encode("+++ " + newp + "\n"));
		}

		private int FindCombinedEnd(IList<Edit> edits, int i)
		{
			int end = i + 1;
			while (end < edits.Count && (CombineA(edits, end) || CombineB(edits, end)))
			{
				end++;
			}
			return end - 1;
		}

		private bool CombineA(IList<Edit> e, int i)
		{
			return e[i].GetBeginA() - e[i - 1].GetEndA() <= 2 * context;
		}

		private bool CombineB(IList<Edit> e, int i)
		{
			return e[i].GetBeginB() - e[i - 1].GetEndB() <= 2 * context;
		}

		private static bool End(Edit edit, int a, int b)
		{
			return edit.GetEndA() <= a && edit.GetEndB() <= b;
		}
	}
}
