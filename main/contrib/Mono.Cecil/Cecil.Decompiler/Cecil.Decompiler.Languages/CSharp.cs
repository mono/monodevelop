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

using Mono.Cecil.Cil;

using Cecil.Decompiler.Steps;

namespace Cecil.Decompiler.Languages {

	public class CSharp : ILanguage {

		public virtual string Name { get { return "C# no transformation"; } }

		public ILanguageWriter GetWriter (IFormatter formatter)
		{
			return new CSharpWriter (this, formatter);
		}

		public virtual DecompilationPipeline CreatePipeline ()
		{
			return new DecompilationPipeline (
				new StatementDecompiler (BlockOptimization.Basic),
				TypeOfStep.Instance,
				DeclareTopLevelVariables.Instance);
		}

		public static ILanguage GetLanguage (CSharpVersion version)
		{
			switch (version) {
			case CSharpVersion.None:
				return new CSharp ();
			case CSharpVersion.V1:
				return new CSharpV1 ();
			case CSharpVersion.V2:
				return new CSharpV2 ();
			case CSharpVersion.V3:
				return new CSharpV3 ();
			default:
				throw new ArgumentException ();
			}
		}
	}

	public class CSharpV1 : CSharp {

		public override string Name { get { return "C#1"; } }

		public override DecompilationPipeline CreatePipeline ()
		{
			return new DecompilationPipeline (
				new StatementDecompiler (BlockOptimization.Detailed),
				RemoveLastReturn.Instance,
				PropertyStep.Instance,
				CanCastStep.Instance,
				RebuildForStatements.Instance,
				RebuildForeachStatements.Instance,
				DeclareVariablesOnFirstAssignment.Instance,
				DeclareTopLevelVariables.Instance,
				SelfAssignement.Instance,
				OperatorStep.Instance);
		}
	}

	public class CSharpV2 : CSharpV1 {

		public override string Name { get { return "C#2"; } }
	}

	public class CSharpV3 : CSharpV2 {

		public override string Name { get { return "C#3"; } }
	}

	public class CSharpV4 : CSharpV3 {

		public override string Name { get { return "C#4"; } }
	}

	public enum CSharpVersion {
		None,
		V1,
		V2,
		V3,
		V4,
	}
}
