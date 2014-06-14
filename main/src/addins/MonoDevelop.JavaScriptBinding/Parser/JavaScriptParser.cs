//
// JavaScriptParser.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.JavaScript
{
	class JavaScriptParser : TypeSystemParser
	{
		public const string ParsedDocumentProperty = "JS.ParsedDocument";

		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Projects.Project project = null)
		{
			var parseDocument = new JavaScriptParsedDocument (fileName, content);
			
			return parseDocument;
		}

		public override Type GetProjectContentExtensionType (string buildAction)
		{
			return typeof(JSUpdateableProjectContent);
		}

		public override bool ShouldStoreInProjectContent (string buildAction)
		{
			return true;
		}
	}
}
