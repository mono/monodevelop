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
using NGit;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>
	/// A parsed collection of
	/// <see cref="FileHeader">FileHeader</see>
	/// s from a unified diff patch file
	/// </summary>
	public class Patch
	{
		internal static readonly byte[] DIFF_GIT = Constants.EncodeASCII("diff --git ");

		private static readonly byte[] DIFF_CC = Constants.EncodeASCII("diff --cc ");

		private static readonly byte[] DIFF_COMBINED = Constants.EncodeASCII("diff --combined "
			);

		private static readonly byte[][] BIN_HEADERS = new byte[][] { Constants.EncodeASCII
			("Binary files "), Constants.EncodeASCII("Files ") };

		private static readonly byte[] BIN_TRAILER = Constants.EncodeASCII(" differ\n");

		private static readonly byte[] GIT_BINARY = Constants.EncodeASCII("GIT binary patch\n"
			);

		internal static readonly byte[] SIG_FOOTER = Constants.EncodeASCII("-- \n");

		/// <summary>The files, in the order they were parsed out of the input.</summary>
		/// <remarks>The files, in the order they were parsed out of the input.</remarks>
		private readonly IList<FileHeader> files;

		/// <summary>Formatting errors, if any were identified.</summary>
		/// <remarks>Formatting errors, if any were identified.</remarks>
		private readonly IList<FormatError> errors;

		/// <summary>Create an empty patch.</summary>
		/// <remarks>Create an empty patch.</remarks>
		public Patch()
		{
			files = new AList<FileHeader>();
			errors = new AList<FormatError>(0);
		}

		/// <summary>Add a single file to this patch.</summary>
		/// <remarks>
		/// Add a single file to this patch.
		/// <p>
		/// Typically files should be added by parsing the text through one of this
		/// class's parse methods.
		/// </remarks>
		/// <param name="fh">the header of the file.</param>
		public virtual void AddFile(FileHeader fh)
		{
			files.AddItem(fh);
		}

		/// <returns>list of files described in the patch, in occurrence order.</returns>
		public virtual IList<FileHeader> GetFiles()
		{
			return files;
		}

		/// <summary>Add a formatting error to this patch script.</summary>
		/// <remarks>Add a formatting error to this patch script.</remarks>
		/// <param name="err">the error description.</param>
		public virtual void AddError(FormatError err)
		{
			errors.AddItem(err);
		}

		/// <returns>collection of formatting errors, if any.</returns>
		public virtual IList<FormatError> GetErrors()
		{
			return errors;
		}

		/// <summary>Parse a patch received from an InputStream.</summary>
		/// <remarks>
		/// Parse a patch received from an InputStream.
		/// <p>
		/// Multiple parse calls on the same instance will concatenate the patch
		/// data, but each parse input must start with a valid file header (don't
		/// split a single file across parse calls).
		/// </remarks>
		/// <param name="is">
		/// the stream to read the patch data from. The stream is read
		/// until EOF is reached.
		/// </param>
		/// <exception cref="System.IO.IOException">there was an error reading from the input stream.
		/// 	</exception>
		public virtual void Parse(InputStream @is)
		{
			byte[] buf = ReadFully(@is);
			Parse(buf, 0, buf.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static byte[] ReadFully(InputStream @is)
		{
			TemporaryBuffer b = new TemporaryBuffer.LocalFile();
			try
			{
				b.Copy(@is);
				b.Close();
				return b.ToByteArray();
			}
			finally
			{
				b.Destroy();
			}
		}

		/// <summary>Parse a patch stored in a byte[].</summary>
		/// <remarks>
		/// Parse a patch stored in a byte[].
		/// <p>
		/// Multiple parse calls on the same instance will concatenate the patch
		/// data, but each parse input must start with a valid file header (don't
		/// split a single file across parse calls).
		/// </remarks>
		/// <param name="buf">the buffer to parse.</param>
		/// <param name="ptr">starting position to parse from.</param>
		/// <param name="end">
		/// 1 past the last position to end parsing. The total length to
		/// be parsed is <code>end - ptr</code>.
		/// </param>
		public virtual void Parse(byte[] buf, int ptr, int end)
		{
			while (ptr < end)
			{
				ptr = ParseFile(buf, ptr, end);
			}
		}

		private int ParseFile(byte[] buf, int c, int end)
		{
			while (c < end)
			{
				if (FileHeader.IsHunkHdr(buf, c, end) >= 1)
				{
					// If we find a disconnected hunk header we might
					// have missed a file header previously. The hunk
					// isn't valid without knowing where it comes from.
					//
					Error(buf, c, JGitText.Get().hunkDisconnectedFromFile);
					c = RawParseUtils.NextLF(buf, c);
					continue;
				}
				// Valid git style patch?
				//
				if (RawParseUtils.Match(buf, c, DIFF_GIT) >= 0)
				{
					return ParseDiffGit(buf, c, end);
				}
				if (RawParseUtils.Match(buf, c, DIFF_CC) >= 0)
				{
					return ParseDiffCombined(DIFF_CC, buf, c, end);
				}
				if (RawParseUtils.Match(buf, c, DIFF_COMBINED) >= 0)
				{
					return ParseDiffCombined(DIFF_COMBINED, buf, c, end);
				}
				// Junk between files? Leading junk? Traditional
				// (non-git generated) patch?
				//
				int n = RawParseUtils.NextLF(buf, c);
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
				if (RawParseUtils.Match(buf, c, FileHeader.OLD_NAME) >= 0 && RawParseUtils.Match(
					buf, n, FileHeader.NEW_NAME) >= 0)
				{
					// Probably a traditional patch. Ensure we have at least
					// a "@@ -0,0" smelling line next. We only check the "@@ -".
					//
					int f = RawParseUtils.NextLF(buf, n);
					if (f >= end)
					{
						return end;
					}
					if (FileHeader.IsHunkHdr(buf, f, end) == 1)
					{
						return ParseTraditionalPatch(buf, c, end);
					}
				}
				c = n;
			}
			return c;
		}

		private int ParseDiffGit(byte[] buf, int start, int end)
		{
			FileHeader fh = new FileHeader(buf, start);
			int ptr = fh.ParseGitFileName(start + DIFF_GIT.Length, end);
			if (ptr < 0)
			{
				return SkipFile(buf, start);
			}
			ptr = fh.ParseGitHeaders(ptr, end);
			ptr = ParseHunks(fh, ptr, end);
			fh.endOffset = ptr;
			AddFile(fh);
			return ptr;
		}

		private int ParseDiffCombined(byte[] hdr, byte[] buf, int start, int end)
		{
			CombinedFileHeader fh = new CombinedFileHeader(buf, start);
			int ptr = fh.ParseGitFileName(start + hdr.Length, end);
			if (ptr < 0)
			{
				return SkipFile(buf, start);
			}
			ptr = fh.ParseGitHeaders(ptr, end);
			ptr = ParseHunks(fh, ptr, end);
			fh.endOffset = ptr;
			AddFile(fh);
			return ptr;
		}

		private int ParseTraditionalPatch(byte[] buf, int start, int end)
		{
			FileHeader fh = new FileHeader(buf, start);
			int ptr = fh.ParseTraditionalHeaders(start, end);
			ptr = ParseHunks(fh, ptr, end);
			fh.endOffset = ptr;
			AddFile(fh);
			return ptr;
		}

		private static int SkipFile(byte[] buf, int ptr)
		{
			ptr = RawParseUtils.NextLF(buf, ptr);
			if (RawParseUtils.Match(buf, ptr, FileHeader.OLD_NAME) >= 0)
			{
				ptr = RawParseUtils.NextLF(buf, ptr);
			}
			return ptr;
		}

		private int ParseHunks(FileHeader fh, int c, int end)
		{
			byte[] buf = fh.buf;
			while (c < end)
			{
				// If we see a file header at this point, we have all of the
				// hunks for our current file. We should stop and report back
				// with this position so it can be parsed again later.
				//
				if (RawParseUtils.Match(buf, c, DIFF_GIT) >= 0)
				{
					break;
				}
				if (RawParseUtils.Match(buf, c, DIFF_CC) >= 0)
				{
					break;
				}
				if (RawParseUtils.Match(buf, c, DIFF_COMBINED) >= 0)
				{
					break;
				}
				if (RawParseUtils.Match(buf, c, FileHeader.OLD_NAME) >= 0)
				{
					break;
				}
				if (RawParseUtils.Match(buf, c, FileHeader.NEW_NAME) >= 0)
				{
					break;
				}
				if (FileHeader.IsHunkHdr(buf, c, end) == fh.GetParentCount())
				{
					HunkHeader h = fh.NewHunkHeader(c);
					h.ParseHeader();
					c = h.ParseBody(this, end);
					h.endOffset = c;
					fh.AddHunk(h);
					if (c < end)
					{
						switch (buf[c])
						{
							case (byte)('@'):
							case (byte)('d'):
							case (byte)('\n'):
							{
								break;
							}

							default:
							{
								if (RawParseUtils.Match(buf, c, SIG_FOOTER) < 0)
								{
									Warn(buf, c, JGitText.Get().unexpectedHunkTrailer);
								}
								break;
							}
						}
					}
					continue;
				}
				int eol = RawParseUtils.NextLF(buf, c);
				if (fh.GetHunks().IsEmpty() && RawParseUtils.Match(buf, c, GIT_BINARY) >= 0)
				{
					fh.patchType = FileHeader.PatchType.GIT_BINARY;
					return ParseGitBinary(fh, eol, end);
				}
				if (fh.GetHunks().IsEmpty() && BIN_TRAILER.Length < eol - c && RawParseUtils.Match
					(buf, eol - BIN_TRAILER.Length, BIN_TRAILER) >= 0 && MatchAny(buf, c, BIN_HEADERS
					))
				{
					// The patch is a binary file diff, with no deltas.
					//
					fh.patchType = FileHeader.PatchType.BINARY;
					return eol;
				}
				// Skip this line and move to the next. Its probably garbage
				// after the last hunk of a file.
				//
				c = eol;
			}
			if (fh.GetHunks().IsEmpty() && fh.GetPatchType() == FileHeader.PatchType.UNIFIED 
				&& !fh.HasMetaDataChanges())
			{
				// Hmm, an empty patch? If there is no metadata here we
				// really have a binary patch that we didn't notice above.
				//
				fh.patchType = FileHeader.PatchType.BINARY;
			}
			return c;
		}

		private int ParseGitBinary(FileHeader fh, int c, int end)
		{
			BinaryHunk postImage = new BinaryHunk(fh, c);
			int nEnd = postImage.ParseHunk(c, end);
			if (nEnd < 0)
			{
				// Not a binary hunk.
				//
				Error(fh.buf, c, JGitText.Get().missingForwardImageInGITBinaryPatch);
				return c;
			}
			c = nEnd;
			postImage.endOffset = c;
			fh.forwardBinaryHunk = postImage;
			BinaryHunk preImage = new BinaryHunk(fh, c);
			int oEnd = preImage.ParseHunk(c, end);
			if (oEnd >= 0)
			{
				c = oEnd;
				preImage.endOffset = c;
				fh.reverseBinaryHunk = preImage;
			}
			return c;
		}

		internal virtual void Warn(byte[] buf, int ptr, string msg)
		{
			AddError(new FormatError(buf, ptr, FormatError.Severity.WARNING, msg));
		}

		internal virtual void Error(byte[] buf, int ptr, string msg)
		{
			AddError(new FormatError(buf, ptr, FormatError.Severity.ERROR, msg));
		}

		private static bool MatchAny(byte[] buf, int c, byte[][] srcs)
		{
			foreach (byte[] s in srcs)
			{
				if (RawParseUtils.Match(buf, c, s) >= 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
