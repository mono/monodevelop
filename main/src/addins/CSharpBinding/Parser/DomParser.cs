//
// DomParser.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.CSharp;

namespace MonoDevelop.CSharpBinding
{
	public class DomParser : IParser
	{
		public bool CanParseMimeType (string mimeType)
		{
			return "text/x-csharp" == mimeType;
		}
		
		public bool CanParseProjectType (string projectType)
		{
			return "C#" == projectType;
		}
		
		public bool CanParse (string fileName)
		{
			return Path.GetExtension (fileName) == ".cs";
		}
		
		public IExpressionFinder CreateExpressionFinder ()
		{
			return new CSharpExpressionFinder ();
		}
		
		public IDocumentMetaInformation CreateMetaInformation (Stream stream)
		{
			return null;
		}
		
		static DomRegion Block2Region (Mono.CSharp.Dom.LocationBlock block)
		{
			int startLine;
			int startColumn;
			if (block.Start != null) {
				startLine   = block.Start.Row;
				startColumn = block.Start.Column;
			} else {
				startLine = startColumn = -1;
			}
			
			int endLine;
			int endColumn;
			if (block.End != null) {
				endLine   = block.End.Row;
				endColumn = block.End.Column;
			} else {
				endLine = endColumn = -1;
			}
			
			return new DomRegion (startLine, startColumn, endLine, endColumn);
		}
		
		static MonoDevelop.Projects.Dom.IReturnType TypeName2ReturnType (Mono.CSharp.Dom.ITypeName name)
		{
			if (name == null)
				return DomReturnType.Void;
			List<IReturnType> typeParameters = new List<IReturnType> ();
			if (name.TypeArguments != null) {
				foreach (Mono.CSharp.Dom.ITypeName parameter in name.TypeArguments) {
					typeParameters.Add (TypeName2ReturnType (parameter));
				}
			}
			return new DomReturnType (name.Name,
			                          name.IsNullable,
			                          typeParameters);
			
		}
		
		static DomLocation Location2DomLocation (Mono.CSharp.Dom.ILocation location)
		{
			if (location == null) 
				return new DomLocation (-1, -1);
			return new DomLocation (location.Row, location.Column);
		}
		
		static List<MonoDevelop.Projects.Dom.IParameter> ParseParams (Mono.CSharp.Dom.IParameter[] p)
		{
			if (p == null || p.Length == 0)
				return null;
			List<MonoDevelop.Projects.Dom.IParameter> result = new List<MonoDevelop.Projects.Dom.IParameter> ();
			foreach (Mono.CSharp.Dom.IParameter para in p) {
				if (para == null)
					continue;
				result.Add (new DomParameter (para.Name, TypeName2ReturnType (para.TypeName)));
			}
			
			return result.Count > 0 ? result : null;
		}
		
		static MonoDevelop.Projects.Dom.Modifiers ConvertModifier (int ModFlags)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			if ((ModFlags & Mono.CSharp.Modifiers.PROTECTED) == Mono.CSharp.Modifiers.PROTECTED)
				result |= MonoDevelop.Projects.Dom.Modifiers.Protected;
			if ((ModFlags & Mono.CSharp.Modifiers.PUBLIC) == Mono.CSharp.Modifiers.PUBLIC)
				result |= MonoDevelop.Projects.Dom.Modifiers.Public;
			if ((ModFlags & Mono.CSharp.Modifiers.PRIVATE) == Mono.CSharp.Modifiers.PRIVATE)
				result |= MonoDevelop.Projects.Dom.Modifiers.Private;
			if ((ModFlags & Mono.CSharp.Modifiers.INTERNAL) == Mono.CSharp.Modifiers.INTERNAL)
				result |= MonoDevelop.Projects.Dom.Modifiers.Internal;
			if ((ModFlags & Mono.CSharp.Modifiers.NEW) == Mono.CSharp.Modifiers.NEW)
				result |= MonoDevelop.Projects.Dom.Modifiers.New;
			if ((ModFlags & Mono.CSharp.Modifiers.ABSTRACT) == Mono.CSharp.Modifiers.ABSTRACT)
				result |= MonoDevelop.Projects.Dom.Modifiers.Abstract;
			if ((ModFlags & Mono.CSharp.Modifiers.SEALED) == Mono.CSharp.Modifiers.SEALED)
				result |= MonoDevelop.Projects.Dom.Modifiers.Sealed;
			if ((ModFlags & Mono.CSharp.Modifiers.STATIC) == Mono.CSharp.Modifiers.STATIC)
				result |= MonoDevelop.Projects.Dom.Modifiers.Static;
			if ((ModFlags & Mono.CSharp.Modifiers.READONLY) == Mono.CSharp.Modifiers.READONLY)
				result |= MonoDevelop.Projects.Dom.Modifiers.Readonly;
			if ((ModFlags & Mono.CSharp.Modifiers.VIRTUAL) == Mono.CSharp.Modifiers.VIRTUAL)
				result |= MonoDevelop.Projects.Dom.Modifiers.Virtual;
			if ((ModFlags & Mono.CSharp.Modifiers.OVERRIDE) == Mono.CSharp.Modifiers.OVERRIDE)
				result |= MonoDevelop.Projects.Dom.Modifiers.Override;
			if ((ModFlags & Mono.CSharp.Modifiers.EXTERN) == Mono.CSharp.Modifiers.EXTERN)
				result |= MonoDevelop.Projects.Dom.Modifiers.Extern;
			if ((ModFlags & Mono.CSharp.Modifiers.VOLATILE) == Mono.CSharp.Modifiers.VOLATILE)
				result |= MonoDevelop.Projects.Dom.Modifiers.Volatile;
			if ((ModFlags & Mono.CSharp.Modifiers.UNSAFE) == Mono.CSharp.Modifiers.UNSAFE)
				result |= MonoDevelop.Projects.Dom.Modifiers.Unsafe;
			return result;
		}
		
