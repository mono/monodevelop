// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision: 2657 $</version>
// </file>

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.Output
{
	[TestFixture]
	public class SnippetConversion
	{
		void CS2VB(string input, string expectedOutput)
		{
			SnippetParser parser = new SnippetParser(SupportedLanguage.CSharp);
			INode node = parser.Parse(input);
			// parser.Errors.ErrorOutput contains syntax errors, if any
			Assert.IsNotNull(node);
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			// parser.Specials is the list of comments, preprocessor directives etc.
			PreprocessingDirective.CSharpToVB(parser.Specials);
			// Convert C# constructs to VB.NET:
			node.AcceptVisitor(new CSharpConstructsConvertVisitor(), null);
			node.AcceptVisitor(new ToVBNetConvertVisitor(), null);

			VBNetOutputVisitor output = new VBNetOutputVisitor();
			using (SpecialNodesInserter.Install(parser.Specials, output)) {
				node.AcceptVisitor(output, null);
			}
			// output.Errors.ErrorOutput contains conversion errors/warnings, if any
			// output.Text contains the converted code
			Assert.AreEqual("", output.Errors.ErrorOutput);
			Assert.AreEqual(expectedOutput, output.Text);
		}
		
		void VB2CS(string input, string expectedOutput)
		{
			SnippetParser parser = new SnippetParser(SupportedLanguage.VBNet);
			INode node = parser.Parse(input);
			// parser.Errors.ErrorOutput contains syntax errors, if any
			Assert.IsNotNull(node);
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			// parser.Specials is the list of comments, preprocessor directives etc.
			PreprocessingDirective.VBToCSharp(parser.Specials);
			// Convert VB.NET constructs to C#:
			node.AcceptVisitor(new VBNetConstructsConvertVisitor(), null);
			node.AcceptVisitor(new ToCSharpConvertVisitor(), null);

			CSharpOutputVisitor output = new CSharpOutputVisitor();
			using (SpecialNodesInserter.Install(parser.Specials, output)) {
				node.AcceptVisitor(output, null);
			}
			// output.Errors.ErrorOutput contains conversion errors/warnings, if any
			// output.Text contains the converted code
			Assert.AreEqual("", output.Errors.ErrorOutput);
			Assert.AreEqual(expectedOutput, output.Text);
		}
		
		[Test]
		public void CompilationUnitCS2VB()
		{
			CS2VB(
				@"using System;

public class MyClass
{
   string abc;

   public string Abc { get { return abc; } }

    // This is a test method
    static void M<T>(params T[] args) where T : IDisposable
    {
       Console.WriteLine(""Hello!"");
    }
}",

				@"Imports System

Public Class [MyClass]
	Private m_abc As String

	Public ReadOnly Property Abc() As String
		Get
			Return m_abc
		End Get
	End Property

	' This is a test method
	Private Shared Sub M(Of T As IDisposable)(ParamArray args As T())
		Console.WriteLine(""Hello!"")
	End Sub
End Class
"
			);
		}
		
		
		
		
		[Test]
		public void TypeMembersCS2VB()
		{
			CS2VB(
				"void Test() {}\n" +
				"void Test2() {}",

				@"Private Sub Test()
End Sub
Private Sub Test2()
End Sub
"
			);
		}
		
		[Test]
		public void StatementsCS2VB()
		{
			CS2VB(
				"int a = 3;\n" +
				"a++;",

				@"Dim a As Integer = 3
a += 1
"
			);
		}
		
		
		[Test]
		public void TypeMembersVB2CS()
		{
			VB2CS(
				@"Sub Test()
End Sub
Sub Test2()
End Sub
",
				@"public void Test()
{
}
public void Test2()
{
}
"
			);
		}
		
		[Test]
		public void StatementsVB2CS()
		{
			VB2CS(
				@"Dim a As Integer = 3
a += 1
",
				"int a = 3;\r\n" +
				"a += 1;\r\n"
			);
		}
	}
}
