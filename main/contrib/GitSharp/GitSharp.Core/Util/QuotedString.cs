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

using GitSharp;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace GitSharp.Core.Util
{
	/// <summary>
	/// Utility functions related to quoted string handling.
	/// </summary>
    public abstract class QuotedString
    {
		/// <summary>
		/// Quoting style that obeys the rules Git applies to file names.
		/// </summary>
		public static readonly GitPathStyle GIT_PATH = new GitPathStyle();

		/// <summary>
		/// Quoting style used by the Bourne shell.
		/// <para />
		/// Quotes are unconditionally inserted during <see cref="quote(string)"/>. This
		/// protects shell meta-characters like <code>$</code> or <code>~</code> from
		/// being recognized as special.
		/// </summary>
	    public static readonly BourneStyle BOURNE = new BourneStyle();

		/// <summary>
		/// Bourne style, but permits <code>~user</code> at the start of the string.
		/// </summary>
	    public static readonly BourneUserPathStyle BOURNE_USER_PATH = new BourneUserPathStyle();

		/// <summary>
		/// Quote an input string by the quoting rules.
		/// <para />
		/// If the input string does not require any quoting, the same String
		/// reference is returned to the caller.
		/// <para />
		/// Otherwise a quoted string is returned, including the opening and closing
		/// quotation marks at the start and end of the string. If the style does not
		/// permit raw Unicode characters then the string will first be encoded in
		/// UTF-8, with unprintable sequences possibly escaped by the rules.
		/// </summary>
        /// <param name="instr">any non-null Unicode string</param>
		/// <returns>a quoted <see cref="string"/>. See above for details.</returns>
	    public abstract string quote(string instr);

		/// <summary>
		/// Clean a previously quoted input, decoding the result via UTF-8.
		/// <para />
		/// This method must match quote such that:
		/// <para />
		/// <example>
		/// a.Equals(qequote(quote(a)));
		/// </example>
		/// is true for any <code>a</code>.
		/// </summary>
		/// <param name="instr">a Unicode string to remove quoting from.</param>
		/// <returns>the cleaned string.</returns>
		/// <seealso cref="dequote(byte[], int, int)"/>
	    public string dequote(string instr)
        {
			if (instr == null)
				throw new ArgumentNullException ("instr");
			
		    byte[] b = Constants.encode(instr);
		    return dequote(b, 0, b.Length);
	    }

		/// <summary>
		/// Decode a previously quoted input, scanning a UTF-8 encoded buffer.
		/// <para />
		/// This method must match quote such that:
		/// <para />
		/// <example>
		/// a.Equals(Dequote(Constants.encode(quote(a))));
		/// </example>
		/// is true for any <code>a</code>.
		/// <para />
		/// This method removes any opening/closing quotation marks added by
		/// </summary>
		/// <param name="instr">
		/// The input buffer to parse.
		/// </param>
		/// <param name="offset">
		/// First position within <paramref name="instr"/> to scan.
		/// </param>
		/// <param name="end">
		/// One position past in <paramref name="instr"/> to scan.
		/// </param>
		/// <returns>The cleaned string.</returns>
		/// <seealso cref="quote(string)"/>.
	    public abstract string dequote(byte[] instr, int offset, int end);

		/// <summary>
		/// Quoting style used by the Bourne shell.
		/// <para />
		/// Quotes are unconditionally inserted during <see cref="quote(string)"/>. This
		/// protects shell meta-characters like <code>$</code> or <code>~</code> from
		/// being recognized as special.
		/// </summary>
	    public class BourneStyle : QuotedString
        {
		    public override string quote(string instr)
            {
				if (instr == null)
					throw new ArgumentNullException ("instr");
			    StringBuilder r = new StringBuilder();
			    r.Append('\'');
			    int start = 0, i = 0;
			    for (; i < instr.Length; i++) {
				    switch (instr[i]) {
				    case '\'':
				    case '!':
					    r.Append(instr, start, i - start);
					    r.Append('\'');
					    r.Append('\\');
					    r.Append(instr[i]);
					    r.Append('\'');
					    start = i + 1;
					    break;
				    }
			    }

                r.Append(instr, start, i - start);
			    r.Append('\'');
			    return r.ToString();
		    }

		    public override string dequote(byte[] instr, int offset, int end)
            {
				if (instr==null)
					throw new ArgumentNullException("instr");
			    bool inquote = false;
			    byte[] r = new byte[end - offset];
			    int rPtr = 0;
			    while (offset < end)
                {
				    byte b = instr[offset++];
				    switch (b)
                    {
				    case (byte)'\'':
					    inquote = !inquote;
					    continue;
				    case (byte)'\\':
					    if (inquote || offset == end)
						    r[rPtr++] = b; // literal within a quote
					    else
						    r[rPtr++] = instr[offset++];
					    continue;
				    default:
					    r[rPtr++] = b;
					    continue;
				    }
			    }
			    return RawParseUtils.decode(Constants.CHARSET, r, 0, rPtr);
		    }
	    }

		/// <summary>
		/// Bourne style, but permits <code>~user</code> at the start of the string.
		/// </summary>
        public sealed class BourneUserPathStyle : BourneStyle
        {
		    public override string quote(string instr)
            {
                if (new Regex("^~[A-Za-z0-9_-]+$").IsMatch(instr))
                {
				    // If the string is just "~user" we can assume they
				    // mean "~user/".
				    //
				    return instr + "/";
			    }

			    if (new Regex("^~[A-Za-z0-9_-]*/.*$").IsMatch(instr))
                {
				    // If the string is of "~/path" or "~user/path"
				    // we must not escape ~/ or ~user/ from the shell.
				    //
				    int i = instr.IndexOf('/') + 1;
				    if (i == instr.Length)
					    return instr;
				    return instr.Slice(0, i) + base.quote(instr.Substring(i));
			    }

			    return base.quote(instr);
		    }
	    }

		/// <summary>
		/// Quoting style that obeys the rules Git applies to file names
		/// </summary>
	    public sealed class GitPathStyle : QuotedString
        {
		    private static readonly int[] quote_m;
		    
            static GitPathStyle()
            {
                quote_m = new int[128];
                for (int i = 0; i < quote_m.Length; i++)
                {
                    quote_m[i] = -1;
                }

			    for (int i = '0'; i <= '9'; i++)
                    quote_m[i] = 0;
			    for (int i = 'a'; i <= 'z'; i++)
                    quote_m[i] = 0;
			    for (int i = 'A'; i <= 'Z'; i++)
                    quote_m[i] = 0;

                quote_m[' '] = 0;
                quote_m['+'] = 0;
                quote_m[','] = 0;
                quote_m['-'] = 0;
                quote_m['.'] = 0;
                quote_m['/'] = 0;
                quote_m['='] = 0;
                quote_m['_'] = 0;
                quote_m['^'] = 0;

                quote_m['\u0007'] = (int)'a';
                quote_m['\b'] = (int)'b';
                quote_m['\f'] = (int)'f';
                quote_m['\n'] = (int)'n';
                quote_m['\r'] = (int)'r';
                quote_m['\t'] = (int)'t';
                quote_m['\u000B'] = (int)'v';
                quote_m['\\'] = (int)'\\';
                quote_m['"'] = (int)'"';
		    }

		    public override string quote(string instr)
            {
				if (instr == null)
					throw new ArgumentNullException ("instr");
			    if (instr.Length == 0)
				    return "\"\"";
			    bool reuse = true;
			    byte[] in_str = Constants.encode(instr);
			    StringBuilder r = new StringBuilder(2 + in_str.Length);
			    r.Append('"');
			    for (int i = 0; i < in_str.Length; i++) {
				    int c = in_str[i] & 0xff;
                    if (c < quote_m.Length)
                    {
                        int style = quote_m[c];
					    if (style == 0) {
						    r.Append((char) c);
						    continue;
					    }
					    if (style > 0) {
						    reuse = false;
						    r.Append('\\');
						    r.Append((char) style);
						    continue;
					    }
				    }

				    reuse = false;
				    r.Append('\\');
				    r.Append((char) (((c >> 6) & 03) + '0'));
				    r.Append((char) (((c >> 3) & 07) + '0'));
				    r.Append((char) (((c >> 0) & 07) + '0'));
			    }
			    if (reuse)
				    return instr;
			    r.Append('"');
			    return r.ToString();
		    }

		    public override string dequote(byte[] instr, int offset, int end)
            {
				if (instr==null)
					throw new ArgumentNullException("instr");
			    if (2 <= end - offset && instr[offset] == '"' && instr[end - 1] == '"')
				    return dq(instr, offset + 1, end - 1);
			    return RawParseUtils.decode(Constants.CHARSET, instr, offset, end);
		    }

		    private static string dq(byte[] instr, int offset, int end)
            {
			    byte[] r = new byte[end - offset];
			    int rPtr = 0;
			    while (offset < end)
                {
				    byte b = instr[offset++];
				    if (b != '\\') {
					    r[rPtr++] = b;
					    continue;
				    }

				    if (offset == end) {
					    // Lone trailing backslash. Treat it as a literal.
					    //
					    r[rPtr++] = (byte)'\\';
					    break;
				    }

				    switch (instr[offset++]) {
				    case (byte)'a':
					    r[rPtr++] = 0x07 /* \a = BEL */;
					    continue;
				    case (byte)'b':
					    r[rPtr++] = (byte)'\b';
					    continue;
				    case (byte)'f':
					    r[rPtr++] = (byte)'\f';
					    continue;
				    case (byte)'n':
					    r[rPtr++] = (byte)'\n';
					    continue;
				    case (byte)'r':
					    r[rPtr++] = (byte)'\r';
					    continue;
				    case (byte)'t':
					    r[rPtr++] = (byte)'\t';
					    continue;
				    case (byte)'v':
					    r[rPtr++] = 0x0B/* \v = VT */;
					    continue;

				    case (byte)'\\':
				    case (byte)'"':
					    r[rPtr++] = instr[offset - 1];
					    continue;

				    case (byte)'0':
				    case (byte)'1':
				    case (byte)'2':
				    case (byte)'3': {
					    int cp = instr[offset - 1] - '0';
					    while (offset < end) {
						    byte c = instr[offset];
						    if ('0' <= c && c <= '7') {
							    cp <<= 3;
							    cp |= c - '0';
							    offset++;
						    } else {
							    break;
						    }
					    }
					    r[rPtr++] = (byte) cp;
					    continue;
				    }

				    default:
					    // Any other code is taken literally.
					    //
					    r[rPtr++] = (byte)'\\';
					    r[rPtr++] = instr[offset - 1];
					    continue;
				    }
			    }

			    return RawParseUtils.decode(Constants.CHARSET, r, 0, rPtr);
		    }

		    internal GitPathStyle()
			{
			    // Singleton
		    }
	    }
    }
}