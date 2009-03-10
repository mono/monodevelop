// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3824 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.PrettyPrinter
{
	[TestFixture]
	public class SpecialOutputVisitorTest
	{
		void TestProgram(string program)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor();
			outputVisitor.Options.IndentationChar = ' ';
			outputVisitor.Options.TabSize = 2;
			outputVisitor.Options.IndentSize = 2;
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(),
			                                    outputVisitor)) {
				outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(program.Replace("\r", ""), outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		void TestProgramVB(string program)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.VBNet, new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			outputVisitor.Options.IndentationChar = ' ';
			outputVisitor.Options.TabSize = 2;
			outputVisitor.Options.IndentSize = 2;
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(),
			                                    outputVisitor)) {
				outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(program.Replace("\r", ""), outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		void TestProgramCS2VB(string programCS, string programVB)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(programCS));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			List<ISpecial> specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
			PreprocessingDirective.CSharpToVB(specials);
			outputVisitor.Options.IndentationChar = ' ';
			outputVisitor.Options.IndentSize = 2;
			using (SpecialNodesInserter.Install(specials, outputVisitor)) {
				outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(programVB.Replace("\r", ""), outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		[Test]
		public void BlankLine()
		{
			TestProgram("using A;\n\nusing B;");
		}
		
		[Test]
		public void BlankLineAtBeginning()
		{
			TestProgram("\nusing A;\n\nusing B;");
		}
		
		[Test]
		public void SimpleComments()
		{
			TestProgram("// before class\n" +
			            "class A\n" +
			            "{\n" +
			            "  // in class\n" +
			            "}\n" +
			            "// after class");
		}
		
		[Test]
		public void BlockComment()
		{
			TestProgram("/* before class */\n" +
			            "class A\n" +
			            "{\n" +
			            "  /* in class */\n" +
			            "}\n" +
			            "/* after class */");
		}
		
		[Test]
		public void ComplexCommentMix()
		{
			TestProgram("/* before class */\n" +
			            "// line comment before\n" +
			            "/* block comment before */\n" +
			            "class A\n" +
			            "{\n" +
			            "  /* in class */\n" +
			            "  // in class 2" +
			            "  /* in class 3 */\n" +
			            "}\n" +
			            "/* after class */\n" +
			            "// after class 2\n" +
			            "/* after class 3*/");
		}
		
		[Test]
		public void PreProcessing()
		{
			TestProgram("#if WITH_A\n" +
			            "class A\n" +
			            "{\n" +
			            "}\n" +
			            "#end if");
		}
		
		[Test]
		public void Enum()
		{
			TestProgram("enum Test\n" +
			            "{\n" +
			            "  // a\n" +
			            "  m1,\n" +
			            "  // b\n" +
			            "  m2\n" +
			            "  // c\n" +
			            "}\n" +
			            "// d");
		}
		
		[Test]
		public void EnumVB()
		{
			TestProgramVB("Enum Test\n" +
			              "  ' a\n" +
			              "  m1\n" +
			              "  ' b\n" +
			              "  m2\n" +
			              "  ' c\n" +
			              "End Enum\n" +
			              "' d");
		}
		
		[Test]
		public void RegionInsideMethod()
		{
			TestProgram(@"public class Class1
{
  private bool test(int l, int lvw)
  {
    #region Metodos Auxiliares
    int i = 1;
    return false;
    #endregion
  }
}");
		}
		
		[Test]
		public void CommentsInsideMethodVB()
		{
			TestProgramVB(@"Public Class Class1
  Private Function test(l As Integer, lvw As Integer) As Boolean
    ' Begin
    Dim i As Integer = 1
    Return False
    ' End of method
  End Function
End Class");
		}
		
		[Test]
		public void BlankLinesVB()
		{
			TestProgramVB("Imports System\n" +
			              "\n" +
			              "Imports System.IO");
			TestProgramVB("Imports System\n" +
			              "\n" +
			              "\n" +
			              "Imports System.IO");
			TestProgramVB("\n" +
			              "' Some comment\n" +
			              "\n" +
			              "Imports System.IO");
		}
		
		[Test]
		public void CommentAfterAttribute()
		{
			TestProgramCS2VB("class A { [PreserveSig] public void B(// comment\nint c) {} }",
			                 "Class A\n" +
			                 "  ' comment\n" +
			                 "  <PreserveSig> _\n" +
			                 "  Public Sub B(c As Integer)\n" +
			                 "  End Sub\n" +
			                 "End Class");
		}
		
		[Test]
		public void ConditionalAttribute()
		{
			TestProgram("class A\n" +
			            "{\n" +
			            "  #if TEST\n" +
			            "  [MyAttribute()]\n" +
			            "  #endif\n" +
			            "  public int Field;\n" +
			            "}\n" +
			            "#end if");
		}
		
		[Test]
		public void ConditionalCompilationCS2VB()
		{
			TestProgramCS2VB("class A\n" +
			                 "{\n" +
			                 "  #if TEST\n" +
			                 "  public int Field;\n" +
			                 "  #endif\n" +
			                 "}",
			                 "Class A\n" +
			                 "  #If TEST Then\n" +
			                 "  Public Field As Integer\n" +
			                 "  #End If\n" +
			                 "End Class");
		}
		
		[Test]
		public void RegionInsideMethodCS2VB()
		{
			TestProgramCS2VB("class A { void M() {\n" +
			                 "  #region PP\n" +
			                 "  return;" +
			                 "  #endregion\n" +
			                 "} }",
			                 "Class A\n" +
			                 "  Sub M()\n" +
			                 "    '#Region \"PP\"\n" +
			                 "    Return\n" +
			                 "    '#End Region\n" +
			                 "  End Sub\n" +
			                 "End Class");
		}
	}
}
