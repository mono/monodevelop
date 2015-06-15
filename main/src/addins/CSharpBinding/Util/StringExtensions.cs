// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class StringExtensions
	{
		public static int? GetFirstNonWhitespaceOffset(this string line)
		{
			// Contract.ThrowIfNull(line);

			for (int i = 0; i < line.Length; i++)
			{
				if (!char.IsWhiteSpace(line[i]))
				{
					return i;
				}
			}

			return null;
		}

		public static string GetLeadingWhitespace(this string lineText)
		{
			// Contract.ThrowIfNull(lineText);

			var firstOffset = lineText.GetFirstNonWhitespaceOffset();

			return firstOffset.HasValue
				? lineText.Substring(0, firstOffset.Value)
					: lineText;
		}

		public static int GetTextColumn(this string text, int tabSize, int initialColumn)
		{
			var lineText = text.GetLastLineText();
			if (text != lineText)
			{
				return lineText.GetColumnFromLineOffset(lineText.Length, tabSize);
			}

			return text.ConvertTabToSpace(tabSize, initialColumn, text.Length) + initialColumn;
		}

		public static int ConvertTabToSpace(this string textSnippet, int tabSize, int initialColumn, int endPosition)
		{
			// Contract.Requires(tabSize > 0);
			// Contract.Requires(endPosition >= 0 && endPosition <= textSnippet.Length);

			int column = initialColumn;

			// now this will calculate indentation regardless of actual content on the buffer except TAB
			for (int i = 0; i < endPosition; i++)
			{
				if (textSnippet[i] == '\t')
				{
					column += tabSize - column % tabSize;
				}
				else
				{
					column++;
				}
			}

			return column - initialColumn;
		}

		public static int IndexOf(this string text, Func<char, bool> predicate)
		{
			if (text == null)
			{
				return -1;
			}

			for (int i = 0; i < text.Length; i++)
			{
				if (predicate(text[i]))
				{
					return i;
				}
			}

			return -1;
		}

		public static string GetFirstLineText(this string text)
		{
			var lineBreak = text.IndexOf('\n');
			if (lineBreak < 0)
			{
				return text;
			}

			return text.Substring(0, lineBreak + 1);
		}

		public static string GetLastLineText(this string text)
		{
			var lineBreak = text.LastIndexOf('\n');
			if (lineBreak < 0)
			{
				return text;
			}

			return text.Substring(lineBreak + 1);
		}

		public static bool ContainsLineBreak(this string text)
		{
			foreach (char ch in text)
			{
				if (ch == '\n' || ch == '\r')
				{
					return true;
				}
			}

			return false;
		}

		public static int GetNumberOfLineBreaks(this string text)
		{
			int lineBreaks = 0;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\n')
				{
					lineBreaks++;
				}
				else if (text[i] == '\r')
				{
					if (i + 1 == text.Length || text[i + 1] != '\n')
					{
						lineBreaks++;
					}
				}
			}

			return lineBreaks;
		}

		public static bool ContainsTab(this string text)
		{
			// PERF: Tried replacing this with "text.IndexOf('\t')>=0", but that was actually slightly slower
			foreach (char ch in text)
			{
				if (ch == '\t')
				{
					return true;
				}
			}

			return false;
		}

		public static ImmutableArray<SymbolDisplayPart> ToSymbolDisplayParts(this string text)
		{
			return ImmutableArray.Create<SymbolDisplayPart>(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text));
		}

		public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this string line, int tabSize)
		{
			var firstNonWhitespaceChar = line.GetFirstNonWhitespaceOffset();

			if (firstNonWhitespaceChar.HasValue)
			{
				return line.GetColumnFromLineOffset(firstNonWhitespaceChar.Value, tabSize);
			}
			else
			{
				// It's all whitespace, so go to the end
				return line.GetColumnFromLineOffset(line.Length, tabSize);
			}
		}

		public static int GetColumnFromLineOffset(this string line, int endPosition, int tabSize)
		{
//			Contract.ThrowIfNull(line);
//			Contract.ThrowIfFalse(0 <= endPosition && endPosition <= line.Length);
//			Contract.ThrowIfFalse(tabSize > 0);

			return ConvertTabToSpace(line, tabSize, 0, endPosition);
		}

		public static int GetLineOffsetFromColumn(this string line, int column, int tabSize)
		{
//			Contract.ThrowIfNull(line);
//			Contract.ThrowIfFalse(column >= 0);
//			Contract.ThrowIfFalse(tabSize > 0);

			var currentColumn = 0;

			for (int i = 0; i < line.Length; i++)
			{
				if (currentColumn >= column)
				{
					return i;
				}

				if (line[i] == '\t')
				{
					currentColumn += tabSize - (currentColumn % tabSize);
				}
				else
				{
					currentColumn++;
				}
			}

			// We're asking for a column past the end of the line, so just go to the end.
			return line.Length;
		}

