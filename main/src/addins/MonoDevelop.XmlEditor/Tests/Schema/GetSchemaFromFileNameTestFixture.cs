using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
/*	[TestFixture]
	public class GetSchemaFromFileNameTestFixture
	{
		XmlSchemaCompletionDataCollection schemas;
		string expectedNamespace;
		XmlCompletionDataProvider provider;
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			schemas = new XmlSchemaCompletionDataCollection();
			XmlSchemaCompletionData completionData = new XmlSchemaCompletionData(ResourceManager.GetXsdSchema());
			expectedNamespace = completionData.NamespaceUri;
			completionData.FileName = @"/home/Schemas/MySchema.xsd";
			schemas.Add(completionData);

			provider = new XmlCompletionDataProvider(schemas, completionData, String.Empty, null);
		}
		
		[Test]
		public void SameFileName()
		{
			XmlSchemaCompletionData foundSchema = schemas.GetSchemaFromFileName(@"/home/Schemas/MySchema.xsd");
			Assert.AreEqual(expectedNamespace, foundSchema.NamespaceUri);
		}

		[Test]
		public void SameFileNameFromProvider()
		{
			XmlSchemaCompletionData foundSchema = provider.FindSchemaFromFileName(@"/home/Schemas/MySchema.xsd");
			Assert.AreEqual(expectedNamespace, foundSchema.NamespaceUri);
		}

		[Test]
		public void MissingFileName()
		{
			Assert.IsNull(schemas.GetSchemaFromFileName(@"/Test/test.xsd"));
		}
	}*/
}
