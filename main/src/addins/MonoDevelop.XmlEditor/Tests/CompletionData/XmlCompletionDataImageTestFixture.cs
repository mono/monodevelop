
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;
using System.IO;
using System.Xml;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.CompletionData
{	
	[TestFixture]
	public class XmlCompletionDataImageTestFixture
	{
		[Test]
		public void ImageNotNull()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlElement);
			Assert.IsNotNull(data.Image);
		}
		
		[Test]
		public void ImageNotEmptyString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlElement);
			Assert.IsTrue(data.Image.Length > 0);
		}
	}
}
