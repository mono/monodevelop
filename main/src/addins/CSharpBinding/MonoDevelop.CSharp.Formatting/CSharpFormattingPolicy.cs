// 
// CSharpFormattingPolicy.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//  
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Reflection;
using System.Xml;
using System.Text;
using System.Linq;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.Formatting
{
	[PolicyType ("C# formatting (roslyn)")]
	public class CSharpFormattingPolicy : IEquatable<CSharpFormattingPolicy>
	{
		OptionSet options;
		
		public string Name {
			get;
			set;
		}
		
		public bool IsBuiltIn {
			get;
			set;
		}
		
		public CSharpFormattingPolicy Clone ()
		{
			return new CSharpFormattingPolicy (options);
		}

		public static OptionSet Apply (OptionSet options, TextStylePolicy policy)
		{
			var result = options;
			if (policy != null) {
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentationSize, LanguageNames.CSharp, policy.IndentWidth);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.NewLine, LanguageNames.CSharp, policy.GetEolMarker ());
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.SmartIndent, LanguageNames.CSharp, FormattingOptions.IndentStyle.Smart);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.TabSize, LanguageNames.CSharp, policy.TabWidth);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, !policy.TabsToSpaces);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.UseTabOnlyForIndentation, LanguageNames.CSharp, !policy.TabsToSpaces);
			}
			return result;
		}

		public static OptionSet Apply (OptionSet options, ITextEditorOptions policy)
		{
			var result = options;
			if (policy != null) {
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentationSize, LanguageNames.CSharp, policy.IndentationSize);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.NewLine, LanguageNames.CSharp, policy.DefaultEolMarker);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.SmartIndent, LanguageNames.CSharp, FormattingOptions.IndentStyle.Smart);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.TabSize, LanguageNames.CSharp, policy.TabSize);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, !policy.TabsToSpaces);
				result = result.WithChangedOption (Microsoft.CodeAnalysis.Formatting.FormattingOptions.UseTabOnlyForIndentation, LanguageNames.CSharp, !policy.TabsToSpaces);
			}
			return result;
		}

		public OptionSet CreateOptions (TextStylePolicy policy)
		{
			return Apply (options, policy);
		}

		public OptionSet CreateOptions (ITextEditorOptions policy)
		{
			return Apply (options, policy);
		}

		static CSharpFormattingPolicy ()
		{
			if (!PolicyService.InvariantPolicies.ReadOnly)
				 PolicyService.InvariantPolicies.Set<CSharpFormattingPolicy> (new CSharpFormattingPolicy (), "text/x-csharp");
		}
		
		protected CSharpFormattingPolicy (OptionSet options)
		{
			this.options = options;
		}

		#region Indent options
		public bool IndentBlock {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentBlock);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentBlock, value);
			}
		}

		public bool IndentBraces {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentBraces);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentBraces, value);
			}
		}

		public bool IndentSwitchSection {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchSection);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchSection, value);
			}
		}

		public bool IndentSwitchCaseSection {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchCaseSection);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchCaseSection, value);
			}
		}

		public Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions LabelPositioning {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.LabelPositioning);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.LabelPositioning, value);
			}
		}
		#endregion

		#region New line options

		public bool NewLinesForBracesInTypes {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInTypes);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInTypes, value);
			}
		}

		public bool NewLinesForBracesInMethods {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInMethods);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInMethods, value);
			}
		}

		public bool NewLinesForBracesInAnonymousMethods {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, value);
			}
		}
			
		public bool NewLinesForBracesInControlBlocks {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInControlBlocks);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInControlBlocks, value);
			}
		}

		public bool NewLinesForBracesInAnonymousTypes {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, value);
			}
		}

		public bool NewLinesForBracesInObjectInitializers {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInObjectInitializers);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInObjectInitializers, value);
			}
		}

		public bool NewLinesForBracesInLambdaExpressionBody {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, value);
			}
		}

		public bool NewLineForElse {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForElse);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForElse, value);
			}
		}
			
		public bool NewLineForCatch {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForCatch);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForCatch, value);
			}
		}

		public bool NewLineForFinally {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForFinally);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForFinally, value);
			}
		}

		public bool NewLineForMembersInObjectInit {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInObjectInit);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInObjectInit, value);
			}
		}

		public bool NewLineForMembersInAnonymousTypes {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInAnonymousTypes);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, value);
			}
		}

		public bool NewLineForClausesInQuery {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForClausesInQuery);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForClausesInQuery, value);
			}
		}
		#endregion

		#region Spacing options

		public bool SpacingAfterMethodDeclarationName {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacingAfterMethodDeclarationName);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacingAfterMethodDeclarationName, value);
			}
		}

		public bool SpaceWithinMethodDeclarationParenthesis {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, value);
			}
		}

		public bool SpaceBetweenEmptyMethodDeclarationParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, value);
			}
		}

		public bool SpaceAfterMethodCallName {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterMethodCallName);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterMethodCallName, value);
			}
		}

		public bool SpaceWithinMethodCallParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinMethodCallParentheses, value);
			}
		}

		public bool SpaceBetweenEmptyMethodCallParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, value);
			}
		}

		public bool SpaceAfterControlFlowStatementKeyword {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, value);
			}
		}

		public bool SpaceWithinExpressionParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinExpressionParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinExpressionParentheses, value);
			}
		}

		public bool SpaceWithinCastParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinCastParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinCastParentheses, value);
			}
		}

		public bool SpaceWithinOtherParentheses {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinOtherParentheses);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinOtherParentheses, value);
			}
		}

		public bool SpaceAfterCast {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterCast);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterCast, value);
			}
		}

		public bool SpacesIgnoreAroundVariableDeclaration {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, value);
			}
		}

		public bool SpaceBeforeOpenSquareBracket {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeOpenSquareBracket);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, value);
			}
		}

		public bool SpaceBetweenEmptySquareBrackets {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, value);
			}
		}

		public bool SpaceWithinSquareBrackets {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinSquareBrackets);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceWithinSquareBrackets, value);
			}
		}

		public bool SpaceAfterColonInBaseTypeDeclaration {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, value);
			}
		}

		public bool SpaceAfterComma {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterComma);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterComma, value);
			}
		}

		public bool SpaceAfterDot {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterDot);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterDot, value);
			}
		}

		public bool SpaceAfterSemicolonsInForStatement {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, value);
			}
		}

		public bool SpaceBeforeColonInBaseTypeDeclaration {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, value);
			}
		}

		public bool SpaceBeforeComma {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeComma);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeComma, value);
			}
		}

		public bool SpaceBeforeDot {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeDot);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeDot, value);
			}
		}

		public bool SpaceBeforeSemicolonsInForStatement {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, value);
			}
		}

		public Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions SpacingAroundBinaryOperator {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacingAroundBinaryOperator);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacingAroundBinaryOperator, value);
			}
		}
		#endregion

		#region Wrapping options

		public bool WrappingPreserveSingleLine {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.WrappingPreserveSingleLine);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.WrappingPreserveSingleLine, value);
			}
		}

		public bool WrappingKeepStatementsOnSingleLine {
			get {
				return options.GetOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine);
			}
			set {
				options = options.WithChangedOption (Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, value);
			}
		}

		#endregion

		public CSharpFormattingPolicy ()
		{
			this.options = RoslynTypeSystemService.Workspace.Options;
		}
		
		public static CSharpFormattingPolicy Load (FilePath selectedFile)
		{
			using (var stream = System.IO.File.OpenRead (selectedFile)) {
				return Load (stream);
			}
		}
		
		public static CSharpFormattingPolicy Load (System.IO.Stream input)
		{
			var result = new CSharpFormattingPolicy ();
			result.Name = "noname";
			using (var reader = new XmlTextReader (input)) {
				while (reader.Read ()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "Property") {
							var info = typeof (CSharpFormattingPolicy).GetProperty (reader.GetAttribute ("name"));
							string valString = reader.GetAttribute ("value");
							object value;
							if (info.PropertyType == typeof (bool)) {
								value = Boolean.Parse (valString);
							} else if (info.PropertyType == typeof (int)) {
								value = Int32.Parse (valString);
							} else {
								value = Enum.Parse (info.PropertyType, valString);
							}
							info.SetValue (result, value, null);
						} else if (reader.LocalName == "FormattingProfile") {
							result.Name = reader.GetAttribute ("name");
						}
					} else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FormattingProfile") {
						//Console.WriteLine ("result:" + result.Name);
						return result;
					}
				}
			}
			return result;
		}
		
		public void Save (string fileName)
		{
			using (var writer = new XmlTextWriter (fileName, Encoding.Default)) {
				writer.Formatting = System.Xml.Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				writer.WriteStartElement ("FormattingProfile");
				writer.WriteAttributeString ("name", Name);
				foreach (PropertyInfo info in typeof (CSharpFormattingPolicy).GetProperties ()) {
					if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
						writer.WriteStartElement ("Property");
						writer.WriteAttributeString ("name", info.Name);
						writer.WriteAttributeString ("value", info.GetValue (this, null).ToString ());
						writer.WriteEndElement ();
					}
				}
				writer.WriteEndElement ();
			}
		}
		
		public bool Equals (CSharpFormattingPolicy other)
		{
			foreach (PropertyInfo info in typeof (CSharpFormattingPolicy).GetProperties ()) {
				if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
					object val = info.GetValue (this, null);
					object otherVal = info.GetValue (other, null);
					if (val == null) {
						if (otherVal == null)
							continue;
						return false;
					}
					if (!val.Equals (otherVal)) {
						//Console.WriteLine ("!equal");
						return false;
					}
				}
			}
			//Console.WriteLine ("== equal");
			return true;
		}
	}
}
