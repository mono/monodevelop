//
// CompletionData.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.JavaScript
{
	class CompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
	{
		IconId image;
		string text;
		string description;
		string completionString;

		public CompletionData (JSVariableDeclaration statement)
		{
			image = Stock.Method;
			text = statement.Name;
			completionString = text;
			description = string.Empty; // TODO
		}

		public CompletionData (JSFunctionStatement statement)
		{
			image = Stock.Method;
			text = statement.FunctionSignature;
			completionString = text;
			description = string.Empty; // TODO
		}

		public override IconId Icon {
			get { return image; }
		}

		public override string DisplayText {
			get { return text; }
		}

		public override string Description {
			get { return description; }
		}

		public override string CompletionText {
			get { return completionString; }
		}
	}
}

