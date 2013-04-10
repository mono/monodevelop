// 
// NRefactoryCodeAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory;
using System.Threading;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	public class NRefactoryCodeAction : MonoDevelop.CodeActions.CodeAction
	{
		readonly ICSharpCode.NRefactory.CSharp.Refactoring.CodeAction act;
		
		public NRefactoryCodeAction (string id, string title, ICSharpCode.NRefactory.CSharp.Refactoring.CodeAction act)
		{
			this.IdString = id;
			this.Title = title;
			this.act = act;
			this.DocumentRegion = new Mono.TextEditor.DocumentRegion (act.Start, act.End);
		}

		public override void Run (Document document, TextLocation loc)
		{
			var context = new MDRefactoringContext (document, loc);
			using (var script = context.StartScript ())
				act.Run (script);
		}
	}
	
}
