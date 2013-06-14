using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	/// <summary>
	/// Tests the XmlParser.GetActiveElementStartPathAtIndex which finds the element
	/// path where the index is at. The index may be in the middle or start of the element
	/// tag.
	/// </summary>
	[TestFixture]
	public class ActiveElementUnderCursorTests
	{
		XmlElementPath elementPath;
		XmlElementPath expectedElementPath;
		string namespaceURI = "http://foo.com/foo.xsd";
		
		[Test]
		public void PathTest1()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar ";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.IndexOf("bar "));
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}		
		
		[Test]
		public void PathTest2()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.IndexOf("bar>"));
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest3()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.IndexOf("ar>"));
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest4()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.Length - 1);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest5()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar a='a'>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.IndexOf("='a'"));
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest6()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar a='a'";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.Length - 1);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest7()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar a='a'";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest8()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest9()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar ";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest10()
		{
			string text = "<foo xmlns='" + namespaceURI + "'><bar Id=\r\n</foo>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, text.IndexOf("Id="));
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest11()
		{
			string text = "<foo xmlns='" + namespaceURI + "'>";
			elementPath = XmlParser.GetActiveElementStartPathAtIndex(text, 2);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}

	}
}
