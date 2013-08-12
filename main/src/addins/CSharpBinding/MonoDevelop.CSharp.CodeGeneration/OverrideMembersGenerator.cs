// 
// OverrideMethodsGenerator.cs
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

using Gtk;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	class OverrideMembersGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-method";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Override members");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to be overridden.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new OverrideMethods (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			OverrideMethods overrideMethods = new OverrideMethods (options);
			overrideMethods.Initialize (treeView);
			return overrideMethods;
		}
		
		class OverrideMethods : AbstractGenerateAction
		{
			public OverrideMethods (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				var type = Options.EnclosingType;
				if (type == null || Options.EnclosingMember != null)
					yield break;
				HashSet<string> memberName = new HashSet<string> ();
				foreach (var member in Options.EnclosingType.GetMembers ()) {
					if (member.IsSynthetic)
						continue;
					if (member.IsOverridable) {
						string id = AmbienceService.DefaultAmbience.GetString (member, OutputFlags.ClassBrowserEntries);
						if (memberName.Contains (id))
							continue;
						memberName.Add (id);
						yield return member;
					}
				}
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var generator = Options.CreateCodeGenerator ();
				generator.AutoIndent = false;
				foreach (IMember member in includedMembers) 
					yield return generator.CreateMemberImplementation (Options.EnclosingType, Options.EnclosingPart, member, false).Code;
			}
		}
	}
}
