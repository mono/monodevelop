//
// AbstractParameterDataProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;

using MonoDevelop.Core;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.CSharp.Completion
{
	abstract class AbstractParameterDataProvider : ParameterDataProvider
	{
		protected CSharpCompletionTextEditorExtension ext;

		public AbstractParameterDataProvider (CSharpCompletionTextEditorExtension ext, int startOffset) : base (startOffset)
		{
			if (ext == null)
				throw new ArgumentNullException ("ext");
			this.ext = ext;
		}

		TypeSystemAstBuilder builder = null;
		protected string GetShortType (IType type)
		{
			if (builder == null) {
				var ctx = ext.CSharpUnresolvedFile.GetTypeResolveContext (ext.UnresolvedFileCompilation, ext.Document.Editor.Caret.Location) as CSharpTypeResolveContext;
				var state = new CSharpResolver (ctx);
				builder = new TypeSystemAstBuilder (state);
				var dt = state.CurrentTypeDefinition;
				var declaring = ctx.CurrentTypeDefinition != null ? ctx.CurrentTypeDefinition.DeclaringTypeDefinition : null;
				if (declaring != null) {
					while (dt != null) {
						if (dt.Equals (declaring)) {
							builder.AlwaysUseShortTypeNames = true;
							break;
						}
						dt = dt.DeclaringTypeDefinition;
					}
				}
			}
			try {
				return GLib.Markup.EscapeText (builder.ConvertType(type).GetText (ext.FormattingPolicy.CreateOptions ()));
			} catch (Exception e) {
				LoggingService.LogError ("Exception while getting short type.", e);
				return "";
			}
		}

		protected string GetParameterString (IParameter parameter)
		{
			var sb = new StringBuilder ();
			if (parameter.IsOut)
				sb.Append ("out ");
			if (parameter.IsRef)
				sb.Append ("ref ");
			sb.Append (GetShortType (parameter.Type));
			sb.Append (" ");
			sb.Append (parameter.Name);
			return sb.ToString ();
		}

	}
}

