// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn
{
	[TestFixture]
	public class EnumAndCompletionListTagCompletionProviderTests : CompletionTestBase
	{
		[Test]
		public void NullableEnum()
		{
			var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors? d = $$
        Colors c = Colors.Blue;
    }
}
 
enum Colors
{
    Red,
    Blue,
    Green,
}
";
			VerifyItemExists(markup, "Colors");
		}

//		[Fact]
//		[WorkItem(545678)]
//		[Trait(Traits.Feature, Traits.Features.Completion)]
//		public void EditorBrowsable_EnumMemberAlways()
//		{
//			var markup = @"
//class Program
//{
//    public void M()
//    {
//        Foo d = $$
//    }
//}
//";
//			var referencedCode = @"
//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
//public enum Foo
//{
//    Member
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
//
//		[Fact]
//		[WorkItem(545678)]
//		[Trait(Traits.Feature, Traits.Features.Completion)]
//		public void EditorBrowsable_EnumMemberNever()
//		{
//			var markup = @"
//class Program
//{
//    public void M()
//    {
//        Foo d = $$
//    }
//}
//";
//			var referencedCode = @"
//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
//public enum Foo
//{
//    Member
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
//
//		[Fact]
//		[WorkItem(545678)]
//		[Trait(Traits.Feature, Traits.Features.Completion)]
//		public void EditorBrowsable_EnumMemberAdvanced()
//		{
//			var markup = @"
//class Program
//{
//    public void M()
//    {
//        Foo d = $$
//    }
//}
//";
//			var referencedCode = @"
//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
//public enum Foo
//{
//    Member
//}";
//			VerifyItemInEditorBrowsableContexts(
//				markup: markup,
//				referencedCode: referencedCode,
//				item: "Foo",
//				expectedSymbolsSameSolution: 1,
//				expectedSymbolsMetadataReference: 0,
//				sourceLanguage: LanguageNames.CSharp,
//				referencedLanguage: LanguageNames.CSharp,
//				hideAdvancedMembers: true);
//
//			VerifyItemInEditorBrowsableContexts(
//				markup: markup,
//				referencedCode: referencedCode,
//				item: "Foo",
//				expectedSymbolsSameSolution: 1,
//				expectedSymbolsMetadataReference: 1,
//				sourceLanguage: LanguageNames.CSharp,
//				referencedLanguage: LanguageNames.CSharp,
//				hideAdvancedMembers: false);
//		}
//
		[Test]
		public void NotInComment()
		{
			var markup = @"class Program
{
    static void Main(string[] args)
    {
        Colors c = // $$
    }
}
 
enum Colors
{
    Red,
    Blue,
    Green,
}
";
			VerifyNoItemsExist(markup);
		}

		[Test]
		public void InYieldReturn()
		{
			var markup =
				@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<DayOfWeek> M()
    {
        yield return $$
    }
}";
			VerifyItemExists(markup, "DayOfWeek");
		}

//		[WorkItem(827897)]
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void InAsyncMethodReturnStatement()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
//class Program
//{
//    async Task<DayOfWeek> M()
//    {
//        await Task.Delay(1);
//        return $$
//    }
//}";
//			VerifyItemExists(markup, "DayOfWeek");
//		}
//
//		[Test]
//		public void NoCompletionListTag()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
//class C
//{
//    
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyNoItemsExist(markup);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CompletionList()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
///// <completionlist cref=""C""/>
//class C
//{
//    
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyItemExists(markup, "C");
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CompletionListCrefToString()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
///// <completionlist cref=""string""/>
//class C
//{
//    
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyItemExists(markup, "string", glyph: (int)Glyph.ClassPublic);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CompletionListEmptyCref()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
///// <completionlist cref=""""/>
//class C
//{
//    
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyNoItemsExist(markup);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CompletionListInaccessibleType()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
///// <completionlist cref=""C.Inner""/>
//class C
//{
//    private class Inner
//    {   
//    }
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyNoItemsExist(markup);
//		}
//
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CompletionListNotAType()
//		{
//			var markup =
//				@"using System;
//using System.Threading.Tasks;
//
///// <completionlist cref=""C.Z()""/>
//class C
//{
//    public void Z()
//    {   
//    }
//}
//
//class Program
//{
//    void Foo()
//    {
//        C c = $$
//    }
//}";
//			VerifyNoItemsExist(markup);
//		}
//
		[Test]
		public void SuggestAlias()
		{
			var markup = @"
using D = System.Globalization.DigitShapes; 
class Program
{
    static void Main(string[] args)
    {
        D d=  $$
    }
}";
			VerifyItemExists(markup, "D");
		}

		[Test]
		public void SuggestAlias2()
		{
			var markup = @"
namespace N
{
using D = System.Globalization.DigitShapes; 

class Program
{
    static void Main(string[] args)
    {
        D d=  $$
    }
}
}
";
			VerifyItemExists(markup, "D");
		}

		[Test]
		public void SuggestAlias3()
		{
			var markup = @"
namespace N
{
using D = System.Globalization.DigitShapes; 

class Program
{
    private void Foo(System.Globalization.DigitShapes shape)
    {
    }

    static void Main(string[] args)
    {
        Foo($$
    }
}
}
";
			VerifyItemExists(markup, "D");
		}

		[Test]
		public void NotInParameterNameContext()
		{
			var markup = @"
enum E
{
    a
}

class C
{
    void foo(E first, E second) 
    {
        foo(first: E.a, $$
    }
}
";
			VerifyItemIsAbsent(markup, "E.a");
		}
	}
}
