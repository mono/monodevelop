// 
// NamingConventions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICS = ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;

namespace MonoDevelop.CSharp.Inspection
{
	public enum RequiredPhraseKind
	{
		None,
		Verb,
		VerbPast,
		VerbPresent,
		Noun,
		NounSingular,
		NounPlural,
	}
	
	[Flags]
	public enum DeclarationKinds
	{
		None = 0,
		
		Local = LocalVariable | Parameter,
		LocalVariable = 1 << 0,
		Parameter     = 1 << 1,
		
		Member = Property | Method | Field | Event | EnumMember,
		Property      = 1 << 2,
		Method        = 1 << 3,
		Field         = 1 << 4,
		Event         = 1 << 5,
		EnumMember    = 1 << 6,
		
		Type = Class | Delegate | Struct | Interface | Enum,
		Class         = 1 << 7,
		Delegate      = 1 << 8,
		Struct        = 1 << 9,
		Interface     = 1 << 10,
		Enum          = 1 << 11,
		
		TypeParameter = 1 << 12,
		Namespace     = 1 << 13,
		
		All = Local | Member | Type | Namespace,
	}
	
	public class NamingRule
	{
		/// <summary>
		/// If set, matches only on these kinds of nodes.
		/// </summary>
		public DeclarationKinds MatchKind { get; set; }
		
		/// <summary>
		/// If set, matches only on nodes with at least one of these modifiers.
		/// </summary>
		public ICS.Modifiers MatchAnyModifiers { get; set; }
		
		/// <summary>
		/// If set, matches only on nodes with all of these modifiers.
		/// </summary>
		public ICS.Modifiers MatchAllModifiers { get; set; }
		
		/// <summary>
		/// If set, matches only on nodes with none of these modifiers.
		/// </summary>
		public ICS.Modifiers MatchNoModifiers { get; set; }
		
		/// <summary>
		/// If set, identifiers are required to be prefixed with one of these values.
		/// </summary>
		public string[] RequiredPrefixes { get; set; }
		
		/// <summary>
		/// If set, identifiers are required to be suffixed with one of these values.
		/// </summary>
		public string[] RequiredSuffixes { get; set; }
		
		/// <summary>
		/// If set, identifiers cannot be prefixed by any of these values.
		/// </summary>
		
		public string[] ForbiddenPrefixes { get; set; }
		/// <summary>
		/// If set, identifiers cannot be suffixed by with any of these values.
		/// </summary
		public string[] ForbiddenSuffixes { get; set; }
		
		/// <summary>
		/// The way that the identifier is cased and that words are separated.
		/// </summary
		public NamingStyle NamingStyle { get; set; }
		
		/// <summary>
		/// NOT IMPLEMENTED. Requires that acronyms of three or more letters are cased like words.
		/// </summary>
		//FIXME: Implement
		public bool CaseThreeLetterAcronymsAsWords { get; set; }
		
		/// <summary>
		/// NOT IMPLEMENTED. Requires that the identifier uses a particular kind of word or phrase.
		/// </summary>
		//FIXME: Implement
		public RequiredPhraseKind RequiredPhraseKind { get; set; }
		
		/// <summary>
		/// NOT IMPLEMENTED. Requires that the identifier is composed of correctly spelled words.
		/// </summary>
		//FIXME: Implement
		public bool RequireCorrectSpelling { get; set; }
		
		public NamingRule ()
		{
		}
		
		public string AppliedAttributeType { get; set; }
		public string BaseType { get; set; }
		
		bool CheckAttributedNode (ICS.AttributedNode node, ICS.Modifiers defaultVisibility)
		{
			if (!CheckModifiers (node.Modifiers, defaultVisibility))
				return false;
			
			if (!string.IsNullOrEmpty (AppliedAttributeType) && node != null) {
				node.Attributes.Any (section => section.Attributes.Any (a => {
					var st = a.Type as ICS.SimpleType;
					return st != null && st.Identifier == AppliedAttributeType;
				}));
			}
			return true;
		}
		
