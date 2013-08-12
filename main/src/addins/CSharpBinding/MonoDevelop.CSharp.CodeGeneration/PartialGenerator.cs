//
// PartialGenerator.cs
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
using System.Collections.Generic;
using Gtk;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	class PartialGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-method";
			}
		}

		public string Text {
			get {
				return GettextCatalog.GetString ("Partial methods");
			}
		}

		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select methods to be implemented.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new PartialMethods (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var overrideMethods = new PartialMethods (options);
			overrideMethods.Initialize (treeView);
			return overrideMethods;
		}

		class PartialMethods : AbstractGenerateAction
		{
			public PartialMethods (CodeGenerationOptions options) : base (options)
			{
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				var type = Options.EnclosingType;
				if (type == null || Options.EnclosingMember != null)
					yield break;

				foreach (var method in Options.EnclosingType.Methods) {
					if (method.IsPartial && method.BodyRegion.IsEmpty) {
						yield return method;
					}
				}	
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var generator = Options.CreateCodeGenerator ();
				generator.AutoIndent = false;
				foreach (IMethod member in includedMembers) 
					yield return generator.CreateMemberImplementation (Options.EnclosingType, Options.EnclosingPart, member, false).Code;
			}
		}
	}
}

