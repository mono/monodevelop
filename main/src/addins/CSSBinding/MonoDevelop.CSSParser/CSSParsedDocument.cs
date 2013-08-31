//
// CSSParsedDocument.cs
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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

namespace MonoDevelop.CSSParser 
{
	public class CSSParsedDocument : ParsedDocument
	{
		string fileName;
		IList<Error> errors;


		public List<ISegment> Segments { get; private set; }
		public override IList<Error> Errors {
			get {
				return errors;
			}
		}

		public override string FileName {
			get 
			{
				return fileName;
			}
		}

		public CSSParsedDocument (string fileName, List<ISegment> segments, IList<Error> errors)
		{
			this.fileName = fileName;
			this.errors = errors;
			this.Segments = segments;
		}


	

		public override IEnumerable<FoldingRegion> Foldings 
		{
			get 
			{
				foreach (var segment in Segments) 
				{
					CodeSegment ts = segment as CodeSegment;
					DomRegion region = new DomRegion (segment.TagStartLocation.Line, segment.TagStartLocation.Column,
					                                  segment.EndLocation.Line, segment.EndLocation.Column);

					yield return new FoldingRegion (ts.Text, region,FoldType.Undefined ,true);

				}


			}

		}
						
	}



}

