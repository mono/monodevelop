using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

using NUnit.Framework;

namespace CSharpBinding.Test
{
	[TestFixture]
	public class ResolverTest
	{
		string testFileName = "Test.cs";
		string testFileContents = @"
using System;
using System.IO;

public class C
{
	public static void Main()
	{
	}
}
";
		private IParserContext parserContext;

		[TestFixtureSetUp]
		public void SetUp()
		{
			Runtime.Initialize();
			Runtime.AddInService.PreloadAddin(null, "MonoDevelop.Projects");

			TextWriter tw = new StreamWriter(testFileName);
			tw.Write(testFileContents);
			tw.Close();

			IParserDatabase pdb = Services.ParserService.CreateParserDatabase();
			parserContext = pdb.GetFileParserContext(testFileName);
			parserContext.ParseFile(testFileName, testFileContents);
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			File.Delete(testFileName);
		}

		[Test]
		public void ResolveNamespace()
		{
			ILanguageItem languageItem = parserContext.ResolveIdentifier("System", 1, 9, testFileName, testFileContents);
			Assert.IsTrue(languageItem is Namespace);
			Assert.AreEqual("System", (languageItem as Namespace).Name);

			languageItem = parserContext.ResolveIdentifier("System.IO", 2, 14, testFileName, testFileContents);
			Assert.IsTrue(languageItem is Namespace);
			Assert.AreEqual("System.IO", (languageItem as Namespace).Name);
		}
	}
}




