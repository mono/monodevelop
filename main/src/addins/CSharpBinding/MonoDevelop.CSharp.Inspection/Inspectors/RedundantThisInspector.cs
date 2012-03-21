// 
// RedundantThisInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.CSharp.Refactoring.RefactorImports;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.Inspection;

namespace MonoDevelop.CSharp.Inspection
{
	public class RedundantThisInspector : NRefactoryInspectorWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.RedundantThisInspector>
	{
		public override string Category {
			get {
				return DefaultInspectionCategories.Redundancies;
			}
		}

		public override string Title {
			get {
				return GettextCatalog.GetString ("Remove redundant 'this.'");
			}
		}

		public override string Description {
			get {
				return GettextCatalog.GetString ("Removes 'this.' references that are not required.");
			}
		}

		public RedundantThisInspector ()
		{
			inspector.Title = GettextCatalog.GetString ("Remove redundant 'this.'");
		}
	}
}

