using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	static class Sublime3Format
	{
		public static SyntaxHighlightingDefinition ReadHighlighting (TextReader input)
		{
			input.ReadLine ();
			input.ReadLine ();
			string name = null, scope = null, firstLineMatch = null;
			var variables = new Dictionary<string, string> ();
			bool hidden = false;
			var extensions = new List<string> ();
			var contexts = new List<SyntaxContext> ();

			var yaml = new YamlStream ();
			yaml.Load (input);
			var mapping = (YamlMappingNode)yaml.Documents [0].RootNode;

			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "variables":
					foreach (var captureEntry in ((YamlMappingNode)entry.Value).Children) {
						variables [((YamlScalarNode)captureEntry.Key).Value] = ((YamlScalarNode)captureEntry.Value).Value;
					}
					break;
				}
			}

			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "name":
					name = ((YamlScalarNode)entry.Value).Value;
					break;
				case "file_extensions":
					foreach (var nn in entry.Value.AllNodes.OfType<YamlScalarNode> ()) {
						extensions.Add (nn.Value);
					}
					break;
				case "scope":
					scope = ((YamlScalarNode)entry.Value).Value;
					break;
				case "hidden":
					try {
						hidden = bool.Parse (((YamlScalarNode)entry.Value).Value);
					} catch (Exception e) {
						LoggingService.LogError ("Error while parsing hidden flag of " + name, e);
					}
					break;
				case "first_line_match":
					firstLineMatch = CompileRegex (((YamlScalarNode)entry.Value).Value);
					break;
				case "contexts":
					foreach (var contextMapping in ((YamlMappingNode)entry.Value).Children) {
						contexts.Add (ReadContext (contextMapping, variables));
					}
					break;
				}
			}
			return new SyntaxHighlightingDefinition (name, scope, firstLineMatch, hidden, extensions, contexts);
		}


		internal static SyntaxContext ReadContext (KeyValuePair<YamlNode, YamlNode> mapping, Dictionary<string, string> variables)
		{
			var result = new SyntaxContext (((YamlScalarNode)mapping.Key).Value);
			result.ParseMapping (mapping.Value as YamlSequenceNode, variables);
			return result;
		}

		internal static SyntaxMatch ReadMatch (YamlMappingNode mapping, Dictionary<string, string> variables)
		{
			string match = null, scope = null;
			var captures  = new List<Tuple<int, string>> ();

			ContextReference push = null, set = null;
			bool pop = false;
			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "match":
					match = CompileRegex (ReplaceVariables (((YamlScalarNode)entry.Value).Value, variables));
					break;
				case "scope":
					scope = ((YamlScalarNode)entry.Value).Value;
					break;
				case "captures":
					foreach (var captureEntry in ((YamlMappingNode)entry.Value).Children) {
						captures.Add (
							Tuple.Create (
								int.Parse (((YamlScalarNode)captureEntry.Key).Value),
								((YamlScalarNode)captureEntry.Value).Value
							)
						);
					}
					break;
				case "push":
					push = ReadContextReference (entry.Value, variables);
					break;
				case "pop":
					// according to the spec the only accepted value
					pop = true;
					break;
				case "set":
					set = ReadContextReference (entry.Value, variables);
					break;
				case "syntax":
					// ?
					break;
				}
			}
			return new SyntaxMatch (match, scope, captures, push, pop, set);
		}

		internal static ContextReference ReadContextReference (YamlNode value, Dictionary<string, string> variables)
		{
			var seq = value as YamlSequenceNode;
			if (seq != null) {
				var l = seq.Children.OfType<YamlScalarNode> ().Select (s => s.Value).ToList ();
				if (l.Count > 0) {
					return new ContextNameListContextReference (l);
				}

				return ReadAnonymousMatchContextReference (seq, variables);
			}
			if (value.NodeType == YamlNodeType.Scalar)
				return new ContextNameContextReference (((YamlScalarNode)value).Value);
			return null;
		}

		internal static ContextReference ReadAnonymousMatchContextReference (YamlSequenceNode seq, Dictionary<string, string> variables)
		{
			var ctx = new SyntaxContext ("__Anonymous__");

			foreach (var c in seq.Children.OfType<YamlMappingNode> ()) {
				ctx.ParseMapping (c, variables);
			}

			return new AnonymousMatchContextReference (ctx);
		}

		static string ReplaceVariables (string str, Dictionary<string, string> variables)
		{
			if (string.IsNullOrEmpty (str))
				return str;

			var result = new StringBuilder ();
			var wordBuilder = new StringBuilder ();
			bool recordWord = false;
			for (int i = 0; i < str.Length - 1; i++) {
				var ch = str [i];
				var next = str [i + 1];
				if (ch == '{' && next == '{') {
					i++;
					recordWord = true;
					continue;
				}
				if (ch == '}' && next == '}' && recordWord) {
					i++;
					recordWord = false;
					string replacement;
					if (variables.TryGetValue (wordBuilder.ToString (), out replacement)) {
						result.Append (replacement);
					} else { 
						LoggingService.LogWarning ("Sublime3Format: Can't find variable " + wordBuilder.ToString ());
					}
					wordBuilder.Length = 0;
					if (i >= str.Length - 1)
						return result.ToString ();
					continue;
				}

				if (recordWord) {
					wordBuilder.Append (ch);
				} else {
					result.Append (ch);
				}
			}
			result.Append (str [str.Length - 1]);

			return result.ToString ();
		}


		// translates ruby regex -> .NET regexes
		static string CompileRegex (string regex)
		{
			if (string.IsNullOrEmpty (regex))
				return regex;
			var result = new StringBuilder ();
			var wordBuilder = new StringBuilder ();
			bool recordWord = false, inCharacterClass = false;

			for (int i = 0; i < regex.Length - 1; i++) {
				var ch = regex [i];
				var next = regex [i + 1];
				switch (ch) {
				case '\\':
					if (next == 'h') {
						result.Append ("[0-9a-fA-F]");
						i++;
						continue;
					}
					if (next == 'H') {
						result.Append ("[^0-9a-fA-F]");
						i++;
						continue;
					}
					break;
				case '[':
					if (next == ':') {
						recordWord = true;
						i++;
						continue;
					}
					inCharacterClass = true;
					break;
				case '?':
					if (next == '=') { // (?=...) -> (?:...)
						result.Append ("?:");
						i++;
						continue;
					}
					break;
				case ']':
					inCharacterClass = false;
					break;

				case ':':
					if (next == ']' && recordWord) {
						result.Append (ConvertUnicodeCategory (wordBuilder.ToString (), inCharacterClass));
						i++;
						if (i >= regex.Length - 1)
							return result.ToString ();
						wordBuilder.Length = 0;
						recordWord = false;
						continue;
					}
					break;
				}
				if (recordWord) {
					wordBuilder.Append (ch);
				} else {
					result.Append (ch);
				}
			}
			result.Append (regex [regex.Length - 1]);
			return result.ToString ();
		}

		static string ConvertUnicodeCategory (string category, bool inCharacterClass)
		{
			switch (category) {
			case "alnum": // Alphabetic and numeric character
				return inCharacterClass ? "\\w\\d": "[\\w\\d]";
			case "alpha": // Alphabetic character
				return "\\w";
			case "blank": // Space or tab
				return inCharacterClass ? " \t" : "[ \t]";
			case "cntrl": // Control character
				return "\\W"; // TODO
			case "digit": // Digit
				return "\\d";
			case "graph": // Non - blank character (excludes spaces, control characters, and similar)
				return "\\S";
			case "lower": // Lowercase alphabetical character
				return inCharacterClass ? "a-z" : "[a-z]";
			case "print": // Like [:graph:], but includes the space character
				return inCharacterClass ? "\\S\\ " : "[\\S\\ ]";
			case "punct": // Punctuation character
				return "\\W"; // TODO
			case "space": // Whitespace character ([:blank:], newline, carriage return, etc.)
				return "\\s";
			case "upper": // Uppercase alphabetical
				return inCharacterClass ? "A-Z" : "[A-Z]";
			case "xdigit": // Digit allowed in a hexadecimal number (i.e., 0 - 9a - fA - F)
				return inCharacterClass ? "0-9a-fA-F" : "[0-9a-fA-F]";
			}
			LoggingService.LogWarning ("unknown unicode category : " + category);
			return "";
		}
	}
}