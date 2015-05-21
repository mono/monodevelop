//
// DataProvider.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;


using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;

using CBinding.Parser;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Threading;

namespace CBinding
{
	sealed class DataWrapper : ParameterHintingData
	{
		readonly Function f;

		public Function Function {
			get {
				return f;
			}
		}

		public DataWrapper (Function f) : base(null)
		{
			this.f = f;
		}

		public override int ParameterCount {
			get {
				return f.ParameterCount;
			}
		}

		public override bool IsParameterListAllowed {
			get {
				return f.IsParameterListAllowed;
			}
		}

		public override string GetParameterName (int parameter)
		{
			return f.GetParameterName (parameter);
		}

		public override Task<TooltipInformation> CreateTooltipInformation (TextEditor editor, DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken ctoken)
		{
			return Task.FromResult<TooltipInformation> (null);
		}
	}
}
