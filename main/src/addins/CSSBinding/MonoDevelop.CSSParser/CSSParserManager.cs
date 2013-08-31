//
// CSSParserManager.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using System.CodeDom.Compiler;

namespace MonoDevelop.CSSParser
{
	public class CSSParserManager
	{
		string rootFileName;
		CompilerErrorCollection errors = new CompilerErrorCollection ();

		public CSSParserManager (string rootFileName)
		{
			this.rootFileName = rootFileName;
		}

		public CompilerErrorCollection Errors {
			get { return errors; }
		}


		void LogError (string message, Location location, bool isWarning)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = message;
			if (location.FileName != null) {
				err.Line = location.Line;
				err.Column = location.Column;
				err.FileName = location.FileName ?? string.Empty;
			} else {
				err.FileName = rootFileName ?? string.Empty;
			}
			err.IsWarning = isWarning;
			errors.Add (err);
		}

		public void LogError (string message)
		{
			LogError (message, Location.Empty, false);
		}

		public void LogWarning (string message)
		{
			LogError (message, Location.Empty, true);
		}

		public void LogError (string message, Location location)
		{
			LogError (message, location, false);
		}

		public void LogWarning (string message, Location location)
		{
			LogError (message, location, true);
		}
	}


}

