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
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Immutable;

namespace MonoDevelop.DocFood
{
	class DocGenerator : MonoDevelop.Projects.Text.DocGenerator
	{
		public List<Section> sections = new List<Section> ();
		public Dictionary<string, string> tags = new Dictionary<string, string> ();
//		TextEditorData data;
		
		public DocGenerator ()
		{
			
		}
		
		public DocGenerator (IReadonlyTextDocument data)
		{
//			this.data = data;
		}
		
		public static string GetBaseDocumentation (ISymbol member)
		{
			if (member.ContainingType == null)
				return null;
			// TODO: Roslyn port!
//			if (member is IMethodSymbol && (((IMethodSymbol)member).IsConstructor || ((IMethodSymbol)member).IsDestructor))
//				return null;
//			foreach (var type in member.ContainingType.GetAllBaseTypeDefinitions ()) {
//				if (type.Equals (member.ContainingType))
//					continue;
//				IMember documentMember = null;
//				foreach (var searchedMember in type.Members.Where (m => m.Name == member.Name)) {
//					if (searchedMember.SymbolKind == member.SymbolKind && searchedMember.Name == member.Name) {
//						if ((searchedMember is IParameterizedMember) && ((IParameterizedMember)searchedMember).Parameters.Count != ((IParameterizedMember)member).Parameters.Count)
//							continue;
//						if (searchedMember.Accessibility != member.Accessibility)
//							continue;
//						documentMember = searchedMember;
//						break;
//					}
//				}
//				if (documentMember != null) {
//					string documentation = AmbienceService.GetDocumentation (documentMember);
//					if (documentation != null)
//						return documentation;
//				}
//			}
			return null;
		}