		static MonoDevelop.Projects.Dom.IType ConvertType (MonoDevelop.Projects.Dom.CompilationUnit unit, string nsName, Mono.CSharp.Dom.IType type)
		{
			List<MonoDevelop.Projects.Dom.IMember> members = new List<MonoDevelop.Projects.Dom.IMember> ();
			
			if (type.Properties != null) {
				foreach (Mono.CSharp.Dom.IProperty property in type.Properties) {
					DomProperty prop = new DomProperty (property.Name,
					                                    ConvertModifier (property.ModFlags),
					                                    Location2DomLocation (property.Location),
					                                    Block2Region (property.AccessorsBlock),
					                                    TypeName2ReturnType (property.ReturnTypeName));
					if (property.GetAccessor != null) {
						prop.GetMethod = new DomMethod ();
						prop.GetMethod.BodyRegion = Block2Region (property.GetAccessor.LocationBlock);
					}
					if (property.SetAccessor != null) {
						prop.SetMethod = new DomMethod ();
						prop.SetMethod.BodyRegion = Block2Region (property.SetAccessor.LocationBlock);
					}
					members.Add (prop);
					
				}
			}
			
			if (type.Constructors != null) {
				foreach (Mono.CSharp.Dom.IMethod method in type.Constructors) {
					members.Add (new DomMethod (type.Name,
					                            ConvertModifier (method.ModFlags),
					                            true,
					                            Location2DomLocation (method.Location),
					                            Block2Region (method.LocationBlock),
					                            TypeName2ReturnType (method.ReturnTypeName),
					                            ParseParams (method.Parameters)
					                            ));
				}
			}
			if (type.Methods != null) {
				foreach (Mono.CSharp.Dom.IMethod method in type.Methods) {
					members.Add (new DomMethod (method.Name,
					                            ConvertModifier (method.ModFlags),
					                            false,
					                            Location2DomLocation (method.Location),
					                            Block2Region (method.LocationBlock),
					                            TypeName2ReturnType (method.ReturnTypeName),
					                            ParseParams (method.Parameters)
					                            ));
				}
			}
			if (type.Delegates != null) {
				foreach (Mono.CSharp.Dom.IDelegate deleg in type.Delegates) {
					members.Add (DomType.CreateDelegate (unit, deleg.Name, Location2DomLocation (deleg.Location), TypeName2ReturnType (deleg.ReturnTypeName), ParseParams (deleg.Parameters)));
				}
			}
			
			if (type.Events != null) {
				foreach (Mono.CSharp.Dom.IEvent evt in type.Events) {
					members.Add (new DomEvent (evt.Name, ConvertModifier (evt.ModFlags), Location2DomLocation (evt.Location), TypeName2ReturnType (evt.ReturnTypeName)));
				}
			}
			
			if (type.Fields != null) {
				foreach (Mono.CSharp.Dom.ITypeMember field in type.Fields) {
					members.Add (new DomField (field.Name, ConvertModifier (field.ModFlags), Location2DomLocation (field.Location), TypeName2ReturnType (field.ReturnTypeName)));
				}
			}
			
			if (type.Types != null) {
				foreach (Mono.CSharp.Dom.IType t in type.Types) {
					members.Add (ConvertType (unit, "", t));
				}
			}
			MonoDevelop.Projects.Dom.DomType result = new MonoDevelop.Projects.Dom.DomType (unit,
			                                        ClassType.Class,
			                                        type.Name,
			                                        Location2DomLocation (type.MembersBlock.Start), 
			                                        nsName, 
			                                        Block2Region (type.MembersBlock),
			                                        members);
			if (type.BaseTypes != null) {
				foreach (Mono.CSharp.Dom.ITypeName baseType in type.BaseTypes) {
					if (result.BaseType == null) {
						result.BaseType = TypeName2ReturnType (baseType);
						continue;
					}
					result.AddInterfaceImplementation (TypeName2ReturnType (baseType));
				}
			}
			return result;
		}
		
