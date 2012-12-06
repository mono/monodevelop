// 
// ConstructorParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Completion
{
	class ConstructorParameterDataProvider : MethodParameterDataProvider
	{
		IType type;
		
		
		public ConstructorParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IType type, AstNode skipInitializer = null) : base (startOffset, ext)
		{
			this.type = type;
			
			var ctx = ext.CSharpUnresolvedFile.GetTypeResolveContext (ext.UnresolvedFileCompilation, ext.Document.Editor.Caret.Location) as CSharpTypeResolveContext;

			var lookup = new MemberLookup (ctx.CurrentTypeDefinition, ext.Compilation.MainAssembly);
			bool isProtectedAllowed = false;
			var typeDefinition = type.GetDefinition ();
			if (ctx.CurrentTypeDefinition != null && typeDefinition != null) {
				isProtectedAllowed = ctx.CurrentTypeDefinition.IsDerivedFrom (ctx.CurrentTypeDefinition.Compilation.Import (typeDefinition));
			}
			foreach (var method in type.GetConstructors ()) {
				if (!lookup.IsAccessible (method, isProtectedAllowed)) {
					continue;
				}
				if (!method.IsBrowsable ())
					continue;
				if (skipInitializer != null && skipInitializer.Parent.StartLocation == method.Region.Begin)
					continue;
				methods.Add (method);
			}
			methods.Sort (MethodComparer);
		}
		
		protected override string GetPrefix (IMethod method)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			return ambience.GetString (type, flags) + ".";
		}
	}
}

