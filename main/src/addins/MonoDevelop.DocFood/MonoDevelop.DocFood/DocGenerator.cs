// 
// DocGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Xml;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.DocFood
{
	public class DocGenerator : MonoDevelop.Projects.Text.DocGenerator
	{
		public List<Section> sections = new List<Section> ();
		public Dictionary<string, string> tags = new Dictionary<string, string> ();
//		TextEditorData data;
		
		public DocGenerator ()
		{
			
		}
		
		public DocGenerator (TextEditorData data)
		{
//			this.data = data;
		}
		
		public static string GetBaseDocumentation (IEntity member)
		{
			if (member.DeclaringTypeDefinition == null)
				return null;
			if (member is IMethod && (((IMethod)member).IsConstructor || ((IMethod)member).IsDestructor))
				return null;
			foreach (var type in member.DeclaringTypeDefinition.GetAllBaseTypeDefinitions ()) {
				if (type.Equals (member.DeclaringTypeDefinition))
					continue;
				IMember documentMember = null;
				foreach (var searchedMember in type.Members.Where (m => m.Name == member.Name)) {
					if (searchedMember.EntityType == member.EntityType && searchedMember.Name == member.Name) {
						if ((searchedMember is IParameterizedMember) && ((IParameterizedMember)searchedMember).Parameters.Count != ((IParameterizedMember)member).Parameters.Count)
							continue;
						if (searchedMember.Accessibility != member.Accessibility)
							continue;
						documentMember = searchedMember;
						break;
					}
				}
				if (documentMember != null) {
					string documentation = AmbienceService.GetDocumentation (documentMember);
					if (documentation != null)
						return documentation;
				}
			}
			return null;
		}

		void FillDocumentation (string xmlDoc)
		{
			if (string.IsNullOrEmpty (xmlDoc))
				return;
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<root>");
			bool wasWs = false;
			foreach (char ch in xmlDoc) {
				if (char.IsWhiteSpace (ch)) {
					if (!wasWs)
						sb.Append (' ');
					wasWs = true;
					continue;
				}
				wasWs = false;
				sb.Append (ch);
			}
			sb.Append ("</root>");
			try {
				using (var reader = XmlTextReader.Create (new System.IO.StringReader (sb.ToString ()))) {
					while (reader.Read ()) {
						if (reader.NodeType != XmlNodeType.Element)
							continue;
						if (reader.LocalName == "root") {
							continue;
						}
						Section readSection = new Section (reader.LocalName);
						if (reader.MoveToFirstAttribute ()) {
							do {
								readSection.SetAttribute (reader.LocalName, reader.Value);
							} while (reader.MoveToNextAttribute ());
						}
						readSection.Documentation = reader.ReadElementString ();
						sections.Add (readSection);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while filling documentation", e);
			}
		}

		public INamedElement member;
//		MemberVisitor visitor = new MemberVisitor ();
		string currentType;
		int wordCount;
		
		static string GetType (IEntity member)
		{
			switch (member.EntityType) {
			case EntityType.Event:
				return "event";
			case EntityType.Field:
				return "field";
			case EntityType.Constructor:
				return "constructor";
			case EntityType.Destructor:
				return "destructor";
			case EntityType.Operator:
				return "operator";
			case EntityType.Method:
				return "method";
//			case MemberType.Parameter:
//				return "parameter";
			case EntityType.Indexer:
				return "indexer";
			case EntityType.Property:
				return "property";
			case EntityType.TypeDefinition:
				switch (((ITypeDefinition)member).Kind) {
				case TypeKind.Class:
					return "class";
				case TypeKind.Delegate:
					return "delegate";
				case TypeKind.Enum:
					return "enum";
				case TypeKind.Interface:
					return "interface";
				case TypeKind.Struct:
					return "struct";
				}
				break;
			}
			return "unknown";
		}
		
		static string GetOperator (string methodName)
		{
			switch (methodName) {
			case "op_Subtraction":
			case "op_UnaryNegation":
				return "-";
				
			case "op_Addition":
			case "op_UnaryPlus":
				return "+";
			case "op_Multiply":
				return "*";
			case "op_Division":
				return "/";
			case "op_Modulus":
				return "%";
			case "op_LogicalNot":
				return "!";
			case "op_OnesComplement":
				return "~";
			case "op_BitwiseAnd":
				return "&";
			case "op_BitwiseOr":
				return "|";
			case "op_ExclusiveOr":
				return "^";
			case "op_LeftShift":
				return "<<";
			case "op_RightShift":
				return ">>";
			case "op_GreaterThan":
				return ">";
			case "op_GreaterThanOrEqual":
				return ">=";
			case "op_Equality":
				return "==";
			case "op_Inequality":
				return "!=";
			case "op_LessThan":
				return "<";
			case "op_LessThanOrEqual":
				return "<=";
			case "op_Increment":
				return "++";
			case "op_Decrement":
				return "--";
				
			case "op_True":
				return "true";
			case "op_False":
				return "false";
				
			case "op_Implicit":
				return "implicit";
			case "op_Explicit":
				return "explicit";
			}
			return null;
		}
		
		public bool EvaluateCondition (List<KeyValuePair<string, string>> conditions)
		{
			foreach (var condition in conditions) {
				bool result = false;
				foreach (string val in condition.Value.Split (',')) {
					switch (condition.Key) {
					case "type":
						result |= val == currentType;
						break;
					case "modifier":
						if (member is IMember) {
							try {
								var mod = (Accessibility)Enum.Parse (typeof(Accessibility), val);
								result |=  ((IMember)member).Accessibility == mod;
							} catch (Exception) {
							}
						}
						break;
					case "paramCount":
						if (member is IParameterizedMember)
							result |= Int32.Parse (val) == ((IParameterizedMember)member).Parameters.Count;
						break;
					case "parameter":
						if (!(member is IParameterizedMember))
							break;
						string[] par = val.Split(':');
						int idx = Int32.Parse (par[0]);
						string name = par[1];
						result |= idx < ((IParameterizedMember)member).Parameters.Count && name == ((IParameterizedMember)member).Parameters[idx].Name;
						break;
					case "returns":
						if ((member as IMember) == null)
							break;
						result |= val == ((IMember)member).ReturnType.ToString ();
						break;
					case "name":
						IMethod method = member as IMethod;
						if (method != null && method.IsSynthetic) {
							string op = GetOperator (method.Name);
							if (op != null) {
								result |= val == op;
								break;
							}
						}
							
						result |= val == member.Name;
						break;
					case "endsWith":
						if (member == null)
							break;
						result |= member.Name.EndsWith (val);
						break;
					case "startsWith":
						if (member == null)
							break;
						result |= member.Name.StartsWith (val);
						break;
					case "startsWithWord":
						if (member == null)
							break;
						result |= member.Name.StartsWith (val);
						break;
					case "wordCount":
						result |= Int32.Parse (val) == wordCount;
						break;
					default:
						throw new Exception ("unknown condition:" + condition.Key);
					}
				}
				if (!result)
					return false;
			}
			return true;
		}
		
		internal string curName;
		public void GenerateDoc (IEntity member)
		{
			Init (member);
			
			this.member = member;
			this.currentType = GetType (member);
			DocConfig.Instance.Rules.ForEach (r => r.Run (this));
			
			if (member is IParameterizedMember) {
				this.currentType = "parameter";
				foreach (var p in ((IParameterizedMember)member).Parameters) {
					curName = p.Name;
					this.member = member;
					SplitWords (p, p.Name);
					DocConfig.Instance.Rules.ForEach (r => r.Run (this));
				}
			}
			
			if (member is IMethod) {
				IMethod method = (IMethod)member;
				int count = 1;
				foreach (var param in method.TypeParameters) {
					this.currentType = "typeparam";
					curName = param.Name;
					tags["TypeParam"] = param.Name;
					switch (count) {
					case 1:
						tags["TypeParamNumber"] = "1st";
						break;
					case 2:
						tags["TypeParamNumber"] = "2nd";
						break;
					case 3:
						tags["TypeParamNumber"] = "3rd";
						break;
					default:
						tags["TypeParamNumber"] = count + "th";
						break;
					}
					count++;
					DocConfig.Instance.Rules.ForEach (r => r.Run (this));
				}
			}
			
//			ITypeDefinition type;
//			if (member is ITypeDefinition) {
//				type = (ITypeDefinition)member;
//			} else {
//				type = ((IMember)member).DeclaringTypeDefinition;
//			}
			
// TODO: Exceptions!
//			this.currentType = "exception";
//			foreach (var exception in visitor.Exceptions) {
//				var exceptionType = MonoDevelop.Refactoring.HelperMethods.ConvertToReturnType (exception);
//				
//				
//				curName = exceptionType.FullName;
//				tags["Exception"] = exceptionType.ToInvariantString ();
//				SplitWords (exceptionType, exceptionType.Name);
//				
//				if (type != null) {
//					IType resolvedType = type.GetProjectContent ().SearchType (type.CompilationUnit, type, type.Location, exceptionType);
//					string sentence = AmbienceService.GetDocumentationSummary (resolvedType);
//					if (! string.IsNullOrEmpty(sentence)) {
//						sentence = sentence.Trim ();
//						if (sentence.StartsWith ("<para>") && sentence.EndsWith ("</para>"))
//							sentence = sentence.Substring ("<para>".Length, sentence.Length - "<para>".Length - "</para>".Length).Trim ();
//						if (sentence.StartsWith ("Represents the error that occurs when"))
//							sentence = "Is thrown when" + sentence.Substring ("Represents the error that occurs when".Length);
//						if (!string.IsNullOrEmpty (sentence))
//							Set ("exception", curName, sentence);
//					}
//				}
//				
//				DocConfig.Instance.Rules.ForEach (r => r.Run (this));
//			}
		}
		
		void Init (IEntity member)
		{
			FillDocumentation (GetBaseDocumentation (member));
			//			if (provider != null && !member.Location.IsEmpty && member.BodyRegion.EndLine > 1) {
			//				LineSegment start = data.Document.GetLine (member.Region.BeginLine);
			//				LineSegment end = data.Document.GetLine (member.BodyRegion.EndLine);
			//				if (start != null && end != null) {
			//					var result = provider.ParseFile ("class A {" + data.Document.GetTextAt (start.Offset, end.EndOffset - start.Offset) + "}");
			//					result.AcceptVisitor (visitor, null);
			//				}
			//			}
			foreach (var macro in DocConfig.Instance.Macros) {
				tags.Add (macro.Key, macro.Value);
			}
			if (member.DeclaringTypeDefinition != null) {
				tags ["DeclaringType"] = "<see cref=\"" + member.DeclaringTypeDefinition.ReflectionName + "\"/>";
				switch (member.DeclaringTypeDefinition.Kind) {
				case TypeKind.Class:
					tags ["DeclaringTypeKind"] = "class";
					break;
				case TypeKind.Delegate:
					tags ["DeclaringTypeKind"] = "delegate";
					break;
				case TypeKind.Enum:
					tags ["DeclaringTypeKind"] = "enum";
					break;
				case TypeKind.Interface:
					tags ["DeclaringTypeKind"] = "interface";
					break;
				case TypeKind.Struct:
					tags ["DeclaringTypeKind"] = "struct";
					break;
				}
			}
			if (member is IMember)
				tags ["ReturnType"] = ((IMember)member).ReturnType != null ? "<see cref=\"" + ((IMember)member).ReturnType + "\"/>" : "";
			tags ["Member"] = "<see cref=\"" + member.Name + "\"/>";

			
			if (member is IParameterizedMember) {
				List<string> parameterNames = new List<string> (from p in ((IParameterizedMember)member).Parameters select p.Name);
				tags ["ParameterSentence"] = string.Join (" ", parameterNames.ToArray ());
				StringBuilder paramList = new StringBuilder ();
				for (int i = 0; i < parameterNames.Count; i++) {
					if (i > 0) {
						if (i == parameterNames.Count - 1) {
							paramList.Append (" and ");
						} else {
							paramList.Append (", ");
						}
					}
					paramList.Append (parameterNames [i]);
				}
				tags ["ParameterList"] = paramList.ToString ();
				for (int i = 0; i < ((IParameterizedMember)member).Parameters.Count; i++) {
					tags ["Parameter" + i + ".Type"] = ((IParameterizedMember)member).Parameters [i].Type != null ? "<see cref=\"" + ((IParameterizedMember)member).Parameters [i].Type + "\"/>" : "";
					tags ["Parameter" + i + ".Name"] = "<c>" + ((IParameterizedMember)member).Parameters [i].Name + "</c>";
				}
				
				var property = member as IProperty;
				if (property != null) {
					if (property.CanGet && property.CanSet && property.Getter.Accessibility != Accessibility.Private && property.Setter.Accessibility != Accessibility.Private) {
						tags ["AccessText"] = "Gets or sets";
					} else if (property.CanGet && property.Getter.Accessibility != Accessibility.Private) {
						tags ["AccessText"] = "Gets";
					} else if (property.Setter.Accessibility != Accessibility.Private) {
						tags ["AccessText"] = "Sets";
					} else if (property.CanGet && property.CanSet) {
						tags ["AccessText"] = "Gets or sets";
					} else if (property.CanGet) {
						tags ["AccessText"] = "Gets";
					} else {
						tags ["AccessText"] = "Sets";
					}
				}
			}
			
			SplitWords (member, member.Name);
		}
		
		void SplitWords (object obj, string name)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < name.Length; i++) {
				char ch = name[i];
				
				if (char.IsUpper (ch)) {
					if (result.Length > 0)
						result.Append (" ");
					if (i + 1 < name.Length && char.IsUpper (name[i + 1])) {
						while (i + 1 < name.Length && char.IsUpper (name[i + 1])) {
							result.Append (name[i]);
							i++;
						}
						if (i + 1 < name.Length) {
							result.Append (" ");
							result.Append (char.ToLower (name[i]));
						}
						continue;
					}
				}
				result.Append (char.ToLower (ch));
			}
			
			List<string> words = new List<string> (result.ToString ().Split (' '));
			wordCount = words.Count;
			for (int i = 0; i < words.Count; i++) {
				string lowerWord = words[i].ToLower ();
				if (DocConfig.Instance.WordExpansions.ContainsKey (lowerWord)) {
					words[i] = DocConfig.Instance.WordExpansions[lowerWord];
				} else if (DocConfig.Instance.WordLists["acronyms"].Contains (words[i].ToUpper ())) {
					words[i] = words[i].ToUpper ();
				}
			}
			tags["First"] = words[0];
			tags["AllWords"] = string.Join (" ", words.ToArray ());
			tags["AllWordsExceptFirst"] = string.Join (" ", words.ToArray (), 1, words.Count - 1);
			
			int theIndex = 0;
			int ofTheIndex = 0;
			if (obj is IMethod) {
				theIndex = ofTheIndex = 1;
			}
			
			if (ofTheIndex < words.Count && DocConfig.Instance.WordLists["prefixThe"].Contains (words[ofTheIndex].ToLower ()))
				ofTheIndex++;
			
			int ofIndex = words.Count - 1;
			if (ofTheIndex + 1 < words.Count && DocConfig.Instance.WordLists["ofThe"].Contains (words[ofIndex].ToLower ())) {
				string word = words[ofIndex];
				words.RemoveAt (ofIndex);
				words.Insert (ofTheIndex, "the");
				words.Insert (ofTheIndex, "of");
				words.Insert (ofTheIndex, word);
			} 
			
			if (obj is IMethod && words.Count > 1) {
				if (words[0].EndsWith("s")) {
					words[0] += "es";
				} else if (words[0].EndsWith("y")) {
					words[0] = words[0].Substring (0, words[0].Length - 1) + "ies";
				} else {
					words[0] += "s";
				}
				theIndex = 1;
			}
			
			tags["FirstAsVerb"] = words[0];
			
			if (theIndex < words.Count && !DocConfig.Instance.WordLists["noThe"].Contains (words[theIndex].ToLower ()))
				words.Insert (theIndex, "the");
			
			tags["Sentence"] = string.Join (" ", words.ToArray ());
		}
		
		public void Set (string name, string parameterName, string doc)
		{
			if (name.StartsWith ("param") && name.Length > "param".Length) {
				parameterName = ((IParameterizedMember)member).Parameters[int.Parse (name.Substring("param".Length))].Name;
				name = "param";
			}
			Section newSection = new Section (name);
			
			if (parameterName != null)
				newSection.SetAttribute (name == "exception" ? "cref" : "name", parameterName);
			
			newSection.Documentation = doc;
			
			for (int i = 0; i < sections.Count; i++) {
				if (sections[i].Name == name) {
					if (!string.IsNullOrEmpty (parameterName) && !sections[i].Attributes.Any (a => a.Value == parameterName))
						continue;
					sections[i] = newSection;
					return;
				}
			}
			
			sections.Add (newSection);
		}
		
		#region implemented abstract members of MonoDevelop.Projects.Text.DocGenerator
		public override string GenerateDocumentation (IMember member, string linePrefix)
		{
			return DocumentBufferHandler.GenerateDocumentation (null, member, "", linePrefix);
		}
		#endregion
	}
}