		public class MessageRecorder : Report.IMessageRecorder 
		{
			MonoDevelop.Projects.Dom.CompilationUnit unit;
			
			public MessageRecorder (MonoDevelop.Projects.Dom.CompilationUnit unit)
			{
				this.unit = unit;
			}
			
			public void AddMessage (Report.AbstractMessage msg)
			{
				unit.Add (new Error (msg.IsWarning ? ErrorType.Warning : ErrorType.Error,
				                       msg.Location.Row,
				                       msg.Location.Column,
				                       msg.Message));
			}
		}

		
		public ICompilationUnit Parse (string fileName, string content)
		{
//			MemoryStream input = new MemoryStream (Encoding.UTF8.GetBytes (content));
//			SeekableStreamReader reader = new SeekableStreamReader (input, Encoding.UTF8);
//			
//			ArrayList defines = new ArrayList ();
//			SourceFile file = new Mono.CSharp.SourceFile (Path.GetFileName (fileName),
//			                                              Path.GetDirectoryName (fileName), 
//			                                              0);
//			CSharpParser parser = new CSharpParser (reader, file, defines);
//			
//			try {
//				parser.parse ();
//			} finally {
//				input.Close ();
//			}
//			foreach (object o in RootContext.ToplevelTypes.Types) {
//				Mono.CSharp.Class c = (Mono.CSharp.Class)o;
//				if (c != null) {
//					result.Add (ConvertType (c));
//					continue;
//				}
//				Mono.CSharp.Interface i = (Mono.CSharp.Interface)o;
//				if (i != null) {
//					result.Add (ConvertType (i));
//					continue;
//				}
//				
//				Mono.CSharp.Struct s = (Mono.CSharp.Struct)o;
//				if (s != null) {
//					result.Add (ConvertType (s));
//					continue;
//				}
//				
//				Mono.CSharp.Delegate d = (Mono.CSharp.Delegate)o;
//				if (d != null) {
//					result.Add (ConvertType (d));
//					continue;
//				}
//				Mono.CSharp.Enum e = (Mono.CSharp.Enum)o;
//				if (e != null) {
//					result.Add (ConvertType (e));
//					continue;
//				}
//				
//				System.Console.WriteLine ("Unknown:" + o);
//			}
			MemoryStream input = new MemoryStream (Encoding.UTF8.GetBytes (content));
			MonoDevelop.Projects.Dom.CompilationUnit result = new MonoDevelop.Projects.Dom.CompilationUnit (fileName);
			MessageRecorder recorder = new MessageRecorder (result);
			Mono.CSharp.Dom.ICompilationUnit cu = CompilerCallableEntryPoint.ParseStream (input, fileName, new string[] {}, recorder);
			input.Close ();
			
			if (cu.Types != null) {
				foreach (Mono.CSharp.Dom.IType type in cu.Types) {
					result.Add (ConvertType (result, "", type));
				}
			}
			
			StringBuilder namespaceBuilder = new StringBuilder ();
			if (cu.Namespaces != null) {
				foreach (Mono.CSharp.Dom.INamespace namesp in cu.Namespaces) {
					DomUsing domUsing = new DomUsing ();
					string[] names = namesp.Name.Split ('.');
					namespaceBuilder.Length = 0;
					
					for (int i = 0; i < names.Length; i++) {
						if (i > 0)
							namespaceBuilder.Append ('.');
						namespaceBuilder.Append (names[i]);
						System.Console.WriteLine(namespaceBuilder.ToString ());
						domUsing.Add (namespaceBuilder.ToString ());
						
					}
					
					result.Add (domUsing);
					
					if (namesp.Types != null) {
						foreach (Mono.CSharp.Dom.IType type in namesp.Types) {
							result.Add (ConvertType (result, namesp.Name, type));
						}
					}
				}
			}
			if (cu.UsingBlock != null) {
				if (cu.UsingBlock.Usings != null) {
					DomUsing domUsing = new DomUsing ();
					foreach (Mono.CSharp.Dom.INamespaceImport import in cu.UsingBlock.Usings) {
						domUsing.Add (import.Name);
					}
					result.Add (domUsing);
					
				}
					
			}
			
			return result;
		}
	}
}
