
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	[TestFixture]
	public class ParentElementPathTestFixture 
	{		
		XmlElementPath elementPath;
		XmlElementPath expectedElementPath;
		string namespaceURI = "http://foo/foo.xsd";

		[Test]
		public void SuccessTest1()
		{
			string text = "<foo xmlns='" + namespaceURI + "' ><";
			elementPath = XmlParser.GetParentElementPath(text);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void SuccessTest2()
		{
			string text = "<foo xmlns='" + namespaceURI + "' ><bar></bar><";			
			elementPath = XmlParser.GetParentElementPath(text);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}		
		
		[Test]
		public void SuccessTest3()
		{
			string text = "<foo xmlns='" + namespaceURI + "' ><bar/><";
			elementPath = XmlParser.GetParentElementPath(text);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}			
		
		[Test]
		public void SuccessTest4()
		{
			string text = "<bar xmlns='http://test.com'><foo xmlns='" + namespaceURI + "' ><";
			elementPath = XmlParser.GetParentElementPath(text);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}				
	}
}
