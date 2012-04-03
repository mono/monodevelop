//// 
//// CyclomaticComplexity.cs
////  
//// Author:
////       Nikhil Sarda <diff.operator@gmail.com>
//// 
//// Copyright (c) 2009 Nikhil Sarda
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
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//
//using Gtk;
//
//using MonoDevelop.Core;
// 
//using MonoDevelop.Ide.Gui;
//using MonoDevelop.Projects;
//using Mono.TextEditor;
//using ICSharpCode.NRefactory.CSharp;
//
//namespace MonoDevelop.CodeMetrics
//{
//	public partial class ComplexityMetrics
//	{
//		public class LOCEvaluate
//		{
//			internal static void EvaluateNamespaceLOC (MetricsContext ctx, NamespaceDeclaration node)
//			{
//				if(node == null)
//					return;
//				//MessageService.ShowMessage(node.ToString());
//				try {
//					NamespaceProperties namespaceRef = ComplexityMetrics.ProjProp.GetNamespaceReference(node.Name);
//					if(namespaceRef==null)
//						return;
//					Dictionary<int, ICSharpCode.OldNRefactory.Ast.INode> typeLocations = new Dictionary<int, ICSharpCode.OldNRefactory.Ast.INode>();
//					foreach(var childnode in node.Children){
//						if(childnode is TypeDeclaration)
//							typeLocations.Add(childnode.StartLocation.Line, childnode);
//					}
//					
//					if(namespaceRef.FilePath==null||namespaceRef.FilePath=="")
//						namespaceRef.FilePath = ComplexityMetrics.File.FilePath;
//					
//					#region CommonLogic
//					int startIndex = node.StartLocation.Line;
//					int endIndex = node.EndLocation.Line;
//					
//					ulong totalLines = 0, totalRealLines = 0, totalCommentedLines = 0;
//					int realLines = 0;
//					bool isSingleLineComment = false;
//					bool isMultipleLineComment = false;
//					
//					for(int i=startIndex;i<endIndex;i++)
//					{
//						string lineText = ComplexityMetrics.FileDoc.GetTextAt(ComplexityMetrics.FileText[i]).Trim();
//						if(isMultipleLineComment){
//							totalCommentedLines++;
//							if(lineText.EndsWith("*/"))
//								isMultipleLineComment = false;
//							continue;
//						}
//						if(lineText.StartsWith ("/*")){
//							isMultipleLineComment = true;
//							totalCommentedLines++;
//							continue;
//						}
//						isSingleLineComment = lineText.StartsWith ("//");
//						if(isSingleLineComment)
//							totalCommentedLines++;
//						if (lineText.Length > 0 && !isSingleLineComment)
//						{
//							realLines++;
//							if((typeLocations.ContainsKey(i)) && (typeLocations[i] is TypeDeclaration))
//								i = EvaluateTypeLOC(ctx, namespaceRef, (TypeDeclaration)typeLocations[i], i);
//							if((typeLocations.ContainsKey(i+1)) &&(typeLocations[i+1] is TypeDeclaration))
//								i = EvaluateTypeLOC(ctx, namespaceRef, (TypeDeclaration)typeLocations[i+1], i);
//						}
//					}
//				
//					totalLines     += (ulong)(startIndex-endIndex+1);
//					totalRealLines += (ulong)realLines;
//					namespaceRef.LOCReal += totalRealLines;
//					namespaceRef.LOCComments += totalCommentedLines;
//					#endregion CommonLogic
//				} catch (Exception e) {
//					Console.WriteLine(e.ToString());
//				}
//			}
//			
//			internal static int EvaluateTypeLOC (MetricsContext ctx, NamespaceProperties namespaceRef, TypeDeclaration node, int startIndex)
//			{
//				if(node==null)
//					return -1;
//				StringBuilder typeName = new StringBuilder("");;
//				try {
//					string[] prefixArray = ComplexityMetrics.PrefixName.ToArray();
//					for(int i=0;i<prefixArray.Length;i++)
//						typeName.Append(prefixArray[prefixArray.Length-i-1]+".");
//					typeName.Append(node.Name);
//					foreach(var templateDef in node.Templates) {
//						foreach(var bases in templateDef.Bases) {
//							if(bases.Type.Contains("constraint:"))
//								continue;
//							typeName.Append(" " + bases.Type.Substring(bases.Type.LastIndexOf(".")+1));
//						}
//					}
//					
//					IProperties typeRef = null;
//					switch(node.Type)
//					{
//					case ICSharpCode.OldNRefactory.Ast.ClassType.Class:
//						typeRef = ComplexityMetrics.ProjProp.GetClassReference(typeName.ToString());
//						break;
//					case ICSharpCode.OldNRefactory.Ast.ClassType.Enum:
//						typeRef = ComplexityMetrics.ProjProp.GetEnumReference(typeName.ToString(), namespaceRef);
//						break;
//					case ICSharpCode.OldNRefactory.Ast.ClassType.Struct:
//						typeRef = ComplexityMetrics.ProjProp.GetStructReference(typeName.ToString(), namespaceRef);
//						break;
//					case ICSharpCode.OldNRefactory.Ast.ClassType.Interface:
//						typeRef = ComplexityMetrics.ProjProp.GetInterfaceReference(typeName.ToString(), namespaceRef);
//						break;
//					default:
//						return node.EndLocation.Line;
//					}
//					
//					if(typeRef==null)
//						return node.EndLocation.Line;
//					
//					Dictionary<int, AstNode> childLocations = new Dictionary<int, ICSharpCode.OldNRefactory.Ast.INode>(0);
//					foreach(ICSharpCode.OldNRefactory.Ast.INode childNode in node.Children) {
//						if((childNode is TypeDeclaration) || (childNode is ConstructorDeclaration) || (childNode is MethodDeclaration))
//							childLocations.Add(childNode.StartLocation.Line, childNode);
//					}
//					
//					if(typeRef.FilePath==null||typeRef.FilePath=="")
//						typeRef.FilePath=ComplexityMetrics.File.FilePath;
//					
//					startIndex = node.StartLocation.Line;
//					int endIndex = node.EndLocation.Line;
//					ulong totalLines = 0, totalRealLines = 0, totalCommentedLines = 0;
//					int realLines = 0;
//					bool isSingleLineComment = false;
//					bool isMultipleLineComment = false;
//					
//					for(int i=startIndex;i< endIndex;i++)
//					{
//						string lineText = ComplexityMetrics.FileDoc.GetTextAt(ComplexityMetrics.FileText[i]).Trim();
//						
//						if(isMultipleLineComment){
//							totalCommentedLines++;
//							if(lineText.EndsWith("*/"))
//								isMultipleLineComment = false;
//							continue;
//						}
//						if(lineText.StartsWith ("/*")){
//							isMultipleLineComment = true;
//							totalCommentedLines++;
//							continue;
//						}
//						isSingleLineComment = lineText.StartsWith ("//");
//						if(isSingleLineComment)
//							totalCommentedLines++;
//						if (lineText.Length > 0 && !isSingleLineComment)
//						{
//							realLines++;
//							if(childLocations.ContainsKey(i)) {
//								ComplexityMetrics.PrefixName.Push(node.Name);
//								if((childLocations[i] is MethodDeclaration) || (childLocations[i] is ConstructorDeclaration)) {
//									ComplexityMetrics.ProcessMethod(ctx, childLocations[i], typeRef);
//									i = childLocations[i].EndLocation.Line;
//								} else if(childLocations[i] is TypeDeclaration) {
//									i = EvaluateTypeLOC(ctx, namespaceRef, (TypeDeclaration)childLocations[i], i);
//								} 
//								ComplexityMetrics.PrefixName.Pop();
//							}
//						}
//					}
//				
//					totalLines     += (ulong)(endIndex-startIndex+2);
//					totalRealLines += (ulong)realLines;
//					if(typeRef is ClassProperties) {
//						((ClassProperties)typeRef).LOCReal += totalRealLines;
//						((ClassProperties)typeRef).LOCComments += totalCommentedLines;
//					} else if (typeRef is InterfaceProperties) {
//						((InterfaceProperties)typeRef).LOCReal += totalRealLines;
//						((InterfaceProperties)typeRef).LOCComments += totalCommentedLines;
//					} else if (typeRef is EnumProperties) {
//						((EnumProperties)typeRef).LOCReal += totalRealLines;
//						((EnumProperties)typeRef).LOCComments += totalCommentedLines;
//					} else if (typeRef is StructProperties) {
//						((StructProperties)typeRef).LOCReal += totalRealLines;
//						((StructProperties)typeRef).LOCComments += totalCommentedLines;
//					} else if (typeRef is DelegateProperties) {
//						((DelegateProperties)typeRef).LOCReal += totalRealLines;
//						((DelegateProperties)typeRef).LOCComments += totalCommentedLines;
//					} 
//				
//					
//				} catch (Exception e) {
//					Console.WriteLine("Error in class " + typeName.ToString());
//					Console.WriteLine(e.ToString());
//				}
//				return node.EndLocation.Line;
//			}
//		
//			internal static void EvaluateMethodLOC(MethodProperties prop, List<LineSegment> text, Mono.TextEditor.TextDocument doc)
//			{
//				ulong totalLines = 0, totalRealLines = 0, totalCommentedLines = 0;
//				int realLines = 0;
//				bool isSingleLineComment = false;
//				bool isMultipleLineComment = false;
//				
//				int startIndex=prop.StartLine;
//				int endIndex=prop.EndLine;
//				
//				for(int i=startIndex;i< endIndex;i++)
//				{
//					string lineText = "";
//					try{
//					lineText = doc.GetTextAt(text[i]).Trim();
//					} catch (Exception e) {
//						continue;
//					}
//					if(isMultipleLineComment){
//						totalCommentedLines++;
//						if(lineText.EndsWith("*/"))
//							isMultipleLineComment = false;
//						continue;
//					}
//					if(lineText.StartsWith ("/*")){
//						isMultipleLineComment = true;
//						totalCommentedLines++;
//						continue;
//					}
//					isSingleLineComment = lineText.StartsWith ("//");
//					if(isSingleLineComment)
//						totalCommentedLines++;
//					if (lineText.Length > 0 && !isSingleLineComment)
//						realLines++;	
//				}
//			
//				totalLines     += (ulong)(endIndex-startIndex+1);
//				totalRealLines += (ulong)realLines;
//				((MethodProperties)prop).LOCComments = totalCommentedLines;
//				((MethodProperties)prop).LOCReal = totalRealLines + 1;
//			}		
//		}
//	}
//}
//
