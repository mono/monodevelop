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
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	public class ConstructorParameterDataProvider : MethodParameterDataProvider
	{
		IType type;
		
		
		public ConstructorParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IType type) : base (startOffset, ext)
		{
			this.type = type;
			
			var ctx = ext.CSharpParsedFile.GetTypeResolveContext (ext.Compilation, ext.Document.Editor.Caret.Location) as CSharpTypeResolveContext;

			var lookup = new MemberLookup (ctx.CurrentTypeDefinition, ext.Compilation.MainAssembly);
			bool isProtectedAllowed = ctx.CurrentTypeDefinition != null ? ctx.CurrentTypeDefinition.IsDerivedFrom (type.GetDefinition ()) : false;
						
			foreach (var method in type.GetConstructors ()) {
				Console.WriteLine ("constructor:" + method);
				if (!lookup.IsAccessible (method, isProtectedAllowed)) {
					Console.WriteLine ("skip !!!");
					continue;
				}
				methods.Add (method);
			}
		}
		
		protected override string GetPrefix (IMethod method)
		{
			var flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.IncludeGenerics;
			return ambience.GetString (type, flags) + ".";
		}
	}
}

