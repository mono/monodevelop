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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Refactoring;



namespace MonoDevelop.DocFood
{
	public class DocGenerator : MonoDevelop.Projects.Text.DocGenerator
	{
		public List<Section> sections = new List<Section> ();
		public Dictionary<string, string> tags = new Dictionary<string, string> ();
		TextEditorData data;
		INRefactoryASTProvider provider;

		public DocGenerator ()
		{
			
		}

		public DocGenerator (TextEditorData data)
		{
			this.data = data;
			if (data != null)
				provider = RefactoringService.GetASTProvider (data.Document.MimeType);
		}
		
		public static string GetBaseDocumentation (IMember member)
		{
			if (member.DeclaringType == null || member.DeclaringType.SourceProjectDom == null)
				return null;
			if (member is IMethod && (((IMethod)member).IsConstructor || ((IMethod)member).IsFinalizer))
				return null;
			foreach (IType type in member.DeclaringType.SourceProjectDom.GetInheritanceTree (member.DeclaringType)) {
				if (type.DecoratedFullName == member.DeclaringType.DecoratedFullName)
					continue;
				IMember documentMember = null;
				foreach (IMember searchedMember in type.SearchMember (member.Name, true)) {
					if (searchedMember.MemberType == member.MemberType && searchedMember.Name == member.Name && searchedMember.CanHaveParameters == member.CanHaveParameters) {
						if (searchedMember.CanHaveParameters && searchedMember.Parameters.Count != member.Parameters.Count)
							continue;
						if (searchedMember.Modifiers != member.Modifiers)
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

		public IBaseMember member;
		MemberVisitor visitor = new MemberVisitor ();
		string currentType;
		int wordCount;
		
		static string GetType (IBaseMember member)
		{
			switch (member.MemberType) {
			case MemberType.Event:
				return "event";
			case MemberType.Field:
				return "field";
			case MemberType.Method:
				if (((IMethod)member).IsConstructor)
					return "constructor";
				if (((IMethod)member).IsFinalizer)
					return "destructor";
				if (((IMethod)member).IsSpecialName && GetOperator (member.Name) != null)
					return "operator";
				return "method";
			case MemberType.Parameter:
				return "parameter";
			case MemberType.Property:
				if (((IProperty)member).IsIndexer)
					return "indexer";
				return "property";
			case MemberType.Type:
				switch (((IType)member).ClassType) {
				case ClassType.Class:
					return "class";
				case ClassType.Delegate:
					return "delegate";
				case ClassType.Enum:
					return "enum";
				case ClassType.Interface:
					return "interface";
				case ClassType.Struct:
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
							var mod = (Modifiers)Enum.Parse (typeof(Modifiers), val);
							result |=  (mod & ((IMember)member).Modifiers) == mod;
						}
						break;
					case "paramCount":
						if (member is IMember)
							result |= Int32.Parse (val) == ((IMember)member).Parameters.Count;
						break;
					case "parameter":
						if (!(member is IMember))
							break;
						string[] par = val.Split(':');
						int idx = Int32.Parse (par[0]);
						string name = par[1];
						result |= idx < ((IMember)member).Parameters.Count && name == ((IMember)member).Parameters[idx].Name;
						break;
					case "returns":
						if (member == null || member.ReturnType == null)
							break;
						result |= val == member.ReturnType.DecoratedFullName;
						break;
					case "name":

						IMethod method = member as IMethod;
						if (method != null && method.IsSpecialName) {
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
		public void GenerateDoc (IMember member)
		{
			Init (member);
			
			this.member = member;
			this.currentType = GetType (member);
			DocConfig.Instance.Rules.ForEach (r => r.Run (this));
			
			if (member.CanHaveParameters) {
				this.currentType = "parameter";
				foreach (IParameter p in member.Parameters) {
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
			
			IType type;
			if (member is IType) {
				type = (IType)member;
			} else {
				type = member.DeclaringType;
			}
			
			this.currentType = "exception";
			foreach (var exception in visitor.Exceptions) {
				var exceptionType = MonoDevelop.Refactoring.HelperMethods.ConvertToReturnType (exception);
				
				
				curName = exceptionType.FullName;
				tags["Exception"] = exceptionType.ToInvariantString ();
				SplitWords (exceptionType, exceptionType.Name);
				
				if (type != null) {
					IType resolvedType = type.SourceProjectDom.SearchType (type.CompilationUnit, type, type.Location, exceptionType);
					string sentence = AmbienceService.GetDocumentationSummary (resolvedType);
					if (! string.IsNullOrEmpty(sentence)) {
						sentence = sentence.Trim ();
						if (sentence.StartsWith ("<para>") && sentence.EndsWith ("</para>"))
							sentence = sentence.Substring ("<para>".Length, sentence.Length - "<para>".Length - "</para>".Length).Trim ();
						if (sentence.StartsWith ("Represents the error that occurs when"))
							sentence = "Is thrown when" + sentence.Substring ("Represents the error that occurs when".Length);
						if (!string.IsNullOrEmpty (sentence))
							Set ("exception", curName, sentence);
					}
				}
				
				DocConfig.Instance.Rules.ForEach (r => r.Run (this));
			}
		}
		
		void Init (IMember member)
		{
			FillDocumentation (GetBaseDocumentation (member));
			if (provider != null && !member.Location.IsEmpty && member.BodyRegion.End.Line > 1) {
				LineSegment start = data.Document.GetLine (member.Location.Line);
				LineSegment end = data.Document.GetLine (member.BodyRegion.End.Line);
				if (start != null && end != null) {
					var result = provider.ParseFile ("class A {" + data.Document.GetTextAt (start.Offset, end.EndOffset - start.Offset) + "}");
					result.AcceptVisitor (visitor, null);
				}
			}
			foreach (var macro in DocConfig.Instance.Macros) {
				tags.Add (macro.Key, macro.Value);
			}
			if (member.DeclaringType != null) {
				tags["DeclaringType"] = "<see cref=\"" + member.DeclaringType.DecoratedFullName + "\"/>";
				switch (member.DeclaringType.ClassType) {
				case ClassType.Class:
					tags["DeclaringTypeKind"] = "class";
					break;
				case ClassType.Delegate:
					tags["DeclaringTypeKind"] = "delegate";
					break;
				case ClassType.Enum:
					tags["DeclaringTypeKind"] = "enum";
					break;
				case ClassType.Interface:
					tags["DeclaringTypeKind"] = "interface";
					break;
				case ClassType.Struct:
					tags["DeclaringTypeKind"] = "struct";
					break;
				}
			}
			if (member.ReturnType != null)
				tags["ReturnType"] = member.ReturnType != null ? "<see cref=\"" + member.ReturnType.ToInvariantString () + "\"/>" : "";
			tags["Member"] = "<see cref=\"" + member.Name+ "\"/>";

			
			if (member.CanHaveParameters) {
				List<string> parameterNames = new List<string> (from p in member.Parameters select p.Name);
				tags["ParameterSentence"] = string.Join (" ", parameterNames.ToArray ());
				StringBuilder paramList = new StringBuilder ();
				for (int i = 0; i < parameterNames.Count; i++) {
					if (i > 0) {
						if (i == parameterNames.Count - 1) {
							paramList.Append (" and ");
						} else {
							paramList.Append (", ");
						}
					}
					paramList.Append (parameterNames[i]);
				}
				tags["ParameterList"] = paramList.ToString ();
				for (int i = 0; i < member.Parameters.Count; i++) {
					tags["Parameter" + i +  ".Type"] = member.Parameters[i].ReturnType != null ? "<see cref=\"" + member.Parameters[i].ReturnType.ToInvariantString () + "\"/>" : "";
					tags["Parameter" + i +  ".Name"] = "<c>" + member.Parameters[i].Name + "</c>";
				}
				
				IProperty property = member as IProperty;
				if (property != null) {
					if (property.HasGet && property.HasSet) {
						tags["AccessText"] = "Gets or sets";
					} else if (property.HasGet) {
						tags["AccessText"] = "Gets";
					} else {
						tags["AccessText"] = "Sets";
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
				parameterName = ((IMember)member).Parameters[int.Parse (name.Substring("param".Length))].Name;
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

