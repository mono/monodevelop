// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision: 3841 $</version>
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
			@"using System;" + Environment.NewLine +
"" + Environment.NewLine +
"public class MyClass" + Environment.NewLine +
"{" + Environment.NewLine +
"   string abc;" + Environment.NewLine +
"" + Environment.NewLine +
"   public string Abc { get { return abc; } }" + Environment.NewLine +
"" + Environment.NewLine +
"    // This is a test method" + Environment.NewLine +
"    static void M<T>(params T[] args) where T : IDisposable" + Environment.NewLine +
"    {" + Environment.NewLine +
"       Console.WriteLine(\"Hello!\");" + Environment.NewLine +
"    }" + Environment.NewLine +
"}",

				@"Imports System" + Environment.NewLine +
"" + Environment.NewLine +
"Public Class [MyClass]" + Environment.NewLine +
"	Private m_abc As String" + Environment.NewLine +
"" + Environment.NewLine +
"	Public ReadOnly Property Abc() As String" + Environment.NewLine +
"		Get" + Environment.NewLine +
"			Return m_abc" + Environment.NewLine +
"		End Get" + Environment.NewLine +
"	End Property" + Environment.NewLine +
"" + Environment.NewLine +
"	' This is a test method" + Environment.NewLine +
"	Private Shared Sub M(Of T As IDisposable)(ParamArray args As T())" + Environment.NewLine +
"		Console.WriteLine(\"Hello!\")" + Environment.NewLine +
"	End Sub" + Environment.NewLine +
"End Class" + Environment.NewLine +
""
			);
		}
		
		
		
		
		[Test]
		public void TypeMembersCS2VB()
		{
			CS2VB(
				"void Test() {}" + Environment.NewLine +
				"void Test2() {}",

				@"Private Sub Test()" + Environment.NewLine +
"End Sub" + Environment.NewLine +
"Private Sub Test2()" + Environment.NewLine +
"End Sub" + Environment.NewLine 

			);
		}
		
		[Test]
		public void StatementsCS2VB()
		{
			CS2VB(
				"int a = 3;" + Environment.NewLine +
				"a++;",

				@"Dim a As Integer = 3" + Environment.NewLine +
"a += 1" + Environment.NewLine 
			);
		}
		
		
		[Test]
		public void TypeMembersVB2CS()
		{
			VB2CS(
				@"Sub Test()" + Environment.NewLine +
"End Sub" + Environment.NewLine +
"Sub Test2()" + Environment.NewLine +
"End Sub" + Environment.NewLine,
				@"public void Test()" + Environment.NewLine +
"{" + Environment.NewLine +
"}" + Environment.NewLine +
"public void Test2()" + Environment.NewLine +
"{" + Environment.NewLine +
"}" + Environment.NewLine 

			);
		}
		
		[Test]
		public void StatementsVB2CS()
		{
			VB2CS(
				@"Dim a As Integer = 3" + Environment.NewLine +
"a += 1" + Environment.NewLine,
				"int a = 3;" + Environment.NewLine +
				"a += 1;" + Environment.NewLine 
			);
		}
	}
}