		void FillDocumentation (string xmlDoc)
		{
			if (string.IsNullOrEmpty (xmlDoc))
				return;
			StringBuilder sb = StringBuilderCache.Allocate ();
			sb.Append ("<root>");
			bool wasWs = false;
			foreach (char ch in xmlDoc) {
				if (ch =='\r')
					continue;
				if (ch == ' ' || ch == '\t') {
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
				using (var reader = XmlTextReader.Create (new System.IO.StringReader (StringBuilderCache.ReturnAndFree (sb)))) {
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
						readSection.Documentation = reader.ReadElementString ().Trim ();
						sections.Add (readSection);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while filling documentation", e);
			}
		}

		public ISymbol member;
//		MemberVisitor visitor = new MemberVisitor ();
		string currentType;
		int wordCount;
		
		static string GetType (ISymbol member)
		{
			switch (member.Kind) {
			case SymbolKind.Event:
				return "event";
			case SymbolKind.Field:
				return "field";
			case SymbolKind.Method:
				switch (((IMethodSymbol)member).MethodKind) {
				case MethodKind.StaticConstructor:
				case MethodKind.Constructor:
					return "constructor";
				case MethodKind.Destructor:
					return "destructor";
				case MethodKind.UserDefinedOperator:
				case MethodKind.BuiltinOperator:
					return "operator";
				}
				return "method";
			case SymbolKind.Parameter:
				return "parameter";
			case SymbolKind.Property:
				if (((IPropertySymbol)member).IsIndexer)
					return "indexer";
				return "property";
			case SymbolKind.NamedType:
				switch (((INamedTypeSymbol)member).TypeKind) {
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

		static string GetName (ISymbol member)
		{
			return member.Name;
		}
		
		public bool EvaluateCondition (List<KeyValuePair<string, string>> conditions, ISymbol member)
		{
			foreach (var condition in conditions) {
				bool result = false;
				foreach (string val in condition.Value.Split (',')) {
					switch (condition.Key) {
					case "type":
						result |= val == currentType;
						break;
					case "modifier":
						if (val.ToUpperInvariant () == "STATIC"){
							result |= member.IsStatic;
						} else {
							try {
								var mod = (Accessibility)Enum.Parse (typeof(Accessibility), val);
								result |=  member.DeclaredAccessibility == mod;
							} catch (Exception) {
							}
						}
						break;
					case "paramCount":
						var parameters = GetParameters (member);
						result |= Int32.Parse (val) == parameters.Length;
						break;
					case "parameter":
						parameters = GetParameters (member);
						string[] par = val.Split(':');
						int idx = Int32.Parse (par[0]);
						string name = par[1];
						result |= idx < parameters.Length && name == parameters[idx].Name;
						break;
					case "returns":
						if (member is IParameterSymbol) {
							result |= val == ((IParameterSymbol)member).Type.ToString ();
							break;
						}

						result |= val == member.GetReturnType ().ToString ();
						break;
					case "name":
						var method = member as IMethodSymbol;
						if (method != null && method.MethodKind == MethodKind.UserDefinedOperator) {
							string op = GetOperator (method.Name);
							if (op != null) {
								result |= val == op;
								break;
							}
						}
							
						result |= val == GetName (member);
						break;
					case "endsWith":
						if (member == null)
							break;
						result |= GetName (member).EndsWith (val, StringComparison.Ordinal);
						break;
					case "startsWith":
						if (member == null)
							break;
						result |= GetName (member).StartsWith (val, StringComparison.Ordinal);
						break;
					case "startsWithWord":
						if (member == null)
							break;
						var name2 = SeparateWords (GetName (member));
						result |= name2.StartsWith (val + " ");
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
		public void GenerateDoc (ISymbol member)
		{
			Init (member);
			
			this.member = member;
			this.currentType = GetType (member);
			DocConfig.Instance.Rules.ForEach (r => r.Run (this, member));
			
			if (member is IPropertySymbol || member is IMethodSymbol) {
				this.currentType = "parameter";
				foreach (var p in GetParameters (member)) {
					curName = p.Name;
					this.member = member;
					SplitWords (p, p.Name);
					DocConfig.Instance.Rules.ForEach (r => r.Run (this, p));
				}
			}
			
			if (member is IMethodSymbol) {
				var method = (IMethodSymbol)member;
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
					DocConfig.Instance.Rules.ForEach (r => r.Run (this, param));
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

		void Init (ISymbol member)
		{
			if (member == null)
				throw new ArgumentNullException ("member");
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
			if (member.ContainingType != null) {
				tags ["DeclaringType"] = "<see cref=\"" + member.ContainingType.GetDocumentationCommentId () + "\"/>";
				switch (member.ContainingType.TypeKind) {
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
			var returnType = member.GetReturnType ();
			tags ["ReturnType"] = returnType != null ? "<see cref=\"" + returnType.GetDocumentationCommentId () + "\"/>" : "";
			tags ["Member"] = "<see cref=\"" + member.Name + "\"/>";


			var property = member as IPropertySymbol;
			var method = member as IMethodSymbol;
			if (property != null || method != null) {
				var parameters = property != null? property.Parameters : method.Parameters;
				var parameterNames = new List<string> (from p in parameters select p.Name);
				tags ["ParameterSentence"] = string.Join (" ", parameterNames.ToArray ());
				StringBuilder paramList = StringBuilderCache.Allocate ();
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
				tags ["ParameterList"] = StringBuilderCache.ReturnAndFree (paramList);
				for (int i = 0; i < parameters.Length; i++) {
					tags ["Parameter" + i + ".Type"] = parameters [i].Type != null ? "<see cref=\"" + parameters [i].Type + "\"/>" : "";
					tags ["Parameter" + i + ".Name"] = "<c>" + parameters [i].Name + "</c>";
				}
				
				if (property != null) {
					var hasPublicGetter = property.GetMethod != null && property.GetMethod.DeclaredAccessibility != Accessibility.Private;
					var hasPublicSetter = property.SetMethod != null && property.SetMethod.DeclaredAccessibility != Accessibility.Private;

					if (property.GetMethod != null && property.SetMethod != null && hasPublicGetter && hasPublicSetter) {
						tags ["AccessText"] = "Gets or sets";
					} else if (property.GetMethod != null && hasPublicGetter) {
						tags ["AccessText"] = "Gets";
					} else if (hasPublicSetter) {
						tags ["AccessText"] = "Sets";
					} else if (property.GetMethod != null && property.SetMethod != null) {
						tags ["AccessText"] = "Gets or sets";
					} else if (property.GetMethod != null) {
						tags ["AccessText"] = "Gets";
					} else {
						tags ["AccessText"] = "Sets";
					}
				}
			}
			
			SplitWords (member, member.Name);
		}

		static readonly string[,] irregularVerbs = new string[,] {
			{ "arise", "arose", "arisen"},
			{ "awake", "awoke", "awoken"},
			{ "backslide", "backslid", "backslidden"},
			{ "be", "was, were", "been"},
			{ "bear", "bore", "born"},
			{ "beat", "beat", "beaten"},
			{ "become", "became", "become"},
			{ "begin", "began", "begun"},
			{ "bend", "bent", "bent"},
			{ "bet", "bet", "bet"},
			{ "bid", "bid", "bidden"},
			{ "bind", "bound", "bound"},
			{ "bite", "bit", "bitten"},
			{ "bleed", "bled", "bled"},
			{ "blow", "blew", "blown"},
			{ "break", "broke", "broken"},
			{ "breed", "bred", "bred"},
			{ "bring", "brought", "brought"},
			{ "broadcast", "broadcast", "broadcast"},
			{ "browbeat", "browbeat", "browbeaten"},
			{ "build", "built", "built"},
			{ "burn", "burned", "burned"},
			{ "burst", "burst", "burst"},
			{ "bust", "busted", "busted"},
			{ "buy", "bought", "bought"},
			{ "cast", "cast", "cast"},
			{ "catch", "caught", "caught"},
			{ "choose", "chose", "chosen"},
			{ "cling", "clung", "clung"},
			{ "clothe", "clothed", "clothed"},
			{ "come", "came", "come"},
			{ "cost", "cost", "cost"},
			{ "creep", "crept", "crept"},
			{ "crossbreed", "crossbred", "crossbred"},
			{ "cut", "cut", "cut"},
			{ "daydream", "daydreamed", "daydreamed"},
			{ "deal", "dealt", "dealt"},
			{ "dig", "dug", "dug"},
			{ "disprove", "disproved", "disproved"},
			{ "dive", "dove", "dived"},
			{ "do", "did", "done"},
			{ "draw", "drew", "drawn"},
			{ "dream", "dreamed", "dreamed"},
			{ "drink", "drank", "drunk"},
			{ "drive", "drove", "driven"},
			{ "dwell", "dwelt", "dwelt"},
			{ "eat", "ate", "eaten"},
			{ "fall", "fell", "fallen"},
			{ "feed", "fed", "fed"},
			{ "feel", "felt", "felt"},
			{ "fight", "fought", "fought"},
			{ "find", "found", "found"},
			{ "fit", "fitted", "fitted"},
			{ "flee", "fled", "fled"},
			{ "fling", "flung", "flung"},
			{ "fly", "flew", "flown"},
			{ "forbid", "forbade", "forbidden"},
			{ "forecast", "forecast", "forecast"},
			{ "forego", "forewent", "foregone"},
			{ "foresee", "foresaw", "foreseen"},
			{ "foretell", "foretold", "foretold"},
			{ "forget", "forgot", "forgotten"},
			{ "forgive", "forgave", "forgiven"},
			{ "forsake", "forsook", "forsaken"},
			{ "freeze", "froze", "frozen"},
			{ "frostbite", "frostbit", "frostbitten"},
			{ "get", "got", "gotten"},
			{ "give", "gave", "given"},
			{ "go", "went", "gone"},
			{ "grind", "ground", "ground"},
			{ "grow", "grew", "grown"},
			{ "hand-feed", "hand-fed", "hand-fed"},
			{ "handwrite", "handwrote", "handwritten"},
			{ "hang", "hung", "hung"},
			{ "have", "had", "had"},
			{ "hear", "heard", "heard"},
			{ "hew", "hewed", "hewn"},
			{ "hide", "hid", "hidden"},
			{ "hit", "hit", "hit"},
			{ "hold", "held", "held"},
			{ "hurt", "hurt", "hurt"},
			{ "inbreed", "inbred", "inbred"},
			{ "inlay", "inlaid", "inlaid"},
			{ "input", "input", "input"},
			{ "interbreed", "interbred", "interbred"},
			{ "interweave", "interwove", "interwoven"},
			{ "interwind", "interwound", "interwound"},
			{ "jerry-build", "jerry-built", "jerry-built"},
			{ "keep", "kept", "kept"},
			{ "kneel", "knelt", "knelt"},
			{ "knit", "knitted", "knitted"},
			{ "know", "knew", "known"},
			
			{ "lay", "laid", "laid"},
			{ "lead", "led", "led"},
			{ "lean", "leaned", "leaned"},
			{ "leap", "leaped", "leaped"},
			{ "learn", "learned", "learned"},
			{ "leave", "left", "left"},
			{ "lend", "lent", "lent"},
			{ "let", "let", "let"},
			{ "lie", "lay", "lain"},
			{ "lie", "lied", "lied"},
			{ "light", "lit", "lit"},
			{ "lip-read", "lip-read", "lip-read"},
			{ "lose", "lost", "lost"},
			
			{ "make", "made", "made"},
			{ "mean", "meant", "meant"},
			{ "meet", "met", "met"},
			{ "miscast", "miscast", "miscast"},
			{ "misdeal", "misdealt", "misdealt"},
			{ "misdo", "misdid", "misdone"},
			{ "mishear", "misheard", "misheard"},
			{ "mislay", "mislaid", "mislaid"},
			{ "mislead", "misled", "misled"},
			{ "mislearn", "mislearned", "mislearned"},
			{ "misread", "misread", "misread"},
			{ "misset", "misset", "misset"},
			{ "misspeak", "misspoke", "misspoken"},
			{ "misspell", "misspelled", "misspelled"},
			{ "misspend", "misspent", "misspent"},
			{ "mistake", "mistook", "mistaken"},
			{ "misteach", "mistaught", "mistaught"},
			{ "misunderstand", "misunderstood", "misunderstood"},
			{ "miswrite", "miswrote", "miswritten"},
			{ "mow", "mowed", "mowed"},
			
			{ "offset", "offset", "offset"},
			{ "outbid", "outbid", "outbid"},
			{ "outbreed", "outbred", "outbred"},
			{ "outdo", "outdid", "outdone"},
			{ "outdraw", "outdrew", "outdrawn"},
			{ "outdrink", "outdrank", "outdrunk"},
			{ "outdrive", "outdrove", "outdriven"},
			{ "outfight", "outfought", "outfought"},
			{ "outfly", "outflew", "outflown"},
			{ "outgrow", "outgrew", "outgrown"},
			{ "outleap", "outleaped", "outleaped"},
			{ "outlie", "outlied", "outlied"},
			{ "outride", "outrode", "outridden"},
			{ "outrun", "outran", "outrun"},
			{ "outsell", "outsold", "outsold"},
			{ "outshine", "outshined", "outshined"},
			{ "outshoot", "outshot", "outshot"},
			{ "outsing", "outsang", "outsung"},
			{ "outsit", "outsat", "outsat"},
			{ "outsleep", "outslept", "outslept"},
			{ "outsmell", "outsmelled", "outsmelled"},
			{ "outspeak", "outspoke", "outspoken"},
			{ "outspeed", "outsped", "outsped"},
			{ "outspend", "outspent", "outspent"},
			{ "outswear", "outswore", "outsworn"},
			{ "outswim", "outswam", "outswum"},
			{ "outthink", "outthought", "outthought"},
			{ "outthrow", "outthrew", "outthrown"},
			{ "outwrite", "outwrote", "outwritten"},
			{ "overbid", "overbid", "overbid"},
			{ "overbreed", "overbred", "overbred"},
			{ "overbuild", "overbuilt", "overbuilt"},
			{ "overbuy", "overbought", "overbought"},
			{ "overcome", "overcame", "overcome"},
			{ "overdo", "overdid", "overdone"},
			{ "overdraw", "overdrew", "overdrawn"},
			{ "overdrink", "overdrank", "overdrunk"},
			{ "overeat", "overate", "overeaten"},
			{ "overfeed", "overfed", "overfed"},
			{ "overhang", "overhung", "overhung"},
			{ "overhear", "overheard", "overheard"},
			{ "overlay", "overlaid", "overlaid"},
			{ "overpay", "overpaid", "overpaid"},
			{ "override", "overrode", "overridden"},
			{ "overrun", "overran", "overrun"},
			{ "oversee", "oversaw", "overseen"},
			{ "oversell", "oversold", "oversold"},
			{ "oversew", "oversewed", "oversewn"},
			{ "overshoot", "overshot", "overshot"},
			{ "oversleep", "overslept", "overslept"},
			{ "overspeak", "overspoke", "overspoken"},
			{ "overspend", "overspent", "overspent"},
			{ "overspill", "overspilled", "overspilled"},
			{ "overtake", "overtook", "overtaken"},
			{ "overthink", "overthought", "overthought"},
			{ "overthrow", "overthrew", "overthrown"},
			{ "overwind", "overwound", "overwound"},
			{ "overwrite", "overwrote", "overwritten"},
			
			{ "partake", "partook", "partaken"},
			{ "pay", "paid", "paid"},
			{ "plead", "pleaded", "pleaded"},
			{ "prebuild", "prebuilt", "prebuilt"},
			{ "predo", "predid", "predone"},
			{ "premake", "premade", "premade"},
			{ "prepay", "prepaid", "prepaid"},
			{ "presell", "presold", "presold"},
			{ "preset", "preset", "preset"},
			{ "preshrink", "preshrank", "preshrunk"},
			{ "proofread", "proofread", "proofread"},
			{ "prove", "proved", "proven"},
			{ "put", "put", "put"},
			
			{ "quick-freeze", "quick-froze", "quick-frozen"},
			{ "quit", "quit", "quit"},
			
			{ "read", "read", " read"},
			{ "reawake", "reawoke", "reawaken"},
			{ "rebid", "rebid", "rebid"},
			{ "rebind", "rebound", "rebound"},
			{ "rebroadcast", "rebroadcast", "rebroadcast"},
			{ "rebuild", "rebuilt", "rebuilt"},
			{ "recast", "recast", "recast"},
			{ "recut", "recut", "recut"},
			{ "redeal", "redealt", "redealt"},
			{ "redo", "redid", "redone"},
			{ "redraw", "redrew", "redrawn"},
			{ "refit", "refit", "refit"},
			{ "regrind", "reground", "reground"},
			{ "regrow", "regrew", "regrown"},
			{ "rehang", "rehung", "rehung"},
			{ "rehear", "reheard", "reheard"},
			{ "reknit", "reknitted", "reknitted"},
			{ "relay", "relaid", "relaid"},
			{ "relearn", "relearned", "relearned"},
			{ "relight", "relit", "relit"},
			{ "remake", "remade", "remade"},
			{ "repay", "repaid", "repaid"},
			{ "reread", "reread", "reread"},
			{ "rerun", "reran", "rerun"},
			{ "resell", "resold", "resold"},
			{ "resend", "resent", "resent"},
			{ "reset", "reset", "reset"},
			{ "resew", "resewed", "resewn"},
			{ "retake", "retook", "retaken"},
			{ "reteach", "retaught", "retaught"},
			{ "retear", "retore", "retorn"},
			{ "retell", "retold", "retold"},
			{ "rethink", "rethought", "rethought"},
			{ "retread", "retread", "retread"},
			{ "retrofit", "retrofitted", "retrofitted"},
			{ "rewake", "rewoke", "rewaken"},
			{ "rewear", "rewore", "reworn"},
			{ "reweave", "rewove", "rewoven"},
			{ "rewed", "rewed", "rewed"},
			{ "rewet", "rewet", "rewet"},
			{ "rewin", "rewon", "rewon"},
			{ "rewind", "rewound", "rewound"},
			{ "rewrite", "rewrote", "rewritten"},
			{ "rid", "rid", "rid"},
			{ "ride", "rode", "ridden"},
			{ "ring", "rang", "rung"},
			{ "rise", "rose", "risen"},
			{ "roughcast", "roughcast", "roughcast"},
			{ "run", "ran", "run"},
			
			{ "sand-cast", "sand-cast", "sand-cast"},
			{ "saw", "sawed", "sawed"},
			{ "say", "said", "said"},
			{ "see", "saw", "seen"},
			{ "seek", "sought", "sought"},
			{ "sell", "sold", "sold"},
			{ "send", "sent", "sent"},
			{ "set", "set", "set"},
			{ "sew", "sewed", "sewn"},
			{ "shake", "shook", "shaken"},
			{ "shave", "shaved", "shaved"},
			{ "shear", "sheared", "sheared"},
			{ "shed", "shed", "shed"},
			{ "shine", "shined", "shined"},
			{ "shoot", "shot", "shot"},
			{ "show", "showed", "shown"},
			{ "shrink", "shrank", "shrunk"},
			{ "shut", "shut", "shut"},
			{ "sight-read", "sight-read", "sight-read"},
			{ "sing", "sang", "sung"},
			{ "sink", "sank", "sunk"},
			{ "sit", "sat", "sat"},
			{ "slay", "slew", "slain"},
			{ "sleep", "slept", "slept"},
			{ "slide", "slid", "slid"},
			{ "sling", "slung", "slung"},
			{ "slink", "slinked", "slinked"},
			{ "slit", "slit", "slit"},
			{ "smell", "smelled", "smelled"},
			{ "sneak", "sneaked", "sneaked"},
			{ "sow", "sowed", "sown"},
			{ "speak", "spoke", "spoken"},
			{ "speed", "sped", "sped"},
			{ "spell", "spelled", "spelled"},
			{ "spend", "spent", "spent"},
			{ "spill", "spilled", "spilled"},
			{ "spin", "spun", "spun"},
			{ "spit", "spit", "spit"},
			{ "split", "split", "split"},
			{ "spoil", "spoiled", "spoiled"},
			{ "spoon-feed", "spoon-fed", "spoon-fed"},
			{ "spread", "spread", "spread"},
			{ "spring", "sprang", "sprung"},
			{ "stand", "stood", "stood"},
			{ "steal", "stole", "stolen"},
			{ "stick", "stuck", "stuck"},
			{ "sting", "stung", "stung"},
			{ "stink", "stunk", "stunk"},
			{ "strew", "strewed", "strewn"},
			{ "stride", "strode", "stridden"},
			{ "strike", "struck", "struck"},
			{ "string", "strung", "strung"},
			{ "strive", "strove", "striven"},
			{ "sublet", "sublet", "sublet"},
			{ "sunburn", "sunburned", "sunburned"},
			{ "swear", "swore", "sworn"},
			{ "sweat", "sweat", "sweat"},
			{ "sweep", "swept", "swept"},
			{ "swell", "swelled", "swollen"},
			{ "swim", "swam", "swum"},
			{ "swing", "swung", "swung"},
			
			{ "take", "took", "taken"},
			{ "teach", "taught", "taught"},
			{ "tear", "tore", "torn"},
			{ "telecast", "telecast", "telecast"},
			{ "tell", "told", "told"},
			{ "test-drive", "test-drove", "test-driven"},
			{ "test-fly", "test-flew", "test-flown"},
			{ "think", "thought", "thought"},
			{ "throw", "threw", "thrown"},
			{ "thrust", "thrust", "thrust"},
			{ "tread", "trod", "trodden"},
			{ "typecast", "typecast", "typecast"},
			{ "typeset", "typeset", "typeset"},
			{ "typewrite", "typewrote", "typewritten"},
			
			{ "unbend", "unbent", "unbent"},
			{ "unbind", "unbound", "unbound"},
			{ "unclothe", "unclothed", "unclothed"},
			{ "underbid", "underbid", "underbid"},
			{ "undercut", "undercut", "undercut"},
			{ "underfeed", "underfed", "underfed"},
			{ "undergo", "underwent", "undergone"},
			{ "underlie", "underlay", "underlain"},
			{ "undersell", "undersold", "undersold"},
			{ "underspend", "underspent", "underspent"},
			{ "understand", "understood", "understood"},
			{ "undertake", "undertook", "undertaken"},
			{ "underwrite", "underwrote", "underwritten"},
			{ "undo", "undid", "undone"},
			{ "unfreeze", "unfroze", "unfrozen"},
			{ "unhang", "unhung", "unhung"},
			{ "unhide", "unhid", "unhidden"},
			{ "unknit", "unknitted", "unknitted"},
			{ "unlearn", "unlearned", "unlearned"},
			{ "unsew", "unsewed", "unsewn"},
			{ "unsling", "unslung", "unslung"},
			{ "unspin", "unspun", "unspun"},
			{ "unstick", "unstuck", "unstuck"},
			{ "unstring", "unstrung", "unstrung"},
			{ "unweave", "unwove", "unwoven"},
			{ "unwind", "unwound", "unwound"},
			{ "uphold", "upheld", "upheld"},
			{ "upset", "upset", "upset"},
			
			
			{ "wake", "woke", "woken"},
			{ "waylay", "waylaid", "waylaid"},
			{ "wear", "wore", "worn"},
			{ "weave", "wove", "woven"},
			{ "wed", "wed", "wed"},
			{ "weep", "wept", "wept"},
			{ "wet", "wet", "wet"},
			{ "whet", "whetted", "whetted"},
			{ "win", "won", "won"},
			{ "wind", "wound", "wound"},
			{ "withdraw", "withdrew", "withdrawn"},
			{ "withhold", "withheld", "withheld"},
			{ "withstand", "withstood", "withstood"},
			{ "wring", "wrung", "wrung"}
		};

		string GetPastParticipleVerb (string str)
		{
			for (int i = 0; i < irregularVerbs.GetLength (0); i++) {
				if (irregularVerbs[i, 0] == str)
					return irregularVerbs[i, 2];
			}

			if (str.EndsWith ("e"))
				return str +"d";
			return str + "ed";
		}
		
		void SplitWords (object obj, string name)
		{
			if (obj is ITypeSymbol){
				var type = (ITypeSymbol)obj;
				if (type.TypeKind == TypeKind.Interface && name.Length > 1 && name[0] == 'I' && char.IsUpper (name[1])) {
					name = name.Substring (1);
				}
			}

			List<string> words = new List<string> (SeparateWords (name).Split (' '));
			wordCount = words.Count;
			for (int i = 0; i < words.Count; i++) {
				string lowerWord = words [i].ToLower ();
				if (DocConfig.Instance.WordExpansions.ContainsKey (lowerWord)) {
					words [i] = DocConfig.Instance.WordExpansions [lowerWord];
				} else if (DocConfig.Instance.WordLists ["acronyms"].Contains (words [i].ToUpper ())) {
					words [i] = words [i].ToUpper ();
				}
			}
			tags ["First"] = words [0];
			tags ["AllWords"] = string.Join (" ", words.ToArray ());
			tags ["AllWordsExceptFirst"] = string.Join (" ", words.ToArray (), 1, words.Count - 1);

			int theIndex = 0;
			int ofTheIndex = 0;
			if (obj is IMethodSymbol) {
				theIndex = ofTheIndex = 1;
			}

			if (ofTheIndex < words.Count && DocConfig.Instance.WordLists ["prefixThe"].Contains (words [ofTheIndex].ToLower ()))
				ofTheIndex++;

			int ofIndex = words.Count - 1;
			if (ofTheIndex + 1 < words.Count && DocConfig.Instance.WordLists ["ofThe"].Contains (words [ofIndex].ToLower ())) {
				string word = words [ofIndex];
				words.RemoveAt (ofIndex);
				words.Insert (ofTheIndex, "the");
				words.Insert (ofTheIndex, "of");
				words.Insert (ofTheIndex, word);
			}

			tags ["FirstAsVerbPastParticiple"] = GetPastParticipleVerb (words [0]);
			if (obj is IMethodSymbol && words.Count > 1) {
				if (words [0].EndsWith ("s")) {
					words [0] += "es";
				} else if (words [0].EndsWith ("y")) {
					words [0] = words [0].Substring (0, words [0].Length - 1) + "ies";
				} else {
					words [0] += "s";
				}
				theIndex = 1;
			}

			tags ["FirstAsVerb"] = words [0];

			if (theIndex < words.Count && !DocConfig.Instance.WordLists ["noThe"].Contains (words [theIndex].ToLower ()))
				words.Insert (theIndex, "the");

			tags ["Sentence"] = string.Join (" ", words.ToArray ());
		}

		static string SeparateWords (string name)
		{
			var result = StringBuilderCache.Allocate ();
			bool wasUnderscore = false;
			for (int i = 0; i < name.Length; i++) {
				char ch = name [i];
				if (ch == '_') {
					wasUnderscore = true;
					continue;
				}
				if (char.IsUpper (ch) || wasUnderscore) {
					wasUnderscore = false;
					if (result.Length > 0)
						result.Append (" ");
					if (i + 1 < name.Length && char.IsUpper (name [i + 1])) {
						int j = i;
						while (i < name.Length && char.IsUpper (name [i])) {
							i++;
						}
						if (i >= name.Length || name [i] == '_') {
							if (i != j)
								result.Append (name.Substring (j, i - j).ToLower ());
							continue;
						}
						if (i != j)
							result.Append (name, j, i - j);
						if (i + 1 < name.Length) {
							result.Append (' ');
							result.Append (char.ToLower (name [i]));
						}
						continue;
					}
				}
				wasUnderscore = false;
				result.Append (char.ToLower (ch));
			}

			return StringBuilderCache.ReturnAndFree (result);
		}

		static ImmutableArray<IParameterSymbol> GetParameters (ISymbol symbol)
		{
			return (symbol as IPropertySymbol)?.Parameters
				?? (symbol as IMethodSymbol)?.Parameters
				?? ImmutableArray<IParameterSymbol>.Empty;
		}

		public void Set (string name, string parameterName, string doc)
		{
			if (name.StartsWith ("param", StringComparison.Ordinal) && name.Length > "param".Length) {
				var parameters = GetParameters (member);
				var idx = int.Parse (name.Substring ("param".Length));
				parameterName = idx < parameters.Length ? parameters [idx].Name : "unknown";
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
		public override string GenerateDocumentation (ISymbol member, string linePrefix)
		{
			return DocumentBufferHandler.GenerateDocumentation (null, member, "", linePrefix);
		}
		#endregion
	}
}

