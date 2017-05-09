// 
// ICodeGenerator.cs
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
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.CodeGeneration
{
	internal interface ICodeGenerator
	{
		string Icon {
			get;
		}
		
		string Text {
			get;
		}
		
		string GenerateDescription {
			get;
		}
		
		bool IsValid (CodeGenerationOptions options);
		
		IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView);
	}
	
	interface IGenerateAction 
	{
		void GenerateCode (Gtk.TreeView treeView);
	}
	
	static class CodeGenerationService
	{
		static readonly List<ICodeGenerator> codeGenerators = new List<ICodeGenerator>();
		
		static CodeGenerationService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeGenerators", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					codeGenerators.Add ((ICodeGenerator)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					codeGenerators.Remove ((ICodeGenerator)args.ExtensionObject);
					break;
				}
			});
		}

		public static IEnumerable<ICodeGenerator> CodeGenerators {
			get {
				return codeGenerators;
			}
		}
	}
}