		bool CheckModifiers (ICS.Modifiers mods, ICS.Modifiers defaultVisibility)
		{
			if ((mods & ICS.Modifiers.VisibilityMask) == 0)
				mods = mods | defaultVisibility;
			
			if (MatchAnyModifiers != 0 && (MatchAnyModifiers & mods) == 0)
				return false;
			
			if ((MatchNoModifiers & mods) != 0)
				return false;
			
			if (MatchAllModifiers != 0 && (MatchAllModifiers & mods) != MatchAllModifiers)
				return false;
			
			return true;
		}
		
		public bool CheckVariableDeclaration (ICS.VariableDeclarationStatement node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.LocalVariable) == 0) || !CheckModifiers (node.Modifiers, ICS.Modifiers.Private))
				return false;
			var member = data.Document.CompilationUnit.GetMemberAt (node.StartLocation.Line, node.StartLocation.Column);
			foreach (var var in node.Variables) {
				string name = var.Name;
				if (IsValid (name))
					continue;
				var v = new LocalVariable (member, name, DomReturnType.Void,
					new DomRegion (node.StartLocation.Line, node.StartLocation.Column,
					node.EndLocation.Line, node.EndLocation.Column));
				data.Add (GetFixableResult (var.NameToken.StartLocation, v, name));
			}
			
			return true;
		}
		
		public bool CheckProperty (ICS.PropertyDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Property) ==  0) || !CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckMethod (ICS.MethodDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Method) == 0) || !CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckParameter (ICS.ParameterDeclaration node, InspectionData data)
		{
			if (MatchKind != 0 && (MatchKind & DeclarationKinds.Parameter) == 0)
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckField (ICS.FixedFieldDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Field) == 0))
				return false;
			
			if (!CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			foreach (var v in node.Variables) {
				string name = v.Name;
				if (IsValid (name))
					continue;
				data.Add (GetFixableResult (v.NameToken.StartLocation, null, name));
			}
			
			return true;
		}
		
		public bool CheckField (ICS.FieldDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Field) == 0))
				return false;
			
			if (!CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			foreach (var v in node.Variables) {
				string name = v.Name;
				if (IsValid (name))
					continue;
				data.Add (GetFixableResult (v.NameToken.StartLocation, null, name));
			}
			
			return true;
		}
		
		public bool CheckEvent (ICS.EventDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Event) == 0) || !CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			foreach (var v in node.Variables) {
				string name = v.Name;
				if (IsValid (name))
					continue;
				data.Add (GetFixableResult (v.NameToken.StartLocation, null, name));
			}
			
			return true;
		}
		
		public bool CheckEvent (ICS.CustomEventDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Event) == 0) || !CheckAttributedNode (node, ICS.Modifiers.Private))
				return false;
			
			var name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckType (ICS.TypeDeclaration node, InspectionData data)
		{
			if (MatchKind != 0) {
				switch (MatchKind) {
				case DeclarationKinds.Class:
					if (node.ClassType != ICSharpCode.NRefactory.CSharp.ClassType.Class)
						return false;
					break;
				case DeclarationKinds.Enum:
					if (node.ClassType != ICSharpCode.NRefactory.CSharp.ClassType.Enum)
						return false;
					break;
				case DeclarationKinds.Struct:
					if (node.ClassType != ICSharpCode.NRefactory.CSharp.ClassType.Struct)
						return false;
					break;
				case DeclarationKinds.Interface:
					if (node.ClassType != ICSharpCode.NRefactory.CSharp.ClassType.Interface)
						return false;
					break;
//				case DeclarationKinds.Delegate:
//					if (node.ClassType != ICSharpCode.NRefactory.CSharp.ClassType.Delegate)
//						return false;
//					break;
				default:
					return false;
				}
			}
			
			if (!CheckAttributedNode (node, ICS.Modifiers.Internal))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckDelegate (ICS.DelegateDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Delegate) == 0) || !CheckAttributedNode (node, ICS.Modifiers.Internal))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckTypeParameter (ICS.TypeParameterDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.TypeParameter) == 0))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckEnumMember (ICS.EnumMemberDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.EnumMember) == 0))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.NameToken.StartLocation, null, name));
			return true;
		}
		
		public bool CheckNamespace (ICS.NamespaceDeclaration node, InspectionData data)
		{
			if ((MatchKind != 0 && (MatchKind & DeclarationKinds.Namespace) == 0))
				return false;
			
			string name = node.Name;
			if (IsValid (name))
				return true;
			
			data.Add (GetFixableResult (node.Identifiers.First ().StartLocation, null, name));
			return true;
		}

		bool NoUnderscoreWithoutNumber (string id)
		{
			int idx = id.IndexOf ('_');
			while (idx >= 0 && idx < id.Length) {
				if ((idx + 2 >= id.Length || !char.IsDigit (id[idx+1])) && (idx == 0 || !char.IsDigit (id[idx-1])))
					return false;
				idx = id.IndexOf ('_', idx + 1);
			}
			return true;
		}
		
		public string GetPreview ()
		{
			var result = new StringBuilder ();
			if (RequiredPrefixes != null && RequiredPrefixes.Length > 0)
				result.Append (RequiredPrefixes[0]);
			switch (NamingStyle) {
			case NamingStyle.PascalCase:
				result.Append ("PascalCase");
				break;
			case NamingStyle.CamelCase:
				result.Append ("camelCase");
				break;
			case NamingStyle.AllUpper:
				result.Append ("ALL_UPPER");
				break;
			case NamingStyle.AllLower:
				result.Append ("all_lower");
				break;
			case NamingStyle.FirstUpper:
				result.Append ("First_upper");
				break;
			}
			if (RequiredSuffixes != null && RequiredSuffixes.Length > 0)
				result.Append (RequiredSuffixes[0]);
			return result.ToString ();
		}
		
		public bool IsValid (string name)
		{
			string id = name;
			if (RequiredPrefixes != null && RequiredPrefixes.Length > 0) {
				var prefix = RequiredPrefixes.FirstOrDefault (p => id.StartsWith (p));
				if (prefix == null)
					return false;
				id = id.Substring (prefix.Length);
			}
			else if (ForbiddenPrefixes != null && ForbiddenPrefixes.Length > 0) {
				if (ForbiddenPrefixes.Any (p => id.StartsWith (p)))
					return false;
			}
			
			if (RequiredSuffixes != null && RequiredSuffixes.Length > 0) {
				var suffix = RequiredSuffixes.FirstOrDefault (s => id.EndsWith (s));
				if (suffix == null)
					return false;
				id = id.Substring (0, id.Length - suffix.Length);
			}
			else if (ForbiddenSuffixes != null && ForbiddenSuffixes.Length > 0) {
				if (ForbiddenSuffixes.Any (p => id.EndsWith (p)))
					return false;
			}
			
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				return !id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			case NamingStyle.AllUpper:
				return !id.Any (ch => char.IsLetter (ch) && char.IsLower (ch));
			case NamingStyle.CamelCase:
				return id.Length == 0 || (char.IsLower (id [0]) && NoUnderscoreWithoutNumber (id));
			case NamingStyle.PascalCase:
				return id.Length == 0 || (char.IsUpper (id [0]) && NoUnderscoreWithoutNumber (id));
			case NamingStyle.FirstUpper:
				return id.Length == 0 && char.IsUpper (id [0]) && !id.Skip (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch));
			}
			return true;
		}

		public string GetErrorMessage (string name, out IList<string> suggestedNames)
		{
			suggestedNames = new List<string> ();
			string id = name;
			
			string errorMessage = null;
			
			bool missingRequiredPrefix = false;
			bool missingRequiredSuffix = false;
			string prefix = null;
			string suffix = null;
			
			if (RequiredPrefixes != null && RequiredPrefixes.Length > 0) {
				prefix = RequiredPrefixes.FirstOrDefault (p => id.StartsWith (p));
				if (prefix == null) {
					errorMessage = GettextCatalog.GetString ("Name should have prefix '{0}'.", RequiredPrefixes[0]);
					missingRequiredPrefix = true;
				} else {
					id = id.Substring (prefix.Length);
				}
			}
			else if (ForbiddenPrefixes != null && ForbiddenPrefixes.Length > 0) {
				prefix = ForbiddenPrefixes.FirstOrDefault (p => id.StartsWith (p));
				if (prefix != null) {
					errorMessage = GettextCatalog.GetString ("Name has forbidden prefix '{0}'.", prefix);
					id = id.Substring (prefix.Length);
				}
			}
			
			if (RequiredSuffixes != null && RequiredSuffixes.Length > 0) {
				suffix = RequiredSuffixes.FirstOrDefault (s => id.EndsWith (s));
				if (suffix == null) {
					errorMessage = GettextCatalog.GetString ("Name should have suffix '{0}'.", RequiredSuffixes[0]);
					missingRequiredSuffix = true;
				} else {
					id = id.Substring (0, id.Length - suffix.Length);
				}
			}
			else if (ForbiddenSuffixes != null && ForbiddenSuffixes.Length > 0) {
				suffix = ForbiddenSuffixes.FirstOrDefault (p => id.EndsWith (p));
				if (suffix != null) {
					errorMessage = GettextCatalog.GetString ("Name has forbidden suffix '{0}'.", suffix);
					id = id.Substring (0, id.Length - suffix.Length);
				}
			}
			
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				if (id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch))) {
					errorMessage = GettextCatalog.GetString ("'{0}' contains upper case letters.", name);
					suggestedNames.Add (LowerCaseIdentifier (BreakWords (id)));
				} else {
					suggestedNames.Add (id);
				}
				break;
			case NamingStyle.AllUpper:
				if (id.Any (ch => char.IsLetter (ch) && char.IsLower (ch))) {
					errorMessage = GettextCatalog.GetString ("'{0}' contains lower case letters.", name);
					suggestedNames.Add (UpperCaseIdentifier (BreakWords (id)));
				} else {
					suggestedNames.Add (id);
				}
				break;
			case NamingStyle.CamelCase:
				if (id.Length > 0 && char.IsUpper (id [0])) {
					errorMessage = GettextCatalog.GetString ("'{0}' should start with a lower case letter.", name);
				} else if (!NoUnderscoreWithoutNumber (id)) {
					errorMessage = GettextCatalog.GetString ("'{0}' should not separate words with an underscore.", name);
				} else {
					suggestedNames.Add (id);
					break;
				}
				suggestedNames.Add (CamelCaseIdentifier (BreakWords (id)));
				break;
			case NamingStyle.PascalCase:
				if (id.Length > 0 && char.IsLower (id [0])) {
					errorMessage = GettextCatalog.GetString ("'{0}' should start with an upper case letter.", name);
				} else if (!NoUnderscoreWithoutNumber (id)) {
					errorMessage = GettextCatalog.GetString ("'{0}' should not separate words with an underscore.", name);
				} else {
					suggestedNames.Add (id);
					break;
				}
				suggestedNames.Add (PascalCaseIdentifier (BreakWords (id)));
				break;
			case NamingStyle.FirstUpper:
				if (id.Length > 0 && char.IsLower (id [0])) {
					errorMessage = GettextCatalog.GetString ("'{0}' should start with an upper case letter.", name);
				} else if (id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch))) {
					errorMessage = GettextCatalog.GetString ("'{0}' contains an upper case letter after the first.", name);
				} else  {
					suggestedNames.Add (id);
					break;
				}
				suggestedNames.Add (FirstUpperIdentifier (BreakWords (id)));
				break;
			}
			
			if (prefix != null) {
				for (int i = 0; i < suggestedNames.Count; i++) {
					suggestedNames[i] = prefix + suggestedNames[i];
				}
			} else if (missingRequiredPrefix) {
				for (int i = 0; i < suggestedNames.Count; i++) {
					var n = suggestedNames[i];
					bool first = true;
					foreach (var p in RequiredPrefixes) {
						if (first) {
							first = false;
							suggestedNames[i] = p + n;
						} else {
							suggestedNames.Add (p + n);
						}
					}
				}
			}
			
			if (suffix != null) {
				for (int i = 0; i < suggestedNames.Count; i++) {
					suggestedNames[i] = suggestedNames[i] + suffix;
				}
			} else if (missingRequiredSuffix) {
				for (int i = 0; i < suggestedNames.Count; i++) {
					var n = suggestedNames[i];
					bool first = true;
					foreach (var s in RequiredSuffixes) {
						if (first) {
							first = false;
							suggestedNames[i] = n + s;
						} else {
							suggestedNames.Add (n + s);
						}
					}
				}
			}
			
			return errorMessage
				// should never happen.
				?? "no known errors.";
		}
		
		static List<string> BreakWords (string identifier)
		{
			var words = new List<string> ();
			int wordStart = 0;
			bool lastWasLower = false, lastWasUpper = false;
			for (int i = 0; i < identifier.Length; i++) {
				char c = identifier[i];
				if (c == '_') {
					if ((i - wordStart) > 0) {
						words.Add (identifier.Substring (wordStart, i - wordStart));
					}
					wordStart = i + 1;
					lastWasLower = lastWasUpper = false;
				} else if (Char.IsLower (c)) {
					if (lastWasUpper && (i - wordStart) > 2) {
						words.Add (identifier.Substring (wordStart, i - wordStart - 1));
						wordStart = i - 1;
					}
					lastWasLower = true;
					lastWasUpper = false;
				} else if (Char.IsUpper (c)) {
					if (lastWasLower) {
						words.Add (identifier.Substring (wordStart, i - wordStart));
						wordStart = i;
					}
					lastWasLower = false;
					lastWasUpper = true;
				}
			}
			if (wordStart < identifier.Length)
				words.Add (identifier.Substring (wordStart));
			return words;
		}
		
		static string CamelCaseIdentifier (List<string> words)
		{
			var sb = new StringBuilder ();
			sb.Append (words[0].ToLower ());
			for (int i = 1; i < words.Count; i++) {
				if (sb.Length > 0 && (char.IsDigit (sb[sb.Length-1]) || char.IsDigit (words[i][0])))
					sb.Append ('_');
				AppendCapitalized (words[i], sb);
			}
			return sb.ToString ();
		}
		
		static string PascalCaseIdentifier (List<string> words)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < words.Count; i++) {
				if (sb.Length > 0 && (char.IsDigit (sb[sb.Length-1]) || char.IsDigit (words[i][0])))
					sb.Append ('_');
				AppendCapitalized (words[i], sb);
			}
			return sb.ToString ();
		}
		
		static string LowerCaseIdentifier (List<string> words)
		{
			var sb = new StringBuilder ();
			sb.Append (words[0].ToLower ());
			for (int i = 1; i < words.Count; i++) {
				sb.Append ('_');
				sb.Append (words[i].ToLower ());
			}
			return sb.ToString ();
		}
		
		static string UpperCaseIdentifier (List<string> words)
		{
			var sb = new StringBuilder ();
			sb.Append (words[0].ToUpper ());
			for (int i = 1; i < words.Count; i++) {
				sb.Append ('_');
				sb.Append (words[i].ToUpper ());
			}
			return sb.ToString ();
		}
		
		static string FirstUpperIdentifier (List<string> words)
		{
			var sb = new StringBuilder ();
			AppendCapitalized (words[0], sb);
			for (int i = 1; i < words.Count; i++) {
				sb.Append ('_');
				sb.Append (words[i].ToLower ());
			}
			return sb.ToString ();
		}
		
		static void AppendCapitalized (string word, StringBuilder sb)
		{
			sb.Append (word.ToLower ());
			sb[sb.Length - word.Length] = char.ToUpper (sb[sb.Length - word.Length]);
		}
		
		public FixableResult GetFixableResult (ICS.AstLocation location, IBaseMember node, string name)
		{
			IList<string> suggestedFixes;
			var error = GetErrorMessage (name, out suggestedFixes);
			
			var fixes = new List<RenameMemberFix> ();
			fixes.Add (new RenameMemberFix (node, name, null));
			if (suggestedFixes != null)
				fixes.AddRange (suggestedFixes.Select (f => new RenameMemberFix (node, name, f)));
			
			return new FixableResult (
				new DomRegion (location.Line, location.Column, location.Line, location.Column + name.Length),
				error,
				MonoDevelop.SourceEditor.QuickTaskSeverity.Warning,
				ResultCertainty.High, ResultImportance.Medium,
				fixes.ToArray<IAnalysisFix> ());
		}
	}
}