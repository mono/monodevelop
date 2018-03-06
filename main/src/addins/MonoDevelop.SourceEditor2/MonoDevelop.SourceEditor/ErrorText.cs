//
// ErrorText.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using MonoDevelop.Ide;
using System.Text.RegularExpressions;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components;

namespace MonoDevelop.SourceEditor
{
	class ErrorText
	{
		public TaskListEntry Task { get; set; }
		public bool IsError { get; set; }
		public string ErrorMessage { get; set; }
		public string FullErrorMessage { get; set; }

		public ErrorText (TaskListEntry task, bool isError, string errorMessage, string fullErrorMessage)
		{
			this.Task = task;
			this.IsError = isError;
			this.ErrorMessage = errorMessage;
			this.FullErrorMessage = fullErrorMessage;
		}

		public override string ToString ()
		{
			return string.Format ("[ErrorText: IsError={0}, ErrorMessage={1}]", IsError, ErrorMessage);
		}
	}
	
}
