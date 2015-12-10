// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn
{
	[TestFixture]
	public class KeywordCompletionProviderTests : CompletionTestBase
	{
//		internal override ICompletionProvider CreateCompletionProvider()
//		{
//			return new KeywordCompletionProvider();
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void IsCommitCharacterTest()
//		{
//			TestCommonIsCommitCharacter();
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void IsTextualTriggerCharacterTest()
//		{
//			TestCommonIsTextualTriggerCharacter();
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void SendEnterThroughToEditorTest()
//		{
//			VerifySendEnterThroughToEnter("int", "int", sendThroughEnterEnabled: false, expected: false);
//			VerifySendEnterThroughToEnter("int", "int", sendThroughEnterEnabled: true, expected: true);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void InEmptyFile()
//		{
//			var markup = "$$";
//
//			VerifyAnyItemExists(markup);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void NotInInactiveCode()
//		{
//			var markup = @"class C
//{
//    void M()
//    {
//#if false
//$$
//";
//			VerifyNoItemsExist(markup);
//		}
//
		[Test]
		public void NotInCharLiteral()
		{
			var markup = @"class C
{
    void M()
    {
        var c = '$$';
";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInUnterminatedCharLiteral()
		{
			var markup = @"class C
{
    void M()
    {
        var c = '$$   ";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInUnterminatedCharLiteralAtEndOfFile()
		{
			var markup = @"class C
{
    void M()
    {
        var c = '$$";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInString()
		{
			var markup = @"class C
{
    void M()
    {
        var s = ""$$"";
";

			VerifyNoItemsExist(markup);
		}

		//[Test]
		//public void NotInStringInDirective()
		//{
		//	var markup = "#r \"$$\"";

		//	VerifyNoItemsExist(markup, SourceCodeKind.Interactive);
		//}

		[Test]
		public void NotInUnterminatedString()
		{
			var markup = @"class C
{
    void M()
    {
        var s = ""$$   ";

			VerifyNoItemsExist(markup);
		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void NotInUnterminatedStringInDirective()
//		{
//			var markup = "#r \"$$\"";
//
//			VerifyNoItemsExist(markup, SourceCodeKind.Interactive);
//		}
//
		[Test]
		public void NotInUnterminatedStringAtEndOfFile()
		{
			var markup = @"class C
{
    void M()
    {
        var s = ""$$";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInVerbatimString()
		{
			var markup = @"class C
{
    void M()
    {
        var s = @""
$$
"";
";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInUnterminatedVerbatimString()
		{
			var markup = @"class C
{
    void M()
    {
        var s = @""
$$
";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInUnterminatedVerbatimStringAtEndOfFile()
		{
			var markup = @"class C
{
    void M()
    {
        var s = @""$$";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInSingleLineComment()
		{
			var markup = @"class C
{
    void M()
    {
        // $$
";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInSingleLineCommentAtEndOfFile()
		{
			var markup = @"namespace A
{
}// $$";

			VerifyNoItemsExist(markup);
		}

		[Test]
		public void NotInMutliLineComment()
		{
			var markup = @"class C
{
    void M()
    {
/*
    $$
*/
";

			VerifyNoItemsExist(markup);
		}

//		[WorkItem(968256)]
//		[Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
//		public void UnionOfItemsFromBothContexts()
//		{
//			var markup = @"<Workspace>
//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
//        <Document FilePath=""CurrentDocument.cs""><![CDATA[
//class C
//{
//#if FOO
//    void foo() {
//#endif

//$$

//#if FOO
//    }
//#endif
//}
//]]>
//        </Document>
//    </Project>
//    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
//        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
//    </Project>
//</Workspace>";
//			VerifyItemInLinkedFiles(markup, "public", null);
//			VerifyItemInLinkedFiles(markup, "for", null);
//		}
	}
}
