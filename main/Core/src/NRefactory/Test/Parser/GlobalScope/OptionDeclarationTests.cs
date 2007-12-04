// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Drawing;
using System.IO;

using NUnit.Framework;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class OptionDeclarationTests
	{
		[Test]
		public void VBNetStrictOptionDeclarationTest()
		{
			string program = "Option Strict On\n";
			OptionDeclaration opDec = ParseUtilVBNet.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Strict, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetExplicitOptionDeclarationTest()
		{
			string program = "Option Explicit Off\n";
			OptionDeclaration opDec = ParseUtilVBNet.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.Explicit, opDec.OptionType);
			Assert.IsFalse(opDec.OptionValue, "Off option value excepted!");
		}
		
		[Test]
		public void VBNetCompareBinaryOptionDeclarationTest()
		{
			string program = "Option Compare Binary\n";
			OptionDeclaration opDec = ParseUtilVBNet.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.CompareBinary, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetCompareTextOptionDeclarationTest()
		{
			string program = "Option Compare Text\n";
			OptionDeclaration opDec = ParseUtilVBNet.ParseGlobal<OptionDeclaration>(program);
			Assert.AreEqual(OptionType.CompareText, opDec.OptionType);
			Assert.IsTrue(opDec.OptionValue);
		}
		
		[Test]
		public void VBNetInvalidOptionDeclarationTest()
		{
			string program = "Option\n";
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.VBNet, new StringReader(program));
			parser.Parse();
			Assert.IsFalse(parser.Errors.ErrorOutput.Length == 0, "Expected errors, but operation completed successfully");
		}
	}
}
