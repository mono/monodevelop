using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using Mono.Debugging.Client;
using Mono.Debugging.Backend;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	abstract class VSCodeObjectSource : IObjectValueSource
	{
		static readonly char[] CommaDotOrSquareEndBracket = { ',', '.', ']' };
		static readonly char[] CommaOrSquareEndBracket = { ',', ']' };
		static readonly char[] LessThanOrSquareBracket = { '<', '[' };

		ObjectValue[] objValChildren;

		protected VSCodeObjectSource (VSCodeDebuggerSession session, int parentVariablesReference, int frameId)
		{
			ParentVariablesReference = parentVariablesReference;
			Session = session;
			FrameId = frameId;
		}

		protected abstract string Display {
			get;
		}

		protected abstract string Expression {
			get;
		}

		protected ObjectValueFlags Flags {
			get; set;
		}

		protected int FrameId {
			get; private set;
		}

		protected abstract string Name {
			get;
		}

		protected int ParentVariablesReference {
			get;
		}

		protected VSCodeDebuggerSession Session {
			get; private set;
		}

		protected abstract string Type {
			get;
		}

		protected abstract string Value {
			get;
		}

		protected abstract int VariablesReference {
			get;
		}

		protected ObjectValueFlags GetFlags (VariablePresentationHint hint)
		{
			var flags = ObjectValueFlags.None;

			if (hint != null) {
				if (hint.Attributes.HasValue) {
					var attributes = hint.Attributes.Value;

					if ((attributes & VariablePresentationHint.AttributesValue.FailedEvaluation) != 0)
						return ObjectValueFlags.Error;
					if ((attributes & VariablePresentationHint.AttributesValue.Constant) != 0)
						flags |= ObjectValueFlags.Literal;
					if ((attributes & VariablePresentationHint.AttributesValue.ReadOnly) != 0)
						flags |= ObjectValueFlags.ReadOnly;
					if ((attributes & VariablePresentationHint.AttributesValue.Static) != 0)
						flags |= ObjectValueFlags.ReadOnly;
					if ((attributes & VariablePresentationHint.AttributesValue.CanHaveObjectId) != 0)
						flags |= ObjectValueFlags.Primitive;
					if ((attributes & VariablePresentationHint.AttributesValue.HasObjectId) != 0)
						flags |= ObjectValueFlags.Object;
				}

				if (hint.Kind.HasValue) {
					var kind = hint.Kind.Value;

					if ((kind & VariablePresentationHint.KindValue.Property) != 0)
						flags |= ObjectValueFlags.Property;
					if ((kind & VariablePresentationHint.KindValue.BaseClass) != 0)
						flags |= ObjectValueFlags.Type;
					if ((kind & VariablePresentationHint.KindValue.Class) != 0)
						flags |= ObjectValueFlags.Type;
					if ((kind & VariablePresentationHint.KindValue.InnerClass) != 0)
						flags |= ObjectValueFlags.Type;
					if ((kind & VariablePresentationHint.KindValue.Interface) != 0)
						flags |= ObjectValueFlags.Type;
					if ((kind & VariablePresentationHint.KindValue.MostDerivedClass) != 0)
						flags |= ObjectValueFlags.Type;
					if ((kind & VariablePresentationHint.KindValue.Data) != 0)
						flags |= ObjectValueFlags.Variable;
				}

				if (hint.Visibility.HasValue) {
					var visibility = hint.Visibility.Value;

					if ((visibility & VariablePresentationHint.VisibilityValue.Protected) != 0)
						flags |= ObjectValueFlags.Protected;
					if ((visibility & VariablePresentationHint.VisibilityValue.Internal) != 0)
						flags |= ObjectValueFlags.Internal;
					if ((visibility & VariablePresentationHint.VisibilityValue.Private) != 0)
						flags |= ObjectValueFlags.Private;
					if ((visibility & VariablePresentationHint.VisibilityValue.Public) != 0)
						flags |= ObjectValueFlags.Public;
				}
			}

			return flags;
		}

		protected static string GetActualTypeName (string type)
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

		bool IsPotentialEnumValue (string value)
		{
			if (string.IsNullOrEmpty (value))
				return false;

			// Note: An enum value must begin with a letter
			if (!char.IsLetter (value[0]))
				return false;

			// Note: if the value has a '|', it's probably an enum (can it be anything else?)
			if (value.IndexOf ('|') != -1)
				return true;

			for (int i = 1; i < value.Length; i++) {
				if (char.IsLetterOrDigit (value[i]) || value[i] == '_')
					continue;

				return false;
			}

			return true;
		}

		bool IsEnum (string displayType, string value)
		{
			if (string.IsNullOrEmpty (displayType))
				return false;

			// Note: generic types cannot be enums
			if (displayType[displayType.Length - 1] == '>')
				return false;

			// Note: true and false look like enum values but aren't
			if (displayType.Equals ("bool", StringComparison.Ordinal))
				return false;

			if (!IsPotentialEnumValue (value))
				return false;

			return Session.IsEnum (displayType, FrameId);
		}

		// Note: displayType will often have spaces after commas
		protected string GetFixedValue (string value, string canonType, string displayType)
		{
			int arrayIndex;

			if (value.Equals ("null", StringComparison.Ordinal))
				return value;

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
			} else if (canonType.Equals ("char", StringComparison.Ordinal)) {
				int startIndex = value.IndexOf ('\'');

				if (startIndex != -1)
					return value.Substring (startIndex);
			} else if (IsEnum (displayType, value)) {
				int endIndex = value.IndexOf (" | ", StringComparison.Ordinal);

				if (endIndex != -1) {
					// The value is a bitwise-or'd set of enum values
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

			return value;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			if (objValChildren == null) {
				if (VariablesReference > 0) {
					using (var timer = Session.EvaluationStats.StartTimer ()) {
						var response = Session.protocolClient.SendRequestSync (new VariablesRequest (VariablesReference));
						var children = new List<ObjectValue> ();

						foreach (var variable in response.Variables) {
							var source = new VSCodeVariableSource (Session, variable, VariablesReference, FrameId);
							children.Add (source.GetValue (default (ObjectPath), null));
						}

						objValChildren = children.ToArray ();
						timer.Success = true;
					}
				} else {
					objValChildren = new ObjectValue[0];
				}
			}

			return objValChildren;
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
			var unquoted = new char [text.Length - 2];
			bool escaped = false;
			int count = 0;

			for (int i = 1; i < text.Length - 1; i++) {
				char c = text [i];

				switch (c) {
				case '\\':
					if (escaped)
						unquoted [count++] = '\\';
					escaped = !escaped;
					break;
				case '0':
					unquoted [count++] = escaped ? '\0' : c;
					escaped = false;
					break;
				case 'a':
					unquoted [count++] = escaped ? '\a' : c;
					escaped = false;
					break;
				case 'b':
					unquoted [count++] = escaped ? '\b' : c;
					escaped = false;
					break;
				case 'n':
					unquoted [count++] = escaped ? '\n' : c;
					escaped = false;
					break;
				case 'r':
					unquoted [count++] = escaped ? '\r' : c;
					escaped = false;
					break;
				case 't':
					unquoted [count++] = escaped ? '\t' : c;
					escaped = false;
					break;
				case 'v':
					unquoted [count++] = escaped ? '\v' : c;
					escaped = false;
					break;
				default:
					unquoted [count++] = c;
					escaped = false;
					break;
				}
			}

			return new string (unquoted, 0, count);
		}

		class RawString : IRawValueString
		{
			public RawString (string value)
			{
				Value = value;
			}

			public int Length {
				get { return Value.Length; }
			}

			public string Value {
				get; private set;
			}

			public string Substring (int index, int length)
			{
				return Value.Substring (index, length);
			}
		}

		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			// Note: If the type is a string, then we already have the full value
			if (Type.Equals ("string", StringComparison.Ordinal)) {
				var rawValue = Unquote (Value);

				return new RawValueString (new RawString (rawValue));
			}

			//using (var timer = Session.EvaluationStats.StartTimer ()) {
			//	var response = Session.protocolClient.SendRequestSync (new EvaluateRequest (Expression) { FrameId = FrameId });
			//	var rawValue = response.Result;
			//	timer.Success = true;
			//}

			throw new NotImplementedException ();
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			if (Value == "null")
				return ObjectValue.CreateNullObject (this, Name, Type, Flags);

			if (VariablesReference == 0) // This is some kind of primitive...
				return ObjectValue.CreatePrimitive (this, new ObjectPath (Name), Type, new EvaluationResult (Value, Display), Flags);

			return ObjectValue.CreateObject (this, new ObjectPath (Name), Type, new EvaluationResult (Value, Display), Flags, null);
		}

		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			var v = value.ToString ();

			if (Type == "string")
				v = Quote (v);

			Session.protocolClient.SendRequestSync (new SetVariableRequest (ParentVariablesReference, Name, v));
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			return new EvaluationResult (Session.protocolClient.SendRequestSync (new SetVariableRequest (ParentVariablesReference, Name, value)).Value);
		}
	}
}
