/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core.Patch
{
	/// <summary>
	/// A parsed collection of <seealso cref="FileHeader"/>s from a unified diff patch file.
	/// </summary>
	[Serializable]
	public class Patch
	{
		private static readonly byte[] DiffGit = Constants.encodeASCII("diff --git ");
		private static readonly byte[] DiffCc = Constants.encodeASCII("diff --cc ");
		private static readonly byte[] DiffCombined = Constants.encodeASCII("diff --combined ");
		private static readonly byte[][] BinHeaders = new[] { Constants.encodeASCII("Binary files "), Constants.encodeASCII("Files ") };
		private static readonly byte[] BinTrailer = Constants.encodeASCII(" differ\n");
		private static readonly byte[] GitBinary = Constants.encodeASCII("GIT binary patch\n");

		public static readonly byte[] SigFooter = Constants.encodeASCII("-- \n");

		// The files, in the order they were parsed out of the input.
		private readonly List<FileHeader> _files;

		// Formatting errors, if any were identified.
		private readonly List<FormatError> _errors;

		/// <summary>
		/// Create an empty patch.
		/// </summary>
		public Patch()
		{
			_files = new List<FileHeader>();
			_errors = new List<FormatError>(0);
		}

		/**
		 * Add a single file to this patch.
		 * <para />
		 * Typically files should be added by parsing the text through one of this
		 * class's parse methods.
		 *
		 * @param fh
		 *            the header of the file.
		 */
		public void addFile(FileHeader fh)
		{
			_files.Add(fh);
		}

		/** @return list of files described in the patch, in occurrence order. */
		public List<FileHeader> getFiles()
		{
			return _files;
		}

		/**
		 * Add a formatting error to this patch script.
		 *
		 * @param err
		 *            the error description.
		 */
		public void addError(FormatError err)
		{
			_errors.Add(err);
		}

		/** @return collection of formatting errors, if any. */
		public List<FormatError> getErrors()
		{
			return _errors;
		}

		/**
		 * Parse a patch received from an InputStream.
		 * <para />
		 * Multiple parse calls on the same instance will concatenate the patch
		 * data, but each parse input must start with a valid file header (don't
		 * split a single file across parse calls).
		 *
		 * @param is
		 *            the stream to Read the patch data from. The stream is Read
		 *            until EOF is reached.
		 * @throws IOException
		 *             there was an error reading from the input stream.
		 */
		public void parse(Stream iStream)
		{
			byte[] buf = ReadFully(iStream);
			parse(buf, 0, buf.Length);
		}

		private static byte[] ReadFully(Stream stream)
		{
			var b = new LocalFileBuffer();
			try
			{
				b.copy(stream);
				b.close();
				return b.ToArray();
			}
			finally
			{
				b.destroy();
			}
		}

		/**
		 * Parse a patch stored in a byte[].
		 * <para />
		 * Multiple parse calls on the same instance will concatenate the patch
		 * data, but each parse input must start with a valid file header (don't
		 * split a single file across parse calls).
		 *
		 * @param buf
		 *            the buffer to parse.
		 * @param ptr
		 *            starting position to parse from.
		 * @param end
		 *            1 past the last position to end parsing. The total length to
		 *            be parsed is <code>end - ptr</code>.
		 */
		public void parse(byte[] buf, int ptr, int end)
		{
			while (ptr < end)
			{
				ptr = ParseFile(buf, ptr, end);
			}
		}

		public void warn(byte[] buf, int ptr, string msg)
		{
			addError(new FormatError(buf, ptr, FormatError.Severity.WARNING, msg));
		}

		public void error(byte[] buf, int ptr, string msg)
		{
			addError(new FormatError(buf, ptr, FormatError.Severity.ERROR, msg));
		}

		private int ParseFile(byte[] buf, int c, int end)
		{
			while (c < end)
			{
				if (FileHeader.isHunkHdr(buf, c, end) >= 1)
				{
					// If we find a disconnected hunk header we might
					// have missed a file header previously. The hunk
					// isn't valid without knowing where it comes from.
					//
					error(buf, c, "Hunk disconnected from file");
					c = RawParseUtils.nextLF(buf, c);
					continue;
				}

				// Valid git style patch?
				//
				if (RawParseUtils.match(buf, c, DiffGit) >= 0)
				{
					return ParseDiffGit(buf, c, end);
				}
				if (RawParseUtils.match(buf, c, DiffCc) >= 0)
				{
					return ParseDiffCombined(DiffCc, buf, c, end);
				}
				if (RawParseUtils.match(buf, c, DiffCombined) >= 0)
				{
					return ParseDiffCombined(DiffCombined, buf, c, end);
				}

				// Junk between files? Leading junk? Traditional
				// (non-git generated) patch?
				//
				int n = RawParseUtils.nextLF(buf, c);
				if (n >= end)
				{
					// Patches cannot be only one line long. This must be
					// trailing junk that we should ignore.
					//
					return end;
				}

				if (n - c < 6)
				{
					// A valid header must be at least 6 bytes on the
					// first line, e.g. "--- a/b\n".
					//
					c = n;
					continue;
				}

				if (RawParseUtils.match(buf, c, FileHeader.OLD_NAME) >= 0 &&
					RawParseUtils.match(buf, n, FileHeader.NEW_NAME) >= 0)
				{
					// Probably a traditional patch. Ensure we have at least
					// a "@@ -0,0" smelling line next. We only check the "@@ -".
					//
					int f = RawParseUtils.nextLF(buf, n);
					if (f >= end)
						return end;
					if (FileHeader.isHunkHdr(buf, f, end) == 1)
						return ParseTraditionalPatch(buf, c, end);
				}

				c = n;
			}
			return c;
		}

		private int ParseDiffGit(byte[] buf, int start, int end)
		{
			var fileHeader = new FileHeader(buf, start);
			int ptr = fileHeader.parseGitFileName(start + DiffGit.Length, end);
			if (ptr < 0)
			{
				return SkipFile(buf, start);
			}

			ptr = fileHeader.parseGitHeaders(ptr, end);
			ptr = ParseHunks(fileHeader, ptr, end);
			fileHeader.EndOffset = ptr;
			addFile(fileHeader);
			return ptr;
		}

		private int ParseDiffCombined(ICollection<byte> hdr, byte[] buf, int start, int end)
		{
			var fh = new CombinedFileHeader(buf, start);
			int ptr = fh.parseGitFileName(start + hdr.Count, end);
			if (ptr < 0)
			{
				return SkipFile(buf, start);
			}

			ptr = fh.parseGitHeaders(ptr, end);
			ptr = ParseHunks(fh, ptr, end);
			fh.EndOffset = ptr;
			addFile(fh);
			return ptr;
		}

		private int ParseTraditionalPatch(byte[] buf, int start, int end)
		{
			var fh = new FileHeader(buf, start);
			int ptr = fh.parseTraditionalHeaders(start, end);
			ptr = ParseHunks(fh, ptr, end);
			fh.EndOffset = ptr;
			addFile(fh);
			return ptr;
		}

		private static int SkipFile(byte[] buf, int ptr)
		{
			ptr = RawParseUtils.nextLF(buf, ptr);
			if (RawParseUtils.match(buf, ptr, FileHeader.OLD_NAME) >= 0)
			{
				ptr = RawParseUtils.nextLF(buf, ptr);
			}
			return ptr;
		}

		private int ParseHunks(FileHeader fh, int c, int end)
		{
			byte[] buf = fh.Buffer;
			while (c < end)
			{
				// If we see a file header at this point, we have all of the
				// hunks for our current file. We should stop and report back
				// with this position so it can be parsed again later.
				//
				if (RawParseUtils.match(buf, c, DiffGit) >= 0)
					break;
				if (RawParseUtils.match(buf, c, DiffCc) >= 0)
					break;
				if (RawParseUtils.match(buf, c, DiffCombined) >= 0)
					break;
				if (RawParseUtils.match(buf, c, FileHeader.OLD_NAME) >= 0)
					break;
				if (RawParseUtils.match(buf, c, FileHeader.NEW_NAME) >= 0)
					break;

				if (FileHeader.isHunkHdr(buf, c, end) == fh.ParentCount)
				{
					HunkHeader h = fh.newHunkHeader(c);
					h.parseHeader();
					c = h.parseBody(this, end);
					h.EndOffset = c;
					fh.addHunk(h);
					if (c < end)
					{
						switch (buf[c])
						{
							case (byte)'@':
							case (byte)'d':
							case (byte)'\n':
								break;

							default:
								if (RawParseUtils.match(buf, c, SigFooter) < 0)
									warn(buf, c, "Unexpected hunk trailer");
								break;
						}
					}
					continue;
				}

				int eol = RawParseUtils.nextLF(buf, c);
				if (fh.Hunks.isEmpty() && RawParseUtils.match(buf, c, GitBinary) >= 0)
				{
					fh.PatchType = FileHeader.PatchTypeEnum.GIT_BINARY;
					return ParseGitBinary(fh, eol, end);
				}

				if (fh.Hunks.isEmpty() && BinTrailer.Length < eol - c
						&& RawParseUtils.match(buf, eol - BinTrailer.Length, BinTrailer) >= 0
						&& MatchAny(buf, c, BinHeaders))
				{
					// The patch is a binary file diff, with no deltas.
					//
					fh.PatchType = FileHeader.PatchTypeEnum.BINARY;
					return eol;
				}

				// Skip this line and move to the next. Its probably garbage
				// After the last hunk of a file.
				//
				c = eol;
			}

			if (fh.Hunks.isEmpty()
					&& fh.getPatchType() == FileHeader.PatchTypeEnum.UNIFIED
					&& !fh.hasMetaDataChanges())
			{
				// Hmm, an empty patch? If there is no metadata here we
				// really have a binary patch that we didn't notice above.
				//
				fh.PatchType = FileHeader.PatchTypeEnum.BINARY;
			}

			return c;
		}

		private int ParseGitBinary(FileHeader fh, int c, int end)
		{
			var postImage = new BinaryHunk(fh, c);
			int nEnd = postImage.parseHunk(c, end);
			if (nEnd < 0)
			{
				// Not a binary hunk.
				//
				error(fh.Buffer, c, "Missing forward-image in GIT binary patch");
				return c;
			}
			c = nEnd;
			postImage.endOffset = c;
			fh.ForwardBinaryHunk = postImage;

			var preImage = new BinaryHunk(fh, c);
			int oEnd = preImage.parseHunk(c, end);
			if (oEnd >= 0)
			{
				c = oEnd;
				preImage.endOffset = c;
				fh.ReverseBinaryHunk = preImage;
			}

			return c;
		}

		private static bool MatchAny(byte[] buf, int c, IEnumerable<byte[]> srcs)
		{
			foreach (byte[] s in srcs)
			{
				if (RawParseUtils.match(buf, c, s) >= 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}