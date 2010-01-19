#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;
using Cecil.Decompiler.Languages;

namespace Cecil.Decompiler {

	public static class Extensions {

		public static BlockStatement Decompile (this MethodBody body)
		{
			return RunPipeline (
				new DecompilationPipeline (new StatementDecompiler (BlockOptimization.Basic)),
				body);
		}

		public static BlockStatement Decompile (this MethodBody body, ILanguage language)
		{
			return RunPipeline (language.CreatePipeline (), body);
		}

		public static BlockStatement Decompile (this MethodBody body, DecompilationPipeline pipeline)
		{
			return RunPipeline (pipeline, body);
		}

		static BlockStatement RunPipeline (DecompilationPipeline pipeline, MethodBody body)
		{
			pipeline.Run (body);
			return pipeline.Body;
		}

		internal static TElement First<TElement> (this IList<TElement> list)
		{
			return list [0];
		}

		internal static TElement Last<TElement> (this IList<TElement> list)
		{
			return list [list.Count - 1];
		}
	}
}
