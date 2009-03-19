// PythonParser.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.IO;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace PyBinding.Parser
{
	public class PythonParser : AbstractParser
	{
		PythonParserInternal m_Parser;

		public PythonParser () : base ("Python", "text/x-python")
		{
			this.m_Parser = new PythonParserInternal ();
		}

		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			try {
				return this.m_Parser.Parse (fileName, content);
			}
			catch (Exception ex) {
				Console.WriteLine ("Python parser exception:");
				Console.WriteLine (ex.ToString ());
				throw;
			}
		}

		public override bool CanParse (string fileName)
		{
			return Path.GetExtension (fileName) == ".py";
		}
	}
}