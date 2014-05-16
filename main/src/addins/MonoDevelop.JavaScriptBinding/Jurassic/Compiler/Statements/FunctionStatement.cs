using Jurassic.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// HB ADDED
namespace Jurassic.Compiler
{
	public class FunctionStatement : Statement
	{
		public string FunctionName { get; set; }

		public IList<string> ArgumentNames { get; set; }

		public Statement BodyRoot { get; set; }

		public FunctionStatement (string functionName, IList<string> argumentNames, Statement body, SourceCodeSpan sourceSpan, IList<string> labels)
			: base (labels)
		{
			ArgumentNames = argumentNames;
			BodyRoot = body;
			FunctionName = functionName;
			SourceSpan = sourceSpan;
		}

		public override string ToString (int indentLevel)
		{
			throw new NotImplementedException ();
		}
	}
}
