// 
// MethodData.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2009 Nikhil Sarda
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
using System.Collections;
using System.Collections.Generic;
using System.Text;


using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;

//add reference to configure.in file

namespace MonoDevelop.CodeMetrics
{
	public sealed class MethodProperties : IProperties
	{
		private readonly IMethod mthd;
		private readonly AstNode mthdAst;
		
		public List<string> ParameterList;
		
		public IMethod Method {
			get; private set; 
		}
		
		public AstNode MethodAST {
			get; private set;
		}
		
		public string FullName {
			get; private set;
		}
		
		public int ParameterCount {
			get { 
				return ParameterList.Count;
			}
		}
		
		public bool IsDocumented 
		{
			get; private set;
		}
		
		public ClassProperties ParentClass {
			get; internal set;
		}
		
		public int StartLine {
			get; private set;
		}
		
		public int EndLine {
			get; private set;
		}
		
		public int NumberOfVariables {
			get; internal set;
		}
		
		public int CyclometricComplexity {
			get; internal set;
		}
		
		public ulong LOCReal {
			get; internal set;
		}
		
		public ulong LOCComments {
			get; internal set;
		}
		
		public int AfferentCoupling {
			get; internal set;
		}
		
		public int EfferentCoupling {
			get; internal set;
		}
		
		public int ClassCoupling {
			get; internal set;
		}
		
		public int LCOM {
			get; internal set;
		}
		
		public int LCOMHS {
			get; internal set;
		}
		
		public string FilePath {
			get; set;
		}
		
		public MethodProperties (IMethod m)
		{ 
			mthd = m;
			mthdAst = null;
			ParameterList = new List<string> (0);
			foreach(var param in m.Parameters) {
				ParameterList.Add(param.Type.ToString ());
			}
			
			AfferentCoupling=0;
			EfferentCoupling=0;
			FilePath="";
			this.FullName = mthd.FullName;
			this.StartLine = mthd.BodyRegion.BeginLine;
			this.EndLine = mthd.BodyRegion.EndLine;
		}
		
		public MethodProperties (AstNode m, ClassProperties prop)
		{
			mthd=null;
			ParameterList = new List<string> (0);
			if(m is MethodDeclaration) {
				mthdAst = (MethodDeclaration)m;
				VisitMethodMember((MethodDeclaration)m, prop);
				this.FullName = prop.FullName + "." + ((MethodDeclaration)m).Name.Substring(((MethodDeclaration)m).Name.LastIndexOf(".")+1);
			} else if(m is ConstructorDeclaration) {
				mthdAst = (ConstructorDeclaration)m;
				VisitConstructorMember((ConstructorDeclaration)m, prop);
				this.FullName = prop.FullName + "." + ((ConstructorDeclaration)m).Name.Substring(((ConstructorDeclaration)m).Name.LastIndexOf(".")+1);
			}
			
			AfferentCoupling=0;
			EfferentCoupling=0;
			FilePath="";
			this.ParentClass = prop;
			this.StartLine = mthdAst.StartLocation.Line;
			this.EndLine = mthdAst.EndLocation.Line;

		}
		
		public void VisitMethodMember(MethodDeclaration node, ClassProperties prop)
		{
			foreach(var param in node.Parameters) {
				ParameterList.Add(param.Type.ToString ());
			}
			this.StartLine = node.Body.StartLocation.Line;
			this.EndLine = node.Body.EndLocation.Line;
		}
		
		public void VisitConstructorMember(ConstructorDeclaration node, ClassProperties prop)
		{
			foreach(var param in node.Parameters) {
				ParameterList.Add(param.Type.ToString ());
			}
			this.StartLine = node.Body.StartLocation.Line;
			this.EndLine = node.Body.EndLocation.Line;
		}
		
		public bool IsParameters (List<string> parameters)
		{
			if (parameters.Count != ParameterList.Count)
				return false;
			for (int i = 0; i < parameters.Count; i++)
				if (ParameterList[i] != parameters[i])
					return false;
			return true;
		}
	}
}
