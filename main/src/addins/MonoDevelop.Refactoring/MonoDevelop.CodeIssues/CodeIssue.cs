// 
// CodeIssue.cs
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
using Mono.TextEditor;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// A code issue marks an issue inside a text editor. An issue is a description shown in the tooltip and
	/// (optionally) a set of code actions to solve the issue.
	/// </summary>
	public class CodeIssue
	{
		/// <summary>
		/// Gets or sets the description shown in the tooltip.
		/// </summary>
		public string Description {
			get;
			private set;
		}
		
		/// <summary>
		/// Gets or sets the region of the issue.
		/// </summary>
		public DomRegion Region {
			get;
			private set;
		}
		
		/// <summary>
		/// Gets or sets the code actions to solve the issue.
		/// </summary>
		public IEnumerable<MonoDevelop.CodeActions.CodeAction> Actions {
			get;
			private set;
		}
		
		public string InspectorIdString {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.CodeIssue"/> class.
		/// </summary>
		public CodeIssue (string description, string fileName, DocumentLocation start, DocumentLocation end, string inspectorIdString, IEnumerable<MonoDevelop.CodeActions.CodeAction>  actions = null) : this (description, new DomRegion (fileName, start, end), inspectorIdString, actions)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.CodeIssue"/> class.
		/// </summary>
		public CodeIssue (string description, DomRegion region, string inspectorIdString, IEnumerable<MonoDevelop.CodeActions.CodeAction>  actions = null)
		{
			Description = description;
			Region = region;
			Actions = actions;
			InspectorIdString = inspectorIdString;
		}

	}
}

