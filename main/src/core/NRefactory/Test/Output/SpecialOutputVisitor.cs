// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1301 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;
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
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(),
			                                    outputVisitor)) {
				outputVisitor.Visit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(program, outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		void TestProgramVB(string program)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.VBNet, new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(),
			                                    outputVisitor)) {
				outputVisitor.Visit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(program, outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		[Test]
		public void SimpleComments()
		{
			TestProgram("// before class\n" +
			            "class A\n" +
			            "{\n" +
			            "\t// in class\n" +
			            "}\n" +
			            "// after class");
		}
		
		[Test, Ignore("Requires BlankLine to work correctly")]
		public void BlockComment()
		{
			TestProgram("/* before class */\n" +
			            "class A\n" +
			            "{\n" +
			            "\t/* in class */\n" +
			            "}\n" +
			            "/* after class */");
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
			            "\t// a\n" +
			            "\tm1,\n" +
			            "\t// b\n" +
			            "\tm2\n" +
			            "\t// c\n" +
			            "}\n" +
			            "// d");
		}
		
		[Test]
		public void EnumVB()
		{
			TestProgramVB("Enum Test\n" +
			            "\t' a\n" +
			            "\tm1\n" +
			            "\t' b\n" +
			            "\tm2\n" +
			            "\t' c\n" +
			            "End Enum\n" +
			            "' d");
		}
	}
}
