using System;
using System.Globalization;

using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeEvaluationSource : VSCodeObjectSource
	{
		readonly EvaluateResponse response;
		readonly string expression;
		readonly string display;
		readonly string value;
		readonly string name;
		readonly string type;

		public VSCodeEvaluationSource (VSCodeDebuggerSession session, string expression, EvaluateResponse response, int frameId)  : base (session, 0, frameId)
		{
			this.expression = expression;
			this.response = response;

			// FIXME: can we use PresentationHint.Attributes == VariablePresentationHint.AttributesValue.FailedEvaluation instead?
			if (response.Type == null) {
				if (IsCSError (118, "is a namespace but is used like a variable", response.Result, out string ns)) {
					Flags = ObjectValueFlags.Namespace;
					display = name = value = ns;
					type = "<namespace>";
					return;
				}

				if (IsCSError (119, "is a type, which is not valid in the given context", response.Result, out string vtype)) {
					if (expression.StartsWith ("global::", StringComparison.Ordinal))
						vtype = expression.Substring ("global::".Length);

					display = name = value = ObjectValueAdaptor.GetCSharpTypeName (vtype);
					Flags = ObjectValueFlags.Type;
					type = "<type>";
					return;
				}
			}

			var actualType = GetActualTypeName (response.Type);

			Flags = GetFlags (response.PresentationHint);
			type = actualType.Replace (", ", ",");
			name = expression;

			if (actualType != "void")
				value = GetFixedValue (response.Result, type, actualType);
			else
				value = "No return value.";
			display = response.Result;

			if (name[0] == '[')
				Flags |= ObjectValueFlags.ArrayElement;

			if (type == null || value == $"'{name}' threw an exception of type '{type}'")
				Flags = ObjectValueFlags.Error;
		}

		protected override string Display {
			get { return display; }
		}

		protected override string Expression {
			get { return expression; }
		}

		protected override string Name {
			get { return name; }
		}

		protected override string Type {
			get { return type; }
		}

		protected override string Value {
			get { return value; }
		}

		protected override int VariablesReference {
			get { return response.VariablesReference; }
		}

		static bool IsCSError (int code, string message, string value, out string newValue)
		{
			var prefix = string.Format (CultureInfo.InvariantCulture, "error CS{0:D4}: '", code);

			newValue = null;

			if (value == null || !value.StartsWith (prefix, StringComparison.Ordinal))
				return false;

			int startIndex = prefix.Length;
			int index = startIndex;

			while (index < value.Length && value [index] != '\'')
				index++;

			newValue = value.Substring (startIndex, index - startIndex);
			index++;

			if (index >= value.Length || value [index] != ' ')
				return false;

			index++;

			if (index + message.Length != value.Length)
				return false;

			return string.CompareOrdinal (value, index, message, 0, message.Length) == 0;
		}
	}
}
