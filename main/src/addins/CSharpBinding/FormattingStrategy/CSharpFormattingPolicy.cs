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

namespace FormattingStrategy
{
	public enum BraceStyle {
		EndOfLine,
		EndOfLineWithoutSpace,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}
	
	public class CSharpFormattingPolicy : IEquatable<CSharpFormattingPolicy>
	{
		public CSharpFormattingPolicy Clone ()
		{
			return (CSharpFormattingPolicy) MemberwiseClone ();
		}
		
		#region Indentation
		[ItemProperty]
		public bool IndentNamespaceBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentClassBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentInterfaceBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentStructBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentEnumBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentMethodBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentPropertyBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentEventBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentBlocks {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentSwitchBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentCaseBody {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentBreakStatements {
			get;
			set;
		}
		#endregion
		
		#region Braces
		[ItemProperty]
		public BraceStyle NamespaceBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle ClassBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle InterfaceBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle StructBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EnumBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle MethodBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle AnonymousMethodBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle ConstructorBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle DestructorBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertyBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertyGetBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertySetBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowPropertyGetBlockInline {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowPropertySetBlockInline {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventAddBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventRemoveBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowEventAddBlockInline {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowEventRemoveBlockInline {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle StatementBraceStyle {
			get;
			set;
		}
		#endregion
		
		#region NewLines
		[ItemProperty]
		public bool PlaceElseOnNewLine {
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceCatchOnNewLine {
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceFinallyOnNewLine {
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceWhileOnNewLine {
			get;
			set;
		}
		#endregion
		
		#region Spaces
		[ItemProperty]
		public bool BeforeMethodCallParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeMethodDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeConstructorDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeDelegateDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool NewParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool IfParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WhileParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ForParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ForeachParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool CatchParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SwitchParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool LockParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool UsingParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundAssignmentParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundLogicalOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundEqualityOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundRelationalOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundBitwiseOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundAdditiveOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundMultiplicativeOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundShiftOperatorParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinMethodCallParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinMethodDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinIfParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinWhileParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinForParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinForEachParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCatchParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinSwitchParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinLockParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinUsingParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCastParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinSizeOfParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinTypeOfParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCheckedExpressionParantheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeConditionSpace {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterConditionSpace {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeSeparatorSpace {
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterSeparatorSpace {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesWithinBrackets {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesAfterComma {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesBeforeComma {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesAfterSemicolon {
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesAfterTypecast {
			get;
			set;
		}
		#endregion
		
		public CSharpFormattingPolicy ()
		{
			IndentNamespaceBody = true;
			IndentClassBody = IndentInterfaceBody = IndentStructBody = IndentEnumBody = true;
			IndentMethodBody = IndentPropertyBody = IndentEventBody = true;
			IndentBlocks = true;
			IndentSwitchBody = false;
			IndentCaseBody = true;
			IndentBreakStatements = true;
			NamespaceBraceStyle = BraceStyle.NextLine;
			ClassBraceStyle = InterfaceBraceStyle = StructBraceStyle = EnumBraceStyle = BraceStyle.NextLine;
			MethodBraceStyle = ConstructorBraceStyle = DestructorBraceStyle = BraceStyle.NextLine;
			AnonymousMethodBraceStyle = BraceStyle.EndOfLine;

			PropertyBraceStyle = PropertyGetBraceStyle = PropertySetBraceStyle = BraceStyle.EndOfLine;
			AllowPropertyGetBlockInline = AllowPropertySetBlockInline = true;

			EventBraceStyle = EventAddBraceStyle = EventRemoveBraceStyle = BraceStyle.EndOfLine;
			AllowEventAddBlockInline = AllowEventRemoveBlockInline = true;
			StatementBraceStyle = BraceStyle.EndOfLine;

			PlaceElseOnNewLine = false;
			PlaceCatchOnNewLine = false;
			PlaceFinallyOnNewLine = false;
			PlaceWhileOnNewLine = false;

			BeforeMethodCallParentheses = true;
			BeforeMethodDeclarationParentheses = true;
			BeforeConstructorDeclarationParentheses = true;
			BeforeDelegateDeclarationParentheses = true;

			NewParentheses = true;
			IfParentheses = true;
			WhileParentheses = true;
			ForParentheses = true;
			ForeachParentheses = true;
			CatchParentheses = true;
			SwitchParentheses = true;
			LockParentheses = true;
			UsingParentheses = true;
			AroundAssignmentParentheses = true;
			AroundLogicalOperatorParentheses = true;
			AroundEqualityOperatorParentheses = true;
			AroundRelationalOperatorParentheses = true;
			AroundBitwiseOperatorParentheses = true;
			AroundAdditiveOperatorParentheses = true;
			AroundMultiplicativeOperatorParentheses = true;
			AroundShiftOperatorParentheses = true;
			WithinParentheses = false;
			WithinMethodCallParentheses = false;
			WithinMethodDeclarationParentheses = false;
			WithinIfParentheses = false;
			WithinWhileParentheses = false;
			WithinForParentheses = false;
			WithinForEachParentheses = false;
			WithinCatchParentheses = false;
			WithinSwitchParentheses = false;
			WithinLockParentheses = false;
			WithinUsingParentheses = false;
			WithinCastParentheses = false;
			WithinSizeOfParentheses = false;
			WithinTypeOfParentheses = false;
			WithinCheckedExpressionParantheses = false;
			ConditionalOperatorBeforeConditionSpace = true;
			ConditionalOperatorAfterConditionSpace = true;
			ConditionalOperatorBeforeSeparatorSpace = true;
			ConditionalOperatorAfterSeparatorSpace = true;

			SpacesWithinBrackets = false;
			SpacesAfterComma = true;
			SpacesBeforeComma = false;
			SpacesAfterSemicolon = true;
			SpacesAfterTypecast = false;
		}
		
		public bool Equals (CSharpFormattingPolicy other)
		{
			foreach (PropertyInfo info in typeof (CSharpFormattingPolicy).GetProperties ()) {
				object val      = info.GetValue (this, null);
				object otherVal = info.GetValue (other, null);
				if (!val.Equals (otherVal))
					return false;
			}
			return true;
		}
	}
}
