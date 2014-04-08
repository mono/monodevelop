//
// RoslynTypeSystemService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	public class MonoDevelopSourceTextContainer : SourceTextContainer
	{
		Document document;

		public MonoDevelopSourceTextContainer (Document document)
		{
			this.document = document;
		}

		#region implemented abstract members of SourceTextContainer
		public override SourceText CurrentText {
			get {
				Console.WriteLine ("get current text !!! " + document.FilePath);
				return SourceText.From (TextFileProvider.Instance.GetTextEditorData (document.FilePath).Text);
			}
		}

		public override event EventHandler<TextChangeEventArgs> TextChanged;
		#endregion
	}


	

	
}