using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using System.Text;
using MonoDevelop.Core;
using System.Diagnostics;
using System.Xml;
using System.Globalization;

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
						extensions.Add ("." + nn.Value);
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
			string match = null;
			List<string> scope = new List<string> ();
			var captures  = new List<Tuple<int, string>> ();

			ContextReference push = null, set = null, withPrototype = null;
			bool pop = false;
			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "match":
					match = CompileRegex (ReplaceVariables (((YamlScalarNode)entry.Value).Value, variables));
					break;
				case "scope":
					ParseScopes (scope, ((YamlScalarNode)entry.Value).Value);
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
				case "with_prototype":
					withPrototype = ReadContextReference (entry.Value, variables);
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
			return new SyntaxMatch (match, scope, new Captures (captures), push, pop, set, withPrototype);
		}

		internal static void ParseScopes (List<string> scope, string value)
		{
			if (value == null)
				return;
			scope.AddRange (value.Split (new [] { " " }, StringSplitOptions.RemoveEmptyEntries));
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
			var ctx = new SyntaxContext ("__Anonymous__", new List<object> (), metaIncludePrototype: false);

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
						result.Append (ReplaceVariables (replacement, variables));
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


		class CharacterClass
		{
			bool first = true;
			bool negativeGroup;
			StringBuilder wordBuilder;
			StringBuilder unicodeGroupBuilder = new StringBuilder ();

			bool hasLast;
			bool escape, range, readUnicodeGroup;
			char lastChar = '\0';
			char lastPushedChar;
			int[] table = new int [256];
			StringBuilder org = new StringBuilder ();
			List<string> unicodeGroups = new List<string> ();
			CharacterClass subClass;

			public void Push (char ch)
			{
				org.Append (ch);
				switch (ch) {
				case ':':
					if (escape)
						goto default;
					if (first || lastPushedChar == '[') {
						wordBuilder = new StringBuilder ();
						subClass = new CharacterClass ();
						subClass.Add (':');
					} else {
						if (wordBuilder != null) {
							ConvertUnicodeCategory (wordBuilder.ToString ());
							wordBuilder = null;
							subClass = null;
						} else
							goto default;
					}
					break;
				case '^':
					if (first) {
						negativeGroup = true;
						break;
					}
					goto default;
				case '{':
					if (readUnicodeGroup)
						return;
					goto default;
				case '}':
					if (readUnicodeGroup) {
						readUnicodeGroup = false;
						unicodeGroups.Add (unicodeGroupBuilder.ToString ());
						unicodeGroupBuilder.Length = 0;
						return;
					}
					goto default;
				case '[':
					if (escape)
						goto default;
					PushLastChar ();
					break;
				case ']':
					if (escape)
						goto default;
					break;
				case '\\':
					if (escape || wordBuilder != null)
						goto default;
					escape = true;
					break;
				case '-':
					if (escape || first || wordBuilder != null)
						goto default;
					range = true;
					break;
				default:
					if (readUnicodeGroup) {
						unicodeGroupBuilder.Append (ch);
						return;
					}
					if (escape) {
						PushLastChar ();

						switch (ch) {
						case 'w': // A word character ([a - zA - Z0 - 9_])
							AddRange ('a', 'z');
							AddRange ('A', 'Z');
							AddRange ('0', '9');
							table ['_'] = negativeGroup ? -1 : 1;
							break;
						case 'W': // A non-word character ([^a-zA-Z0-9_]). 
							for (int i = 0; i < table.Length; i++) {
								if ('0' <= i && i <= '9')
									continue;
								if ('a' <= i && i <= 'z')
									continue;
								if ('A' <= i && i <= 'Z')
									continue;
								if (i == '_')
									continue;
								table [i] = negativeGroup ? -1 : 1;
							}
							break;
						case 'd': // A digit character ([0-9])
							AddRange ('0', '9');
							break;
						case 'D': // A non-digit character ([^0-9])
							for (int i = 0; i < table.Length; i++) {
								if ('0' <= i && i <= '9')
									continue;
								table [i] = negativeGroup ? -1 : 1;
							}
							break;
						case 'h': // A hexdigit character ([0-9a-fA-F])
							AddRange ('0', '9');
							AddRange ('a', 'f');
							AddRange ('A', 'F');
							break;
						case 'H': // A non-hexdigit character ([^0-9a-fA-F])
							for (int i = 0; i < table.Length; i++) {
								if ('0' <= i && i <= '9')
									continue;
								if ('a' <= i && i <= 'f')
									continue;
								if ('A' <= i && i <= 'F')
									continue;
								table [i] = negativeGroup ? -1 : 1;
							}
							break;
						case 's': // A whitespace character: /[ \t\r\n\f]/
							table [' '] = negativeGroup ? -1 : 1;
							table ['\t'] = negativeGroup ? -1 : 1;
							table ['\r'] = negativeGroup ? -1 : 1;
							table ['\n'] = negativeGroup ? -1 : 1;
							table ['\f'] = negativeGroup ? -1 : 1;
							break;
						case 'S': // A non-whitespace character: /[^ \t\r\n\f]/
							for (int i = 0; i < table.Length; i++) {
								if (" \\t\\r\\n\\f".Contains ((char)i))
									continue;
								table [i] = negativeGroup ? -1 : 1;
							}
							break;
						case 'a':
							table ['\a'] = negativeGroup ? -1 : 1;
							break;
						case 'b':
							table ['\b'] = negativeGroup ? -1 : 1;
							break;
						case 't':
							table ['\t'] = negativeGroup ? -1 : 1;
							break;
						case 'r':
							table ['\r'] = negativeGroup ? -1 : 1;
							break;
						case 'v':
							table ['\v'] = negativeGroup ? -1 : 1;
							break;
						case 'f':
							table ['\f'] = negativeGroup ? -1 : 1;
							break;
						case 'n':
							table ['\n'] = negativeGroup ? -1 : 1;
							break;
						case 'p':
							readUnicodeGroup = true;
							break;
						default:
							lastChar = ch;
							hasLast = true;
							PushLastChar ();
							break;
						}

						escape = false;
						break;
					}

					if (wordBuilder != null) {
						wordBuilder.Append (ch);
						subClass.Push (ch);
						break;
					}

					if (!hasLast) {
						lastChar = ch;
						hasLast = true;
						break;
					}
					if (range) {
						for (int i = (int)lastChar; i <= (int)ch; i++) {
							table [i] = negativeGroup ? -1 : 1;
						}
						hasLast = false;
					} else {
						PushLastChar ();
						lastChar = ch;
						hasLast = true;
					}
					range = false;
					break;
				}
				first = false;
				lastPushedChar = ch;
			}

			void PushLastChar ()
			{
				if (hasLast) {
					table [(int)lastChar] = negativeGroup ? -1 : 1;
					hasLast = false;
				}
			}

			void AddRange (char fromC, char toC)
			{
				for (int i = fromC; i <= toC; i++) {
					table [i] = negativeGroup ? -1 : 1;
				}
			}

			void Add (char ch)
			{
				table [ch] = negativeGroup ? -1 : 1;
			}

			void AddUnicodeCategory (params UnicodeCategory[] categories)
			{
				for (int i = 0; i < table.Length; i++) {
					var cat = char.GetUnicodeCategory ((char)i);
					if (categories.Contains (cat)) {
						table [i] = negativeGroup ? -1 : 1;
					}
				}
			}

			bool HasRange (char fromC, char toC)
			{
				for (int i = fromC; i <= toC; i++) {
					if (table [i] == 0)
						return false;
				}
				return true;
			}
			
			bool Has (string v)
			{
				foreach (var ch in v) {
					if (table [ch] == 0)
						return false;
				}
				return true;
			}
			
			void RemoveRange (char fromC, char toC)
			{
				for (int i = fromC; i <= toC; i++) {
					table [i] = 0;
				}
			}


			void PrepareGeneration ()
			{
				PushLastChar ();

				if (range)
					table ['-'] = negativeGroup ? -1 : 1;
				if (escape)
					table ['\\'] = negativeGroup ? -1 : 1;

			}
			public string Generate ()
			{
				if (subClass != null) {
					subClass.PrepareGeneration ();
					for (int i = 0; i < table.Length; i++) {
						if (subClass.table [i] != 0)
							table [i] = subClass.table [i];
					}
				}
				PrepareGeneration ();
				var result = new StringBuilder ();
				result.Append ('[');
				if (negativeGroup)
					result.Append ('^');
				
				bool hasAllPrintable = true;
				for (int i = 0; i < table.Length; i++) {
					var ch = (char)i;
					if (ch == ' ' || ch == '\t')
						continue;
					var cat = char.GetUnicodeCategory (ch);
					if (cat == UnicodeCategory.Control || cat == UnicodeCategory.LineSeparator)
						continue;
					if (table [i] == 0) {
						hasAllPrintable = false;
					}
				}

				if (hasAllPrintable) {
					for (int i = 0; i < table.Length; i++) {
						var ch = (char)i;
						if (ch == ' ' || ch == '\t')
							continue;
						var cat = char.GetUnicodeCategory (ch);
						if (cat == UnicodeCategory.Control || cat == UnicodeCategory.LineSeparator)
							continue;
						table [i] = 0;
					}
					result.Append ("\\S");
				}

				if (HasRange ('a', 'z') && HasRange ('A', 'Z')) {
					RemoveRange ('a', 'z');
					RemoveRange ('A', 'Z');
					result.Append ("\\w");
				}

				if (HasRange ('0', '9')) {
					RemoveRange ('0', '9');
					result.Append ("\\d");
				}

				if (Has (" \t\r\n")) {
					result.Append ("\\s");
					table [' '] = 0;
					table ['\t'] = 0;
					table ['\r'] = 0;
					table ['\n'] = 0;
					table ['\f'] = 0;
				}

				for (int i = 0; i < table.Length; i++) {
					int cur = table [i];
					if (cur != 0) {
						AddChar (result, (char)i);
						int j = i + 1;
						for (; j < table.Length; j++) {
							if (table [j] == 0)
								break;
						}
						if (j - i > 3) {
							result.Append ("-");
							AddChar (result, (char)(j - 1));
							i = j - 1;
						}
					}
				}
				foreach (var grp in unicodeGroups) {
					result.Append ("\\p{");
					result.Append (grp);
					result.Append ("}");
				}
				result.Append (']');
				return result.ToString ();
			}

			void AddChar (StringBuilder result, char ch)
			{
				if ("[]\\".Contains (ch)) {
					result.Append ('\\');
					result.Append (ch);
					return;
				}
				switch (ch) {
				case '\a':
					result.Append ("\\a");
					break;
				case '\b':
					result.Append ("\\b");
					break;
				case '\t':
					result.Append ("\\t");
					break;
				case '\r':
					result.Append ("\\r");
					break;
				case '\v':
					result.Append ("\\v");
					break;
				case '\f':
					result.Append ("\\f");
					break;
				case '\n':
					result.Append ("\\n");
					break;
				default:
					result.Append (ch);
					break;
				}
			}

			void ConvertUnicodeCategory (string category)
			{
				switch (category) {
				case "alnum": // Alphabetic and numeric character
							  //return inCharacterClass ? "\\w\\d": "[\\w\\d]";
					AddRange ('a', 'z');
					AddRange ('A', 'Z');
					AddRange ('0', '9');
					break;
				case "alpha": // Alphabetic character
					AddRange ('a', 'z');
					AddRange ('A', 'Z');
					break;
				case "blank": // Space or tab
					Add (' ');
					Add ('\t');
					break;
				case "cntrl": // Control character
					AddUnicodeCategory (UnicodeCategory.Control);
					break;
				case "digit": // Digit
					AddRange ('0', '9');
					break;
				case "graph": // Non - blank character (excludes spaces, control characters, and similar)
					for (int i = 0; i < table.Length; i++) {
						var ch = (char)i;
						if (ch == ' ' || ch == '\t')
							continue;
						var cat = char.GetUnicodeCategory (ch);
						if (cat == UnicodeCategory.Control || cat == UnicodeCategory.LineSeparator)
							continue;
						table [i] = negativeGroup ? -1 : 1;
					}
					break;
				case "lower": // Lowercase alphabetical character
					AddRange ('a', 'z');
					break;
				case "print": // Like [:graph:], but includes the space character
					for (int i = 0; i < table.Length; i++) {
						var ch = (char)i;
						var cat = char.GetUnicodeCategory (ch);
						if (cat == UnicodeCategory.Control || cat == UnicodeCategory.LineSeparator)
							continue;
						table [i] = negativeGroup ? -1 : 1;
					}
					break;
				case "punct": // Punctuation character
					AddUnicodeCategory (UnicodeCategory.OpenPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.DashPunctuation,
										UnicodeCategory.OtherPunctuation, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.FinalQuotePunctuation, UnicodeCategory.InitialQuotePunctuation);
					break;
				case "space": // Whitespace character ([:blank:], newline, carriage return, etc.)
					Add (' ');
					Add ('\t');
					Add ('\r');
					Add ('\n');
					break;
				case "upper": // Uppercase alphabetical
					AddRange ('A', 'Z');
					break;
				case "xdigit": // Digit allowed in a hexadecimal number (i.e., 0 - 9a - fA - F)
					AddRange ('a', 'f');
					AddRange ('A', 'F');
					AddRange ('0', '9');
					break;
				default:
					LoggingService.LogWarning ("unknown unicode category : " + category);
					break;
				}
			}
		}

		class Group 
		{
			public string Id { get; set; }
			public StringBuilder groupContent = new StringBuilder ();
			public Group (string id)
			{
				Id = id;
			}
		}

		// translates ruby regex -> .NET regexes
		internal static string CompileRegex (string regex)
		{
			if (string.IsNullOrEmpty (regex))
				return regex;
			regex = StripRegexComments (regex);
			var result = new StringBuilder ();

			var charProperty = new StringBuilder ();

			int characterClassLevel = 0;
			bool escape = false;
			bool readCharacterProperty = false, readCharPropertyIdentifier = false, replaceGroup = false, skipRecordChar = false;
			bool readPlusQuantifier = false, readStarQuantifier = false, readGroupName = false, recordGroupName = false;
			StringBuilder curGroupName = new StringBuilder ();
			var groups = new List<Group> ();
			var groupStack = new Stack<Group> ();
			int groupNumber = 1;
			CharacterClass curClass = null;
			for (int i = 0; i < regex.Length; i++) {
				var ch = regex [i];
				switch (ch) {
				case '+':
					if (readPlusQuantifier)
						continue;
					if (readStarQuantifier) {
						continue;
					}
					readPlusQuantifier = true;
					break;
				case '*':
					if (readStarQuantifier)
						continue;
					if (readPlusQuantifier && result.Length > 0) {
						result.Length--;
						if (!recordGroupName && groupStack.Count > 0 && groupStack.Peek ().groupContent.Length > 0) {
							groupStack.Peek ().groupContent.Length--;
						}
					}

					readStarQuantifier = true;
					break;
				case '\\':
					if (curClass != null) {
						escape = !escape;
						goto addChar;
					}
					if (escape)
						break;
					if (i + 1 >= regex.Length)
						break;
					var next = regex [i + 1];
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
					if (next == 'p') {
						i++;
						readCharacterProperty = true;
						readCharPropertyIdentifier = false;
						continue;
					}
					if (next == 'g') {
						i++;
						replaceGroup = true;
						readGroupName = true;
						continue;
					}
					escape = true;
					goto addChar;
				case '(':
					if (escape)
						break;
					groupStack.Push (new Group(groupNumber.ToString ()));
					groupNumber++;
					skipRecordChar = true;
					break;
				case '>':
					recordGroupName = false;
					if (replaceGroup) {
						bool foundGroup = false;
						foreach (var g in groups) {
							if (g.Id == curGroupName.ToString ()) {
								result.Append (g.groupContent.ToString ());
								foundGroup = true;
								break;
							}
						}
						if (!foundGroup) {
							LoggingService.LogError ("Error can't find back trace group name : " + curGroupName + Environment.NewLine + "Note that it is not possible to backtrack groups inside the same group.");
							result.Append ("ERROR");
						}
						replaceGroup = false;
						curGroupName.Length = 0;
						continue;
					}
					if (groupStack.Count > 0)
						groupStack.Peek ().Id = curGroupName.ToString ();
					skipRecordChar = true;
					curGroupName.Length = 0;
					break;
				case '<':
					if (readGroupName) {
						recordGroupName = true;
						readGroupName = false;
					}
					break;
				case '?':
					if (groupStack.Count > 0 && result[result.Length - 1] == '(') {
						readGroupName = true;
						groupNumber--;
						skipRecordChar = true;
					}
					break;
				case ')':
					if (escape)
						break;
					if (groupStack.Count > 0)
						groups.Add (groupStack.Pop ());
					break;
				case '[':
					if (escape)
						break;
					characterClassLevel++;
					if (curClass == null) {
						curClass = new CharacterClass ();
						continue;
					}
					break;
				case ']':
					if (escape)
						break;
					if (characterClassLevel > 0) {
						characterClassLevel--;
						if (characterClassLevel == 0) {
							var cg = curClass.Generate ();
							result.Append (cg);
							if (!recordGroupName && groupStack.Count > 0)
								groupStack.Peek ().groupContent.Append (cg);
							curClass = null;
							continue;
						}
					}
					break;
				}
				escape = false;
			addChar:
				if (recordGroupName) {
					if (ch == '-')
						ch = '_';
					if (ch != '<') {
						curGroupName.Append (ch);
					}
				}
				if (replaceGroup)
					continue;
				if (ch != '?')
					readGroupName = false;
				if (ch != '+')
					readPlusQuantifier = false;
				if (ch != '*')
					readStarQuantifier = false;
				if (readCharacterProperty) {
					if (ch == '}') {
						result.Append (ConvertCharacterProperty (charProperty.ToString ()));
						charProperty.Length = 0;
						readCharacterProperty = false;
					} else if (ch == '{') {
						if (readCharPropertyIdentifier)
							LoggingService.LogWarning ("invalid regex character property group " + regex);
						readCharPropertyIdentifier = true;
					} else {
						if (readCharPropertyIdentifier) {
							charProperty.Append (ch);
						} else {
							LoggingService.LogWarning ("invalid regex character property group " + regex);
						}
					}
					continue;
				}
				if (curClass != null) {
					curClass.Push (ch);
				} else {
					result.Append (ch);
					if (!recordGroupName && groupStack.Count > 0) {
						if (!skipRecordChar)
							groupStack.Peek ().groupContent.Append (ch);
					}
					skipRecordChar = false;
				}
			}
			return result.ToString ();
		}

		static string StripRegexComments (string regex)
		{
			var result = new StringBuilder ();
			bool inCommment = false;
			bool escape = false;
			bool wasWhitespace = false;
			foreach (var ch in regex) {
				if (inCommment) {
					if (ch == '\n')
						inCommment = false;
					continue;
				}
				if (escape) {
					escape = false;
					result.Append (ch);
					continue;
				}
				if (ch == '\\') {
					escape = true;
				}
				if (ch == '#' && wasWhitespace) {
					inCommment = true;
					continue;
				}
				wasWhitespace = ch == ' ' || ch == '\t';
				result.Append (ch);
			}
			return result.ToString ();
		}

		static string ConvertCharacterProperty (string property)
		{
			switch (property) {
			case "Alnum":
				return "[a-zA-Z0-9]";
			case "Alpha":
				return "[a-zA-Z]";
			case "Blank":
				return "[\\t ]";
			case "Cntrl":
				return "\\p{P}";
			case "Digit":
				return "\\d";
			case "Graph":
				return "\\S";
			case "Lower":
				return "[a-z]";
			case "Print":
				return "[\\S ]";
			case "Punct":
				return "\\p{P}";
			case "Space":
				return "\\s";
			case "Upper":
				return "[A-Z]";
			case "XDigit":
				return "[0-9a-fA-F]";
			case "Word":
				return "\\w";
			case "ASCII":
				return "[\\x00-\\x7F]";
			case "Any":
				return ".";
			case "Assigned": // TODO
				return ".";
			default:
				// assume unicode character category (that's supported by C# regexes)
				return "\\p{" + property + "}";
			}
		}


		internal static TmSnippet ReadSnippet (Stream stream)
		{
			string name = null;
			string content = null;
			string tabTrigger = null;
			var scopes = new List<StackMatchExpression> ();
			using (var reader = XmlReader.Create (stream)) {
				while (reader.Read ()) {
					if (reader.NodeType != XmlNodeType.Element)
						continue;
					switch (reader.LocalName) {
					case "content":
						if (reader.Read ())
							content = reader.Value;
						break;
					case "tabTrigger":
						if (reader.Read ())
							tabTrigger = reader.Value;
						break;
					case "scope":
						if (reader.Read ())
							scopes.Add (StackMatchExpression.Parse (reader.Value));
						break;
					case "description":
						if (reader.Read ())
							name = reader.Value;
						break;
					}
				}
			}
			return new TmSnippet (name, scopes, content, tabTrigger);
		}
	}
}