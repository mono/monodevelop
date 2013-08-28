// 
// ReadonlyPropertyGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace MonoDevelop.CodeGeneration
{
	class ReadonlyPropertyGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-property";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Read-only properties");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members which should be exposed.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new PropertyGenerator.CreateProperty (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			var createProperty = new PropertyGenerator.CreateProperty (options);
			createProperty.ReadOnly = true;
			createProperty.Initialize (treeView);
			return createProperty;
		}
	}
}
