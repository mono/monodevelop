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
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Utility functions related to quoted string handling.</summary>
	/// <remarks>Utility functions related to quoted string handling.</remarks>
	public abstract class QuotedString
	{
		/// <summary>Quoting style that obeys the rules Git applies to file names</summary>
		public static readonly QuotedString.GitPathStyle GIT_PATH = new QuotedString.GitPathStyle
			();

		/// <summary>Quoting style used by the Bourne shell.</summary>
		/// <remarks>
		/// Quoting style used by the Bourne shell.
		/// <p>
		/// Quotes are unconditionally inserted during
		/// <see cref="Quote(string)">Quote(string)</see>
		/// . This
		/// protects shell meta-characters like <code>$</code> or <code>~</code> from
		/// being recognized as special.
		/// </remarks>
		public static readonly QuotedString.BourneStyle BOURNE = new QuotedString.BourneStyle
			();

		/// <summary>Bourne style, but permits <code>~user</code> at the start of the string.
		/// 	</summary>
		/// <remarks>Bourne style, but permits <code>~user</code> at the start of the string.
		/// 	</remarks>
		public static readonly QuotedString.BourneUserPathStyle BOURNE_USER_PATH = new QuotedString.BourneUserPathStyle
			();

		/// <summary>Quote an input string by the quoting rules.</summary>
		/// <remarks>
		/// Quote an input string by the quoting rules.
		/// <p>
		/// If the input string does not require any quoting, the same String
		/// reference is returned to the caller.
		/// <p>
		/// Otherwise a quoted string is returned, including the opening and closing
		/// quotation marks at the start and end of the string. If the style does not
		/// permit raw Unicode characters then the string will first be encoded in
		/// UTF-8, with unprintable sequences possibly escaped by the rules.
		/// </remarks>
		/// <param name="in">any non-null Unicode string.</param>
		/// <returns>a quoted string. See above for details.</returns>
		public abstract string Quote(string @in);

		/// <summary>Clean a previously quoted input, decoding the result via UTF-8.</summary>
		/// <remarks>
		/// Clean a previously quoted input, decoding the result via UTF-8.
		/// <p>
		/// This method must match quote such that:
		/// <pre>
		/// a.equals(dequote(quote(a)));
		/// </pre>
		/// is true for any <code>a</code>.
		/// </remarks>
		/// <param name="in">a Unicode string to remove quoting from.</param>
		/// <returns>the cleaned string.</returns>
		/// <seealso cref="Dequote(byte[], int, int)">Dequote(byte[], int, int)</seealso>
		public virtual string Dequote(string @in)
		{
			byte[] b = Constants.Encode(@in);
			return Dequote(b, 0, b.Length);
		}

		/// <summary>Decode a previously quoted input, scanning a UTF-8 encoded buffer.</summary>
		/// <remarks>
		/// Decode a previously quoted input, scanning a UTF-8 encoded buffer.
		/// <p>
		/// This method must match quote such that:
		/// <pre>
		/// a.equals(dequote(Constants.encode(quote(a))));
		/// </pre>
		/// is true for any <code>a</code>.
		/// <p>
		/// This method removes any opening/closing quotation marks added by
		/// <see cref="Quote(string)">Quote(string)</see>
		/// .
		/// </remarks>
		/// <param name="in">the input buffer to parse.</param>
		/// <param name="offset">first position within <code>in</code> to scan.</param>
		/// <param name="end">one position past in <code>in</code> to scan.</param>
		/// <returns>the cleaned string.</returns>
		public abstract string Dequote(byte[] @in, int offset, int end);

		/// <summary>Quoting style used by the Bourne shell.</summary>
		/// <remarks>
		/// Quoting style used by the Bourne shell.
		/// <p>
		/// Quotes are unconditionally inserted during
		/// <see cref="Quote(string)">Quote(string)</see>
		/// . This
		/// protects shell meta-characters like <code>$</code> or <code>~</code> from
		/// being recognized as special.
		/// </remarks>
		public class BourneStyle : QuotedString
		{
			public override string Quote(string @in)
			{
				StringBuilder r = new StringBuilder();
				r.Append('\'');
				int start = 0;
				int i = 0;
				for (; i < @in.Length; i++)
				{
					switch (@in[i])
					{
						case '\'':
						case '!':
						{
							r.AppendRange(@in, start, i);
							r.Append('\'');
							r.Append('\\');
							r.Append(@in[i]);
							r.Append('\'');
							start = i + 1;
							break;
						}
					}
				}
				r.AppendRange(@in, start, i);
				r.Append('\'');
				return r.ToString();
			}

			public override string Dequote(byte[] @in, int ip, int ie)
			{
				bool inquote = false;
				byte[] r = new byte[ie - ip];
				int rPtr = 0;
				while (ip < ie)
				{
					byte b = @in[ip++];
					switch (b)
					{
						case (byte)('\''):
						{
							inquote = !inquote;
							continue;
							goto case (byte)('\\');
						}

						case (byte)('\\'):
						{
							if (inquote || ip == ie)
							{
								r[rPtr++] = b;
							}
							else
							{
								// literal within a quote
								r[rPtr++] = @in[ip++];
							}
							continue;
							goto default;
						}

						default:
						{
							r[rPtr++] = b;
							continue;
							break;
						}
					}
				}
				return RawParseUtils.Decode(Constants.CHARSET, r, 0, rPtr);
			}
		}

		/// <summary>Bourne style, but permits <code>~user</code> at the start of the string.
		/// 	</summary>
		/// <remarks>Bourne style, but permits <code>~user</code> at the start of the string.
		/// 	</remarks>
		public class BourneUserPathStyle : QuotedString.BourneStyle
		{
			public override string Quote(string @in)
			{
				if (@in.Matches("^~[A-Za-z0-9_-]+$"))
				{
					// If the string is just "~user" we can assume they
					// mean "~user/".
					//
					return @in + "/";
				}
				if (@in.Matches("^~[A-Za-z0-9_-]*/.*$"))
				{
					// If the string is of "~/path" or "~user/path"
					// we must not escape ~/ or ~user/ from the shell.
					//
					int i = @in.IndexOf('/') + 1;
					if (i == @in.Length)
					{
						return @in;
					}
					return Sharpen.Runtime.Substring(@in, 0, i) + base.Quote(Sharpen.Runtime.Substring
						(@in, i));
				}
				return base.Quote(@in);
			}
		}

		/// <summary>Quoting style that obeys the rules Git applies to file names</summary>
		public sealed class GitPathStyle : QuotedString
		{
			private static readonly byte[] quote;

			static GitPathStyle()
			{
				quote = new byte[128];
				Arrays.Fill(quote, unchecked((byte)-1));
				for (int i = '0'; i <= '9'; i++)
				{
					quote[i] = 0;
				}
				for (int i_1 = 'a'; i_1 <= 'z'; i_1++)
				{
					quote[i_1] = 0;
				}
				for (int i_2 = 'A'; i_2 <= 'Z'; i_2++)
				{
					quote[i_2] = 0;
				}
				quote[(byte)(' ')] = 0;
				quote[(byte)('$')] = 0;
				quote[(byte)('%')] = 0;
				quote[(byte)('&')] = 0;
				quote[(byte)('*')] = 0;
				quote[(byte)('+')] = 0;
				quote[(byte)(',')] = 0;
				quote[(byte)('-')] = 0;
				quote[(byte)('.')] = 0;
				quote[(byte)('/')] = 0;
				quote[(byte)(':')] = 0;
				quote[(byte)(';')] = 0;
				quote[(byte)('=')] = 0;
				quote[(byte)('?')] = 0;
				quote[(byte)('@')] = 0;
				quote[(byte)('_')] = 0;
				quote[(byte)('^')] = 0;
				quote[(byte)('|')] = 0;
				quote[(byte)('~')] = 0;
				quote[(byte)('\u0007')] = (byte)('a');
				quote[(byte)('\b')] = (byte)('b');
				quote[(byte)('\f')] = (byte)('f');
				quote[(byte)('\n')] = (byte)('n');
				quote[(byte)('\r')] = (byte)('r');
				quote[(byte)('\t')] = (byte)('t');
				quote[(byte)('\u000B')] = (byte)('v');
				quote[(byte)('\\')] = (byte)('\\');
				quote[(byte)('"')] = (byte)('"');
			}

			public override string Quote(string instr)
			{
				if (instr.Length == 0)
				{
					return "\"\"";
				}
				bool reuse = true;
				byte[] @in = Constants.Encode(instr);
				StringBuilder r = new StringBuilder(2 + @in.Length);
				r.Append('"');
				for (int i = 0; i < @in.Length; i++)
				{
					int c = @in[i] & unchecked((int)(0xff));
					if (c < quote.Length)
					{
						byte style = quote[c];
						if (style == 0)
						{
							r.Append((char)c);
							continue;
						}
						if (style > 0)
						{
							reuse = false;
							r.Append('\\');
							r.Append((char)style);
							continue;
						}
					}
					reuse = false;
					r.Append('\\');
					r.Append((char)(((c >> 6) & 0x3) + '0'));
					r.Append((char)(((c >> 3) & 0x7) + '0'));
					r.Append((char)(((c >> 0) & 0x7) + '0'));
				}
				if (reuse)
				{
					return instr;
				}
				r.Append('"');
				return r.ToString();
			}

			public override string Dequote(byte[] @in, int inPtr, int inEnd)
			{
				if (2 <= inEnd - inPtr && @in[inPtr] == '"' && @in[inEnd - 1] == '"')
				{
					return Dq(@in, inPtr + 1, inEnd - 1);
				}
				return RawParseUtils.Decode(Constants.CHARSET, @in, inPtr, inEnd);
			}

			private static string Dq(byte[] @in, int inPtr, int inEnd)
			{
				byte[] r = new byte[inEnd - inPtr];
				int rPtr = 0;
				while (inPtr < inEnd)
				{
					byte b = @in[inPtr++];
					if (b != '\\')
					{
						r[rPtr++] = b;
						continue;
					}
					if (inPtr == inEnd)
					{
						// Lone trailing backslash. Treat it as a literal.
						//
						r[rPtr++] = (byte)('\\');
						break;
					}
					switch (@in[inPtr++])
					{
						case (byte)('a'):
						{
							r[rPtr++] = unchecked((int)(0x07));
							continue;
							goto case (byte)('b');
						}

						case (byte)('b'):
						{
							r[rPtr++] = (byte)('\b');
							continue;
							goto case (byte)('f');
						}

						case (byte)('f'):
						{
							r[rPtr++] = (byte)('\f');
							continue;
							goto case (byte)('n');
						}

						case (byte)('n'):
						{
							r[rPtr++] = (byte)('\n');
							continue;
							goto case (byte)('r');
						}

						case (byte)('r'):
						{
							r[rPtr++] = (byte)('\r');
							continue;
							goto case (byte)('t');
						}

						case (byte)('t'):
						{
							r[rPtr++] = (byte)('\t');
							continue;
							goto case (byte)('v');
						}

						case (byte)('v'):
						{
							r[rPtr++] = unchecked((int)(0x0B));
							continue;
							goto case (byte)('\\');
						}

						case (byte)('\\'):
						case (byte)('"'):
						{
							r[rPtr++] = @in[inPtr - 1];
							continue;
							goto case (byte)('0');
						}

						case (byte)('0'):
						case (byte)('1'):
						case (byte)('2'):
						case (byte)('3'):
						{
							int cp = @in[inPtr - 1] - '0';
							for (int n = 1; n < 3 && inPtr < inEnd; n++)
							{
								byte c = @in[inPtr];
								if ('0' <= c && ((sbyte)c) <= '7')
								{
									cp <<= 3;
									cp |= c - '0';
									inPtr++;
								}
								else
								{
									break;
								}
							}
							r[rPtr++] = unchecked((byte)cp);
							continue;
							goto default;
						}

						default:
						{
							// Any other code is taken literally.
							//
							r[rPtr++] = (byte)('\\');
							r[rPtr++] = @in[inPtr - 1];
							continue;
							break;
						}
					}
				}
				return RawParseUtils.Decode(Constants.CHARSET, r, 0, rPtr);
			}

			public GitPathStyle()
			{
			}
			// Singleton
		}
	}
}
