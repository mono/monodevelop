using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;
using System;

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
