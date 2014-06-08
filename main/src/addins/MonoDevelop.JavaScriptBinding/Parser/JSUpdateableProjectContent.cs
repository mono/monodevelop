//
// JSUpdateableProjectContent.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.JavaScript
{
	[Serializable]
	public class JSUpdateableProjectContent //: TypeSystemService.IUpdateableProjectContent
	{
		public List<JavaScriptDocumentCache> DocumentsCache {
			get;
			set;
		}

		public JSUpdateableProjectContent()
		{
			DocumentsCache = new List<JavaScriptDocumentCache> ();
		}

		public void AddOrUpdateFiles (IEnumerable<ParsedDocument> docs)
		{
			foreach (var doc in docs.OfType<JavaScriptParsedDocument> ()) {
				DocumentsCache.Add (new JavaScriptDocumentCache{
					FileName = doc.FileName,
					SimpleAst = doc.SimpleAst
				});
			}
		}

		public void RemoveFiles (IEnumerable<string> filenames)
		{
			foreach (var fileName in filenames)
				DocumentsCache.RemoveAll (i=>i.FileName == fileName);			
		}
	}
}

