//// 
//// CoverageAndComplexity.cs
////  
//// Author:
////       Nikhil Sarda <diff.operator@gmail.com>
////		 Michael J. Hutchinson <m.j.hutchinson@gmail.com>
//// 
//// Copyright (c) 2009 Nikhil Sarda, Michael J. Hutchinson
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//
//using System;
//using System.Linq;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//using System.Threading;
//
//using Gtk;
//
//using MonoDevelop.Core;
// 
//using MonoDevelop.Ide.Gui;
//using MonoDevelop.Projects;
//using Mono.TextEditor;
//using ICSharpCode.NRefactory.CSharp;
//namespace MonoDevelop.CodeMetrics
//{
//	public partial class ComplexityMetrics
//	{
//		internal static MetricsContext mctx
//		{ get; private set; }
//
//		internal static ProjectFile File 
//		{ get; private set; }
//		
//		internal static List<LineSegment> FileText
//		{ get; private set; }
//		
//		internal static Mono.TextEditor.TextDocument FileDoc
//		{ get; private set; }
//		
//		internal static ProjectProperties ProjProp
//		{ get; private set; }
//		
//		private static Stack<string> PrefixName; 
//		
//		public static void EvaluateComplexityMetrics (MetricsContext ctx, ProjectProperties project)
//		{
//			mctx = ctx;
//			PrefixName = new Stack<string>(0);
//			lock(mctx)
//			{
//				foreach (var file in project.Project.Files) {
//					/*if(file.BuildAction != BuildAction.Compile)
//						continue;*/
//					// Files not set to compile are sometimes not accessible
//					if(file.Name.Contains("svn-base"))
//						continue;
//					string text="";
//					try {
//						text = System.IO.File.ReadAllText(file.FilePath);
//					} catch (System.UnauthorizedAccessException uae) {
//						continue;
//					} catch (System.IO.FileNotFoundException fnf) {
//						// This exception arises in Nrefactory...WTF? 0_0
//						continue;
//					}
//					ProjProp = project;
//					File = file;
//					Mono.TextEditor.TextDocument doc = new Mono.TextEditor.TextDocument ();
//					doc.Text = text;
//					FileDoc = doc;
//					FileText = new List<LineSegment>();
//					foreach(LineSegment segment in doc.Lines)
//						FileText.Add(segment);
//					
//					using (var reader = new StringReader(text)) {
//						var parser = new CSharpParser ();
//						var unit = parser.Parse(reader, 0);
//						if (parser.HasErrors) {
//							//Error handling TODO
//						} else {
//							foreach (var it in unit.Children) {
//								ProcessNode (ctx, it);
//							}
//						}
//					}
//				}
//			}
//		}
//		
//		
//		
//		private static void ProcessNode (MetricsContext ctx, AstNode node)
//		{
//			if(node is UsingStatement) {
//				//TODO do something (something to do with afferent and efferent coupling of namespaces)
//			} else if (node is NamespaceDeclaration) {
//				try {
//				PrefixName.Push(((NamespaceDeclaration)node).Name);
//				LOCEvaluate.EvaluateNamespaceLOC(ctx, (NamespaceDeclaration)node);
//				PrefixName.Pop(); 
//				} catch (Exception e) {
//				}
//			} else if (node is TypeDeclaration) {
//				LOCEvaluate.EvaluateTypeLOC(ctx, null, (TypeDeclaration)node, node.StartLocation.Line);
//			}
//		} 
//				
//		private static void ProcessMethod (MetricsContext ctx, AstNode method, IProperties parentClass)
//		{
//			if(method==null)
//				return;
//						
//			StringBuilder methodName = new StringBuilder("");
//			string[] PrefixArray = PrefixName.ToArray();
//			for(int i=0;i<PrefixArray.Length;i++)
//				methodName.Append(PrefixArray[PrefixArray.Length-i-1]+".");
//			List<string> methodParameterList = new List<string>(0);
//			if(method is MethodDeclaration) {
//				methodName.Append(((MethodDeclaration)method).Name);
//				foreach(var pde in ((MethodDeclaration)method).Parameters) {
//					string type = pde.Type.ToString ();
//					if(type.Contains("."))
//						type = type.Substring(type.LastIndexOf(".")+1);
//					methodParameterList.Add (type);
//				}
//			} else if(method is ConstructorDeclaration) {
//				methodName.Append(((ConstructorDeclaration)method).Name);
//				foreach(var pde in ((ConstructorDeclaration)method).Parameters) {
//					string type = "";
//					if(type.Contains("."))
//						type = type.Substring(type.LastIndexOf(".")+1);
//					methodParameterList.Add (type);
//				}
//			}
//			
//			StringBuilder MethodKey = new StringBuilder();
//			MethodKey.Append(methodName.ToString()+" ");
//			foreach(string paramName in methodParameterList)
//				MethodKey.Append(paramName+" ");
//			try{
//				if(parentClass is ClassProperties) {
//					if(!(parentClass as ClassProperties).Methods.ContainsKey(MethodKey.ToString())) {
//						if(method is MethodDeclaration)
//							(parentClass as ClassProperties).Methods.Add(MethodKey.ToString(), new MethodProperties((MethodDeclaration)method, parentClass as ClassProperties));
//						else if (method is ConstructorDeclaration)
//							(parentClass as ClassProperties).Methods.Add(MethodKey.ToString(), new MethodProperties((ConstructorDeclaration)method, parentClass as ClassProperties));
//					}
//					var currentMethodReference = (parentClass as ClassProperties).Methods[MethodKey.ToString()];
//					//Calculate all metrics here
//					ASTVisitor.EvaluateComplexityMetrics (method, currentMethodReference);
//					LOCEvaluate.EvaluateMethodLOC(currentMethodReference, FileText, FileDoc);
//					currentMethodReference.FilePath = File.FilePath;
//				}
//			} catch (NullReferenceException ex){
//				LoggingService.LogError ("Error in '" + methodName.ToString() + "'", ex);
//				Console.WriteLine(MethodKey.ToString()+" hoo");
//			}
//		}
//	}
//}