//		public static void AppendToAliasNameSet(this string alias, ImmutableHashSet<string>.Builder builder)
//		{
//			if (string.IsNullOrWhiteSpace(alias))
//			{
//				return;
//			}
//
//			builder.Add(alias);
//
//			var caseSensitive = builder.KeyComparer == StringComparer.Ordinal;
//		//	Contract.Requires(builder.KeyComparer == StringComparer.Ordinal || builder.KeyComparer == StringComparer.OrdinalIgnoreCase);
//
//			string aliasWithoutAttribute;
//			if (alias.TryGetWithoutAttributeSuffix(caseSensitive, out aliasWithoutAttribute))
//			{
//				builder.Add(aliasWithoutAttribute);
//				return;
//			}
//
//			builder.Add(alias.GetWithSingleAttributeSuffix(caseSensitive));
//		}


		private static ImmutableArray<string> s_lazyNumerals;

		internal static string GetNumeral(int number)
		{
			var numerals = s_lazyNumerals;
			if (numerals.IsDefault)
			{
				numerals = ImmutableArray.Create("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
				ImmutableInterlocked.InterlockedInitialize(ref s_lazyNumerals, numerals);
			}

			Debug.Assert(number >= 0);
			return (number < numerals.Length) ? numerals[number] : number.ToString();
		}

		public static string Join(this IEnumerable<string> source, string separator)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			if (separator == null)
			{
				throw new ArgumentNullException("separator");
			}

			return string.Join(separator, source);
		}

		public static bool LooksLikeInterfaceName(this string name)
		{
			return name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]);
		}

		public static bool LooksLikeTypeParameterName(this string name)
		{
			return name.Length >= 3 && name[0] == 'T' && char.IsUpper(name[1]) && char.IsLower(name[2]);
		}

		private static readonly Func<char, char> s_toLower = char.ToLower;
		private static readonly Func<char, char> s_toUpper = char.ToUpper;

		public static string ToPascalCase(
			this string shortName,
			bool trimLeadingTypePrefix = true)
		{
			return ConvertCase(shortName, trimLeadingTypePrefix, s_toUpper);
		}

		public static string ToCamelCase(
			this string shortName,
			bool trimLeadingTypePrefix = true)
		{
			return ConvertCase(shortName, trimLeadingTypePrefix, s_toLower);
		}

		private static string ConvertCase(
			this string shortName,
			bool trimLeadingTypePrefix,
			Func<char, char> convert)
		{
			// Special case the common .net pattern of "IFoo" as a type name.  In this case we
			// want to generate "foo" as the parameter name.  
			if (!string.IsNullOrEmpty(shortName))
			{
				if (trimLeadingTypePrefix && (shortName.LooksLikeInterfaceName() || shortName.LooksLikeTypeParameterName()))
				{
					return convert(shortName[1]) + shortName.Substring(2);
				}

				if (convert(shortName[0]) != shortName[0])
				{
					return convert(shortName[0]) + shortName.Substring(1);
				}
			}

			return shortName;
		}

		internal static bool IsValidClrTypeName(this string name)
		{
			return !string.IsNullOrEmpty(name) && name.IndexOf('\0') == -1;
		}

		/// <summary>
		/// Checks if the given name is a sequence of valid CLR names separated by a dot.
		/// </summary>
		internal static bool IsValidClrNamespaceName(this string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			char lastChar = '.';
			foreach (char c in name)
			{
				if (c == '\0' || (c == '.' && lastChar == '.'))
				{
					return false;
				}

				lastChar = c;
			}

			return lastChar != '.';
		}

		internal static string GetWithSingleAttributeSuffix(
			this string name,
			bool isCaseSensitive)
		{
			string cleaned = name;
			while ((cleaned = GetWithoutAttributeSuffix(cleaned, isCaseSensitive)) != null)
			{
				name = cleaned;
			}

			return name + "Attribute";
		}

		internal static bool TryGetWithoutAttributeSuffix(
			this string name,
			out string result)
		{
			return TryGetWithoutAttributeSuffix(name, isCaseSensitive: true, result: out result);
		}

		internal static string GetWithoutAttributeSuffix(
			this string name,
			bool isCaseSensitive)
		{
			string result;
			return TryGetWithoutAttributeSuffix(name, isCaseSensitive, out result) ? result : null;
		}

		internal static bool TryGetWithoutAttributeSuffix(
			this string name,
			bool isCaseSensitive,
			out string result)
		{
			const string AttributeSuffix = "Attribute";
			var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			if (name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, comparison))
			{
				result = name.Substring(0, name.Length - AttributeSuffix.Length);
				return true;
			}

			result = null;
			return false;
		}

		internal static bool IsValidUnicodeString(this string str)
		{
			int i = 0;
			while (i < str.Length)
			{
				char c = str[i++];

				// (high surrogate, low surrogate) makes a valid pair, anything else is invalid:
				if (char.IsHighSurrogate(c))
				{
					if (i < str.Length && char.IsLowSurrogate(str[i]))
					{
						i++;
					}
					else
					{
						// high surrogate not followed by low surrogate
						return false;
					}
				}
				else if (char.IsLowSurrogate(c))
				{
					// previous character wasn't a high surrogate
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Remove one set of leading and trailing double quote characters, if both are present.
		/// </summary>
		internal static string Unquote(this string arg)
		{
			bool quoted;
			return Unquote(arg, out quoted);
		}

		internal static string Unquote(this string arg, out bool quoted)
		{
			if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
			{
				quoted = true;
				return arg.Substring(1, arg.Length - 2);
			}
			else
			{
				quoted = false;
				return arg;
			}
		}

		internal static int IndexOfBalancedParenthesis(this string str, int openingOffset, char closing)
		{
			char opening = str[openingOffset];

			int depth = 1;
			for (int i = openingOffset + 1; i < str.Length; i++)
			{
				var c = str[i];
				if (c == opening)
				{
					depth++;
				}
				else if (c == closing)
				{
					depth--;
					if (depth == 0)
					{
						return i;
					}
				}
			}

			return -1;
		}

		// String isn't IEnumerable<char> in the current Portable profile. 
		internal static char First(this string arg)
		{
			return arg[0];
		}

		// String isn't IEnumerable<char> in the current Portable profile. 
		internal static char Last(this string arg)
		{
			return arg[arg.Length - 1];
		}

		// String isn't IEnumerable<char> in the current Portable profile. 
		internal static bool All(this string arg, Predicate<char> predicate)
		{
			foreach (char c in arg)
			{
				if (!predicate(c))
				{
					return false;
				}
			}

			return true;
		}

		public static string EscapeIdentifier(
            this string identifier,
            bool isQueryContext = false)
        {
            var nullIndex = identifier.IndexOf('\0');
            if (nullIndex >= 0)
            {
                identifier = identifier.Substring(0, nullIndex);
            }

            var needsEscaping = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None;

            // Check if we need to escape this contextual keyword
			needsEscaping = needsEscaping || (isQueryContext && SyntaxFacts.IsQueryContextualKeyword(SyntaxFacts.GetContextualKeywordKind(identifier)));

            return needsEscaping ? "@" + identifier : identifier;
        }

		public static SyntaxToken ToIdentifierToken (
			this string identifier,
			bool isQueryContext = false)
		{
			var escaped = identifier.EscapeIdentifier (isQueryContext);

			if (escaped.Length == 0 || escaped [0] != '@') {
				return SyntaxFactory.Identifier (escaped);
			}

			var unescaped = identifier.StartsWith ("@", StringComparison.Ordinal)
									  ? identifier.Substring (1)
									  : identifier;

			var token = SyntaxFactory.Identifier (
				default(SyntaxTriviaList), SyntaxKind.None, "@" + unescaped, unescaped, default(SyntaxTriviaList));

			if (!identifier.StartsWith ("@", StringComparison.Ordinal)) {
				token = token.WithAdditionalAnnotations (Simplifier.Annotation);
			}

			return token;
		}

		public static IdentifierNameSyntax ToIdentifierName (this string identifier)
		{
			return SyntaxFactory.IdentifierName (identifier.ToIdentifierToken ());
		}
	}
}
