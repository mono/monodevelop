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
	public class ParseUtilCSharp
	{
		public static T ParseGlobal<T>(string program) where T : INode
		{
			return ParseGlobal<T>(program, false);
		}
		
		public static T ParseGlobal<T>(string program, bool expectError) where T : INode
		{
			return ParseGlobal<T>(program, expectError, false);
		}
		
		public static T ParseGlobal<T>(string program, bool expectError, bool skipMethodBodies) where T : INode
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(program));
			parser.ParseMethodBodies = !skipMethodBodies;
			parser.Parse();
			Assert.IsNotNull(parser.Errors);
			if (expectError)
				Assert.IsTrue(parser.Errors.ErrorOutput.Length > 0, "There were errors expected, but parser finished without errors.");
			else
				Assert.AreEqual("", parser.Errors.ErrorOutput);
			Assert.IsNotNull(parser.CompilationUnit);
			Assert.IsNotNull(parser.CompilationUnit.Children);
			Assert.IsNotNull(parser.CompilationUnit.Children[0]);
			Assert.IsTrue(parser.CompilationUnit.Children.Count > 0);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(parser.CompilationUnit.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parser.CompilationUnit.Children[0].GetType(), type, parser.CompilationUnit.Children[0]));
			return (T)parser.CompilationUnit.Children[0];
		}
		
		public static T ParseTypeMember<T>(string typeMember) where T : INode
		{
			return ParseTypeMember<T>(typeMember, false);
		}
		
		public static T ParseTypeMember<T>(string typeMember, bool expectError) where T : INode
		{
			TypeDeclaration td = ParseGlobal<TypeDeclaration>("class MyClass {" + typeMember + "}", expectError);
			Assert.IsTrue(td.Children.Count > 0);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(td.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", td.GetType(), type, td));
			return (T)td.Children[0];
		}
		
		public static T ParseStatement<T>(string statement) where T : INode
		{
			MethodDeclaration md = ParseTypeMember<MethodDeclaration>("void A() { " + statement + " }");
			Assert.IsTrue(md.Body.Children.Count > 0);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(md.Body.Children[0].GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", md.GetType(), type, md));
			return (T)md.Body.Children[0];
		}
		
		public static T ParseExpression<T>(string expr) where T : INode
		{
			return ParseExpression<T>(expr, false);
		}
		
		public static T ParseExpression<T>(string expr, bool expectErrors) where T : INode
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(expr + ";"));
			object parsedExpression = parser.ParseExpression();
			if (expectErrors)
				Assert.IsTrue(parser.Errors.ErrorOutput.Length > 0, "There were errors expected, but parser finished without errors.");
			else
				Assert.AreEqual("", parser.Errors.ErrorOutput);
			Type type = typeof(T);
			Assert.IsTrue(type.IsAssignableFrom(parsedExpression.GetType()), String.Format("Parsed expression was {0} instead of {1} ({2})", parsedExpression.GetType(), type, parsedExpression));
			return (T)parsedExpression;
		}
	}
}
