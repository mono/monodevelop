using System;
using System.Linq;
using System.Text;
using System.Globalization;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeObjectSource : IObjectValueSource
	{
		const VariablePresentationHint.AttributesValue ConstantReadOnlyStatic = VariablePresentationHint.AttributesValue.Constant | VariablePresentationHint.AttributesValue.ReadOnly | VariablePresentationHint.AttributesValue.Static;
		static readonly char[] CommaDotOrSquareEndBracket = { ',', '.', ']' };
		static readonly char[] CommaOrSquareEndBracket = { ',', ']' };
		static readonly char[] LessThanOrSquareBracket = { '<', '[' };

		ObjectValue[] objValChildren;

		readonly VSCodeDebuggerSession vsCodeDebuggerSession;
		readonly int parentVariablesReference;
		readonly ObjectValueFlags flags;
		readonly int variablesReference;
		readonly int frameId;
		readonly string evalName;
		readonly string display;
		readonly string name;
		readonly string type;
		readonly string val;

		static string GetActualTypeName (string type)
		{
			int startIndex;

			if (type == null)
				return string.Empty;

			if ((startIndex = type.IndexOf (" {", StringComparison.Ordinal)) != -1) {
				// The type is boxed. The string between the {}'s is the actual type name.
				int endIndex = type.LastIndexOf ('}');

				startIndex += 2;

				if (endIndex > startIndex)
					return type.Substring (startIndex, endIndex - startIndex);
			}

			return type;
		}

		static string GetFixedVariableName (string name)
		{
			// Check for a type attribute and strip it off.
			var index = name.LastIndexOf (" [", StringComparison.Ordinal);

			if (index != -1)
				return name.Remove (index);

			return name;
		}

		static bool IsMultiDimensionalArray (string type, out int arrayIndexer)
		{
			int index = type.IndexOfAny (LessThanOrSquareBracket);

			arrayIndexer = -1;

			if (index == -1)
				return false;

			if (type[index] == '<') {
				int depth = 1;

				index++;
				while (index < type.Length && depth > 0) {
					switch (type[index++]) {
					case '<': depth++; break;
					case '>': depth--; break;
					}
				}

				if (index >= type.Length || type[index] != '[')
					return false;
			}

			arrayIndexer = index++;

			return index < type.Length && type[index] == ',';
		}

		// Note: displayType will often have spaces after commas
		string GetFixedValue (string value, string canonType, string displayType)
		{
			int arrayIndex;

			if (IsMultiDimensionalArray (displayType, out arrayIndex)) {
				var arrayType = displayType.Substring (0, arrayIndex);
				var prefix = $"{{{arrayType}[";

				if (value.StartsWith (prefix, StringComparison.Ordinal)) {
					var compacted = new StringBuilder (prefix.Replace (", ", ","));
					int index = prefix.Length;

					while (index < value.Length) {
						int endIndex = value.IndexOfAny (CommaDotOrSquareEndBracket, index);
						string number;

						if (endIndex == -1)
							return value;

						if (endIndex + 1 < value.Length && value[endIndex] == '.' && value[endIndex + 1] == '.') {
							int min, max;

							number = value.Substring (index, endIndex - index);

							if (!int.TryParse (number, NumberStyles.Integer, CultureInfo.InvariantCulture, out min))
								return value;

							index = endIndex + 2;

							if ((endIndex = value.IndexOfAny (CommaOrSquareEndBracket, index)) == -1)
								return value;

							number = value.Substring (index, endIndex - index);

							if (!int.TryParse (number, NumberStyles.Integer, CultureInfo.InvariantCulture, out max))
								return value;

							compacted.Append (((max - min) + 1).ToString (CultureInfo.InvariantCulture));
						} else {
							compacted.Append (value, index, endIndex - index);
						}

						compacted.Append (value[endIndex]);
						index = endIndex + 1;

						if (value[endIndex] == ']')
							break;

						if (index < value.Length && value[index] == ' ')
							index++;
					}

					compacted.Append ('}');

					return compacted.ToString ();
				}
			} else if (canonType == "char") {
				int startIndex = value.IndexOf ('\'');

				if (startIndex != -1)
					return value.Substring (startIndex);
			} else {
				var request = new EvaluateRequest ($"typeof ({displayType}).IsEnum") { FrameId = frameId };
				var result = vsCodeDebuggerSession.protocolClient.SendRequestSync (request);

				if (result.Result.Equals ("true", StringComparison.OrdinalIgnoreCase)) {
					int endIndex = value.IndexOf (" | ", StringComparison.Ordinal);

					if (endIndex != -1) {
						// The value a bitwise-or'd set of enum values
						var expanded = new StringBuilder ();
						int index = 0;

						while (index < value.Length) {
							endIndex = value.IndexOf (" | ", index, StringComparison.Ordinal);
							string enumValue;

							if (endIndex != -1)
								enumValue = value.Substring (index, endIndex - index);
							else if (index > 0)
								enumValue = value.Substring (index);
							else
								enumValue = value;

							expanded.Append (canonType).Append ('.').Append (enumValue);

							if (endIndex == -1)
								break;

							expanded.Append (" | ");
							index = endIndex + 3;
						}

						return expanded.ToString ();
					}

					return canonType + "." + value;
				}
			}

			return value;
		}

		static bool IsCSError (int code, string message, string value, out string newValue)
		{
			var prefix = string.Format (CultureInfo.InvariantCulture, "error CS{0:D4}: '", code);

			newValue = null;

			if (value == null || !value.StartsWith (prefix, StringComparison.Ordinal))
				return false;

			int startIndex = prefix.Length;
			int index = startIndex;

			while (index < value.Length && value[index] != '\'')
				index++;

			newValue = value.Substring (startIndex, index - startIndex);
			index++;

			if (index >= value.Length || value[index] != ' ')
				return false;

			index++;

			if (index + message.Length != value.Length)
				return false;

			return string.CompareOrdinal (value, index, message, 0, message.Length) == 0;
		}

		//static bool IsCSError (string text, out int code, out string message)
		//{
		//	message = null;
		//	code = -1;

		//	if (!text.StartsWith ("error CS", StringComparison.Ordinal))
		//		return false;

		//	int startIndex = "error CS".Length;

		//	if (startIndex + 6 >= text.Length)
		//		return false;

		//	for (int i = startIndex; i < startIndex + 4; i++) {
		//		if (!char.IsDigit (text[i]))
		//			return false;
		//	}

		//	if (text[startIndex + 4] != ':' || text[startIndex + 5] != ' ')
		//		return false;

		//	code = int.Parse (text.Substring (startIndex, 4), NumberStyles.None, CultureInfo.InvariantCulture);
		//	message = text.Substring (startIndex + 6);

		//	return true;
		//}

		public VSCodeObjectSource (VSCodeDebuggerSession vsCodeDebuggerSession, int variablesReference, int parentVariablesReference, string name, string type, string evalName, int frameId, string val)
		{
			this.vsCodeDebuggerSession = vsCodeDebuggerSession;
			this.parentVariablesReference = parentVariablesReference;
			this.variablesReference = variablesReference;
			this.evalName = evalName;
			this.frameId = frameId;

			if (type == null) {
				if (IsCSError (118, "is a namespace but is used like a variable", val, out string ns)) {
					this.display = this.name = this.val = ns;
					this.flags = ObjectValueFlags.Namespace;
					this.type = "<namespace>";
					return;
				}

				if (IsCSError (119, "is a type, which is not valid in the given context", val, out string vtype)) {
					if (name.StartsWith ("global::", StringComparison.Ordinal))
						vtype = name.Substring ("global::".Length);

					this.display = this.name = this.val = ObjectValueAdaptor.GetCSharpTypeName (vtype);
					this.flags = ObjectValueFlags.Type;
					this.type = "<type>";
					return;
				}
			}

			var actualType = GetActualTypeName (type);

			this.flags = parentVariablesReference > 0 ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly;
			this.type = actualType.Replace (", ", ",");
			this.name = GetFixedVariableName (name);

			if (actualType != "void")
				this.val = GetFixedValue (val, this.type, actualType);
			else
				this.val = "No return value.";
			this.display = val;

			if (this.name[0] == '[')
				flags |= ObjectValueFlags.ArrayElement;

			if (type == null || val == $"'{this.name}' threw an exception of type '{this.type}'")
				flags |= ObjectValueFlags.Error;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			if (objValChildren == null) {
				if (variablesReference <= 0) {
					objValChildren = new ObjectValue[0];
				} else {
					using (var timer = vsCodeDebuggerSession.EvaluationStats.StartTimer ()) {
						var children = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (
							variablesReference
						)).Variables;
						objValChildren = children.Select (c => VSCodeDebuggerBacktrace.VsCodeVariableToObjectValue (vsCodeDebuggerSession, c.Name, c.EvaluateName, c.Type, c.Value, c.VariablesReference, variablesReference, frameId)).ToArray ();
						timer.Success = true;
					}
				}
			}
			return objValChildren;
		}

		class RawString : IRawValueString
		{
			string val;

			public RawString (string val)
			{
				this.val = val;
			}

			public int Length {
				get {
					return val.Length;
				}
			}

			public string Value {
				get {
					return val;
				}
			}

			public string Substring (int index, int length)
			{
				return val.Substring (index, length);
			}
		}

		static string Quote (string text)
		{
			var quoted = new StringBuilder (text.Length + 2);

			quoted.Append ('"');
			for (int i = 0; i < text.Length; i++) {
				char c = text [i];

				switch (c) {
				case '\0': quoted.Append ("\\0"); break;
				case '\a': quoted.Append ("\\a"); break;
				case '\b': quoted.Append ("\\b"); break;
				case '\n': quoted.Append ("\\n"); break;
				case '\r': quoted.Append ("\\r"); break;
				case '\t': quoted.Append ("\\t"); break;
				case '\v': quoted.Append ("\\v"); break;
				case '"': quoted.Append ("\\\""); break;
				case '\\': quoted.Append ("\\\\"); break;
				default:
					if (c < ' ') {
						quoted.AppendFormat (CultureInfo.InvariantCulture, "\\x{0:2}", c);
					} else {
						quoted.Append (c);
					}
					break;
				}
			}
			quoted.Append ('"');

			return quoted.ToString ();
		}

		static string Unquote (string text)
		{
			var unquoted = new char[text.Length - 2];
			bool escaped = false;
			int count = 0;

			for (int i = 1; i < text.Length - 1; i++) {
				char c = text[i];

				switch (c) {
				case '\\':
					if (escaped)
						unquoted[count++] = '\\';
					escaped = !escaped;
					break;
				case '0':
					unquoted[count++] = escaped ? '\0' : c;
					escaped = false;
					break;
				case 'a':
					unquoted[count++] = escaped ? '\a' : c;
					escaped = false;
					break;
				case 'b':
					unquoted[count++] = escaped ? '\b' : c;
					escaped = false;
					break;
				case 'n':
					unquoted[count++] = escaped ? '\n' : c;
					escaped = false;
					break;
				case 'r':
					unquoted[count++] = escaped ? '\r' : c;
					escaped = false;
					break;
				case 't':
					unquoted[count++] = escaped ? '\t' : c;
					escaped = false;
					break;
				case 'v':
					unquoted[count++] = escaped ? '\v' : c;
					escaped = false;
					break;
				default:
					unquoted[count++] = c;
					escaped = false;
					break;
				}
			}

			return new string (unquoted, 0, count);
		}

		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			string rawValue;

			if (type == "string") {
				// We already have this cached
				rawValue = val;

				if (rawValue.Length >= 2 && rawValue[0] == '"' && rawValue[rawValue.Length - 1] == '"')
					rawValue = Unquote (rawValue);

				return new RawValueString (new RawString (rawValue));
			}

			//using (var timer = vsCodeDebuggerSession.EvaluationStats.StartTimer ()) {
			//	rawValue = vsCodeDebuggerSession.protocolClient.SendRequestSync (new EvaluateRequest (evalName) { FrameId = frameId }).Result;
			//	timer.Success = true;
			//}

			throw new NotImplementedException ();
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			if (val == "null")
				return ObjectValue.CreateNullObject (this, name, type, flags);
			if (variablesReference == 0)//This is some kind of primitive...
				return ObjectValue.CreatePrimitive (this, new ObjectPath (name), type, new EvaluationResult (val, display), flags);
			return ObjectValue.CreateObject (this, new ObjectPath (name), type, new EvaluationResult (val, display), flags, null);
		}

		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			var v = value.ToString ();

			if (type == "string")
				v = Quote (v);

			vsCodeDebuggerSession.protocolClient.SendRequestSync (new SetVariableRequest (parentVariablesReference, name, v));
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			return new EvaluationResult (vsCodeDebuggerSession.protocolClient.SendRequestSync (new SetVariableRequest (parentVariablesReference, name, value)).Value);
		}
	}
}
