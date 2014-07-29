//
// CodeCompletionUtility.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran Bath
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
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using System.IO;
using System.Reflection;
using MonoDevelop.Core;

namespace MonoDevelop.JavaScript
{
	public static class CodeCompletionUtility
	{
		public static void UpdateCodeCompletion (List<JSStatement> astNodes, ref JSCompletionList dataList)
		{
			if (dataList == null)
				dataList = new JSCompletionList ();

			if (astNodes == null)
				return;

			foreach (JSStatement node in astNodes) {
				var variableDeclaration = node as JSVariableDeclaration;
				if (variableDeclaration != null) {
					if (!dataList.Exists (i => i.DisplayText == variableDeclaration.Name && i is VariableCompletion && (i as VariableCompletion).FileName == variableDeclaration.Filename) &&
					    !string.IsNullOrWhiteSpace (variableDeclaration.Name))
						dataList.Add (new VariableCompletion (variableDeclaration, node.Filename));

					UpdateCodeCompletion (node.ChildNodes, ref dataList);

					continue;
				}

				var functionStatement = node as JSFunctionStatement;
				if (functionStatement != null) {
					if (!string.IsNullOrWhiteSpace (functionStatement.Name)) {
						var existingDefinition = dataList.FirstOrDefault (i => i.DisplayText == functionStatement.Name &&
						                         i is FunctionCompletion && (i as FunctionCompletion).FileName == functionStatement.Filename);
						if (existingDefinition == null)
							dataList.Add (new FunctionCompletion (functionStatement, node.Filename));
						else {
							existingDefinition.AddOverload (new FunctionCompletion (functionStatement, node.Filename));
						}
					}

					UpdateCodeCompletion (node.ChildNodes, ref dataList);

					continue;
				}

				UpdateCodeCompletion (node.ChildNodes, ref dataList);
			}
		}

		public static void AddDefaultKeywords (ref JSCompletionList dataList)
		{
			if (dataList == null)
				dataList = new JSCompletionList ();

			foreach (var token in Jurassic.Compiler.KeywordToken.Keywords) {
				dataList.Add (new CompletionData (token.Text, string.Empty));
			}
		}

		public static void AddNativeVariablesAndFunctions (ref JSCompletionList dataList)
		{
			if (dataList == null)
				dataList = new JSCompletionList ();

			string currentDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
			string referencesDir = Path.Combine (currentDir, "JSReferences");

			foreach (string file in Directory.GetFiles (referencesDir)) {
				try {
					var javaScriptDocument = new JavaScriptParsedDocument (file, File.ReadAllText (file));
					UpdateCodeCompletion (javaScriptDocument.SimpleAst.AstNodes, ref dataList);
				} catch (Exception ex) {
					LoggingService.LogError (string.Format ("AddNativeVariablesAndFunctions() Parsing File {0} Failed", file), ex);
				}
			}
		}

		public static JSCompletionList FilterCodeCompletion (JSCompletionList dataList, string query)
		{
			if (dataList == null)
				return null;

			var codeCompletion = new JSCompletionList ();

			foreach (var item in dataList.OfType<CompletionData> ()) {
				if (item.CompletionText.ToUpper ().StartsWith (query.ToUpper ()))
					codeCompletion.Add (item);
			}

			return codeCompletion;
		}

		public static void RemoveAllMembersByFileName (this JSCompletionList list, string filename)
		{
			for (int i = list.Count - 1; i >= 0; i--) {
				var completionMember = list [i] as CompletionData;
				if (completionMember != null && completionMember.FileName == filename)
					list.RemoveAt (i);
			}
		}
	}
}

