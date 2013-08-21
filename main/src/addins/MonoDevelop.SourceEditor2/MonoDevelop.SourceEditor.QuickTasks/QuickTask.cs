// 
// QuickTask.cs
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
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	
	public class QuickTask
	{
		Lazy<string> description;
		public string Description {
			get {
				return description.Value;
			}
		}
		
		public TextLocation Location {
			get;
			private set;
		}
		
		public Severity Severity {
			get;
			private set;
		}
		
		public QuickTask (Func<string> descriptionFunc, TextLocation location, Severity severity)
		{
			this.description = new Lazy<string> (descriptionFunc);
			this.Location = location;
			this.Severity = severity;
		}

		public QuickTask (string description, TextLocation location, Severity severity)
		{
			this.description = new Lazy<string> (() => description);
			this.Location = location;
			this.Severity = severity;
		}

		public override string ToString ()
		{
			return string.Format ("[QuickTask: Description={0}, Location={1}, Severity={2}]", Description, Location, Severity);
		}
	}
	
}
