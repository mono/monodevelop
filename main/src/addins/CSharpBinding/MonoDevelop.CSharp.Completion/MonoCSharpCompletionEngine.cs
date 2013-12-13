//
// MonoCSharpCompletionEngine.cs
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
using ICSharpCode.NRefactory.CSharp.Completion;
using System.Collections.Generic;
using MonoDevelop.CodeGeneration;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using ICSharpCode.NRefactory.Editor;

namespace MonoDevelop.CSharp.Completion
{
	class MonoCSharpCompletionEngine : CSharpCompletionEngine
	{
		readonly CSharpCompletionTextEditorExtension ext;

		public CSharpCompletionTextEditorExtension Ext {
			get {
				return ext;
			}
		}

		public MDRefactoringContext MDRefactoringCtx {
			get {
				return ext.MDRefactoringCtx;
			}
		}

		public MonoCSharpCompletionEngine (CSharpCompletionTextEditorExtension ext, ICSharpCode.NRefactory.Editor.IDocument document, ICompletionContextProvider completionContextProvider, ICompletionDataFactory factory, ICSharpCode.NRefactory.TypeSystem.IProjectContent content, ICSharpCode.NRefactory.CSharp.TypeSystem.CSharpTypeResolveContext ctx) : base (document, completionContextProvider, factory, content, ctx)
		{
			this.ext = ext;
		}

		protected override void AddVirtuals (List<IMember> alreadyInserted, CompletionDataWrapper col, string modifiers, IType curType, int declarationBegin)
		{
			base.AddVirtuals (alreadyInserted, col, modifiers, curType, declarationBegin);
			foreach (var member in GetProtocolMembers (curType)) {
				if (alreadyInserted.Contains (member))
					continue;
				if (BaseExportCodeGenerator.IsImplemented (curType, member))
					continue;
				alreadyInserted.Add (member);
				var data = new ProtocolCompletionData (this, declarationBegin, member);
				col.Add (data);
			}
		}

		IEnumerable<IMember> GetProtocolMembers (IType curType)
		{
			foreach (var t in curType.DirectBaseTypes) {
				string name;
				if (!BaseExportCodeGenerator.HasProtocolAttribute (t, out name))
					continue;
				var protocolType = Compilation.FindType (new FullTypeName (new TopLevelTypeName (t.Namespace, name)));
				if (protocolType == null)
					break;
				foreach (var member in protocolType.GetMethods (null, GetMemberOptions.IgnoreInheritedMembers)) {
					if (member.ImplementedInterfaceMembers.Any () || member.IsAbstract || !member.IsVirtual)
						continue;
					if (member.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation")) {
						yield return member;
					}
				}
				foreach (var member in protocolType.GetProperties (null, GetMemberOptions.IgnoreInheritedMembers)) {
					if (member.ImplementedInterfaceMembers.Any () || member.IsAbstract || !member.IsVirtual)
						continue;
					if (member.CanGet && member.Getter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation") ||
						member.CanSet && member.Setter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation"))
						yield return member;
				}
			}
		}
	}
}

