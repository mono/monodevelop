// 
// Coupling.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Nikhil Sarda
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
using System.Text;
using System.IO;

using Gtk;

using MonoDevelop.Core;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CodeMetrics
{
	public partial class ComplexityMetrics
	{
		class Coupling
		{
			/*
			 * #  Coupling Between Objects (CBO): CBO is defined as the number of non-inherited classes associated with the target class. It is counted as the number of types that are used in attributes, parameters, return types, throws clauses, etc. Primitive types and system types (e.g. java.lang.*) are not counted.
				# Data Abstraction Coupling (DAC): DAC is defined as the total number of referred types in attribute declarations. Primitive types, system types, and types inherited from the super classes are not counted.
				# Method Invocation Coupling (MIC): MIC is defined as the relative number of classes that receive messages from a particular class. 
			 */
			
			/// <summary>
			///  Very experimental, lots of things to be done here (handling function calls in parameters, fetching correct method references from incomplete names). Refer to ORG files
			/// </summary>
			/// <param name="expression">
			/// A <see cref="InvocationExpression"/>
			/// </param>
			/// <param name="meth">
			/// A <see cref="MethodProperties"/>
			/// </param>
			private readonly static object coupleLock = new object();
			
			public static string EvaluateMethodCoupling(InvocationExpression expression, MethodProperties meth)
			{
				Console.WriteLine(expression.ToString());
				
				meth.EfferentCoupling++;
				
				StringBuilder calleeName = new StringBuilder();
				List<string> paramList = new List<string>();
				MethodProperties methCallee = null;
				lock(coupleLock)
				{
					if(expression.Target is MemberReferenceExpression) {
					
			   			calleeName.Append(ExtractCalleeFullName((MemberReferenceExpression)(expression.Target), meth));
						paramList = ExtractParamList(expression, meth);
					
						try {
//							methCallee = ComplexityMetrics.ProjProp.GetMethodReference(calleeName.ToString(), paramList);
							methCallee.AfferentCoupling++;
						} catch (Exception e) {
							Console.WriteLine(e.ToString());
						}
					
					} else if (expression.Target is IdentifierExpression) {
					
						calleeName.Append(((IdentifierExpression)expression.Target).Identifier);
						paramList = ExtractParamList(expression, meth);
					
						try {
							Console.WriteLine(calleeName.ToString());
//							methCallee = ComplexityMetrics.ProjProp.GetMethodReference(calleeName.ToString(), paramList);
							methCallee.AfferentCoupling++;
						} catch (Exception e) {
							Console.WriteLine(e.ToString());
						}
					} 
				}
				return methCallee.Method.ReturnType.ToString ();
			}
			
			private static List<string> ExtractParamList(InvocationExpression expression, MethodProperties meth)
			{
				List<string> retVal = new List<string>();
				foreach(Expression param in expression.Arguments)
				{
					if(param is PrimitiveExpression) {
						retVal.Add(((PrimitiveExpression)param).Value.GetType().Name);
						continue;
					} else if(param is BinaryOperatorExpression) {
						if(((BinaryOperatorExpression)param).Operator == BinaryOperatorType.ConditionalAnd || ((BinaryOperatorExpression)param).Operator == BinaryOperatorType.ConditionalOr)
							retVal.Add("System.Boolean");
						continue;
					} else if (param is InvocationExpression) {
						// TODO Deal with method calls within method calls
						// Lots of cases
						retVal.Add(EvaluateMethodCoupling((InvocationExpression)param, meth));
					}
					
				}
				return retVal;
			}
			
			private static string ExtractCalleeFullName(MemberReferenceExpression expression, MethodProperties meth)
			{
				if(expression.Target is MemberReferenceExpression)
				{
					return ExtractCalleeFullName((MemberReferenceExpression)(expression.Target), meth)+"."+expression.MemberName; 
				}
				if(expression.Target is IdentifierExpression)
				{
					return ((IdentifierExpression)(expression.Target)).Identifier+"."+expression.MemberName;
				}
				return "";
			}
			
			/// <summary>
			/// Afferent coupling at field level
			/// </summary>
			/// <param name="expression">
			/// A <see cref="IdentifierExpression"/>
			/// </param>
			/// <param name="meth">
			/// A <see cref="MethodProperties"/>
			/// </param>
			public static void EvaluateFieldCoupling(IdentifierExpression expression, MethodProperties meth)
			{
				//TODO
			}
		}
	}
}
