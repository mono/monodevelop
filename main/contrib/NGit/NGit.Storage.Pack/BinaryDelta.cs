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
using System.Text;
using NGit;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>Recreate a stream from a base stream and a GIT pack delta.</summary>
	/// <remarks>
	/// Recreate a stream from a base stream and a GIT pack delta.
	/// <p>
	/// This entire class is heavily cribbed from <code>patch-delta.c</code> in the
	/// GIT project. The original delta patching code was written by Nicolas Pitre
	/// (&lt;nico@cam.org&gt;).
	/// </p>
	/// </remarks>
	public class BinaryDelta
	{
		/// <summary>Length of the base object in the delta stream.</summary>
		/// <remarks>Length of the base object in the delta stream.</remarks>
		/// <param name="delta">the delta stream, or at least the header of it.</param>
		/// <returns>the base object's size.</returns>
		public static long GetBaseSize(byte[] delta)
		{
			int p = 0;
			long baseLen = 0;
			int c;
			int shift = 0;
			do
			{
				c = delta[p++] & unchecked((int)(0xff));
				baseLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			return baseLen;
		}

		/// <summary>Length of the resulting object in the delta stream.</summary>
		/// <remarks>Length of the resulting object in the delta stream.</remarks>
		/// <param name="delta">the delta stream, or at least the header of it.</param>
		/// <returns>the resulting object's size.</returns>
		public static long GetResultSize(byte[] delta)
		{
			int p = 0;
			// Skip length of the base object.
			//
			int c;
			do
			{
				c = delta[p++] & unchecked((int)(0xff));
			}
			while ((c & unchecked((int)(0x80))) != 0);
			long resLen = 0;
			int shift = 0;
			do
			{
				c = delta[p++] & unchecked((int)(0xff));
				resLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			return resLen;
		}

		/// <summary>
		/// Apply the changes defined by delta to the data in base, yielding a new
		/// array of bytes.
		/// </summary>
		/// <remarks>
		/// Apply the changes defined by delta to the data in base, yielding a new
		/// array of bytes.
		/// </remarks>
		/// <param name="base">some byte representing an object of some kind.</param>
		/// <param name="delta">
		/// a git pack delta defining the transform from one version to
		/// another.
		/// </param>
		/// <returns>patched base</returns>
		public static byte[] Apply(byte[] @base, byte[] delta)
		{
			return Apply(@base, delta, null);
		}

		/// <summary>
		/// Apply the changes defined by delta to the data in base, yielding a new
		/// array of bytes.
		/// </summary>
		/// <remarks>
		/// Apply the changes defined by delta to the data in base, yielding a new
		/// array of bytes.
		/// </remarks>
		/// <param name="base">some byte representing an object of some kind.</param>
		/// <param name="delta">
		/// a git pack delta defining the transform from one version to
		/// another.
		/// </param>
		/// <param name="result">
		/// array to store the result into. If null the result will be
		/// allocated and returned.
		/// </param>
		/// <returns>
		/// either
		/// <code>result</code>
		/// , or the result array allocated.
		/// </returns>
		public static byte[] Apply(byte[] @base, byte[] delta, byte[] result)
		{
			int deltaPtr = 0;
			// Length of the base object (a variable length int).
			//
			int baseLen = 0;
			int c;
			int shift = 0;
			do
			{
				c = delta[deltaPtr++] & unchecked((int)(0xff));
				baseLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			if (@base.Length != baseLen)
			{
				throw new ArgumentException(JGitText.Get().baseLengthIncorrect);
			}
			// Length of the resulting object (a variable length int).
			//
			int resLen = 0;
			shift = 0;
			do
			{
				c = delta[deltaPtr++] & unchecked((int)(0xff));
				resLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			if (result == null)
			{
				result = new byte[resLen];
			}
			else
			{
				if (result.Length != resLen)
				{
					throw new ArgumentException(JGitText.Get().resultLengthIncorrect);
				}
			}
			int resultPtr = 0;
			while (deltaPtr < delta.Length)
			{
				int cmd = delta[deltaPtr++] & unchecked((int)(0xff));
				if ((cmd & unchecked((int)(0x80))) != 0)
				{
					// Determine the segment of the base which should
					// be copied into the output. The segment is given
					// as an offset and a length.
					//
					int copyOffset = 0;
					if ((cmd & unchecked((int)(0x01))) != 0)
					{
						copyOffset = delta[deltaPtr++] & unchecked((int)(0xff));
					}
					if ((cmd & unchecked((int)(0x02))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 8;
					}
					if ((cmd & unchecked((int)(0x04))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 16;
					}
					if ((cmd & unchecked((int)(0x08))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 24;
					}
					int copySize = 0;
					if ((cmd & unchecked((int)(0x10))) != 0)
					{
						copySize = delta[deltaPtr++] & unchecked((int)(0xff));
					}
					if ((cmd & unchecked((int)(0x20))) != 0)
					{
						copySize |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 8;
					}
					if ((cmd & unchecked((int)(0x40))) != 0)
					{
						copySize |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 16;
					}
					if (copySize == 0)
					{
						copySize = unchecked((int)(0x10000));
					}
					System.Array.Copy(@base, copyOffset, result, resultPtr, copySize);
					resultPtr += copySize;
				}
				else
				{
					if (cmd != 0)
					{
						// Anything else the data is literal within the delta
						// itself.
						//
						System.Array.Copy(delta, deltaPtr, result, resultPtr, cmd);
						deltaPtr += cmd;
						resultPtr += cmd;
					}
					else
					{
						// cmd == 0 has been reserved for future encoding but
						// for now its not acceptable.
						//
						throw new ArgumentException(JGitText.Get().unsupportedCommand0);
					}
				}
			}
			return result;
		}

		/// <summary>Format this delta as a human readable string.</summary>
		/// <remarks>Format this delta as a human readable string.</remarks>
		/// <param name="delta">the delta instruction sequence to format.</param>
		/// <returns>the formatted delta.</returns>
		public static string Format(byte[] delta)
		{
			return Format(delta, true);
		}

		/// <summary>Format this delta as a human readable string.</summary>
		/// <remarks>Format this delta as a human readable string.</remarks>
		/// <param name="delta">the delta instruction sequence to format.</param>
		/// <param name="includeHeader">
		/// true if the header (base size and result size) should be
		/// included in the formatting.
		/// </param>
		/// <returns>the formatted delta.</returns>
		public static string Format(byte[] delta, bool includeHeader)
		{
			StringBuilder r = new StringBuilder();
			int deltaPtr = 0;
			long baseLen = 0;
			int c;
			int shift = 0;
			do
			{
				c = delta[deltaPtr++] & unchecked((int)(0xff));
				baseLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			long resLen = 0;
			shift = 0;
			do
			{
				c = delta[deltaPtr++] & unchecked((int)(0xff));
				resLen |= (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			while ((c & unchecked((int)(0x80))) != 0);
			if (includeHeader)
			{
				r.Append("DELTA( BASE=" + baseLen + " RESULT=" + resLen + " )\n");
			}
			while (deltaPtr < delta.Length)
			{
				int cmd = delta[deltaPtr++] & unchecked((int)(0xff));
				if ((cmd & unchecked((int)(0x80))) != 0)
				{
					// Determine the segment of the base which should
					// be copied into the output. The segment is given
					// as an offset and a length.
					//
					int copyOffset = 0;
					if ((cmd & unchecked((int)(0x01))) != 0)
					{
						copyOffset = delta[deltaPtr++] & unchecked((int)(0xff));
					}
					if ((cmd & unchecked((int)(0x02))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 8;
					}
					if ((cmd & unchecked((int)(0x04))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 16;
					}
					if ((cmd & unchecked((int)(0x08))) != 0)
					{
						copyOffset |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 24;
					}
					int copySize = 0;
					if ((cmd & unchecked((int)(0x10))) != 0)
					{
						copySize = delta[deltaPtr++] & unchecked((int)(0xff));
					}
					if ((cmd & unchecked((int)(0x20))) != 0)
					{
						copySize |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 8;
					}
					if ((cmd & unchecked((int)(0x40))) != 0)
					{
						copySize |= (delta[deltaPtr++] & unchecked((int)(0xff))) << 16;
					}
					if (copySize == 0)
					{
						copySize = unchecked((int)(0x10000));
					}
					r.Append("  COPY  (" + copyOffset + ", " + copySize + ")\n");
				}
				else
				{
					if (cmd != 0)
					{
						// Anything else the data is literal within the delta
						// itself.
						//
						r.Append("  INSERT(");
						r.Append(QuotedString.GIT_PATH.Quote(RawParseUtils.Decode(delta, deltaPtr, deltaPtr
							 + cmd)));
						r.Append(")\n");
						deltaPtr += cmd;
					}
					else
					{
						// cmd == 0 has been reserved for future encoding but
						// for now its not acceptable.
						//
						throw new ArgumentException(JGitText.Get().unsupportedCommand0);
					}
				}
			}
			return r.ToString();
		}
	}
}
