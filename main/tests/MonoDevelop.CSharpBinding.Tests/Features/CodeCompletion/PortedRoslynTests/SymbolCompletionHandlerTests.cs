// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn
{
	[TestFixture]
	partial class SymbolCompletionProviderTests : CompletionTestBase
	{
		internal override CompletionContextHandler CreateContextHandler ()
		{
			return (CompletionContextHandler)Activator.CreateInstance(typeof(CompletionEngine).Assembly.GetType ("ICSharpCode.NRefactory6.CSharp.Completion.RoslynRecommendationsCompletionContextHandler"));
		}

		[Test]
		public void EmptyFile ()
		{
			VerifyItemIsAbsent (@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
		}

		//		[Test]
		//		public void EmptyFile_Interactive()
		//		{
		//			VerifyItemIsAbsent(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		//			VerifyItemExists(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		//		}
		//
		[Test]
		public void EmptyFileWithUsing ()
		{
			VerifyItemIsAbsent (@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
		}

		//		[Test]
		//		public void EmptyFileWithUsing_Interactive()
		//		{
		//			VerifyItemExists(@"using System;
		//$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		//			VerifyItemExists(@"using System;
		//$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		//		}

		[Test]
		public void NotAfterHashR ()
		{
			VerifyItemIsAbsent (@"#r $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void NotAfterHashLoad ()
		{
			VerifyItemIsAbsent (@"#load $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void UsingDirective ()
		{
			VerifyItemIsAbsent (@"using $$", @"String");
			VerifyItemIsAbsent (@"using $$ = System", @"System");
			VerifyItemExists (@"using $$", @"System");
			VerifyItemExists (@"using T = $$", @"System");
		}

		[Test]
		public void InactiveRegion ()
		{
			VerifyItemIsAbsent (@"class C {
#if false 
$$
#endif", @"String");
			VerifyItemIsAbsent (@"class C {
#if false 
$$
#endif", @"System");
		}
		//
		[Test]
		public void ActiveRegion ()
		{
			VerifyItemIsAbsent (@"class C {
#if true 
$$
#endif", @"String");
			VerifyItemExists (@"class C {
#if true 
$$
#endif", @"System");
		}

		[Test]
		public void InactiveRegionWithUsing ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
#if false 
$$
#endif", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
#if false 
$$
#endif", @"System");
		}

		[Test]
		public void ActiveRegionWithUsing ()
		{
			VerifyItemExists (@"using System;

class C {
#if true 
$$
#endif", @"String");
			VerifyItemExists (@"using System;

class C {
#if true 
$$
#endif", @"System");
		}

		[Test]
		public void SingleLineComment1 ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
// $$", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
// $$", @"System");
		}

		[Test]
		public void SingleLineComment2 ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
// $$
", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
// $$
", @"System");
			VerifyItemIsAbsent (@"using System;

class C {
  // $$
", @"System");
		}

		[Test]
		public void MultiLineComment ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
/*  $$", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
/*  $$", @"System");
			VerifyItemIsAbsent (@"using System;

class C {
/*  $$   */", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
/*  $$   */", @"System");
			VerifyItemExists (@"using System;

class C {
/*    */$$", @"System");
			VerifyItemExists (@"using System;

class C {
/*    */$$
", @"System");
			VerifyItemExists (@"using System;

class C {
  /*    */$$
", @"System");
		}

		[Test]
		public void SingleLineXmlComment1 ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
/// $$", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
/// $$", @"System");
		}

		[Test]
		public void SingleLineXmlComment2 ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
/// $$
", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
/// $$
", @"System");
			VerifyItemIsAbsent (@"using System;

class C {
  /// $$
", @"System");
		}

		[Test]
		public void MultiLineXmlComment ()
		{
			VerifyItemIsAbsent (@"using System;

class C {
/**  $$   */", @"String");
			VerifyItemIsAbsent (@"using System;

class C {
/**  $$   */", @"System");
			VerifyItemExists (@"using System;

class C {
/**     */$$", @"System");
			VerifyItemExists (@"using System;

class C {
/**     */$$
", @"System");
			VerifyItemExists (@"using System;

class C {
  /**     */$$
", @"System");
		}

		[Test]
		public void OpenStringLiteral ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("string s = \"$$")), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("string s = \"$$")), @"System");
		}

		[Test]
		public void OpenStringLiteralInDirective ()
		{
			VerifyItemIsAbsent ("#r \"$$", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
			VerifyItemIsAbsent ("#r \"$$", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void StringLiteral ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("string s = \"$$\";")), @"System");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("string s = \"$$\";")), @"String");
		}

		[Test]
		public void StringLiteralInDirective ()
		{
			VerifyItemIsAbsent ("#r \"$$\"", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
			VerifyItemIsAbsent ("#r \"$$\"", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		//[Test]
		public void OpenCharLiteral ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("char c = '$$")), @"System");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod ("char c = '$$")), @"String");
		}

		[Test]
		public void AssemblyAttribute1 ()
		{
			VerifyItemExists (@"[assembly: $$]", @"System");
			VerifyItemIsAbsent (@"[assembly: $$]", @"String");
		}

		[Test]
		public void AssemblyAttribute2 ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"[assembly: $$]"), @"System");
			VerifyItemExists (AddUsingDirectives ("using System;", @"[assembly: $$]"), @"AttributeUsage");
		}

		[Test]
		public void SystemAttributeIsNotAnAttribute ()
		{
			var content = @"[$$]
class CL {}";

			VerifyItemIsAbsent (AddUsingDirectives ("using System;", content), @"Attribute");
		}

		[Test]
		public void TypeAttribute ()
		{
			var content = @"[$$]
class CL {}";

			VerifyItemExists (AddUsingDirectives ("using System;", content), @"AttributeUsage");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void TypeParamAttribute ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<[A$$]T> {}"), @"AttributeUsage");
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<[A$$]T> {}"), @"System");
		}

		[Test]
		public void MethodAttribute ()
		{
			var content = @"class CL {
    [$$]
    void Method() {}
}";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"AttributeUsage");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void MethodTypeParamAttribute ()
		{
			var content = @"class CL{
    void Method<[A$$]T> () {}
}";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"AttributeUsage");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void MethodParamAttribute ()
		{
			var content = @"class CL{
    void Method ([$$]int i) {}
}";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"AttributeUsage");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void NamespaceName1 ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"namespace $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"namespace $$"), @"System");
		}

		[Test]
		public void NamespaceName2 ()
		{
			VerifyItemIsAbsent (@"namespace $$", @"String");
			VerifyItemExists (@"namespace $$", @"System");
		}

		[Test]
		public void UnderNamespace ()
		{
			VerifyItemIsAbsent (@"namespace NS { $$", @"String");
			VerifyItemIsAbsent (@"namespace NS { $$", @"System");
		}

		[Test]
		public void OutsideOfType1 ()
		{
			VerifyItemIsAbsent (@"namespace NS {
class CL {}
$$", @"String");
			VerifyItemIsAbsent (@"namespace NS {
class CL {}
$$", @"System");
		}

		[Test]
		public void OutsideOfType2 ()
		{
			var content = @"namespace NS {
class CL {}
$$";
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void CompletionInsideProperty ()
		{
			var content = @"class C
{
    private string name;
    public string Name
    {
        set
        {
            name = $$";
			VerifyItemExists (content, @"value");
			VerifyItemExists (content, @"C");
		}

		[Test]
		public void AfterDot ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"[assembly: A.$$"), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"[assembly: A.$$"), @"System");
		}

		[Test]
		public void UsingAlias ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"using MyType = $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"using MyType = $$"), @"System");
		}

		[Test]
		public void IncompleteMember ()
		{
			var content = @"class CL {
    $$
";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void IncompleteMemberAccessibility ()
		{
			var content = @"class CL {
    public $$
";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void BadStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = $$)c")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = $$)c")), @"System");
		}

		[Test]
		public void TypeTypeParameter ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<$$"), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<$$"), @"System");
		}

		[Test]
		public void TypeTypeParameterList ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T, $$"), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T, $$"), @"System");
		}

		[Test]
		public void CastExpressionTypePart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = ($$)c")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = ($$)c")), @"System");
		}

		[Test]
		public void ObjectCreationExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new $$")), @"System");
		}

		[Test]
		public void ArrayCreationExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new $$ [")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new $$ [")), @"System");
		}

		[Test]
		public void StackAllocArrayCreationExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = stackalloc $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = stackalloc $$")), @"System");
		}

		[Test]
		public void FromClauseTypeOptPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from $$ c")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from $$ c")), @"System");
		}

		[Test]
		public void JoinClause ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join $$ j")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join $$ j")), @"System");
		}

		[Test]
		public void DeclarationStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ i =")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ i =")), @"System");
		}

		[Test]
		public void VariableDeclaration ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"fixed($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"fixed($$")), @"System");
		}

		[Test]
		public void ForEachStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach($$")), @"System");
		}

		[Test]
		public void ForEachStatementNoToken ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach $$")), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach $$")), @"System");
		}

		[Test]
		public void CatchDeclaration ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"try {} catch($$")), @"Exception");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"try {} catch($$")), @"System");
		}

		[Test]
		public void FieldDeclaration ()
		{
			var content = @"class CL {
    $$ i";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void EventFieldDeclaration ()
		{
			var content = @"class CL {
    event $$";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void ConversionOperatorDeclaration ()
		{
			var content = @"class CL {
    explicit operator $$";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void ConversionOperatorDeclarationNoToken ()
		{
			var content = @"class CL {
    explicit $$";
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void PropertyDeclaration ()
		{
			var content = @"class CL {
    $$ Prop {";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void EventDeclaration ()
		{
			var content = @"class CL {
    event $$ Event {";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void IndexerDeclaration ()
		{
			var content = @"class CL {
    $$ this";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void Parameter ()
		{
			var content = @"class CL {
    void Method($$";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void ArrayType ()
		{
			var content = @"class CL {
    $$ [";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void PointerType ()
		{
			var content = @"class CL {
    $$ *";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void NullableType ()
		{
			var content = @"class CL {
    $$ ?";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void DelegateDeclaration ()
		{
			var content = @"class CL {
    delegate $$";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void MethodDeclaration ()
		{
			var content = @"class CL {
    $$ M(";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void OperatorDeclaration ()
		{
			var content = @"class CL {
    $$ operator";
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", content), @"System");
		}

		[Test]
		public void ParenthesizedExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"($$")), @"System");
		}

		[Test]
		public void InvocationExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$(")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$(")), @"System");
		}

		[Test]
		public void ElementAccessExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$[")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$[")), @"System");
		}

		[Test]
		public void Argument ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"i[$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"i[$$")), @"System");
		}

		[Test]
		public void CastExpressionExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"(c)$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"(c)$$")), @"System");
		}

		[Test]
		public void FromClauseInPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in $$")), @"System");
		}

		[Test]
		public void LetClauseExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C let n = $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C let n = $$")), @"System");
		}

		[Test]
		public void OrderingExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C orderby $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C orderby $$")), @"System");
		}

		[Test]
		public void SelectClauseExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C select $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C select $$")), @"System");
		}

		[Test]
		public void ExpressionStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$")), @"System");
		}

		[Test]
		public void ReturnStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"return $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"return $$")), @"System");
		}

		[Test]
		public void ThrowStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"throw $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"throw $$")), @"System");
		}

		[Test]
		public void YieldReturnStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"yield return $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"yield return $$")), @"System");
		}

		[Test]
		public void ForEachStatementExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach(T t in $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"foreach(T t in $$")), @"System");
		}

		//		[Test]
		public void UsingStatementExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"using($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"using($$")), @"System");
		}

		[Test]
		public void LockStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"lock($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"lock($$")), @"System");
		}

		[Test]
		public void EqualsValueClause ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var i = $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var i = $$")), @"System");
		}

		[Test]
		public void ForStatementInitializersPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for($$")), @"System");
		}

		[Test]
		public void ForStatementConditionOptPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for(i=0;$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for(i=0;$$")), @"System");
		}

		[Test]
		public void ForStatementIncrementorsPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for(i=0;i>10;$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"for(i=0;i>10;$$")), @"System");
		}

		[Test]
		public void DoStatementConditionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"do {} while($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"do {} while($$")), @"System");
		}

		[Test]
		public void WhileStatementConditionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"while($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"while($$")), @"System");
		}

		[Test]
		public void ArrayRankSpecifierSizesPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"int [$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"int [$$")), @"System");
		}

		[Test]
		public void PrefixUnaryExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"+$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"+$$")), @"System");
		}

		[Test]
		public void PostfixUnaryExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$++")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$++")), @"System");
		}

		[Test]
		public void BinaryExpressionLeftPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ + 1")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ + 1")), @"System");
		}

		[Test]
		public void BinaryExpressionRightPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"1 + $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"1 + $$")), @"System");
		}

		[Test]
		public void AssignmentExpressionLeftPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ = 1")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$ = 1")), @"System");
		}

		[Test]
		public void AssignmentExpressionRightPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"1 = $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"1 = $$")), @"System");
		}

		[Test]
		public void ConditionalExpressionConditionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$? 1:")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"$$? 1:")), @"System");
		}

		[Test]
		public void ConditionalExpressionWhenTruePart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"true? $$:")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"true? $$:")), @"System");
		}

		[Test]
		public void ConditionalExpressionWhenFalsePart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"true? 1:$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"true? 1:$$")), @"System");
		}

		[Test]
		public void JoinClauseInExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in $$")), @"System");
		}

		[Test]
		public void JoinClauseLeftExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in P on $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in P on $$")), @"System");
		}

		[Test]
		public void JoinClauseRightExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in P on id equals $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C join p in P on id equals $$")), @"System");
		}

		[Test]
		public void WhereClauseConditionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C where $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C where $$")), @"System");
		}

		[Test]
		public void GroupClauseGroupExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C group $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C group $$")), @"System");
		}

		[Test]
		public void GroupClauseByExpressionPart ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C group g by $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = from c in C group g by $$")), @"System");
		}

		[Test]
		public void IfStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"if ($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"if ($$")), @"System");
		}

		[Test]
		public void SwitchStatement ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"switch($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"switch($$")), @"System");
		}

		[Test]
		public void SwitchLabelCase ()
		{
			var content = @"switch(i)
    {
        case $$";
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (content)), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (content)), @"System");
		}

		[Test]
		public void InitializerExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new [] { $$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"var t = new [] { $$")), @"System");
		}

		[Test]
		public void TypeParameterConstraintClause ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<T> where T : $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<T> where T : $$"), @"System");
		}

		[Test]
		public void TypeParameterConstraintClauseList ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<T> where T : A, $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL<T> where T : A, $$"), @"System");
		}

		[Test]
		public void TypeParameterConstraintClauseAnotherWhere ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T> where T : A where$$"), @"System");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T> where T : A where$$"), @"String");
		}

		[Test]
		public void TypeSymbolOfTypeParameterConstraintClause1 ()
		{
			VerifyItemExists (@"class CL<T> where $$", @"T");
			VerifyItemExists (@"class CL{ delegate void F<T>() where $$} ", @"T");
			VerifyItemExists (@"class CL{ void F<T>() where $$", @"T");
		}

		[Test]
		public void TypeSymbolOfTypeParameterConstraintClause2 ()
		{
			VerifyItemIsAbsent (@"class CL<T> where $$", @"System");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T> where $$"), @"String");
		}

		[Test]
		public void TypeSymbolOfTypeParameterConstraintClause3 ()
		{
			VerifyItemIsAbsent (@"class CL<T1> { void M<T2> where $$", @"T1");
			VerifyItemExists (@"class CL<T1> { void M<T2>() where $$", @"T2");
		}

		[Test]
		public void BaseList1 ()
		{
			// VerifyItemExists(AddUsingDirectives("using System;", @"class CL : $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL : $$"), @"System");
		}

		[Test]
		public void BaseList2 ()
		{
			//VerifyItemExists(AddUsingDirectives("using System;", @"class CL : B, $$"), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", @"class CL : B, $$"), @"System");
		}

		[Test]
		public void BaseListWhere ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T> : B where$$"), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class CL<T> : B where$$"), @"System");
		}

		[Test]
		public void AliasedName ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", AddInsideMethod (@"global::$$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"global::$$")), @"System");
		}

		[Test]
		public void AliasedNamespace ()
		{
			VerifyItemExists (AddUsingDirectives ("using S = System;", AddInsideMethod (@"S.$$")), @"String");
		}

		[Test]
		public void AliasedType ()
		{
			VerifyItemExists (AddUsingDirectives ("using S = System.String;", AddInsideMethod (@"S.$$")), @"Empty");
		}

		[Test]
		public void ConstructorInitializer ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class C { C() : $$"), @"String");
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class C { C() : $$"), @"System");
		}

		[Test]
		public void Typeof1 ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"typeof($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"typeof($$")), @"System");
		}

		[Test]
		public void Typeof2 ()
		{
			VerifyItemIsAbsent (AddInsideMethod (@"var x = 0; typeof($$"), @"x");
		}

		[Test]
		public void Sizeof1 ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"sizeof($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"sizeof($$")), @"System");
		}

		[Test]
		public void Sizeof2 ()
		{
			VerifyItemIsAbsent (AddInsideMethod (@"var x = 0; sizeof($$"), @"x");
		}

		[Test]
		public void Default1 ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"default($$")), @"String");
			VerifyItemExists (AddUsingDirectives ("using System;", AddInsideMethod (@"default($$")), @"System");
		}

		[Test]
		public void Default2 ()
		{
			VerifyItemIsAbsent (AddInsideMethod (@"var x = 0; default($$"), @"x");
		}

		[Test]
		public void Checked ()
		{
			VerifyItemExists (AddInsideMethod (@"var x = 0; checked($$"), @"x");
		}

		[Test]
		public void Unchecked ()
		{
			VerifyItemExists (AddInsideMethod (@"var x = 0; unchecked($$"), @"x");
		}

		[Test]
		public void Locals ()
		{
			VerifyItemExists (@"class c { void M() { string foo; $$", "foo");
		}

		[Test]
		public void Parameters ()
		{
			VerifyItemExists (@"class c { void M(string args) { $$", "args");
		}

		[Test]
		public void CommonTypesInNewExpressionContext ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"class c { void M() { new $$"), "Exception");
		}

		[Test]
		public void NoCompletionForUnboundTypes ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class c { void M() { foo.$$"), "Equals");
		}

		[Test]
		public void NoParametersInTypeOf ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class c { void M(int x) { typeof($$"), "x");
		}

		[Test]
		public void NoParametersInDefault ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"class c { void M(int x) { default($$"), "x");
		}

		[Test]
		public void NoParametersInSizeOf ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"public class C { void M(int x) { unsafe { sizeof($$"), "x");
		}

		[Test]
		public void NoParametersInGenericParameterList ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"public class Generic<T> { void M(int x) { Generic<$$"), "x");
		}

		[Test]
		public void NoMembersAfterNullLiteral ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"public class C { void M() { null.$$"), "Equals");
		}

		[Test]
		public void MembersAfterTrueLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { true.$$"), "Equals");
		}

		[Test]
		public void MembersAfterFalseLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { false.$$"), "Equals");
		}

		[Test]
		public void MembersAfterCharLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { 'c'.$$"), "Equals");
		}

		[Test]
		public void MembersAfterStringLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { """".$$"), "Equals");
		}

		[Test]
		public void MembersAfterVerbatimStringLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { @"""".$$"), "Equals");
		}

		[Test]
		public void MembersAfterNumericLiteral ()
		{
			// NOTE: the Completion command handler will suppress this case if the user types '.',
			// but we still need to show members if the user specifically invokes statement completion here.
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { 2.$$"), "Equals");
		}

		[Test]
		public void NoMembersAfterParenthesizedNullLiteral ()
		{
			VerifyItemIsAbsent (AddUsingDirectives ("using System;", @"public class C { void M() { (null).$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedTrueLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { (true).$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedFalseLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { (false).$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedCharLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { ('c').$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedStringLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { ("""").$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedVerbatimStringLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { (@"""").$$"), "Equals");
		}

		[Test]
		public void MembersAfterParenthesizedNumericLiteral ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { (2).$$"), "Equals");
		}

		[Test]
		public void MembersAfterArithmeticExpression ()
		{
			VerifyItemExists (AddUsingDirectives ("using System;", @"public class C { void M() { (1 + 1).$$"), "Equals");
		}

		[Test]
		public void InstanceTypesAvailableInUsingAlias ()
		{
			VerifyItemExists (@"using S = System.$$", "String");
		}

		[Test]
		public void InheritedMember1 ()
		{
			var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void InheritedMember2 ()
		{
			var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        this.$$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void InheritedMember3 ()
		{
			var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        base.$$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
			VerifyItemIsAbsent (markup, "Bar");
		}

		[Test]
		public void InheritedStaticMember1 ()
		{
			var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void InheritedStaticMember2 ()
		{
			var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        B.$$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void InheritedStaticMember3 ()
		{
			var markup = @"
class A
{
     private static void Hidden() { }
     protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        A.$$
    }
}
";
			VerifyItemIsAbsent (markup, "Hidden");
			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void InheritedInstanceAndStatcMembers ()
		{
			var markup = @"
class A
{
     private static void HiddenStatic() { }
     protected static void FooStatic() { }

     private void HiddenInstance() { }
     protected void FooInstance() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
			VerifyItemIsAbsent (markup, "HiddenStatic");
			VerifyItemExists (markup, "FooStatic");
			VerifyItemIsAbsent (markup, "HiddenInstance");
			VerifyItemExists (markup, "FooInstance");
		}

		[Test]
		public void ForLoopIndexer1 ()
		{
			var markup = @"
class C
{
    void M()
    {
        for (int i = 0; $$
";
			VerifyItemExists (markup, "i");
		}

		[Test]
		public void ForLoopIndexer2 ()
		{
			var markup = @"
class C
{
    void M()
    {
        for (int i = 0; i < 10; $$
";
			VerifyItemExists (markup, "i");
		}

		[Test]
		public void NoInstanceMembersAfterType1 ()
		{
			var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

			VerifyItemIsAbsent (markup, "Dispose");
		}

		[Test]
		public void NoInstanceMembersAfterType2 ()
		{
			var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
			VerifyItemIsAbsent (markup, "Dispose");
		}

		[Test]
		public void NoInstanceMembersAfterType3 ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

			VerifyItemIsAbsent (markup, "Dispose");
		}

		[Test]
		public void NoInstanceMembersAfterType4 ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

			VerifyItemIsAbsent (markup, "Dispose");
		}

		[Test]
		public void StaticMembersAfterType1 ()
		{
			var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

			VerifyItemExists (markup, "ReferenceEquals");
		}

		[Test]
		public void StaticMembersAfterType2 ()
		{
			var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
			VerifyItemIsAbsent (markup, "ReferenceEquals");
		}

		[Test]
		public void StaticMembersAfterType3 ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

			VerifyItemExists (markup, "ReferenceEquals");
		}

		[Test]
		public void StaticMembersAfterType4 ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

			VerifyItemIsAbsent (markup, "ReferenceEquals");
		}

		[Test]
		public void TypeParametersInClass ()
		{
			var markup = @"
class C<T, R>
{
    $$
}
";
			VerifyItemExists (markup, "T");
		}

		[Test]
		public void AfterRefInLambda ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        Func<int, int> f = (ref $$
    }
}
";
			VerifyItemExists (markup, "String");
		}


		[Test]
		public void AfterOutInLambda ()
		{
			var markup = @"
using System;
class C
{
    void M()
    {
        Func<int, int> f = (out $$
    }
}
";
			VerifyItemExists (markup, "String");
		}


		[Test]
		public void NestedType1 ()
		{
			var markup = @"
class Q
{
    $$
    class R
    {

    }
}
";
			VerifyItemExists (markup, "Q");
			VerifyItemExists (markup, "R");
		}


		[Test]
		public void NestedType2 ()
		{
			var markup = @"
class Q
{
    class R
    {
        $$
    }
}
";
			VerifyItemExists (markup, "Q");
			VerifyItemExists (markup, "R");
		}


		[Test]
		public void NestedType3 ()
		{
			var markup = @"
class Q
{
    class R
    {
    }
    $$
}
";
			VerifyItemExists (markup, "Q");
			VerifyItemExists (markup, "R");
		}


		[Test]
		public void NestedType4_Regular ()
		{
			var markup = @"
class Q
{
    class R
    {
    }
}
$$"; // At EOF
			VerifyItemIsAbsent (markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
		}


//		[Test]
//		public void NestedType4_Script ()
//		{
//			var markup = @"
//class Q
//{
//    class R
//    {
//    }
//}
//$$"; // At EOF
//			VerifyItemExists (markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
//			VerifyItemIsAbsent (markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
//		}


		[Test]
		public void NestedType5 ()
		{
			var markup = @"
class Q
{
    class R
    {
    }
    $$"; // At EOF
			VerifyItemExists (markup, "Q");
			VerifyItemExists (markup, "R");
		}


		[Test]
		public void NestedType6 ()
		{
			var markup = @"
class Q
{
    class R
    {
        $$"; // At EOF
			VerifyItemExists (markup, "Q");
			VerifyItemExists (markup, "R");
		}


		[Test]
		public void AmbiguityBetweenTypeAndLocal ()
		{
			var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public void foo() {
        int i = 5;
        i.$$
        List<string> ml = new List<string>();
    }
}";

			VerifyItemExists (markup, "CompareTo");
		}


//		[Test]
//		public void CompletionAfterNewInScript ()
//		{
//			var markup = @"
//using System;

//new $$";

//			VerifyItemExists (markup, "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
//		}


//		[Test]
//		public void ExtensionMethodsInScript ()
//		{
//			var markup = @"
//using System.Linq;
//var a = new int[] { 1, 2 };
//a.$$";

//			VerifyItemExists (markup, "ElementAt", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
//		}


		[Test]
		public void ExpressionsInForLoopInitializer ()
		{
			var markup = @"
public class C
{
    public void M()
    {
        int count = 0;
        for ($$
";

			VerifyItemExists (markup, "count");
		}


		[Test]
		public void AfterLambdaExpression1 ()
		{
			var markup = @"
public class C
{
    public void M()
    {
        System.Func<int, int> f = arg => { arg = 2; return arg; }.$$
    }
}
";

			VerifyItemIsAbsent (markup, "ToString");
		}


		[Test]
		public void AfterLambdaExpression2 ()
		{
			var markup = @"
public class C
{
    public void M()
    {
        ((System.Func<int, int>)(arg => { arg = 2; return arg; })).$$
    }
}
";

			VerifyItemExists (markup, "ToString");
			VerifyItemExists (markup, "Invoke");
		}


		[Test]
		public void InMultiLineCommentAtEndOfFile ()
		{
			var markup = @"
using System;
/*$$";

			VerifyItemIsAbsent (markup, "Console", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}


		[Test]
		public void TypeParametersAtEndOfFile ()
		{
			var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Outer<T>
{
class Inner<U>
{
static void F(T t, U u)
{
return;
}
public static void F(T t)
{
Outer<$$";

			VerifyItemExists (markup, "T");
		}


		[Test]
		public void LabelInCaseSwitchAbsentForCase ()
		{
			var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

			VerifyItemIsAbsent (markup, "case 0:");
		}


		[Test]
		public void LabelInCaseSwitchAbsentForDefaultWhenAbsent ()
		{
			var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

			VerifyItemIsAbsent (markup, "default:");
		}


		[Test]
		public void LabelInCaseSwitchPresentForDefault ()
		{
			var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            default:
                goto $$";

			VerifyItemExists (markup, "default:");
		}

		[Test]
		public void LabelAfterGoto1 ()
		{
			var markup = @"
class Program
{
    static void Main()
    {
    Foo:
        int Foo;
        goto $$";

			VerifyItemExists (markup, "Foo");
		}

		[Test]
		public void LabelAfterGoto2 ()
		{
			var markup = @"
class Program
{
    static void Main()
    {
    Foo:
        int Foo;
        goto Foo $$";

			VerifyItemIsAbsent (markup, "Foo");
		}


		[Test]
		public void AttributeName ()
		{
			var markup = @"
using System;
[$$";

			VerifyItemExists (markup, "CLSCompliant");
			VerifyItemIsAbsent (markup, "CLSCompliantAttribute");
		}


		[Test]
		public void AttributeNameAfterSpecifier ()
		{
			var markup = @"
using System;
[assembly:$$
";

			VerifyItemExists (markup, "CLSCompliant");
			VerifyItemIsAbsent (markup, "CLSCompliantAttribute");
		}


		[Test]
		public void AttributeNameInAttributeList ()
		{
			var markup = @"
using System;
[CLSCompliant, $$";

			VerifyItemExists (markup, "CLSCompliant");
			VerifyItemIsAbsent (markup, "CLSCompliantAttribute");
		}


		[Test]
		public void AttributeNameBeforeClass ()
		{
			var markup = @"
using System;
[$$
class C { }";

			VerifyItemExists (markup, "CLSCompliant");
			VerifyItemIsAbsent (markup, "CLSCompliantAttribute");
		}


		[Test]
		public void AttributeNameAfterSpecifierBeforeClass ()
		{
			var markup = @"
using System;
[assembly:$$
class C { }";

			VerifyItemExists (markup, "CLSCompliant");
			VerifyItemIsAbsent (markup, "CLSCompliantAttribute");
		}


		[Test]
		public void AttributeNameInAttributeArgumentList ()
		{
			var markup = @"
using System;
[CLSCompliant($$
class C { }";

			VerifyItemExists (markup, "CLSCompliantAttribute");
			VerifyItemIsAbsent (markup, "CLSCompliant");
		}


		[Test]
		public void AttributeNameInsideClass ()
		{
			var markup = @"
using System;
class C { $$ }";

			VerifyItemExists (markup, "CLSCompliantAttribute");
			VerifyItemIsAbsent (markup, "CLSCompliant");
		}


		[Test]
		public void NamespaceAliasInAttributeName1 ()
		{
			var markup = @"
using Alias = System;

[$$
class C { }";

			VerifyItemExists (markup, "Alias");
		}


		[Test]
		public void NamespaceAliasInAttributeName2 ()
		{
			var markup = @"
using Alias = Foo;

namespace Foo { }

[$$
class C { }";

			VerifyItemIsAbsent (markup, "Alias");
		}


		[Test]
		public void NamespaceAliasInAttributeName3 ()
		{
			var markup = @"
using Alias = Foo;

namespace Foo { class A : System.Attribute { } }

[$$
class C { }";

			VerifyItemExists (markup, "Alias");
		}



		[Test]
		public void AttributeNameAfterNamespace ()
		{
			var markup = @"
namespace Test
{
    class MyAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
			VerifyItemExists (markup, "My");
			VerifyItemIsAbsent (markup, "MyAttribute");
		}



		[Test]
		public void AttributeNameAfterNamespace2 ()
		{
			var markup = @"
namespace Test
{
    namespace Two
    {
        class MyAttribute : System.Attribute { }
        [Test.Two.$$
        class Program { }
    }
}";
			VerifyItemExists (markup, "My");
			VerifyItemIsAbsent (markup, "MyAttribute");
		}



		[Test]
		public void AttributeNameWhenSuffixlessFormIsKeyword ()
		{
			var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [$$
    class Program { }
}";
			VerifyItemExists (markup, "namespaceAttribute");
			VerifyItemIsAbsent (markup, "namespace");
			VerifyItemIsAbsent (markup, "@namespace");
		}



		[Test]
		public void AttributeNameAfterNamespaceWhenSuffixlessFormIsKeyword ()
		{
			var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
			VerifyItemExists (markup, "namespaceAttribute");
			VerifyItemIsAbsent (markup, "namespace");
			VerifyItemIsAbsent (markup, "@namespace");
		}



		[Test]
		public void KeywordsUsedAsLocals ()
		{
			var markup = @"
class C
{
    void M()
    {
        var error = 0;
        var method = 0;
        var @int = 0;
        Console.Write($$
    }
}";

			// preprocessor keyword
			VerifyItemExists (markup, "error");
			VerifyItemIsAbsent (markup, "@error");

			// contextual keyword
			VerifyItemExists (markup, "method");
			VerifyItemIsAbsent (markup, "@method");

			// full keyword
			VerifyItemExists (markup, "@int");
			VerifyItemIsAbsent (markup, "int");
		}



		[Test]
		public void QueryContextualKeywords1 ()
		{
			var markup = @"
class C
{
    void M()
    {
        var from = new[]{1,2,3};
        var r = from x in $$
    }
}";

			VerifyItemExists (markup, "@from");
			VerifyItemIsAbsent (markup, "from");
		}



		[Test]
		public void QueryContextualKeywords2 ()
		{
			var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where $$ == @where.Length
                select @from;
    }
}";

			VerifyItemExists (markup, "@from");
			VerifyItemIsAbsent (markup, "from");
			VerifyItemExists (markup, "@where");
			VerifyItemIsAbsent (markup, "where");
		}



		[Test]
		public void QueryContextualKeywords3 ()
		{
			var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where @from == @where.Length
                select $$;
    }
}";

			VerifyItemExists (markup, "@from");
			VerifyItemIsAbsent (markup, "from");
			VerifyItemExists (markup, "@where");
			VerifyItemIsAbsent (markup, "where");
		}



		[Test]
		public void AttributeNameAfterGlobalAlias ()
		{
			var markup = @"
class MyAttribute : System.Attribute { }
[global::$$
class Program { }";
			VerifyItemExists (markup, "My", sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (markup, "MyAttribute", sourceCodeKind: SourceCodeKind.Regular);
		}



		[Test]
		public void AttributeNameAfterGlobalAliasWhenSuffixlessFormIsKeyword ()
		{
			var markup = @"
class namespaceAttribute : System.Attribute { }
[global::$$
class Program { }";
			VerifyItemExists (markup, "namespaceAttribute", sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (markup, "namespace", sourceCodeKind: SourceCodeKind.Regular);
			VerifyItemIsAbsent (markup, "@namespace", sourceCodeKind: SourceCodeKind.Regular);
		}


		[Test]
		public void RangeVariableInQuerySelect ()
		{
			var markup = @"
using System.Linq;
class P
{
    void M()
    {
        var src = new string[] { ""Foo"", ""Bar"" };
        var q = from x in src
                select x.$$";

			VerifyItemExists (markup, "Length");
		}


		[Test]
		public void ConstantsInSwitchCase ()
		{
			var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case $$";

			VerifyItemExists (markup, "MAX_SIZE");
		}


		[Test]
		public void ConstantsInSwitchGotoCase ()
		{
			var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case MAX_SIZE:
                break;
            case FOO:
                goto case $$";

			VerifyItemExists (markup, "MAX_SIZE");
		}


		[Test]
		public void ConstantsInEnumMember ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    enum E
    {
        A = $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInAttribute1 ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage($$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInAttribute2 ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(FOO, $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInAttribute3 ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(validOn: $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInAttribute4 ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(AllowMultiple = $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInParameterDefaultValue ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    void M(int x = $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInConstField ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    const int BAR = $$";

			VerifyItemExists (markup, "FOO");
		}


		[Test]
		public void ConstantsInConstLocal ()
		{
			var markup = @"
class C
{
    public const int FOO = 0;
    void M()
    {
        const int BAR = $$";

			VerifyItemExists (markup, "FOO");
		}

		[Test]
		public void DescriptionWith1Overload ()
		{
			var markup = @"
class C
{
    void M(int i) { }
    void M()
    {
        $$";

			VerifyItemExists (markup, "M", expectedDescriptionOrNull: "void C.M(int i) (+ 1 overload)");
		}

		[Test]
		public void DescriptionWith2Overloads ()
		{
			var markup = @"
class C
{
    void M(int i) { }
    void M(out int i) { }
    void M()
    {
        $$";

			VerifyItemExists (markup, "M", expectedDescriptionOrNull: "void C.M(int i) (+ 2 overloads)");
		}

		[Test]
		public void DescriptionWith1GenericOverload ()
		{
			var markup = @"
class C
{
    void M<T>(T i) { }
    void M<T>()
    {
        $$";

			VerifyItemExists (markup, "M", expectedDescriptionOrNull: "void C.M<T>(T i) (+ 1 generic overload)");
		}

		[Test]
		public void DescriptionWith2GenericOverloads ()
		{
			var markup = @"
class C
{
    void M<T>(int i) { }
    void M<T>(out int i) { }
    void M<T>()
    {
        $$";

			VerifyItemExists (markup, "M", expectedDescriptionOrNull: "void C.M<T>(int i) (+ 2 generic overloads)");
		}

		[Test]
		public void DescriptionNamedGenericType ()
		{
			var markup = @"
class C<T>
{
    void M()
    {
        $$";

			VerifyItemExists (markup, "C", expectedDescriptionOrNull: "class C<T>");
		}

		[Test]
		public void DescriptionParameter ()
		{
			var markup = @"
class C<T>
{
    void M(T foo)
    {
        $$";

			VerifyItemExists (markup, "foo", expectedDescriptionOrNull: "(parameter) T foo");
		}

		[Test]
		public void DescriptionGenericTypeParameter ()
		{
			var markup = @"
class C<T>
{
    void M()
    {
        $$";

			VerifyItemExists (markup, "T", expectedDescriptionOrNull: "T in C<T>");
		}

		[Test]
		public void DescriptionAnonymousType ()
		{
			var markup = @"
class C
{
    void M()
    {
        var a = new { };
        $$
";

			var expectedDescription =
				@"(local variable) 'a a

Anonymous Types:
    'a is new {  }";

			VerifyItemExists (markup, "a", expectedDescription);
		}


		[Test]
		public void AfterNewInAnonymousType ()
		{
			var markup = @"
class Program {
    string field = 0;
    static void Main()     {
        var an = new {  new $$  }; 
    }
}
";

			VerifyItemExists (markup, "Program");
		}


		[Test]
		public void NoInstanceFieldsInStaticMethod ()
		{
			var markup = @"
class C
{
    int x = 0;
    static void M()
    {
        $$
    }
}
";

			VerifyItemIsAbsent (markup, "x");
		}


		[Test]
		public void NoInstanceFieldsInStaticFieldInitializer ()
		{
			var markup = @"
class C
{
    int x = 0;
    static int y = $$
}
";

			VerifyItemIsAbsent (markup, "x");
		}


		[Test]
		public void StaticFieldsInStaticMethod ()
		{
			var markup = @"
class C
{
    static int x = 0;
    static void M()
    {
        $$
    }
}
";

			VerifyItemExists (markup, "x");
		}


		[Test]
		public void StaticFieldsInStaticFieldInitializer ()
		{
			var markup = @"
class C
{
    static int x = 0;
    static int y = $$
}
";

			VerifyItemExists (markup, "x");
		}


		[Test]
		public void NoInstanceFieldsFromOuterClassInInstanceMethod ()
		{
			var markup = @"
class outer
{
    int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

			VerifyItemIsAbsent (markup, "i");
		}


		[Test]
		public void StaticFieldsFromOuterClassInInstanceMethod ()
		{
			var markup = @"
class outer
{
    static int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

			VerifyItemExists (markup, "i");
		}


		[Test]
		public void OnlyEnumMembersInEnumMemberAccess ()
		{
			var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        x.$$
    }
}
";

			VerifyItemExists (markup, "a");
			VerifyItemExists (markup, "b");
			VerifyItemExists (markup, "c");
			VerifyItemIsAbsent (markup, "Equals");
		}


		[Test]
		public void NoEnumMembersInEnumLocalAccess ()
		{
			var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        var y = x.a;
        y.$$
    }
}
";

			VerifyItemIsAbsent (markup, "a");
			VerifyItemIsAbsent (markup, "b");
			VerifyItemIsAbsent (markup, "c");
			VerifyItemExists (markup, "Equals");
		}


		[Test]
		public void AfterLambdaParameterDot ()
		{
			var markup = @"
using System;
using System.Linq;
class A
{
    public event Func<String, String> E;
}
 
class Program
{
    static void Main(string[] args)
    {
        new A().E += ss => ss.$$
    }
}
";

			VerifyItemExists (markup, "Substring");
		}

		[Test]
		public void ValueNotAtRoot_Interactive ()
		{
			VerifyItemIsAbsent (
				@"$$",
				"value",
				expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void ValueNotAfterClass_Interactive ()
		{
			VerifyItemIsAbsent (
				@"class C { }
$$",
				"value",
				expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void ValueNotAfterGlobalStatement_Interactive ()
		{
			VerifyItemIsAbsent (
				@"System.Console.WriteLine();
$$",
				"value",
				expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void ValueNotAfterGlobalVariableDeclaration_Interactive ()
		{
			VerifyItemIsAbsent (
				@"int i = 0;
$$",
				"value",
				expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
		}

		[Test]
		public void ValueNotInUsingAlias ()
		{
			VerifyItemIsAbsent (
				@"using Foo = $$",
				"value");
		}

		[Test]
		public void ValueNotInEmptyStatement ()
		{
			VerifyItemIsAbsent (AddInsideMethod (
				@"$$"),
				"value");
		}

		[Test]
		public void ValueInsideSetter ()
		{
			VerifyItemExists (
				@"class C {
    int Foo {
      set {
        $$",
				"value");
		}

		[Test]
		public void ValueInsideAdder ()
		{
			VerifyItemExists (
				@"class C {
    event int Foo {
      add {
        $$",
				"value");
		}

		[Test]
		public void ValueInsideRemover ()
		{
			VerifyItemExists (
				@"class C {
    event int Foo {
      remove {
        $$",
				"value");
		}

		[Test]
		public void ValueNotAfterDot ()
		{
			VerifyItemIsAbsent (
				@"class C {
    int Foo {
      set {
        this.$$",
				"value");
		}

		[Test]
		public void ValueNotAfterArrow ()
		{
			VerifyItemIsAbsent (
				@"class C {
    int Foo {
      set {
        a->$$",
				"value");
		}

		[Test]
		public void ValueNotAfterColonColon ()
		{
			VerifyItemIsAbsent (
				@"class C {
    int Foo {
      set {
        a::$$",
				"value");
		}

		[Test]
		public void ValueNotInGetter ()
		{
			VerifyItemIsAbsent (
				@"class C {
    int Foo {
      get {
        $$",
				"value");
		}


		[Test]
		public void NotAfterNullableType ()
		{
			VerifyItemIsAbsent (
				@"class C {
    void M() {
        int foo = 0;
        C? $$",
				"foo");
		}


		[Test]
		public void NotAfterNullableTypeAlias ()
		{
			VerifyItemIsAbsent (
				@"using A = System.Int32;
class C {
    void M() {
        int foo = 0;
        A? $$",
				"foo");
		}


		[Test]
		public void NotAfterNullableTypeAndPartialIdentifier ()
		{
			VerifyItemIsAbsent (
				@"class C {
    void M() {
        int foo = 0;
        C? f$$",
				"foo");
		}


		[Test]
		public void AfterQuestionMarkInConditional ()
		{
			VerifyItemExists (
				@"class C {
    void M() {
        bool b = false;
        int foo = 0;
        b? $$",
				"foo");
		}


		[Test]
		public void AfterQuestionMarkAndPartialIdentifierInConditional ()
		{
			VerifyItemExists (
				@"class C {
    void M() {
        bool b = false;
        int foo = 0;
        b? f$$",
				"foo");
		}


		[Test]
		public void NotAfterPointerType ()
		{
			VerifyItemIsAbsent (
				@"class C {
    void M() {
        int foo = 0;
        C* $$",
				"foo");
		}


		[Test]
		public void NotAfterPointerTypeAlias ()
		{
			VerifyItemIsAbsent (
				@"using A = System.Int32;
class C {
    void M() {
        int foo = 0;
        A* $$",
				"foo");
		}


		[Test]
		public void NotAfterPointerTypeAndPartialIdentifier ()
		{
			VerifyItemIsAbsent (
				@"class C {
    void M() {
        int foo = 0;
        C* f$$",
				"foo");
		}


		[Test]
		public void AfterAsteriskInMultiplication ()
		{
			VerifyItemExists (
				@"class C {
    void M() {
        int i = 0;
        int foo = 0;
        i* $$",
				"foo");
		}


		[Test]
		public void AfterAsteriskAndPartialIdentifierInMultiplication ()
		{
			VerifyItemExists (
				@"class C {
    void M() {
        int i = 0;
        int foo = 0;
        i* f$$",
				"foo");
		}


		[Test]
		public void AfterEventFieldDeclaredInSameType ()
		{
			VerifyItemExists (
				@"class C {
    public event System.EventHandler E;
    void M() {
        E.$$",
				"Invoke");
		}


		[Test]
		public void NotAfterFullEventDeclaredInSameType ()
		{
			VerifyItemIsAbsent (
				@"class C {
        public event System.EventHandler E { add { } remove { } }
    void M() {
        E.$$",
				"Invoke");
		}


		[Test]
		public void NotAfterEventDeclaredInDifferentType ()
		{
			VerifyItemIsAbsent (
				@"class C {
    void M() {
        System.Console.CancelKeyPress.$$",
				"Invoke");
		}


		[Test]
		public void NotInObjectInitializerMemberContext ()
		{
			VerifyItemIsAbsent (@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$",
				"x");
		}


		[Test]
		public void AfterPointerMemberAccess ()
		{
			VerifyItemExists (@"
struct MyStruct
{
    public int MyField;
}

class Program
{
    static unsafe void Main(string[] args)
    {
        MyStruct s = new MyStruct();
        MyStruct* ptr = &s;
        ptr->$$
    }}",
				"MyField");
		}

		// After @ both X and XAttribute are legal. We think this is an edge case in the language and
		// are not fixing the bug 11931. This test captures that XAttribute doesnt show up indeed.

		//		[Test]
		//		public void VerbatimAttributes()
		//		{
		//			var code = @"
		//using System;
		//public class X : Attribute
		//{ }

		//public class XAttribute : Attribute
		//{ }


		//[@X$$]
		//class Class3 { }
		//";
		//			VerifyItemExists(code, "X");
		//			AssertEx.Throws<Xunit.Sdk.TrueException>(() => VerifyItemExists(code, "XAttribute"));
		//		}


		[Test]
		public void InForLoopIncrementor1 ()
		{
			VerifyItemExists (@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; $$
    }
}
", "Console");
		}


		[Test]
		public void InForLoopIncrementor2 ()
		{
			VerifyItemExists (@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; Console.WriteLine(), $$
    }
}
", "Console");
		}


		[Test]
		public void InForLoopInitializer1 ()
		{
			VerifyItemExists (@"
using System;
 
class Program
{
    static void Main()
    {
        for ($$
    }
}
", "Console");
		}


		[Test]
		public void InForLoopInitializer2 ()
		{
			VerifyItemExists (@"
using System;
 
class Program
{
    static void Main()
    {
        for (Console.WriteLine(), $$
    }
}
", "Console");
		}


		[Test]
		public void LocalVariableInItsDeclaration ()
		{
			// "int foo = foo = 1" is a legal declaration
			VerifyItemExists (@"
class Program
{
    void M()
    {
        int foo = $$
    }
}", "foo");
		}


		[Test]
		public void LocalVariableInItsDeclarator ()
		{
			// "int bar = bar = 1" is legal in a declarator
			VerifyItemExists (@"
class Program
{
    void M()
    {
        int foo = 0, int bar = $$, int baz = 0;
    }
}", "bar");
		}


		[Test]
		public void LocalVariableNotBeforeDeclaration ()
		{
			VerifyItemIsAbsent (@"
class Program
{
    void M()
    {
        $$
        int foo = 0;
    }
}", "foo");
		}


		[Test]
		public void LocalVariableNotBeforeDeclarator ()
		{
			VerifyItemIsAbsent (@"
class Program
{
    void M()
    {
        int foo = $$, bar = 0;
    }
}", "bar");
		}


		[Test]
		public void LocalVariableAfterDeclarator ()
		{
			VerifyItemExists (@"
class Program
{
    void M()
    {
        int foo = 0, int bar = $$
    }
}", "foo");
		}


		[Test]
		public void LocalVariableAsOutArgumentInInitializerExpression ()
		{
			VerifyItemExists (@"
class Program
{
    void M()
    {
        int foo = Bar(out $$
    }
    int Bar(out int x)
    {
        x = 3;
        return 5;
    }
}", "foo");
		}
		 /*


		//		[Test]
		//		public void EditorBrowsable_Method_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar() 
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Method_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar() 
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Method_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public static void Bar() 
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Method_Overloads_BothBrowsableAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar() 
		//    {
		//    }

		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(int x) 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 2,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar() 
		//    {
		//    }

		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar(int x) 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Method_Overloads_BothBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        Foo.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar() 
		//    {
		//    }

		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar(int x) 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_ExtensionMethod_BrowsableAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(this Foo foo, int x)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_ExtensionMethod_BrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar(this Foo foo, int x)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_ExtensionMethod_BrowsableAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public static void Bar(this Foo foo, int x)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_ExtensionMethod_BrowsableMixed()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(this Foo foo, int x)
		//    {
		//    }

		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public static void Bar(this Foo foo, int x, int y)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public void Bar(int x)
		//    {
		//    }
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(this Foo foo, int x, int y)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 2,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableMixed()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Bar(int x)
		//    {
		//    }
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(this Foo foo, int x, int y)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_SameSigExtensionMethodAndMethod_InstanceMethodBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Bar(int x)
		//    {
		//    }
		//}

		//public static class FooExtensions
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public static void Bar(this Foo foo, int x)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void OverriddenSymbolsFilteredFromCompletionList()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        D d = new D();
		//        d.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class B
		//{
		//    public virtual void Foo(int original) 
		//    {
		//    }
		//}

		//public class D : B
		//{
		//    public override void Foo(int derived) 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        C c = new C();
		//        c.$$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//public class C
		//{
		//    public void Foo() 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        D d = new D();
		//        d.$$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//public class B
		//{
		//    public void Foo() 
		//    {
		//    }
		//}

		//public class D : B
		//{
		//    public void Foo(int x)
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 2,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
		//		{
		//			var markup = @"
		//class Program : B
		//{
		//    void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//public class B
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo() 
		//    {
		//    }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var ci = new C<int>();
		//        ci.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T>
		//{
		//    public void Foo(T t) { }
		//    public void Foo(int i) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 2,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var ci = new C<int>();
		//        ci.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T>
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(T t) { }
		//    public void Foo(int i) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var ci = new C<int>();
		//        ci.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T>
		//{
		//    public void Foo(T t) { }
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(int i) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var ci = new C<int>();
		//        ci.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T>
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(T t) { }
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(int i) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var cii = new C<int, int>();
		//        cii.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T, U>
		//{
		//    public void Foo(T t) { }
		//    public void Foo(U u) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 2,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var cii = new C<int, int>();
		//        cii.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T, U>
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(T t) { }
		//    public void Foo(U u) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        var cii = new C<int, int>();
		//        cii.$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class C<T, U>
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(T t) { }
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public void Foo(U u) { }
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 2,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Field_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public int bar;
		//}";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Field_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Field_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);
		//		}



		//		[Fact(Skip = "674611"), Trait(Traits.Feature, Traits.Features.Completion)]
		//		public void EditorBrowsable_Property_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public int Bar {get; set;}
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    public int Bar {
		//        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//        get { return 5; }
		//        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//        set { }
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Property_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public int Bar {get; set;}
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Property_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public int Bar {get; set;}
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Constructor_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public Foo()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Constructor_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
		//    public Foo()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}

		////		
		////		[Test]
		////		public void EditorBrowsable_Constructor_BrowsableStateAdvanced()
		////		{
		////			var markup = @"
		////class Program
		////{
		////    void M()
		////    {
		////        new $$
		////    }
		////}";

		////			var referencedCode = @"
		////public class Foo
		////{
		////    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		////    public Foo()
		////    {
		////    }
		////}";
		////			VerifyItemInEditorBrowsableContexts(
		////				markup: markup,
		////				referencedCode: referencedCode,
		////				item: "Foo",
		////				expectedSymbolsSameSolution: 1,
		////				expectedSymbolsMetadataReference: 1,
		////				sourceLanguage: LanguageNames.CSharp,
		////				referencedLanguage: LanguageNames.CSharp,
		////				hideAdvancedMembers: true);

		////			VerifyItemInEditorBrowsableContexts(
		////				markup: markup,
		////				referencedCode: referencedCode,
		////				item: "Foo",
		////				expectedSymbolsSameSolution: 1,
		////				expectedSymbolsMetadataReference: 1,
		////				sourceLanguage: LanguageNames.CSharp,
		////				referencedLanguage: LanguageNames.CSharp,
		////				hideAdvancedMembers: false);
		////		}


		//		[Test]
		//		public void EditorBrowsable_Constructor_MixedOverloads1()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public Foo()
		//    {
		//    }

		//    public Foo(int x)
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Constructor_MixedOverloads2()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public Foo()
		//    {
		//    }

		//    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
		//    public Foo(int x)
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Event_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new C().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public delegate void Handler();

		//public class C
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//    public event Handler Changed;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Changed",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Event_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new C().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public delegate void Handler();

		//public class C
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//    public event Handler Changed;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Changed",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Event_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new C().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public delegate void Handler();

		//public class C
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public event Handler Changed;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Changed",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Changed",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Delegate_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public event $$
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public delegate void Handler();";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Handler",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Delegate_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public event $$
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public delegate void Handler();";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Handler",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Delegate_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public event $$
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public delegate void Handler();";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Handler",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Handler",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateNever_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateNever_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateNever_FullyQualifiedInUsing()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        using (var x = new NS.$$
		//    }
		//}";

		//			var referencedCode = @"
		//namespace NS
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//    public class Foo : System.IDisposable
		//    {
		//        public void Dispose()
		//        {
		//        }
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAlways_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAlways_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAlways_FullyQualifiedInUsing()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        using (var x = new NS.$$
		//    }
		//}";

		//			var referencedCode = @"
		//namespace NS
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//    public class Foo : System.IDisposable
		//    {
		//        public void Dispose()
		//        {
		//        }
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_BrowsableStateAdvanced_FullyQualifiedInUsing()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        using (var x = new NS.$$
		//    }
		//}";

		//			var referencedCode = @"
		//namespace NS
		//{
		//    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//    public class Foo : System.IDisposable
		//    {
		//        public void Dispose()
		//        {
		//        }
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Class_IgnoreBaseClassBrowsableNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo : Bar
		//{
		//}

		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public class Bar
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateNever_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateNever_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateAlways_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Struct_BrowsableStateAdvanced_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public struct Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Enum_BrowsableStateNever()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public enum Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Enum_BrowsableStateAlways()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public enum Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Enum_BrowsableStateAdvanced()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public enum Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateNever_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateNever_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    public void M()
		//    {
		//        $$    
		//    }
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom()
		//		{
		//			var markup = @"
		//class Program : $$
		//{
		//}";

		//			var referencedCode = @"
		//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
		//public interface Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: false);

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp,
		//				hideAdvancedMembers: true);
		//		}


		//		[Test]
		//		public void EditorBrowsable_CrossLanguage_CStoVB_Always()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
		//Public Class Foo
		//End Class";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.VisualBasic,
		//				hideAdvancedMembers: false);
		//		}


		//		[Test]
		//		public void EditorBrowsable_CrossLanguage_CStoVB_Never()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        $$
		//    }
		//}";

		//			var referencedCode = @"
		//<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
		//Public Class Foo
		//End Class";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 0,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.VisualBasic,
		//				hideAdvancedMembers: false);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_NotHidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_Hidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_HiddenAndOtherFlags()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_NotHidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_Hidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibType_HiddenAndOtherFlags_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new $$
		//    }
		//}";

		//			var referencedCode = @"
		//[System.Runtime.InteropServices.TypeLibType((short)(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed))]
		//public class Foo
		//{
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Foo",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_NotHidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_Hidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_NotHidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_Hidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibFunc((short)(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable))]
		//    public void Bar()
		//    {
		//    }
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "Bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibVar_NotHidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibVar_Hidden()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibVar_HiddenAndOtherFlags()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		[Test]
		//		public void EditorBrowsable_TypeLibVar_NotHidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibVar_Hidden_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void EditorBrowsable_TypeLibVar_HiddenAndOtherFlags_Int16Constructor()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void M()
		//    {
		//        new Foo().$$
		//    }
		//}";

		//			var referencedCode = @"
		//public class Foo
		//{
		//    [System.Runtime.InteropServices.TypeLibVar((short)(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable))]
		//    public int bar;
		//}";
		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "bar",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 0,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.CSharp);
		//		}


		//		[Test]
		//		public void TestColorColor1()
		//		{
		//			var markup = @"
		//class A
		//{
		//    static void Foo() { }
		//    void Bar() { }

		//    static void Main()
		//    {
		//        A A = new A();
		//        A.$$
		//    }
		//}";

		//			VerifyItemExists(markup, "Foo");
		//			VerifyItemExists(markup, "Bar");
		//		}


		//		[Test]
		//		public void TestLaterLocalHidesType1()
		//		{
		//			var markup = @"
		//using System;
		//class C
		//{
		//    public static void Main()
		//    {
		//        $$
		//        Console.WriteLine();
		//    }
		//}";

		//			VerifyItemExists(markup, "Console");
		//		}


		//		[Test]
		//		public void TestLaterLocalHidesType2()
		//		{
		//			var markup = @"
		//using System;
		//class C
		//{
		//    public static void Main()
		//    {
		//        C$$
		//        Console.WriteLine();
		//    }
		//}";

		//			VerifyItemExists(markup, "Console");
		//		}

		//		[Test]
		//		public void TestIndexedProperty()
		//		{
		//			var markup = @"class Program
		//{
		//    void M()
		//    {
		//            CCC c = new CCC();
		//            c.$$
		//    }
		//}";

		//			// Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
		//			var referencedCode = @"Imports System.Runtime.InteropServices

		//<ComImport()>
		//<GuidAttribute(CCC.ClassId)>
		//Public Class CCC

		//#Region ""COM GUIDs""
		//    Public Const ClassId As String = ""9d965fd2-1514-44f6-accd-257ce77c46b0""
		//    Public Const InterfaceId As String = ""a9415060-fdf0-47e3-bc80-9c18f7f39cf6""
		//    Public Const EventsId As String = ""c6a866a5-5f97-4b53-a5df-3739dc8ff1bb""
		//# End Region

		//            ''' <summary>
		//    ''' An index property from VB
		//    ''' </summary>
		//    ''' <param name=""p1"">p1 is an integer index</param>
		//    ''' <returns>A string</returns>
		//    Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
		//        Get
		//            Return Nothing
		//        End Get
		//        Set(ByVal value As String)

		//        End Set
		//    End Property
		//End Class";

		//			VerifyItemInEditorBrowsableContexts(
		//				markup: markup,
		//				referencedCode: referencedCode,
		//				item: "IndexProp",
		//				expectedSymbolsSameSolution: 1,
		//				expectedSymbolsMetadataReference: 1,
		//				sourceLanguage: LanguageNames.CSharp,
		//				referencedLanguage: LanguageNames.VisualBasic);
		//		}


		//		[Test]
		//		public void TestDeclarationAmbiguity()
		//		{
		//			var markup = @"
		//using System;

		//class Program
		//{
		//    void Main()
		//    {
		//        Environment.$$
		//        var v;
		//    }
		//}";

		//			VerifyItemExists(markup, "CommandLine");
		//		}

		//		[Test]
		//		public void TestCursorOnClassCloseBrace()
		//		{
		//			var markup = @"
		//using System;

		//class Outer
		//{
		//    class Inner { }

		//$$}";

		//			VerifyItemExists(markup, "Inner");
		//		}

		//		[Test]
		//		public void AfterAsync1()
		//		{
		//			var markup = @"
		//using System.Threading.Tasks;
		//class Program
		//{
		//    async $$
		//}";

		//			VerifyItemExists(markup, "Task");
		//		}

		//		[Test]
		//		public void AfterAsync2()
		//		{
		//			var markup = @"
		//using System.Threading.Tasks;
		//class Program
		//{
		//    public async T$$
		//}";

		//			VerifyItemExists(markup, "Task");
		//		}

		//		[Test]
		//		public void NotAfterAsyncInMethodBody()
		//		{
		//			var markup = @"
		//using System.Threading.Tasks;
		//class Program
		//{
		//    void foo()
		//    {
		//        var x = async $$
		//    }
		//}";

		//			VerifyItemIsAbsent(markup, "Task");
		//		}

		//		[Test]
		//		public void NotAwaitable1()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    void foo()
		//    {
		//        $$
		//    }
		//}";

		//			VerifyItemWithMscorlib45(markup, "foo", "void Program.foo()", "C#");
		//		}

		//		[Test]
		//		public void NotAwaitable2()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    async void foo()
		//    {
		//        $$
		//    }
		//}";

		//			VerifyItemWithMscorlib45(markup, "foo", "void Program.foo()", "C#");
		//		}

		//		[Test]
		//		public void Awaitable1()
		//		{
		//			var markup = @"
		//using System.Threading;
		//using System.Threading.Tasks;

		//class Program
		//{
		//    async Task foo()
		//    {
		//        $$
		//    }
		//}";

		//			var description = @"(awaitable) Task Program.foo()
		//Usage:
		//  await foo();";

		//			VerifyItemWithMscorlib45(markup, "foo", description, "C#");
		//		}

		//		[Test]
		//		public void Awaitable2()
		//		{
		//			var markup = @"
		//using System.Threading.Tasks;

		//class Program
		//{
		//    async Task<int> foo()
		//    {
		//        $$
		//    }
		//}";

		//			var description = @"(awaitable) Task<int> Program.foo()
		//Usage:
		//  int x = await foo();";

		//			VerifyItemWithMscorlib45(markup, "foo", description, "C#");
		//		}

		//		[Test]
		//		public void ObsoleteItem()
		//		{
		//			var markup = @"
		//using System;

		//class Program
		//{
		//    [Obsolete]
		//    public void foo()
		//    {
		//        $$
		//    }
		//}";
		//			VerifyItemExists(markup, "foo", "[deprecated] void Program.foo()");
		//		}


		//		[Test]
		//		public void NoMembersOnDottingIntoUnboundType()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    RegistryKey foo;

		//    static void Main(string[] args)
		//    {
		//        foo.$$
		//    }
		//}";
		//			VerifyNoItemsExist(markup);
		//		}


		//		[Test]
		//		public void TypeArgumentsInConstraintAfterBaselist()
		//		{
		//			var markup = @"
		//public class Foo<T> : System.Object where $$
		//{
		//}";
		//			VerifyItemExists(markup, "T");
		//		}


		//		[Test]
		//		public void NoDestructor()
		//		{
		//			var markup = @"
		//class C
		//{
		//    ~C()
		//    {
		//        $$
		//";
		//			VerifyItemIsAbsent(markup, "Finalize");
		//		}


		//		[Test]
		//		public void ExtensionMethodOnCovariantInterface()
		//		{
		//			var markup = @"
		//class Schema<T> { }

		//interface ISet<out T> { }

		//static class SetMethods
		//{
		//    public static void ForSchemaSet<T>(this ISet<Schema<T>> set) { }
		//}

		//class Context
		//{
		//    public ISet<T> Set<T>() { return null; }
		//}

		//class CustomSchema : Schema<int> { }

		//class Program
		//{
		//    static void Main(string[] args)
		//    {
		//        var set = new Context().Set<CustomSchema>();

		//        set.$$
		//";

		//			VerifyItemExists(markup, "ForSchemaSet<>", sourceCodeKind: SourceCodeKind.Regular);
		//		}


		//		[Test]
		//		public void ForEachInsideParentheses()
		//		{
		//			var markup = @"
		//using System;
		//class C
		//{
		//    void M()
		//    {
		//        foreach($$)
		//";

		//			VerifyItemExists(markup, "String");
		//		}


		//		[Test]
		//		public void TestFieldInitializerInP2P()
		//		{
		//			var markup = @"
		//class Class
		//{
		//    int i = Consts.$$;
		//}";

		//			var referencedCode = @"
		//public static class Consts
		//{
		//    public const int C = 1;
		//}";
		//			VerifyItemWithProjectReference(markup, referencedCode, "C", 1, LanguageNames.CSharp, LanguageNames.CSharp, false);
		//		}


		//		[Test]
		//		public void ShowWithEqualsSign()
		//		{
		//			var markup = @"
		//class c { public int value {set; get; }}

		//class d
		//{
		//    void foo()
		//    {
		//       c foo = new c { value$$=
		//    }
		//}";

		//			VerifyNoItemsExist(markup);
		//		}


		//		[Test]
		//		public void NothingAfterThisDotInStaticContext()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M1() { }

		//    static void M2()
		//    {
		//        this.$$
		//    }
		//}";

		//			VerifyNoItemsExist(markup);
		//		}


		//		[Test]
		//		public void NothingAfterBaseDotInStaticContext()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M1() { }

		//    static void M2()
		//    {
		//        base.$$
		//    }
		//}";

		//			VerifyNoItemsExist(markup);
		//		}


		//		[Test]
		//		public void NoNestedTypeWhenDisplayingInstance()
		//		{
		//			var markup = @"
		//class C
		//{
		//    class D
		//    {
		//    }

		//    void M2()
		//    {
		//        new C().$$
		//    }
		//}";

		//			VerifyItemIsAbsent(markup, "D");
		//		}


		//		[Test]
		//		public void CatchVariableInExceptionFilter()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        try
		//        {
		//        }
		//        catch (System.Exception myExn) when ($$";

		//			VerifyItemExists(markup, "myExn");
		//		}


		//		[Test]
		//		public void CompletionAfterExternAlias()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void foo()
		//    {
		//        global::$$
		//    }
		//}";

		//			VerifyItemExists(markup, "System", usePreviousCharAsTrigger: true);
		//		}


		//		[Test]
		//		public void ExternAliasSuggested()
		//		{
		//			var markup = @"
		//extern alias Bar;
		//class C
		//{
		//    void foo()
		//    {
		//        $$
		//    }
		//}";
		//			VerifyItemWithAliasedMetadataReferences(markup, "Bar", "Bar", 1, "C#", "C#", false);
		//		}


		//		[Test]
		//		public void ClassDestructor()
		//		{
		//			var markup = @"
		//class C
		//{
		//    class N
		//    {
		//    ~$$
		//    }
		//}";
		//			VerifyItemExists(markup, "N");
		//			VerifyItemIsAbsent(markup, "C");
		//		}


		//		[Test]
		//		public void TildeOutsideClass()
		//		{
		//			var markup = @"
		//class C
		//{
		//    class N
		//    {
		//    }
		//}
		//~$$";
		//			VerifyNoItemsExist(markup, SourceCodeKind.Regular);
		//		}


		//		[Test]
		//		public void StructDestructor()
		//		{
		//			var markup = @"
		//struct C
		//{
		//   ~$$
		//}";
		//			VerifyItemExists(markup, "C");
		//		}

		//		[Test]
		//		public void FieldAvailableInBothLinkedFiles()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//    int x;
		//    void foo()
		//    {
		//        $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";

		//			VerifyItemInLinkedFiles(markup, "x", "(field) int C.x");
		//		}

		//		[Test]
		//		public void FieldUnavailableInOneLinkedFile()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//#if FOO
		//    int x;
		//#endif
		//    void foo()
		//    {
		//        $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(field) int C.x\r\n\r\n    Proj1 - Available\r\n    Proj2 - Not Available\r\n\r\nYou can use the navigation bar to switch context.";

		//			VerifyItemInLinkedFiles(markup, "x", expectedDescription);
		//		}

		//		[Test]
		//		public void FieldUnavailableInTwoLinkedFiles()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//#if FOO
		//    int x;
		//#endif
		//    void foo()
		//    {
		//        $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(field) int C.x\r\n\r\n    Proj1 - Available\r\n    Proj2 - Not Available\r\n    Proj3 - Not Available\r\n\r\nYou can use the navigation bar to switch context.";

		//			VerifyItemInLinkedFiles(markup, "x", expectedDescription);
		//		}

		//		[Test]
		//		public void ExcludeFilesWithInactiveRegions()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//#if FOO
		//    int x;
		//#endif

		//#if BAR
		//    void foo()
		//    {
		//        $$
		//    }
		//#endif
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs"" />
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(field) int C.x\r\n\r\n    Proj1 - Available\r\n    Proj3 - Not Available\r\n\r\nYou can use the navigation bar to switch context.";

		//			VerifyItemInLinkedFiles(markup, "x", expectedDescription);
		//		}

		//		[Test]
		//		public void UnionOfItemsFromBothContexts()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//#if FOO
		//    int x;
		//#endif

		//#if BAR
		//    class G
		//    {
		//        public void DoGStuff() {}
		//    }
		//#endif
		//    void foo()
		//    {
		//        new G().$$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""BAR"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "void G.DoGStuff()\r\n\r\n    Proj1 - Not Available\r\n    Proj2 - Available\r\n    Proj3 - Not Available\r\n\r\nYou can use the navigation bar to switch context.";

		//			VerifyItemInLinkedFiles(markup, "DoGStuff", expectedDescription);
		//		}


		//		[Test]
		//		public void LocalsValidInLinkedDocuments()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//    void M()
		//    {
		//        int xyz;
		//        $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(local variable) int xyz";
		//			VerifyItemInLinkedFiles(markup, "xyz", expectedDescription);
		//		}


		//		[Test]
		//		public void LocalWarningInLinkedDocuments()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""PROJ1"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//    void M()
		//    {
		//#if PROJ1
		//        int xyz;
		//#endif
		//        $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(local variable) int xyz\r\n\r\n    Proj1 - Available\r\n    Proj2 - Not Available\r\n\r\nYou can use the navigation bar to switch context.";
		//			VerifyItemInLinkedFiles(markup, "xyz", expectedDescription);
		//		}


		//		[Test]
		//		public void LabelsValidInLinkedDocuments()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//class C
		//{
		//    void M()
		//    {
		//LABEL:  int xyz;
		//        goto $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(label) LABEL";
		//			VerifyItemInLinkedFiles(markup, "LABEL", expectedDescription);
		//		}


		//		[Test]
		//		public void RangeVariablesValidInLinkedDocuments()
		//		{
		//			var markup = @"<Workspace>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
		//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
		//using System.Linq;
		//class C
		//{
		//    void M()
		//    {
		//        var x = from y in new[] { 1, 2, 3 } select $$
		//    }
		//}
		//]]>
		//        </Document>
		//    </Project>
		//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
		//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
		//    </Project>
		//</Workspace>";
		//			var expectedDescription = "(range variable) ? y";
		//			VerifyItemInLinkedFiles(markup, "y", expectedDescription);
		//		}

		//		[Test]
		//		public void ConditionalAccessWalkUp()
		//		{
		//			var markup = @"
		//public class B
		//{
		//    public A BA;
		//    public B BB;
		//}

		//class A
		//{
		//    public A AA;
		//    public A AB;
		//    public int? x;

		//    public void foo()
		//    {
		//        A a = null;
		//        var q = a?.$$AB.BA.AB.BA;
		//    }
		//}";
		//			VerifyItemExists(markup, "AA", experimental: true);
		//			VerifyItemExists(markup, "AB", experimental: true);
		//		}

		//		[Test]
		//		public void ConditionalAccessNullableIsUnwrapped()
		//		{
		//			var markup = @"
		//public struct S
		//{
		//    public int? i;
		//}

		//class A
		//{
		//    public S? s;

		//    public void foo()
		//    {
		//        A a = null;
		//        var q = a?.s?.$$;
		//    }
		//}";
		//			VerifyItemExists(markup, "i", experimental: true);
		//			VerifyItemIsAbsent(markup, "value", experimental: true);
		//		}
		//
		//		[Test]
		//		public void ConditionalAccessNullableIsUnwrapped2()
		//		{
		//			var markup = @"
		//public struct S
		//{
		//    public int? i;
		//}
		//
		//class A
		//{
		//    public S? s;
		//
		//    public void foo()
		//    {
		//        var q = s?.$$i?.ToString();
		//    }
		//}";
		//			VerifyItemExists(markup, "i", experimental: true);
		//			VerifyItemIsAbsent(markup, "value", experimental: true);
		//		}
		//
		//		[Test]
		//		public void CompletionAfterConditionalIndexing()
		//		{
		//			var markup = @"
		//public struct S
		//{
		//    public int? i;
		//}
		//
		//class A
		//{
		//    public S[] s;
		//
		//    public void foo()
		//    {
		//        A a = null;
		//        var q = a?.s?[$$;
		//    }
		//}";
		//			VerifyItemExists(markup, "System", experimental: true);
		//		}
		//
		//		
		//		[Test]
		//		public void WithinChainOfConditionalAccesses()
		//		{
		//			var markup = @"
		//class Program
		//{
		//    static void Main(string[] args)
		//    {
		//        A a;
		//        var x = a?.$$b?.c?.d.e;
		//    }
		//}
		//
		//class A { public B b; }
		//class B { public C c; }
		//class C { public D d; }
		//class D { public int e; }";
		//			VerifyItemExists(markup, "b");
		//		}
		//
		//		
		//		[Test]
		//		public void NestedAttributeAccessibleOnSelf()
		//		{
		//			var markup = @"using System;
		//[My]
		//class X
		//{
		//    [My$$]
		//    class MyAttribute : Attribute
		//    {
		//
		//    }
		//}";
		//			VerifyItemExists(markup, "My");
		//		}
		//
		//		
		//		[Test]
		//		public void NestedAttributeAccessibleOnOuterType()
		//		{
		//			var markup = @"using System;
		//
		//[My]
		//class Y
		//{
		//
		//}
		//
		//[$$]
		//class X
		//{
		//    [My]
		//    class MyAttribute : Attribute
		//    {
		//
		//    }
		//}";
		//			VerifyItemExists(markup, "My");
		//		}
		//
		//		
		//		[Test]
		//		public void NoTypeParametersDefinedInCrefs()
		//		{
		//			var markup = @"using System;
		//
		///// <see cref=""Program{T$$}""/>
		//class Program<T> { }";
		//			VerifyItemIsAbsent(markup, "T");
		//		}
		//
		//		
		//		[Test]
		//		public void ShowTypesInGenericMethodTypeParameterList1()
		//		{
		//			var markup = @"
		//class Class1<T, D>
		//{
		//    public static Class1<T, D> Create() { return null; }
		//}
		//static class Class2
		//{
		//    public static void Test<T,D>(this Class1<T, D> arg)
		//    {
		//    }
		//}
		//class Program
		//{
		//    static void Main(string[] args)
		//    {
		//        Class1<string, int>.Create().Test<$$
		//    }
		//}
		//";
		//			VerifyItemExists(markup, "Class1<>", sourceCodeKind: SourceCodeKind.Regular);
		//		}
		//
		//		
		//		[Test]
		//		public void ShowTypesInGenericMethodTypeParameterList2()
		//		{
		//			var markup = @"
		//class Class1<T, D>
		//{
		//    public static Class1<T, D> Create() { return null; }
		//}
		//static class Class2
		//{
		//    public static void Test<T,D>(this Class1<T, D> arg)
		//    {
		//    }
		//}
		//class Program
		//{
		//    static void Main(string[] args)
		//    {
		//        Class1<string, int>.Create().Test<string,$$
		//    }
		//}
		//";
		//			VerifyItemExists(markup, "Class1<>", sourceCodeKind: SourceCodeKind.Regular);
		//		}
		//
		//		
		//		[Test]
		//		public void DescriptionInAlaisedType()
		//		{
		//			var markup = @"
		//using IAlias = IFoo;
		/////<summary>summary for interface IFoo</summary>
		//interface IFoo {  }
		//class C 
		//{ 
		//    I$$
		//}
		//";
		//			VerifyItemExists(markup, "IAlias", expectedDescriptionOrNull: "interface IFoo\r\nsummary for interface IFoo");
		//		}
		//
		//		[Test]
		//		public void WithinNameOf()
		//		{
		//			var markup = @"
		//class C 
		//{ 
		//    void foo()
		//    {
		//        var x = nameof($$)
		//    }
		//}
		//";
		//			VerifyAnyItemExists(markup);
		//		}
		//
		//		
		//		[Test]
		//		public void InstanceMemberInNameOfInStaticContext()
		//		{
		//			var markup = @"
		//class C
		//{
		//  int y1 = 15;
		//  static int y2 = 1;
		//  static string x = nameof($$
		//";
		//			VerifyItemExists(markup, "y1");
		//		}
		//
		//		
		//		[Test]
		//		public void StaticMemberInNameOfInStaticContext()
		//		{
		//			var markup = @"
		//class C
		//{
		//  int y1 = 15;
		//  static int y2 = 1;
		//  static string x = nameof($$
		//";
		//			VerifyItemExists(markup, "y2");
		//		}
		//
		//		
		//		[Test]
		//		public void IncompleteDeclarationExpressionType()
		//		{
		//			var markup = @"
		//using System;
		//class C
		//{
		//  void foo()
		//    {
		//        var x = Console.$$
		//        var y = 3;
		//    }
		//}
		//";
		//			VerifyItemExists(markup, "WriteLine", experimental: true);
		//		}
		//
		//		
		//		[Test]
		//		public void StaticAndInstanceInNameOf()
		//		{
		//			var markup = @"
		//using System;
		//class C
		//{
		//    class D
		//    {
		//        public int x;
		//        public static int y;   
		//    }
		//
		//  void foo()
		//    {
		//        var z = nameof(C.D.$$
		//    }
		//}
		//";
		//			VerifyItemExists(markup, "x");
		//			VerifyItemExists(markup, "y");
		//		}
		//
		//		
		//		[Test]
		//		public void NameOfMembersListedOnlyForNamespacesAndTypes1()
		//		{
		//			var markup = @"class C
		//{
		//    void M()
		//    {
		//        var x = nameof(T.z$$)
		//    }
		//}
		// 
		//public class T
		//{
		//    public U z; 
		//}
		// 
		//public class U
		//{
		//    public int nope;
		//}
		//";
		//			VerifyItemIsAbsent(markup, "nope");
		//		}
		//
		//		
		//		[Test]
		//		public void NameOfMembersListedOnlyForNamespacesAndTypes2()
		//		{
		//			var markup = @"class C
		//{
		//    void M()
		//    {
		//        var x = nameof(U.$$)
		//    }
		//}
		// 
		//public class T
		//{
		//    public U z; 
		//}
		// 
		//public class U
		//{
		//    public int nope;
		//}
		//";
		//			VerifyItemExists(markup, "nope");
		//		}
		//
		//		
		//		[Test]
		//		public void NameOfMembersListedOnlyForNamespacesAndTypes3()
		//		{
		//			var markup = @"class C
		//{
		//    void M()
		//    {
		//        var x = nameof(N.$$)
		//    }
		//}
		//
		//namespace N
		//{
		//public class U
		//{
		//    public int nope;
		//}
		//} ";
		//			VerifyItemExists(markup, "U");
		//		}
		//
		//		
		//		[Test]
		//		public void NameOfMembersListedOnlyForNamespacesAndTypes4()
		//		{
		//			var markup = @"
		//using z = System;
		//class C
		//{
		//    void M()
		//    {
		//        var x = nameof(z.$$)
		//    }
		//}
		//";
		//			VerifyItemExists(markup, "Console");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings1()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $""{$$
		//";
		//			VerifyItemExists(markup, "a");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings2()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $""{$$}"";
		//    }
		//}";
		//			VerifyItemExists(markup, "a");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings3()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $""{a}, {$$
		//";
		//			VerifyItemExists(markup, "b");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings4()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $""{a}, {$$}"";
		//    }
		//}";
		//			VerifyItemExists(markup, "b");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings5()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $@""{a}, {$$
		//";
		//			VerifyItemExists(markup, "b");
		//		}
		//
		//		[Test]
		//		public void InterpolatedStrings6()
		//		{
		//			var markup = @"
		//class C
		//{
		//    void M()
		//    {
		//        var a = ""Hello"";
		//        var b = ""World"";
		//        var c = $@""{a}, {$$}"";
		//    }
		//}";
		//			VerifyItemExists(markup, "b");
		//		}
		//
		//		
		//		[Test]
		//		public void NotBeforeFirstStringHole()
		//		{
		//			VerifyNoItemsExist(AddInsideMethod(
		//				@"var x = ""\{0}$$\{1}\{2}"""));
		//		}
		//
		//		
		//		[Test]
		//		public void NotBetweenStringHoles()
		//		{
		//			VerifyNoItemsExist(AddInsideMethod(
		//				@"var x = ""\{0}\{1}$$\{2}"""));
		//		}
		//
		//		
		//		[Test]
		//		public void NotAfterStringHoles()
		//		{
		//			VerifyNoItemsExist(AddInsideMethod(
		//				@"var x = ""\{0}\{1}\{2}$$"""));
		//		}
		//
		//		
		//		[Test]
		//		public void CompletionAfterTypeOfGetType()
		//		{
		//			VerifyItemExists(AddInsideMethod(
		//				"typeof(int).GetType().$$"), "GUID");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives1()
		//		{
		//			var markup = @"
		//using $$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemIsAbsent(markup, "A");
		//			VerifyItemIsAbsent(markup, "B");
		//			VerifyItemExists(markup, "N");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives2()
		//		{
		//			var markup = @"
		//using N.$$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemIsAbsent(markup, "C");
		//			VerifyItemIsAbsent(markup, "D");
		//			VerifyItemExists(markup, "M");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives3()
		//		{
		//			var markup = @"
		//using G = $$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "A");
		//			VerifyItemExists(markup, "B");
		//			VerifyItemExists(markup, "N");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives4()
		//		{
		//			var markup = @"
		//using G = N.$$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "C");
		//			VerifyItemExists(markup, "D");
		//			VerifyItemExists(markup, "M");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives5()
		//		{
		//			var markup = @"
		//using static $$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "A");
		//			VerifyItemExists(markup, "B");
		//			VerifyItemExists(markup, "N");
		//		}
		//
		//		[Test]
		//		public void UsingDirectives6()
		//		{
		//			var markup = @"
		//using static N.$$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "C");
		//			VerifyItemExists(markup, "D");
		//			VerifyItemExists(markup, "M");
		//		}
		//
		//		[Test]
		//		public void UsingStaticDoesNotShowDelegates1()
		//		{
		//			var markup = @"
		//using static $$
		//
		//class A { }
		//delegate void B();
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "A");
		//			VerifyItemIsAbsent(markup, "B");
		//			VerifyItemExists(markup, "N");
		//		}
		//
		//		[Test]
		//		public void UsingStaticDoesNotShowDelegates2()
		//		{
		//			var markup = @"
		//using static N.$$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    delegate void D();
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "C");
		//			VerifyItemIsAbsent(markup, "D");
		//			VerifyItemExists(markup, "M");
		//		}
		//
		//		[Test]
		//		public void UsingStaticDoesNotShowInterfaces1()
		//		{
		//			var markup = @"
		//using static N.$$
		//
		//class A { }
		//static class B { }
		//
		//namespace N
		//{
		//    class C { }
		//    interface I { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "C");
		//			VerifyItemIsAbsent(markup, "I");
		//			VerifyItemExists(markup, "M");
		//		}
		//
		//		[Test]
		//		public void UsingStaticDoesNotShowInterfaces2()
		//		{
		//			var markup = @"
		//using static $$
		//
		//class A { }
		//interface I { }
		//
		//namespace N
		//{
		//    class C { }
		//    static class D { }
		//
		//    namespace M { }
		//}";
		//
		//			VerifyItemExists(markup, "A");
		//			VerifyItemIsAbsent(markup, "I");
		//			VerifyItemExists(markup, "N");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods1()
		//		{
		//			var markup = @"
		//using static A;
		//using static B;
		//
		//static class A
		//{
		//    public static void Foo(this string s) { }
		//}
		//
		//static class B
		//{
		//    public static void Bar(this string s) { }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        $$
		//    }
		//}
		//";
		//
		//			VerifyItemIsAbsent(markup, "Foo");
		//			VerifyItemIsAbsent(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods2()
		//		{
		//			var markup = @"
		//using N;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        $$
		//    }
		//}
		//";
		//
		//			VerifyItemIsAbsent(markup, "Foo");
		//			VerifyItemIsAbsent(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods3()
		//		{
		//			var markup = @"
		//using N;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        string s;
		//        s.$$
		//    }
		//}
		//";
		//
		//			VerifyItemExists(markup, "Foo");
		//			VerifyItemExists(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods4()
		//		{
		//			var markup = @"
		//using static N.A;
		//using static N.B;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        string s;
		//        s.$$
		//    }
		//}
		//";
		//
		//			VerifyItemExists(markup, "Foo");
		//			VerifyItemExists(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods5()
		//		{
		//			var markup = @"
		//using static N.A;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        string s;
		//        s.$$
		//    }
		//}
		//";
		//
		//			VerifyItemExists(markup, "Foo");
		//			VerifyItemIsAbsent(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods6()
		//		{
		//			var markup = @"
		//using static N.B;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        string s;
		//        s.$$
		//    }
		//}
		//";
		//
		//			VerifyItemIsAbsent(markup, "Foo");
		//			VerifyItemExists(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void UsingStaticAndExtensionMethods7()
		//		{
		//			var markup = @"
		//using N;
		//using static N.B;
		//
		//namespace N
		//{
		//    static class A
		//    {
		//        public static void Foo(this string s) { }
		//    }
		//
		//    static class B
		//    {
		//        public static void Bar(this string s) { }
		//    }
		//}
		//
		//class C
		//{
		//    void M()
		//    {
		//        string s;
		//        s.$$;
		//    }
		//}
		//";
		//
		//			VerifyItemExists(markup, "Foo");
		//			VerifyItemExists(markup, "Bar");
		//		}
		//
		//		[Test]
		//		public void ExceptionFilter1()
		//		{
		//			var markup = @"
		//using System;
		//
		//class C
		//{
		//    void M(bool x)
		//    {
		//        try
		//        {
		//        }
		//        catch when ($$
		//";
		//
		//			VerifyItemExists(markup, "x");
		//		}
		//
		//		[Test]
		//		public void ExceptionFilter2()
		//		{
		//			var markup = @"
		//using System;
		//
		//class C
		//{
		//    void M(bool x)
		//    {
		//        try
		//        {
		//        }
		//        catch (Exception ex) when ($$
		//";
		//
		//			VerifyItemExists(markup, "x");
		//		}
*/
	}
}
