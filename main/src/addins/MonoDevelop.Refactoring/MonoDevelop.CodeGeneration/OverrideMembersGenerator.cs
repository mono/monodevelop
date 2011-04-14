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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Refactoring;
using MonoDevelop.Projects.CodeGeneration;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CodeGeneration
{
	public class OverrideMembersGenerator : ICodeGenerator
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
			
			protected override IEnumerable<IBaseMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				HashSet<string> memberName = new HashSet<string> ();
				foreach (IType type in Options.Dom.GetInheritanceTree (Options.EnclosingType)) {
					if (type.Equals (Options.EnclosingType))
						continue;
					foreach (IMember member in type.Members) {
						if (member.IsSpecialName)
							continue;
						if (type.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface || member.IsAbstract || member.IsVirtual || member.IsOverride) {
							string id = AmbienceService.DefaultAmbience.GetString (member, OutputFlags.ClassBrowserEntries);
							if (memberName.Contains (id))
								continue;
							memberName.Add (id);
							yield return member;
						}
					}
				}
			}
			
			static ICSharpCode.NRefactory.CSharp.ParameterModifier GetModifier (IParameter para)
			{
				if (para.IsOut)
					return ICSharpCode.NRefactory.CSharp.ParameterModifier.Out;
				if (para.IsRef)
					return ICSharpCode.NRefactory.CSharp.ParameterModifier.Ref;
				if (para.IsParams)
					return ICSharpCode.NRefactory.CSharp.ParameterModifier.Params;
				return ICSharpCode.NRefactory.CSharp.ParameterModifier.None;
			}
			
			static FieldDirection GetDirection (IParameter para)
			{
				if (para.IsOut)
					return FieldDirection.Out;
				if (para.IsRef)
					return FieldDirection.Ref;
				return FieldDirection.None;
			}
			
			static readonly ThrowStatement throwNotImplemented = new ThrowStatement (new ObjectCreateExpression (new SimpleType ("System.NotImplementedException"), null));
			
			protected override IEnumerable<string> GenerateCode (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers)
			{
				CodeGenerator generator = Options.Document.CreateCodeGenerator ();
				
				foreach (IMember member in includedMembers) 
					yield return generator.CreateMemberImplementation (Options.EnclosingType, member, false).Code;
			}
		}
	}
}
