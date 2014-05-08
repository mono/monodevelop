using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.JavaScript.Extensions
{
	static class FunctionExtensions
	{
		public static string BuildFunctionSignature (this Jurassic.Compiler.FunctionExpression function)
		{
			return buildFunctionSignature (function.FunctionName, function.ArgumentNames);
		}

		public static string BuildFunctionSignature (this Jurassic.Compiler.FunctionStatement function)
		{
			return buildFunctionSignature (function.FunctionName, function.ArgumentNames);
		}

		static string buildFunctionSignature (string functionName, IList<string> argumentNames)
		{
			if (string.IsNullOrWhiteSpace (functionName))
				functionName = "function"; // for inline functions, callbacks, delegates, etc.

			var builder = new StringBuilder (functionName);

			builder.Append (" (");
			if (argumentNames.Count > 0) {
				for (int i = 0; i < argumentNames.Count; i++) {
					var currentArgument = argumentNames [i];
					if (i + 1 != argumentNames.Count) {
						builder.Append (string.Concat (currentArgument, ", "));
					} else {
						builder.Append (string.Concat (currentArgument));
					}
				}
			}
			builder.Append (")");

			return builder.ToString ();
		}
	}
}
