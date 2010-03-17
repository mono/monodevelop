
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Xml;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.Schema.Uri
{
	/// <summary>
	/// Tests the <see cref="XmlSchemaCompletionData.GetUri"/> method.
	/// </summary>
	[TestFixture]
	public class GetUriTestFixture
	{
		[Test]
		public void SimpleFileName()
		{
			string fileName = @"C:\temp\foo.xml";
			string expectedUri = "file:///C:/temp/foo.xml";
			
			Assert.AreEqual(expectedUri, XmlSchemaCompletionData.GetUri(fileName));
		}
		
		[Test]
		public void NullFileName()
		{
			Assert.AreEqual(String.Empty, XmlSchemaCompletionData.GetUri(null));
		}
		
		[Test]
		public void EmptyString()
		{
			Assert.AreEqual(String.Empty, XmlSchemaCompletionData.GetUri(String.Empty));
		}
	}
}
