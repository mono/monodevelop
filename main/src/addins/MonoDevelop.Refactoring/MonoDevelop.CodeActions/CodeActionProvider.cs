// 
// ContextActionProvider.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using System.Threading;
using System.Collections.Generic;
using System;

namespace MonoDevelop.CodeActions
{
	/// <summary>
	/// A code action provider is a factory that creates code actions for a document at a given location.
	/// Note: There is only one code action provider generated therfore providers need to be state less.
	/// </summary>
	public abstract class CodeActionProvider
	{
		/// <summary>
		/// Gets or sets the type of the MIME the provider is attached to.
		/// </summary>
		public string MimeType { get; set; }

		/// <summary>
		/// Gets or sets the category of the provider (used in the option panel).
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Gets or sets the title of the provider (used in the option panel).
		/// </summary>
		public string Title { get; set; }
		
		/// <summary>
		/// Gets or sets the description of the provider (used in the option panel).
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the code issue all actions are bound to. 
		/// This allows to split the action and the issue provider.
		/// </summary>
		public Type BoundToIssue { get; set; }

		/// <summary>
		/// Gets the identifier string used as property ID tag.
		/// </summary>
		public virtual string IdString {
			get {
				return GetType ().FullName;
			}
		}

		/// <summary>
		/// Gets all the code actions in document at given location.
		/// </summary>
		public abstract IEnumerable<CodeAction> GetActions (MonoDevelop.Ide.Gui.Document document, object refactoringContext, TextLocation loc, CancellationToken cancellationToken);
	}
}
