// 
// PropertyGenerator.cs
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
	class PropertyGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-property";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Properties");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members which should be exposed.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateProperty (options).IsValid ();
		}
		
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateProperty createProperty = new CreateProperty (options);
			createProperty.Initialize (treeView);
			return createProperty;
		}
		
		internal class CreateProperty : AbstractGenerateAction
		{
			public bool ReadOnly {
				get;
				set;
			}
			
			public CreateProperty (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSynthetic)
						continue;
					var list = Options.EnclosingType.Fields.Where (f => f.Name == CreatePropertyName (field));;
					if (!list.Any ())
						yield return field;
				}
			}
			
			static string CreatePropertyName (IMember member)
			{
				int i = 0;
				while (i + 1 < member.Name.Length && member.Name[i] == '_')
					i++;
				if (i + 1 >= member.Name.Length)
					return char.ToUpper (member.Name [i]).ToString ();
				return char.ToUpper (member.Name [i]) + member.Name.Substring (i + 1);
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var generator = Options.CreateCodeGenerator ();
				generator.AutoIndent = false;
				foreach (IField field in includedMembers)
					yield return generator.CreateFieldEncapsulation (Options.EnclosingPart, field, CreatePropertyName (field), Accessibility.Public, ReadOnly);
			}
		}
	}
}
