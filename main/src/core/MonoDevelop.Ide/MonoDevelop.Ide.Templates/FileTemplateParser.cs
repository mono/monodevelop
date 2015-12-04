//
// FileTemplateParser.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Templates
{
	internal static class FileTemplateParser
	{
		class Statement
		{
			public int Start;
			public int End;
			public StatementType StatementType;
		}

		enum StatementType {
			None,
			ElseStatement,
			EndIfStatement
		}

		class ConditionStatement
		{
			public int End;
			public string ParameterName;
			public string ParameterValue;
		};

		class IfStatement
		{
			public int Start;
			public ConditionStatement Condition;
			public Statement ElseStatement;
			public Statement EndIfStatement;

			public IfStatement (int startIndex, ConditionStatement condition, Statement elseStatement, Statement endIfStatement)
			{
				Start = startIndex;
				Condition = condition;
				ElseStatement = elseStatement;
					EndIfStatement = endIfStatement;
			}

			public IfStatement (int startIndex, ConditionStatement condition, Statement endIfStatement)
				: this (startIndex, condition, null, endIfStatement)
			{
			}

			public string GetReplacementText (string input, IStringTagModel parameters)
			{
				if (IsConditionTrue (parameters)) {
					int start = Condition.End + 1;
					int end = GetTrueConditionEnd ();
					return input.Substring (start, end - start);
				} else if (ElseStatement != null) {
					int start = ElseStatement.End + 1;
					int end = EndIfStatement.Start;
					return input.Substring (start, end - start);
				}

				return String.Empty;
			}

			bool IsConditionTrue (IStringTagModel parameters)
			{
				string value = parameters.GetValue (Condition.ParameterName) as string;
				if (value != null) {
					return String.Equals (value, Condition.ParameterValue, StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}

			int GetTrueConditionEnd ()
			{
				if (ElseStatement != null) {
					return ElseStatement.Start;
				}
				return EndIfStatement.Start;
			}
		};

		public static string Parse (string input, IStringTagModel parameters)
		{
			var builder = new StringBuilder (input.Length);

			int i = 0;

			while (i < input.Length) {

				char ch = input [i];
				if ('$' == ch) {
					IfStatement ifStatement = FindIfStatement (input, i);
					if (ifStatement != null) {
						string text = ifStatement.GetReplacementText (input, parameters)
							.Replace ("$$", "$");
						builder.Append (text);
						i = ifStatement.EndIfStatement.End;
					} else if (IsEscaped (input, i)) {
						builder.Append (ch);
						i++;
					} else {
						builder.Append (ch);
					}

				} else {
					builder.Append (ch);
				}

				i++;
			}

			return builder.ToString ();
		}

		static bool IsEscaped (string input, int index)
		{
			int next = index + 1;
			return (next < input.Length) && (input [next] == '$');
		}

		static readonly string IfStatementText = "$if$";
		static readonly string ElseStatementText = "$else$";
		static readonly string EndIfStatementText = "$endif$";

		static bool IsMatch (string input, string statement, int startIndex)
		{
			if ((startIndex + statement.Length) > input.Length) {
				return false;
			}

			return input.IndexOf (statement, startIndex, statement.Length) >= 0;
		}

		static IfStatement FindIfStatement (string input, int startIndex)
		{
			if (!IsMatch (input, IfStatementText, startIndex)) {
				return null;
			}

			ConditionStatement condition = FindCondition (input, startIndex + IfStatementText.Length);
			if (condition == null) {
				return null;
			}

			Statement elseStatement = null;
			Statement endIfStatement = FindElseOrEndIfStatement (input, condition.End + 1);
			if (endIfStatement == null) {
				return null;
			}

			if (endIfStatement.StatementType == StatementType.EndIfStatement) {
				return new IfStatement(startIndex, condition, endIfStatement);
			} else {
				elseStatement = endIfStatement;
			}

			endIfStatement = FindEndIfStatement (input, elseStatement.End + 1);
			if (endIfStatement == null) {
				return null;
			}

			return new IfStatement (startIndex, condition, elseStatement, endIfStatement);
		}

		static ConditionStatement FindCondition (string input, int startIndex)
		{
			int i = startIndex;
			while (i < input.Length) {
				char ch = input [i];
				if (ch == ' ') {
					// Skip.
				} else if (ch == '(') {
					int endIndex = input.IndexOf (')', i + 1);
					if (endIndex > 0) {
						return CreateIfCondition (input, i + 1, endIndex);
					} else {
						return null;
					}
				} else {
					return null;
				}

				i++;
			}

			return null;
		}

		static ConditionStatement CreateIfCondition (string input, int startIndex, int endIndex)
		{
			string condition = input.Substring (startIndex, endIndex - startIndex);
			int index = condition.IndexOf ("==");
			if (index < 0) {
				return null;
			}

			string parameterName = condition.Substring (0, index).Trim ();
			if (parameterName.Length < 2) {
				return null;
			}

			parameterName = parameterName.Substring (1, parameterName.Length - 2);
			string parameterValue = condition.Substring (index + 2).Trim ();

			return new ConditionStatement {
				ParameterName = parameterName,
				ParameterValue = parameterValue,
				End = endIndex
			};
		}

		static Statement FindElseOrEndIfStatement (string input, int startIndex)
		{
			int i = startIndex;
			while (i < input.Length) {
				char ch = input [i];
				if (ch == '$') {
					if (IsEscapedOrStringParserText (input, i)) {
						i++;
					} else if (IsMatch (input, EndIfStatementText, i)) {
						return new Statement {
							Start = i,
							End = i + EndIfStatementText.Length - 1,
							StatementType = StatementType.EndIfStatement
						};
					} else if (IsMatch (input, ElseStatementText, i)) {
						return new Statement {
							Start = i,
							End = i + ElseStatementText.Length - 1,
							StatementType = StatementType.ElseStatement
						};
					} else {
						return null;
					}
				}

				i++;
			}

			return null;
		}

		static bool IsEscapedOrStringParserText (string input, int index)
		{
			int next = index + 1;
			return (next < input.Length) && 
				((input [next] == '$') || (input [next] == '{'));
		}

		static Statement FindEndIfStatement (string input, int startIndex)
		{
			int i = startIndex;
			while (i < input.Length) {
				char ch = input [i];
				if (ch == '$') {
					if (IsEscapedOrStringParserText (input, i)) {
						i++;
					} else if (IsMatch (input, EndIfStatementText, i)) {
						return new Statement {
							Start = i,
							End = i + EndIfStatementText.Length - 1
						};
					} else {
						return null;
					}
				}

				i++;
			}

			return null;
		}
	}
}