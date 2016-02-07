// 
// NamingRule.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Serialization;
using RefactoringEssentials.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.Diagnostics.InconsistentNaming
{
	[DataItem ("NamingRule")]
	sealed class NameConventionRule
	{
		NamingRule wrappedRule = new NamingRule (AffectedEntity.None);

		[ItemProperty]
		public string Name {
			get { return wrappedRule.Name; } 
			set { wrappedRule.Name = value;} 
		}
		
		[ItemProperty]
		public string[] RequiredPrefixes {
			get { return wrappedRule.RequiredPrefixes; } 
			set { wrappedRule.RequiredPrefixes = value;} 
		}
		
		[ItemProperty]
		public string[] AllowedPrefixes {
			get { return wrappedRule.AllowedPrefixes; } 
			set { wrappedRule.AllowedPrefixes = value;} 
		}
		
		[ItemProperty]
		public string[] RequiredSuffixes {
			get { return wrappedRule.RequiredSuffixes; } 
			set { wrappedRule.RequiredSuffixes = value;} 
		}

		[ItemProperty]
		public string[] ForbiddenPrefixes {
			get { return wrappedRule.ForbiddenPrefixes; } 
			set { wrappedRule.ForbiddenPrefixes = value;} 
		}

		[ItemProperty]
		public string[] ForbiddenSuffixes {
			get { return wrappedRule.ForbiddenSuffixes; } 
			set { wrappedRule.ForbiddenSuffixes = value;} 
		}

		[ItemProperty]
		public AffectedEntity AffectedEntity {
			get { return wrappedRule.AffectedEntity; } 
			set { wrappedRule.AffectedEntity = value;} 
		}

		[ItemProperty]
		public Modifiers VisibilityMask {
			get { return wrappedRule.VisibilityMask; } 
			set { wrappedRule.VisibilityMask = value;} 
		}

		[ItemProperty]
		public NamingStyle NamingStyle {
			get { return wrappedRule.NamingStyle; } 
			set { wrappedRule.NamingStyle = value;} 
		}

		[ItemProperty]
		public bool IncludeInstanceMembers {
			get { return wrappedRule.IncludeInstanceMembers; } 
			set { wrappedRule.IncludeInstanceMembers = value;} 
		}

		[ItemProperty]
		public bool IncludeStaticEntities {
			get { return wrappedRule.IncludeStaticEntities; } 
			set { wrappedRule.IncludeStaticEntities = value;} 
		}

		internal NameConventionRule (NamingRule wrappedRule)
		{
			this.wrappedRule = wrappedRule;
		}
		
		public NameConventionRule ()
		{
		}

		public NameConventionRule Clone ()
		{
			return new NameConventionRule () {
				wrappedRule = this.wrappedRule.Clone ()
			};
		}

		public string GetPreview ()
		{
			return wrappedRule.GetPreview ();
		}

		internal NamingRule GetNRefactoryRule ()
		{
			return wrappedRule;
		}

		public string CorrectName (string name)
		{
			string prefix = null, suffix = null;
			string realName = name;
			string id = name;

			// check prefix
			if (AllowedPrefixes != null && AllowedPrefixes.Length > 0) {
				var allowedPrefix = AllowedPrefixes.FirstOrDefault (p => id.StartsWith (p, StringComparison.Ordinal));
				if (allowedPrefix != null) {
					prefix = allowedPrefix;
					id = id.Substring (allowedPrefix.Length);
				}
			}

			if (prefix == null && RequiredPrefixes != null && RequiredPrefixes.Length > 0) {
				var requiredPrefix = RequiredPrefixes.FirstOrDefault (p => id.StartsWith (p, StringComparison.Ordinal));
				if (requiredPrefix == null) {
					prefix = RequiredPrefixes[0];
				} else {
					prefix = requiredPrefix;
					id = id.Substring (requiredPrefix.Length);
				}
			}

			if (prefix == null && ForbiddenPrefixes != null && ForbiddenPrefixes.Length > 0) {
				var forbiddenPrefix = ForbiddenPrefixes.FirstOrDefault (p => id.StartsWith (p, StringComparison.Ordinal));
				if (forbiddenPrefix != null) {
					id = id.Substring (forbiddenPrefix.Length);
				}
			}

			// check suffix
			if (RequiredSuffixes != null && RequiredSuffixes.Length > 0) {
				var requiredSuffix = RequiredSuffixes.FirstOrDefault (s => id.EndsWith (s, StringComparison.Ordinal));
				if (requiredSuffix == null) {
					suffix = RequiredSuffixes[0];
				} else {
					suffix = requiredSuffix;
					id = id.Substring (0, id.Length - requiredSuffix.Length);
				}
			} 

			if (suffix == null && ForbiddenSuffixes != null && ForbiddenSuffixes.Length > 0) {
				var forbiddenSuffix = ForbiddenSuffixes.FirstOrDefault (p => id.EndsWith (p, StringComparison.Ordinal));
				if (forbiddenSuffix != null) {
					id = id.Substring (0, id.Length - forbiddenSuffix.Length);
				}
			}
			Console.WriteLine ("style: " + NamingStyle);
			switch (NamingStyle) {
			case NamingStyle.AllLower:
				if (id.Any (ch => char.IsLetter (ch) && char.IsUpper (ch))) {
					realName = LowerCaseIdentifier (WordParser.BreakWords (id));
				} else {
					realName = id;
				}
				break;
			case NamingStyle.AllUpper:
				if (id.Any (ch => char.IsLetter (ch) && char.IsLower (ch))) {
					realName = UpperCaseIdentifier (WordParser.BreakWords (id));
				} else {
					realName = id;
				}
				break;

			case NamingStyle.CamelCase:
				if (id.Length > 0 && !char.IsLower (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.Forbid)) {
				} else {
					realName = id;
					break;
				}
				realName = CamelCaseIdentifier (id);
				break;
			case NamingStyle.CamelCaseWithLowerLetterUnderscore:
				if (id.Length > 0 && !char.IsLower (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.AllowWithLowerStartingLetter)) {
				} else {
					realName = id;
					break;
				}
				realName = CamelCaseWithLowerLetterUnderscore (id);
				break;
			case NamingStyle.CamelCaseWithUpperLetterUnderscore:
				if (id.Length > 0 && !char.IsLower (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.AllowWithUpperStartingLetter)) {
				} else {
					realName = id;
					break;
				}
				realName = CamelCaseWithUpperLetterUnderscore (id);
				break;

			case NamingStyle.PascalCase:
				if (id.Length > 0 && !char.IsUpper (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.Forbid)) {
				} else {
					realName = id;
					break;
				}
				realName = PascalCaseIdentifier (id);
				break;
			case NamingStyle.PascalCaseWithLowerLetterUnderscore:
				if (id.Length > 0 && !char.IsUpper (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.AllowWithLowerStartingLetter)) {
				} else {
					realName = id;
					break;
				}
				realName = PascalCaseWithLowerLetterUnderscore (id);
				break;
			case NamingStyle.PascalCaseWithUpperLetterUnderscore:
				if (id.Length > 0 && !char.IsUpper (id [0])) {
				} else if (!CheckUnderscore (id, UnderscoreHandling.AllowWithUpperStartingLetter)) {
				} else {
					realName = id;
					break;
				}
				realName = PascalCaseWithUpperLetterUnderscore (id);
				break;
			case NamingStyle.FirstUpper:
				if (id.Length > 0 && !char.IsUpper (id [0])) {
				} else if (id.Take (1).Any (ch => char.IsLetter (ch) && char.IsUpper (ch))) {
				} else {
					realName = id;
					break;
				}
				realName = FirstUpperIdentifier (WordParser.BreakWords (id));
				break;
			}

			return prefix + realName + suffix;
		}

		static string ConvertToValidName(string id, Func<char, char> firstCharFunc, Func<char, char> followingCharFunc)
		{
			var sb = new StringBuilder();
			bool first = true;
			for (int i = 0; i < id.Length; i++) {
				char ch = id[i];
				if (i == 0 && ch == '_')
					continue;
				if (first && char.IsLetter(ch)) {
					sb.Append(firstCharFunc(ch));
					firstCharFunc = followingCharFunc;
					first = false;
					continue;
				}
				if (ch == '_') {
					if (first)
						continue;
					if (i + 1 < id.Length && id[i + 1] == '_')
						continue;

					if (i + 1 < id.Length) {
						if (char.IsDigit(id[i + 1])) {
							sb.Append('_');
						} else {
							first = true;
						}
					}
					continue;
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}

		static string ConvertToValidNameWithSpecialUnderscoreHandling(string id, Func<char, char> firstCharFunc, Func<char, char> afterUnderscoreLetter)
		{
			var sb = new StringBuilder();
			bool first = true;
			for (int i = 0; i < id.Length; i++) {
				char ch = id[i];
				if (first && char.IsLetter(ch)) {
					sb.Append(firstCharFunc(ch));
					first = false;
					continue;
				}
				if (ch == '_') {
					if (first)
						continue;
					if (i + 1 < id.Length && id[i + 1] == '_')
						continue;
					sb.Append('_');
					i++;
					if (i < id.Length)
						sb.Append(afterUnderscoreLetter (id[i]));
					continue;
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}

		static string CamelCaseIdentifier(string id)
		{
			return ConvertToValidName(id, ch => char.ToLower(ch), ch => char.ToUpper (ch));
		}

		static string CamelCaseWithLowerLetterUnderscore(string id)
		{
			return ConvertToValidNameWithSpecialUnderscoreHandling(id, ch => char.ToLower(ch), ch => char.ToLower(ch));
		}

		static string CamelCaseWithUpperLetterUnderscore(string id)
		{
			return ConvertToValidNameWithSpecialUnderscoreHandling(id, ch => char.ToLower(ch), ch => char.ToUpper(ch));
		}

		static string PascalCaseIdentifier(string id)
		{
			return ConvertToValidName(id, ch => char.ToUpper(ch), ch => char.ToUpper (ch));
		}

		static string PascalCaseWithLowerLetterUnderscore(string id)
		{
			return ConvertToValidNameWithSpecialUnderscoreHandling(id, ch => char.ToUpper(ch), ch => char.ToLower (ch));
		}

		static string PascalCaseWithUpperLetterUnderscore(string id)
		{
			return ConvertToValidNameWithSpecialUnderscoreHandling(id, ch => char.ToUpper(ch), ch => char.ToUpper(ch));
		}

		static string LowerCaseIdentifier(List<string> words)
		{
			var sb = new StringBuilder();
			sb.Append(words [0].ToLower());
			for (int i = 1; i < words.Count; i++) {
				sb.Append('_');
				sb.Append(words [i].ToLower());
			}
			return sb.ToString();
		}

		static string UpperCaseIdentifier(List<string> words)
		{
			var sb = new StringBuilder();
			sb.Append(words [0].ToUpper());
			for (int i = 1; i < words.Count; i++) {
				sb.Append('_');
				sb.Append(words [i].ToUpper());
			}
			return sb.ToString();
		}

		static string FirstUpperIdentifier(List<string> words)
		{
			var sb = new StringBuilder();
			AppendCapitalized(words [0], sb);
			for (int i = 1; i < words.Count; i++) {
				sb.Append('_');
				sb.Append(words [i].ToLower());
			}
			return sb.ToString();
		}

		static void AppendCapitalized(string word, StringBuilder sb)
		{
			sb.Append(word.ToLower());
			sb [sb.Length - word.Length] = char.ToUpper(sb [sb.Length - word.Length]);
		}

		static bool CheckUnderscore(string id, UnderscoreHandling handling)
		{
			for (int i = 1; i < id.Length; i++) {
				char ch = id [i];
				if (ch == '_' && !HandleUnderscore(handling, id, ref i))
					return false;
			}
			return true;
		}

		enum UnderscoreHandling {
			Forbid,
			Allow,
			AllowWithLowerStartingLetter,
			AllowWithUpperStartingLetter
		}

		static bool HandleUnderscore(UnderscoreHandling handling, string id, ref int i)
		{
			switch (handling) {
				case UnderscoreHandling.Forbid:
				if (i + 1 < id.Length) {
					char ch = id [i + 1];
					if (char.IsDigit(ch)) {
						i++;
						return true;
					}
				}
				return false;
				case UnderscoreHandling.Allow:
				return true;
				case UnderscoreHandling.AllowWithLowerStartingLetter:
				if (i + 1 < id.Length) {
					char ch = id [i + 1];
					if (char.IsLetter(ch) && !char.IsLower(ch) || ch =='_')
						return false;
					i++;
				}
				return true;
				case UnderscoreHandling.AllowWithUpperStartingLetter:
				if (i + 1 < id.Length) {
					char ch = id [i + 1];
					if (char.IsLetter(ch) && !char.IsUpper(ch) || ch =='_')
						return false;
					i++;
				}
				return true;
				default:
				throw new ArgumentOutOfRangeException();
			}
		}

		internal class NamePropsalStrategy : RefactoringEssentials.INameProposalStrategy
		{
			static readonly char[] s_underscoreCharArray = new[] { '_' };
			static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");

			string DefaultGetNameProposal(string baseName, SyntaxKind syntaxKindHint, Document document, int position)
			{
				switch (syntaxKindHint)
				{
					case SyntaxKind.ClassDeclaration:
					case SyntaxKind.StructDeclaration:
					case SyntaxKind.InterfaceDeclaration:
					case SyntaxKind.EnumDeclaration:
					case SyntaxKind.DelegateDeclaration:
					case SyntaxKind.MethodDeclaration:
					case SyntaxKind.PropertyDeclaration:
					case SyntaxKind.EventDeclaration:
					case SyntaxKind.EventFieldDeclaration:
					case SyntaxKind.EnumMemberDeclaration:

					// Trim leading underscores
					var newBaseName = baseName.TrimStart(s_underscoreCharArray);

					// Trim leading "m_"
					if (newBaseName.Length >= 2 && newBaseName[0] == 'm' && newBaseName[1] == '_')
					{
						newBaseName = newBaseName.Substring(2);
					}

					// Take original name if no characters left
					if (newBaseName.Length == 0)
					{
						newBaseName = baseName;
					}

					// Make the first character upper case using the "en-US" culture.  See discussion at
					// https://github.com/dotnet/roslyn/issues/5524.
					var firstCharacter = EnUSCultureInfo.TextInfo.ToUpper(newBaseName[0]);
					return firstCharacter.ToString() + newBaseName.Substring(1);

					case SyntaxKind.Parameter:
					case SyntaxKind.FieldDeclaration:
					case SyntaxKind.VariableDeclaration:
					case SyntaxKind.LocalDeclarationStatement:
					return char.ToLower(baseName[0]).ToString() + baseName.Substring(1);
				}
				return baseName;
			}

			public string GetNameProposal(string baseName, SyntaxKind syntaxKindHint, Accessibility accessibility, bool isStatic, Document document, int position)
			{
				var container = PolicyService.DefaultPolicies;
				var project = TypeSystemService.GetMonoProject (document.Id);
				if (project == null)
					project = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (project != null)
					container = project.Policies;
				var policy = container.Get<NameConventionPolicy> ();
				var entity = GetAffectedEntity (syntaxKindHint);

				var mod = Modifiers.None;
				switch (accessibility) {
				case Accessibility.Private:
					mod = Modifiers.Private;
					break;
				case Accessibility.ProtectedAndInternal:
					mod = Modifiers.Internal | Modifiers.Protected;
					break;
				case Accessibility.Protected:
					mod = Modifiers.Protected;
					break;
				case Accessibility.Internal:
					mod = Modifiers.Internal;
					break;
				case Accessibility.ProtectedOrInternal:
					mod = Modifiers.Internal | Modifiers.Protected;
					break;
				case Accessibility.Public:
					mod = Modifiers.Public;
					break;
				}

				foreach (var rule in policy.Rules) {
					if ((rule.AffectedEntity & entity) != entity)
						continue;
					if (isStatic && !rule.IncludeStaticEntities)
						continue;
					if ((rule.VisibilityMask & mod) != mod)
						continue;
					return rule.CorrectName (baseName);
				}

				return DefaultGetNameProposal (baseName, syntaxKindHint, document, position);
			}

			AffectedEntity GetAffectedEntity (SyntaxKind syntaxKindHint)
			{
				switch (syntaxKindHint) {
				case SyntaxKind.ClassDeclaration:
					return AffectedEntity.Class;
				case SyntaxKind.StructDeclaration:
					return AffectedEntity.Struct;
				case SyntaxKind.InterfaceDeclaration:
					return AffectedEntity.Interface;
				case SyntaxKind.EnumDeclaration:
					return AffectedEntity.Enum;
				case SyntaxKind.DelegateDeclaration:
					return AffectedEntity.Delegate;
				case SyntaxKind.MethodDeclaration:
					return AffectedEntity.Method;
				case SyntaxKind.PropertyDeclaration:
					return AffectedEntity.Property;
				case SyntaxKind.EventDeclaration:
					return AffectedEntity.Event;
				case SyntaxKind.EventFieldDeclaration:
					return AffectedEntity.Event;
				case SyntaxKind.EnumMemberDeclaration:
					return AffectedEntity.EnumMember;
				case SyntaxKind.Parameter:
					return AffectedEntity.Parameter;
				case SyntaxKind.FieldDeclaration:
					return AffectedEntity.Field;
				case SyntaxKind.VariableDeclaration:
					return AffectedEntity.LocalVariable;
				case SyntaxKind.LocalDeclarationStatement:
					return AffectedEntity.LocalVariable;
				}
				return AffectedEntity.None;
			}
		}
	}
}