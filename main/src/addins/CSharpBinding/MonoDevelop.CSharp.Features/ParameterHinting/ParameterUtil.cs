//
// ParameterUtil.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class ParameterIndexResult
	{
		public readonly static ParameterIndexResult Invalid = new ParameterIndexResult (null, -1);
		public readonly static ParameterIndexResult First   = new ParameterIndexResult (null, 0);
		
		public readonly string[] UsedNamespaceParameters;
		public readonly int      ParameterIndex;
		
		
		internal ParameterIndexResult(string[] usedNamespaceParameters, int parameterIndex)
		{
			UsedNamespaceParameters = usedNamespaceParameters;
			ParameterIndex = parameterIndex;
		}
	}
		
	public static class ParameterUtil
	{
		public static async Task<ParameterIndexResult> GetCurrentParameterIndex (Document document, int startOffset, int caretOffset, CancellationToken cancellationToken = default(CancellationToken))
		{
			var usedNamedParameters =new List<string> ();
			var parameter = new Stack<int> ();
			var bracketStack = new Stack<Stack<int>> ();
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			var word = new StringBuilder ();
			bool foundCharAfterOpenBracket = false;
			
			var text = await document.GetTextAsync(cancellationToken);
			for (int i = startOffset; i < caretOffset; i++) {
				char ch = text[i];
				char nextCh = i + 1 < text.Length ? text[i + 1] : '\0';
				if (ch == ':') {
					usedNamedParameters.Add (word.ToString ());
					word.Length = 0;
				} else if (char.IsLetterOrDigit (ch) || ch =='_') {
					word.Append (ch);
				} else if (char.IsWhiteSpace (ch)) {

				} else {
					word.Length = 0;
				}
				if (!char.IsWhiteSpace(ch) && parameter.Count > 0)
					foundCharAfterOpenBracket = true;

				switch (ch) {
					case '{':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						bracketStack.Push (parameter);
						parameter = new Stack<int> ();
						break;
					case '[':
					case '(':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push (0);
						break;
					case '}':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (bracketStack.Count > 0) {
							parameter = bracketStack.Pop ();
						} else {
							return ParameterIndexResult.Invalid;
						}
						break;
					case ']':
					case ')':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop ();
						} else {
							return ParameterIndexResult.Invalid;
						}
						break;
					case '<':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push (0);
						break;
					case '=':
						if (nextCh == '>') {
							i++;
							continue;
						}
						break;
					case '>':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop ();
						}
						break;
					case ',':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Push (parameter.Pop () + 1);
						}
						break;
					case '/':
						if (inString || inChar || inVerbatimString) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inSingleComment = true;
						}
						if (nextCh == '*') {
							inMultiLineComment = true;
						}
						break;
					case '*':
						if (inString || inChar || inVerbatimString || inSingleComment) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inMultiLineComment = false;
						}
						break;
					case '@':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (nextCh == '"') {
							i++;
							inVerbatimString = true;
						}
						break;
					case '\\':
						if (inString || inChar) {
							i++;
						}
						break;
					case '"':
						if (inSingleComment || inMultiLineComment || inChar) {
							break;
						}
						if (inVerbatimString) {
							if (nextCh == '"') {
								i++;
								break;
							}
							inVerbatimString = false;
							break;
						}
						inString = !inString;
						break;
					case '\'':
						if (inSingleComment || inMultiLineComment || inString || inVerbatimString) {
							break;
						}
						inChar = !inChar;
						break;
					default:
						if (NewLine.IsNewLine(ch)) {
							inSingleComment = false;
							inString = false;
							inChar = false;
						}
						break;
				}
			}
			if (parameter.Count != 1 || bracketStack.Count > 0) {
				return ParameterIndexResult.Invalid;
			}
			if (!foundCharAfterOpenBracket)
				return ParameterIndexResult.First;
			return new ParameterIndexResult (usedNamedParameters.ToArray(), parameter.Pop() + 1);
		}
	}
}
