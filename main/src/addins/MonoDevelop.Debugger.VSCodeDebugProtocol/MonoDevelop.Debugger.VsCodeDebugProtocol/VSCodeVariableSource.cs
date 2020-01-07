using System;

using Mono.Debugging.Client;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeVariableSource : VSCodeObjectSource
	{
		readonly Variable variable;
		readonly string display;
		readonly string value;
		readonly string name;
		readonly string type;

		public VSCodeVariableSource (VSCodeDebuggerSession session, Variable variable, int parentVariablesReference, int frameId) : base (session, parentVariablesReference, frameId)
		{
			this.variable = variable;

			var actualType = GetActualTypeName (variable.Type);

			Flags = parentVariablesReference > 0 ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly;
			Flags |= GetFlags (variable.PresentationHint);
			name = GetFixedVariableName (variable.Name);
			type = actualType.Replace (", ", ",");

			if (actualType != "void")
				value = GetFixedValue (variable.Value, type, actualType);
			else
				value = "No return value.";
			display = variable.Value;

			if (name[0] == '[')
				Flags |= ObjectValueFlags.ArrayElement;

			if (type == null || value == $"'{name}' threw an exception of type '{type}'")
				Flags = ObjectValueFlags.Error;
		}

		protected override string Display {
			get { return display; }
		}

		protected override string Expression {
			get { return variable.EvaluateName; }
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
			get { return variable.VariablesReference; }
		}

		static string GetFixedVariableName (string name)
		{
			// Check for a type attribute and strip it off.
			var index = name.LastIndexOf (" [", StringComparison.Ordinal);

			if (index != -1)
				return name.Remove (index);

			return name;
		}
	}
}
