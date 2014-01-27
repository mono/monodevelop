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

namespace MonoDevelop.CSharp.Formatting
{
	[PolicyType ("C# formatting")]
	public class CSharpFormattingPolicy : IEquatable<CSharpFormattingPolicy>
	{
		readonly CSharpFormattingOptions options = FormattingOptionsFactory.CreateMono ();
		
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
			return new CSharpFormattingPolicy (options.Clone ());
		}

		public CSharpFormattingOptions CreateOptions ()
		{
			return options;
		}
		
		static CSharpFormattingPolicy ()
		{
			if (!PolicyService.InvariantPolicies.ReadOnly)
				 PolicyService.InvariantPolicies.Set<CSharpFormattingPolicy> (new CSharpFormattingPolicy (), "text/x-csharp");
		}
		
		protected CSharpFormattingPolicy (CSharpFormattingOptions options)
		{
			this.options = options;
		}
		
		
		#region Indentation
		[ItemProperty]
		public bool IndentNamespaceBody {
			get {
				return options.IndentNamespaceBody;
			}
			set {
				options.IndentNamespaceBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentClassBody {
			get {
				return options.IndentClassBody;
			}
			set {
				options.IndentClassBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentInterfaceBody {
			get {
				return options.IndentInterfaceBody;
			}
			set {
				options.IndentInterfaceBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentStructBody {
			get {
				return options.IndentStructBody;
			}
			set {
				options.IndentStructBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentEnumBody {
			get {
				return options.IndentEnumBody;
			}
			set {
				options.IndentEnumBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentMethodBody {
			get {
				return options.IndentMethodBody;
			}
			set {
				options.IndentMethodBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentPropertyBody {
			get {
				return options.IndentPropertyBody;
			}
			set {
				options.IndentPropertyBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentEventBody {
			get {
				return options.IndentEventBody;
			}
			set {
				options.IndentEventBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentBlocks {
			get {
				return options.IndentBlocks;
			}
			set {
				options.IndentBlocks = value;
			}
		}
		
		[ItemProperty]
		public bool IndentSwitchBody {
			get {
				return options.IndentSwitchBody;
			}
			set {
				options.IndentSwitchBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentCaseBody {
			get {
				return options.IndentCaseBody;
			}
			set {
				options.IndentCaseBody = value;
			}
		}
		
		[ItemProperty]
		public bool IndentBreakStatements {
			get {
				return options.IndentBreakStatements;
			}
			set {
				options.IndentBreakStatements = value;
			}
		}
		
		[ItemProperty]
		public bool AlignEmbeddedUsingStatements {
			get {
				return options.AlignEmbeddedUsingStatements;
			}
			set {
				options.AlignEmbeddedUsingStatements = value;
			}
		}
		
		[ItemProperty]
		public bool AlignEmbeddedIfStatements {
			get {
				return options.AlignEmbeddedIfStatements;
			}
			set {
				options.AlignEmbeddedIfStatements = value;
			}
		}
		
		[ItemProperty]
		public PropertyFormatting SimplePropertyFormatting {
			get {
				return options.SimplePropertyFormatting;
			}
			set {
				options.SimplePropertyFormatting = value;
			}
		}

		[ItemProperty]
		public PropertyFormatting AutoPropertyFormatting {
			get {
				return options.AutoPropertyFormatting;
			}
			set {
				options.AutoPropertyFormatting = value;
			}
		}

		[ItemProperty]
		public bool IndentPreprocessorDirectives {
			get {
				return options.IndentPreprocessorDirectives;
			}
			set {
				options.IndentPreprocessorDirectives = value;
			}
		}
		#endregion
		
		#region Braces
		[ItemProperty]
		public BraceStyle NamespaceBraceStyle {
			get {
				return options.NamespaceBraceStyle;
			}
			set {
				options.NamespaceBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle ClassBraceStyle {
			get {
				return options.ClassBraceStyle;
			}
			set {
				options.ClassBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle InterfaceBraceStyle {
			get {
				return options.InterfaceBraceStyle;
			}
			set {
				options.InterfaceBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle StructBraceStyle {
			get {
				return options.StructBraceStyle;
			}
			set {
				options.StructBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle EnumBraceStyle {
			get {
				return options.EnumBraceStyle;
			}
			set {
				options.EnumBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle MethodBraceStyle {
			get {
				return options.MethodBraceStyle;
			}
			set {
				options.MethodBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle AnonymousMethodBraceStyle {
			get {
				return options.AnonymousMethodBraceStyle;
			}
			set {
				options.AnonymousMethodBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle ConstructorBraceStyle {
			get {
				return options.ConstructorBraceStyle;
			}
			set {
				options.ConstructorBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle DestructorBraceStyle {
			get {
				return options.DestructorBraceStyle;
			}
			set {
				options.DestructorBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle PropertyBraceStyle {
			get {
				return options.PropertyBraceStyle;
			}
			set {
				options.PropertyBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle PropertyGetBraceStyle {
			get {
				return options.PropertyGetBraceStyle;
			}
			set {
				options.PropertyGetBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle PropertySetBraceStyle {
			get {
				return options.PropertySetBraceStyle;
			}
			set {
				options.PropertySetBraceStyle = value;
			}
		}

		[ItemProperty]
		public PropertyFormatting SimpleGetBlockFormatting {
			get {
				return options.SimpleGetBlockFormatting;
			}
			set {
				options.SimpleGetBlockFormatting = value;
			}
		}

		[ItemProperty]
		public PropertyFormatting SimpleSetBlockFormatting {
			get {

				return options.SimpleSetBlockFormatting;
			}
			set {
				options.SimpleSetBlockFormatting = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle EventBraceStyle {
			get {
				return options.EventBraceStyle;
			}
			set {
				options.EventBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle EventAddBraceStyle {
			get {
				return options.EventAddBraceStyle;
			}
			set {
				options.EventAddBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle EventRemoveBraceStyle {
			get {
				return options.EventRemoveBraceStyle;
			}
			set {
				options.EventRemoveBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public bool AllowEventAddBlockInline {
			get {
				return options.AllowEventAddBlockInline;
			}
			set {
				options.AllowEventAddBlockInline = value;
			}
		}
		
		[ItemProperty]
		public bool AllowEventRemoveBlockInline {
			get {
				return options.AllowEventRemoveBlockInline;
			}
			set {
				options.AllowEventRemoveBlockInline = value;
			}
		}
		
		[ItemProperty]
		public BraceStyle StatementBraceStyle {
			get {
				return options.StatementBraceStyle;
			}
			set {
				options.StatementBraceStyle = value;
			}
		}
		
		[ItemProperty]
		public bool AllowIfBlockInline {
			get {
				return options.AllowIfBlockInline;
			}
			set {
				options.AllowIfBlockInline = value;
			}
		}
		
		#endregion

		#region NewLines
		[ItemProperty]
		public NewLinePlacement ElseNewLinePlacement {
			get {
				return options.ElseNewLinePlacement;
			}
			set {
				options.ElseNewLinePlacement = value;
			}
		}
		
		[ItemProperty]
		public NewLinePlacement ElseIfNewLinePlacement {
			get {
				return options.ElseIfNewLinePlacement;
			}
			set {
				options.ElseIfNewLinePlacement = value;
			}
		}
		
		[ItemProperty]
		public NewLinePlacement CatchNewLinePlacement {
			get {
				return options.CatchNewLinePlacement;
			}
			set {
				options.CatchNewLinePlacement = value;
			}
		}
		
		[ItemProperty]
		public NewLinePlacement FinallyNewLinePlacement {
			get {
				return options.FinallyNewLinePlacement;
			}
			set {
				options.FinallyNewLinePlacement = value;
			}
		}
		
		[ItemProperty]
		public NewLinePlacement WhileNewLinePlacement {
			get {
				return options.WhileNewLinePlacement;
			}
			set {
				options.WhileNewLinePlacement = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement EmbeddedStatementPlacement {
			get {
				return options.EmbeddedStatementPlacement;
			}
			set {
				options.EmbeddedStatementPlacement = value;
			}
		}


		
		[ItemProperty]
		public Wrapping ArrayInitializerWrapping {
			get {
				return options.ArrayInitializerWrapping;
			}
			set {
				options.ArrayInitializerWrapping = value;
			}
		}

	[ItemProperty]
		public BraceStyle ArrayInitializerBraceStyle {
			get {
				return options.ArrayInitializerBraceStyle;
			}
			set {
				options.ArrayInitializerBraceStyle = value;
			}
		}

		[ItemProperty]
		public bool KeepCommentsAtFirstColumn {
			get {
				return options.KeepCommentsAtFirstColumn;
			}
			set {
				options.KeepCommentsAtFirstColumn = value;
			}
		}

		#endregion
		
		#region Spaces
		// Methods
		[ItemProperty]
		public bool BeforeMethodDeclarationParentheses {
			get {
				return options.SpaceBeforeMethodDeclarationParentheses;
			}
			set {
				options.SpaceBeforeMethodDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BetweenEmptyMethodDeclarationParentheses {
			get {
				return options.SpaceBetweenEmptyMethodDeclarationParentheses;
			}
			set {
				options.SpaceBetweenEmptyMethodDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeMethodDeclarationParameterComma {
			get {
				return options.SpaceBeforeMethodDeclarationParameterComma;
			}
			set {
				options.SpaceBeforeMethodDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterMethodDeclarationParameterComma {
			get {
				return options.SpaceAfterMethodDeclarationParameterComma;
			}
			set {
				options.SpaceAfterMethodDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool WithinMethodDeclarationParentheses {
			get {
				return options.SpaceWithinMethodDeclarationParentheses;
			}
			set {
				options.SpaceWithinMethodDeclarationParentheses = value;
			}
		}
		
		// Method calls
		[ItemProperty]
		public bool BeforeMethodCallParentheses {
			get {
				return options.SpaceBeforeMethodCallParentheses;
			}
			set {
				options.SpaceBeforeMethodCallParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BetweenEmptyMethodCallParentheses {
			get {
				return options.SpaceBetweenEmptyMethodCallParentheses;
			}
			set {
				options.SpaceBetweenEmptyMethodCallParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeMethodCallParameterComma {
			get {
				return options.SpaceBeforeMethodCallParameterComma;
			}
			set {
				options.SpaceBeforeMethodCallParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterMethodCallParameterComma {
			get {
				return options.SpaceAfterMethodCallParameterComma;
			}
			set {
				options.SpaceAfterMethodCallParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool WithinMethodCallParentheses {
			get {
				return options.SpaceWithinMethodCallParentheses;
			}
			set {
				options.SpaceWithinMethodCallParentheses = value;
			}
		}
		
		// fields
		
		[ItemProperty]
		public bool BeforeFieldDeclarationComma {
			get {
				return options.SpaceBeforeFieldDeclarationComma;
			}
			set {
				options.SpaceBeforeFieldDeclarationComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterFieldDeclarationComma {
			get {
				return options.SpaceAfterFieldDeclarationComma;
			}
			set {
				options.SpaceAfterFieldDeclarationComma = value;
			}
		}
		
		// local variables
		
		[ItemProperty]
		public bool BeforeLocalVariableDeclarationComma {
			get {
				return options.SpaceBeforeLocalVariableDeclarationComma;
			}
			set {
				options.SpaceBeforeLocalVariableDeclarationComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterLocalVariableDeclarationComma {
			get {
				return options.SpaceAfterLocalVariableDeclarationComma;
			}
			set {
				options.SpaceAfterLocalVariableDeclarationComma = value;
			}
		}
		
		// constructors
		
		[ItemProperty]
		public bool BeforeConstructorDeclarationParentheses {
			get {
				return options.SpaceBeforeConstructorDeclarationParentheses;
			}
			set {
				options.SpaceBeforeConstructorDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BetweenEmptyConstructorDeclarationParentheses {
			get {
				return options.SpaceBetweenEmptyConstructorDeclarationParentheses;
			}
			set {
				options.SpaceBetweenEmptyConstructorDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeConstructorDeclarationParameterComma {
			get {
				return options.SpaceBeforeConstructorDeclarationParameterComma;
			}
			set {
				options.SpaceBeforeConstructorDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterConstructorDeclarationParameterComma {
			get {
				return options.SpaceAfterConstructorDeclarationParameterComma;
			}
			set {
				options.SpaceAfterConstructorDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool WithinConstructorDeclarationParentheses {
			get {
				return options.SpaceWithinConstructorDeclarationParentheses;
			}
			set {
				options.SpaceWithinConstructorDeclarationParentheses = value;
			}
		}
		
		// indexer
		[ItemProperty]
		public bool BeforeIndexerDeclarationBracket {
			get {
				return options.SpaceBeforeIndexerDeclarationBracket;
			}
			set {
				options.SpaceBeforeIndexerDeclarationBracket = value;
			}
		}
		
		[ItemProperty]
		public bool WithinIndexerDeclarationBracket {
			get {
				return options.SpaceWithinIndexerDeclarationBracket;
			}
			set {
				options.SpaceWithinIndexerDeclarationBracket = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeIndexerDeclarationParameterComma {
			get {
				return options.SpaceBeforeIndexerDeclarationParameterComma;
			}
			set {
				options.SpaceBeforeIndexerDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterIndexerDeclarationParameterComma {
			get {
				return options.SpaceAfterIndexerDeclarationParameterComma;
			}
			set {
				options.SpaceAfterIndexerDeclarationParameterComma = value;
			}
		}
		
		// delegates
		
		[ItemProperty]
		public bool BeforeDelegateDeclarationParentheses {
			get {
				return options.SpaceBeforeDelegateDeclarationParentheses;
			}
			set {
				options.SpaceBeforeDelegateDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BetweenEmptyDelegateDeclarationParentheses {
			get {
				return options.SpaceBetweenEmptyDelegateDeclarationParentheses;
			}
			set {
				options.SpaceBetweenEmptyDelegateDeclarationParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeDelegateDeclarationParameterComma {
			get {
				return options.SpaceBeforeDelegateDeclarationParameterComma;
			}
			set {
				options.SpaceBeforeDelegateDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterDelegateDeclarationParameterComma {
			get {
				return options.SpaceAfterDelegateDeclarationParameterComma;
			}
			set {
				options.SpaceAfterDelegateDeclarationParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool WithinDelegateDeclarationParentheses {
			get {
				return options.SpaceWithinDelegateDeclarationParentheses;
			}
			set {
				options.SpaceWithinDelegateDeclarationParentheses = value;
			}
		}
		
		
		[ItemProperty]
		public bool NewParentheses {
			get {
				return options.SpaceBeforeNewParentheses;
			}
			set {
				options.SpaceBeforeNewParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool IfParentheses {
			get {
				return options.SpaceBeforeIfParentheses;
			}
			set {
				options.SpaceBeforeIfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WhileParentheses {
			get {
				return options.SpaceBeforeWhileParentheses;
			}
			set {
				options.SpaceBeforeWhileParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool ForParentheses {
			get {
				return options.SpaceBeforeForParentheses;
			}
			set {
				options.SpaceBeforeForParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool ForeachParentheses {
			get {
				return options.SpaceBeforeForeachParentheses;
			}
			set {
				options.SpaceBeforeForeachParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool CatchParentheses {
			get {
				return options.SpaceBeforeCatchParentheses;
			}
			set {
				options.SpaceBeforeCatchParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool SwitchParentheses {
			get {
				return options.SpaceBeforeSwitchParentheses;
			}
			set {
				options.SpaceBeforeSwitchParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool LockParentheses {
			get {
				return options.SpaceBeforeLockParentheses;
			}
			set {
				options.SpaceBeforeLockParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool UsingParentheses {
			get {
				return options.SpaceBeforeUsingParentheses;
			}
			set {
				options.SpaceBeforeUsingParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool AroundAssignmentParentheses {
			get {
				return options.SpaceAroundAssignment;
			}
			set {
				options.SpaceAroundAssignment = value;
			}
		}
		
		[ItemProperty]
		public bool AroundLogicalOperatorParentheses {
			get {
				return options.SpaceAroundLogicalOperator;
			}
			set {
				options.SpaceAroundLogicalOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundEqualityOperatorParentheses {
			get {
				return options.SpaceAroundEqualityOperator;
			}
			set {
				options.SpaceAroundEqualityOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundRelationalOperatorParentheses {
			get {
				return options.SpaceAroundRelationalOperator;
			}
			set {
				options.SpaceAroundRelationalOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundBitwiseOperatorParentheses {
			get {
				return options.SpaceAroundBitwiseOperator;
			}
			set {
				options.SpaceAroundBitwiseOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundAdditiveOperatorParentheses {
			get {
				return options.SpaceAroundAdditiveOperator;
			}
			set {
				options.SpaceAroundAdditiveOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundMultiplicativeOperatorParentheses {
			get {
				return options.SpaceAroundMultiplicativeOperator;
			}
			set {
				options.SpaceAroundMultiplicativeOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundShiftOperatorParentheses {
			get {
				return options.SpaceAroundShiftOperator;
			}
			set {
				options.SpaceAroundShiftOperator = value;
			}
		}
		
		[ItemProperty]
		public bool AroundNullCoalescingOperator {
			get {
				return options.SpaceAroundNullCoalescingOperator;
			}
			set {
				options.SpaceAroundNullCoalescingOperator = value;
			}
		}

		[ItemProperty]
		public bool SpaceAfterUnsafeAddressOfOperator {
			get {
				return options.SpaceAfterUnsafeAddressOfOperator;
			}
			set {
				options.SpaceAfterUnsafeAddressOfOperator = value;
			}
		}


		[ItemProperty]
		public bool SpaceAfterUnsafeAsteriskOfOperator {
			get {
				return options.SpaceAfterUnsafeAsteriskOfOperator;
			}
			set {
				options.SpaceAfterUnsafeAsteriskOfOperator = value;
			}
		}

		[ItemProperty]
		public bool SpaceAroundUnsafeArrowOperator {
			get {
				return options.SpaceAroundUnsafeArrowOperator;
			}
			set {
				options.SpaceAroundUnsafeArrowOperator = value;
			}
		}

		[ItemProperty]
		public bool WithinParentheses {
			get {
				return options.SpacesWithinParentheses;
			}
			set {
				options.SpacesWithinParentheses = value;
			}
		}
		
		
		[ItemProperty]
		public bool WithinIfParentheses {
			get {
				return options.SpacesWithinIfParentheses;
			}
			set {
				options.SpacesWithinIfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinWhileParentheses {
			get {
				return options.SpacesWithinWhileParentheses;
			}
			set {
				options.SpacesWithinWhileParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinForParentheses {
			get {
				return options.SpacesWithinForParentheses;
			}
			set {
				options.SpacesWithinForParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinForEachParentheses {
			get {
				return options.SpacesWithinForeachParentheses;
			}
			set {
				options.SpacesWithinForeachParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinCatchParentheses {
			get {
				return options.SpacesWithinCatchParentheses;
			}
			set {
				options.SpacesWithinCatchParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinSwitchParentheses {
			get {
				return options.SpacesWithinSwitchParentheses;
			}
			set {
				options.SpacesWithinSwitchParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinLockParentheses {
			get {
				return options.SpacesWithinLockParentheses;
			}
			set {
				options.SpacesWithinLockParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinUsingParentheses {
			get {
				return options.SpacesWithinUsingParentheses;
			}
			set {
				options.SpacesWithinUsingParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinCastParentheses {
			get {
				return options.SpacesWithinCastParentheses;
			}
			set {
				options.SpacesWithinCastParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinSizeOfParentheses {
			get {
				return options.SpacesWithinSizeOfParentheses;
			}
			set {
				options.SpacesWithinSizeOfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeSizeOfParentheses {
			get {
				return options.SpaceBeforeSizeOfParentheses;
			}
			set {
				options.SpaceBeforeSizeOfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinTypeOfParentheses {
			get {
				return options.SpacesWithinTypeOfParentheses;
			}
			set {
				options.SpacesWithinTypeOfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinNewParentheses {
			get {
				return options.SpacesWithinNewParentheses;
			}
			set {
				options.SpacesWithinNewParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BetweenEmptyNewParentheses {
			get {
				return options.SpacesBetweenEmptyNewParentheses;
			}
			set {
				options.SpacesBetweenEmptyNewParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeNewParameterComma {
			get {
				return options.SpaceBeforeNewParameterComma;
			}
			set {
				options.SpaceBeforeNewParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool AfterNewParameterComma {
			get {
				return options.SpaceAfterNewParameterComma;
			}
			set {
				options.SpaceAfterNewParameterComma = value;
			}
		}
		
		[ItemProperty]
		public bool BeforeTypeOfParentheses {
			get {
				return options.SpaceBeforeTypeOfParentheses;
			}
			set {
				options.SpaceBeforeTypeOfParentheses = value;
			}
		}
		
		[ItemProperty]
		public bool WithinCheckedExpressionParantheses {
			get {
				return options.SpacesWithinCheckedExpressionParantheses;
			}
			set {
				options.SpacesWithinCheckedExpressionParantheses = value;
			}
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeConditionSpace {
			get {
				return options.SpaceBeforeConditionalOperatorCondition;
			}
			set {
				options.SpaceBeforeConditionalOperatorCondition = value;
			}
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterConditionSpace {
			get {
				return options.SpaceAfterConditionalOperatorCondition;
			}
			set {
				options.SpaceAfterConditionalOperatorCondition = value;
			}
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeSeparatorSpace {
			get {
				return options.SpaceBeforeConditionalOperatorSeparator;
			}
			set {
				options.SpaceBeforeConditionalOperatorSeparator = value;
			}
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterSeparatorSpace {
			get {
				return options.SpaceAfterConditionalOperatorSeparator;
			}
			set {
				options.SpaceAfterConditionalOperatorSeparator = value;
			}
		}
		
		// brackets
		[ItemProperty]
		public bool SpacesWithinBrackets {
			get {
				return options.SpacesWithinBrackets;
			}
			set {
				options.SpacesWithinBrackets = value;
			}
		}
		[ItemProperty]
		public bool SpacesBeforeBrackets {
			get {
				return options.SpacesBeforeBrackets;
			}
			set {
				options.SpacesBeforeBrackets = value;
			}
		}
		[ItemProperty]
		public bool BeforeBracketComma {
			get {
				return options.SpaceBeforeBracketComma;
			}
			set {
				options.SpaceBeforeBracketComma = value;
			}
		}
		[ItemProperty]
		public bool AfterBracketComma {
			get {
				return options.SpaceAfterBracketComma;
			}
			set {
				options.SpaceAfterBracketComma = value;
			}
		}
		
		
		[ItemProperty]
		public bool SpacesBeforeForSemicolon {
			get {
				return options.SpaceBeforeForSemicolon;
			}
			set {
				options.SpaceBeforeForSemicolon = value;
			}
		}

		[ItemProperty]
		public bool SpaceBeforeSemicolon {
			get {
				return options.SpaceBeforeSemicolon;
			}
			set {
				options.SpaceBeforeSemicolon = value;
			}
		}
		
		[ItemProperty]
		public bool SpacesAfterForSemicolon {
			get {
				return options.SpaceAfterForSemicolon;
			}
			set {
				options.SpaceAfterForSemicolon = value;
			}
		}
		
		[ItemProperty]
		public bool SpacesAfterTypecast {
			get {
				return options.SpaceAfterTypecast;
			}
			set {
				options.SpaceAfterTypecast = value;
			}
		}
		
		[ItemProperty]
		public bool SpacesBeforeArrayDeclarationBrackets {
			get {
				return options.SpaceBeforeArrayDeclarationBrackets;
			}
			set {
				options.SpaceBeforeArrayDeclarationBrackets = value;
			}
		}
		#endregion
		
		#region Blank Lines
		[ItemProperty]
		public int BlankLinesBeforeUsings {
			get {
				return options.BlankLinesBeforeUsings;
			}
			set {
				options.BlankLinesBeforeUsings = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesAfterUsings {
			get {
				return options.BlankLinesAfterUsings;
			}
			set {
				options.BlankLinesAfterUsings = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesBeforeFirstDeclaration {
			get {
				return options.BlankLinesBeforeFirstDeclaration;
			}
			set {
				options.BlankLinesBeforeFirstDeclaration = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesBetweenTypes {
			get {
				return options.BlankLinesBetweenTypes;
			}
			set {
				options.BlankLinesBetweenTypes = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesBetweenFields {
			get {
				return options.BlankLinesBetweenFields;
			}
			set {
				options.BlankLinesBetweenFields = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesBetweenEventFields {
			get {
				return options.BlankLinesBetweenEventFields;
			}
			set {
				options.BlankLinesBetweenEventFields = value;
			}
		}
		
		[ItemProperty]
		public int BlankLinesBetweenMembers {
			get {
				return options.BlankLinesBetweenMembers;
			}
			set {
				options.BlankLinesBetweenMembers = value;
			}
		}

		[ItemProperty]
		public int BlankLinesAroundRegion {
			get {
				return options.BlankLinesAroundRegion;
			}
			set {
				options.BlankLinesAroundRegion = value;
			}
		}

		[ItemProperty]
		public int BlankLinesInsideRegion {
			get {
				return options.BlankLinesInsideRegion;
			}
			set {
				options.BlankLinesInsideRegion = value;
			}
		}
		#endregion

		#region Wrapping
		[ItemProperty]
		public Wrapping MethodCallArgumentWrapping {
			get {
				return options.MethodCallArgumentWrapping;
			}
			set {
				options.MethodCallArgumentWrapping = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement NewLineAferMethodCallOpenParentheses {
			get {
				return options.NewLineAferMethodCallOpenParentheses;
			}
			set {
				options.NewLineAferMethodCallOpenParentheses = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement MethodCallClosingParenthesesOnNewLine {
			get {
				return options.MethodCallClosingParenthesesOnNewLine;
			}
			set {
				options.MethodCallClosingParenthesesOnNewLine = value;
			}
		}

		[ItemProperty]
		public bool AlignToFirstMethodCallArgument {
			get {
				return options.AlignToFirstMethodCallArgument;
			}
			set {
				options.AlignToFirstMethodCallArgument = value;
			}
		}

		[ItemProperty]
		public Wrapping MethodDeclarationParameterWrapping {
			get {
				return options.MethodDeclarationParameterWrapping;
			}
			set {
				options.MethodDeclarationParameterWrapping = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement NewLineAferMethodDeclarationOpenParentheses {
			get {
				return options.NewLineAferMethodDeclarationOpenParentheses;
			}
			set {
				options.NewLineAferMethodDeclarationOpenParentheses = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement MethodDeclarationClosingParenthesesOnNewLine {
			get {
				return options.MethodDeclarationClosingParenthesesOnNewLine;
			}
			set {
				options.MethodDeclarationClosingParenthesesOnNewLine = value;
			}
		}

		[ItemProperty]
		public bool AlignToFirstMethodDeclarationParameter {
			get {
				return options.AlignToFirstMethodDeclarationParameter;
			}
			set {
				options.AlignToFirstMethodDeclarationParameter = value;
			}
		}
	
		[ItemProperty]
		public Wrapping IndexerDeclarationParameterWrapping {
			get {
				return options.IndexerDeclarationParameterWrapping;
			}
			set {
				options.IndexerDeclarationParameterWrapping = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement NewLineAferIndexerDeclarationOpenBracket {
			get {
				return options.NewLineAferIndexerDeclarationOpenBracket;
			}
			set {
				options.NewLineAferIndexerDeclarationOpenBracket = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement IndexerDeclarationClosingBracketOnNewLine {
			get {
				return options.IndexerDeclarationClosingBracketOnNewLine;
			}
			set {
				options.IndexerDeclarationClosingBracketOnNewLine = value;
			}
		}

		[ItemProperty]
		public bool AlignToFirstIndexerDeclarationParameter {
			get {
				return options.AlignToFirstIndexerDeclarationParameter;
			}
			set {
				options.AlignToFirstIndexerDeclarationParameter = value;
			}
		}

		[ItemProperty]
		public Wrapping IndexerArgumentWrapping {
			get {
				return options.IndexerArgumentWrapping;
			}
			set {
				options.IndexerArgumentWrapping = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement NewLineAferIndexerOpenBracket {
			get {
				return options.NewLineAferIndexerOpenBracket;
			}
			set {
				options.NewLineAferIndexerOpenBracket = value;
			}
		}

		[ItemProperty]
		public NewLinePlacement IndexerClosingBracketOnNewLine {
			get {
				return options.IndexerClosingBracketOnNewLine;
			}
			set {
				options.IndexerClosingBracketOnNewLine = value;
			}
		}

		[ItemProperty]
		public bool AlignToFirstIndexerArgument {
			get {
				return options.AlignToFirstIndexerArgument;
			}
			set {
				options.AlignToFirstIndexerArgument = value;
			}
		}

		#endregion

		public CSharpFormattingPolicy ()
		{
			this.options = FormattingOptionsFactory.CreateMono ();
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
