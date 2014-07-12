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
using MonoDevelop.Ide.CodeCompletion;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MonoDevelop.JavaScript
{
	[Serializable]
	public class JSUpdateableProjectContent : TypeSystemService.IUpdateableProjectContent
	{
		public JSCompletionList CodeCompletionCache;

		public List<JavaScriptDocumentCache> DocumentsCache {
			get;
			set;
		}

		public JSUpdateableProjectContent ()
		{
			DocumentsCache = new List<JavaScriptDocumentCache> ();
			CodeCompletionCache = new JSCompletionList ();

			CodeCompletionUtility.AddDefaultKeywords (ref CodeCompletionCache);
			CodeCompletionUtility.AddNativeVariablesAndFunctions (ref CodeCompletionCache);
		}

		public void AddOrUpdateFiles (IEnumerable<ParsedDocument> docs)
		{
			foreach (var doc in docs.OfType<JavaScriptParsedDocument> ()) {
				var existing = DocumentsCache.FirstOrDefault (i => i.FileName == doc.FileName);
				if (existing == null) {
					DocumentsCache.Add (new JavaScriptDocumentCache {
						FileName = doc.FileName,
						SimpleAst = doc.SimpleAst
					});
				} else {
					existing.SimpleAst = doc.SimpleAst;
				}

				CodeCompletionCache.RemoveAllMembersByFileName (doc.FileName);
				CodeCompletionUtility.UpdateCodeCompletion (doc.SimpleAst.AstNodes, ref CodeCompletionCache);
			}
		}

		public void RemoveFiles (IEnumerable<string> filenames)
		{
			foreach (var fileName in filenames) {
				DocumentsCache.RemoveAll (i => i.FileName == fileName);	
				if (CodeCompletionCache != null) {
					CodeCompletionCache.RemoveAllMembersByFileName (fileName);
				}
			}
		}
	}
}

