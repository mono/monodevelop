//
// RevisionHelpers.cs
//
// Author:
//       Marius <>
//
// Copyright (c) 2017 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Text;

namespace MonoDevelop.VersionControl
{
	static class RevisionHelpers
	{
		static readonly char[] trimChars = { ' ', '\t' };

		internal static string FormatMessage(string msg)
		{
			StringBuilder sb = new StringBuilder();
			bool wasWs = true, wasNewLine = true, foundStar = false, trimmedEndWhitespace = false;
			int offset = 0;

			foreach (char ch in msg)
			{
				// Fast-fail if we don't have any ':', meaning this is not a 
				// changelog-style commit message 
				if (msg.IndexOf (':') == -1)
					return msg.TrimStart (trimChars);

				// Parse change-log style commit message
				if (ch == ' ' || ch == '\t') {
					if (!wasWs)
						sb.Append(' ');
					wasWs = true;
					continue;
				}

				if (offset == 0) {
					if (!foundStar && wasNewLine) {
						foundStar = ch == '*';
					}

					if (foundStar && ch == ':') {
						offset = sb.Length + 1;
					}

					if (ch == '\n') {
						// wasNewLine will remain true until the next non-whitespace char.
						wasNewLine = true;
					} else {
						wasNewLine = false;
					}
				} else if (!trimmedEndWhitespace) {
					if (ch == ' ' || ch == '\t')
						continue;

					if (ch == '\n') {
						trimmedEndWhitespace = true;
						continue;
					}
				}

				wasWs = false;
				sb.Append(ch);
			}

			if (offset != 0 && offset < sb.Length)
				msg = sb.ToString (offset, sb.Length - offset);
			
			return msg.TrimStart (trimChars);
		}
	}
}
