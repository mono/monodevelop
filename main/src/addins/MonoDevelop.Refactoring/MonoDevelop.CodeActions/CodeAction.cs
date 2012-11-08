// 
// Result.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory;
using System;

namespace MonoDevelop.CodeActions
{
	/// <summary>
	/// A code action represents a menu entry that does edit operation in one document.
	/// </summary>
	public abstract class CodeAction
	{
		/// <summary>
		/// Gets or sets the menu item text.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets the id string. The id is used to identify a specific code action.
		/// </summary>
		public string IdString { get; set; }

		/// <summary>
		/// Gets or sets the code issue this action is bound to. 
		/// This allows to split the action and the issue provider.
		/// </summary>
		public Type BoundToIssue { get; set; }

		public CodeAction ()
		{
			IdString = GetType ().FullName;
		}

		/// <summary>
		/// Performs the specified code action in document at loc.
		/// </summary>
		public abstract void Run (MonoDevelop.Ide.Gui.Document document, TextLocation loc);
	}

	public class DefaultCodeAction : CodeAction
	{
		public Action<MonoDevelop.Ide.Gui.Document, TextLocation> act;

		public DefaultCodeAction (string title, Action<MonoDevelop.Ide.Gui.Document, TextLocation> act)
		{
			Title = title;
			this.act = act;
		}

		public override void Run (MonoDevelop.Ide.Gui.Document document, TextLocation loc)
		{
			act (document, loc);
		}
	}

}
