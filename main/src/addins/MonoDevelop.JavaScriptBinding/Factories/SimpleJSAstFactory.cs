//
// SimpleJSAstFactory.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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

namespace MonoDevelop.JavaScript
{
	static class SimpleJSAstFactory
	{
		public static SimpleJSAst CreateFromJavaScriptParsedDocument(IEnumerable<Jurassic.Compiler.JSAstNode> jsAstNodes)
		{
			var simpleAst = new SimpleJSAst ();
			simpleAst.AstNodes = mapAstNodes (jsAstNodes);
			return simpleAst;
		}

		static List<JSStatement> mapAstNodes(IEnumerable<Jurassic.Compiler.JSAstNode> nodes)
		{
			if (nodes == null)
				return new List<JSStatement> ();

			var jsCacheNodes = new List<JSStatement> ();

			foreach (Jurassic.Compiler.JSAstNode node in nodes) {
				var variableStatement = node as Jurassic.Compiler.VarStatement;
				if (variableStatement != null) {
					foreach (Jurassic.Compiler.VariableDeclaration variableDeclaration in variableStatement.Declarations) {
						jsCacheNodes.Add (new JSVariableDeclaration(variableDeclaration));
					}
					jsCacheNodes.AddRange (mapAstNodes (variableStatement.ChildNodes));
					continue;
				}

				var functionStatement = node as Jurassic.Compiler.FunctionStatement;
				if (functionStatement != null) {
					var jsFunctionAst = new JSFunctionStatement (functionStatement);
					jsFunctionAst.ChildNodes.AddRange (mapAstNodes (functionStatement.BodyRoot.ChildNodes));
					jsCacheNodes.Add (jsFunctionAst);
					continue;
				}

				var functionExpression = node as Jurassic.Compiler.FunctionExpression;
				if (functionExpression != null) {
					var jsFunctionAst = new JSFunctionStatement (functionExpression);
					jsFunctionAst.ChildNodes.AddRange (mapAstNodes (functionExpression.BodyRoot.ChildNodes));
					jsCacheNodes.Add (jsFunctionAst);
					continue;
				}

				var literal = node as Jurassic.Compiler.LiteralExpression;
				if (literal != null) {
					if (literal.Value != null) {
						var properties = literal.Value as Dictionary<string, object>;
						if (properties != null) {
							for (int i = 0; i < properties.Count; i++) {
								string key = properties.Keys.ElementAt (i); // Key holds the value for then 
								object value = properties.Values.ElementAt (i);

								var objFuncExpression = value as Jurassic.Compiler.FunctionExpression;
								if (objFuncExpression != null) {
									var jsFunctionAst = new JSFunctionStatement (objFuncExpression);
									jsFunctionAst.ChildNodes.AddRange (mapAstNodes (objFuncExpression.BodyRoot.ChildNodes));
									jsCacheNodes.Add (jsFunctionAst);
									continue;
								}

								var objGetSetFunc = value as Jurassic.Compiler.Parser.ObjectLiteralAccessor;
								if (objGetSetFunc != null) {
									if (objGetSetFunc.Getter != null) {
										var jsFunctionAst = new JSFunctionStatement (objGetSetFunc.Getter);
										jsFunctionAst.ChildNodes.AddRange (mapAstNodes (objGetSetFunc.Getter.BodyRoot.ChildNodes));
										jsCacheNodes.Add (jsFunctionAst);
									}
									if (objGetSetFunc.Setter != null) {
										var jsFunctionAst = new JSFunctionStatement (objGetSetFunc.Setter);
										jsFunctionAst.ChildNodes.AddRange (mapAstNodes (objGetSetFunc.Setter.BodyRoot.ChildNodes));
										jsCacheNodes.Add (jsFunctionAst);
									}
								}
							}
						}
					}

					continue;
				}

				jsCacheNodes.AddRange (mapAstNodes (node.ChildNodes));
			}

			return jsCacheNodes;
		}
	}
}

