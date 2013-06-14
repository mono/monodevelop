using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	[TestFixture]
	public class ActiveElementStartPathTestFixture 
	{		
		XmlElementPath elementPath;
		XmlElementPath expectedElementPath;
		string namespaceURI = "http://foo.com/foo.xsd";
		
		[Test]
		public void PathTest1()
		{
			string text = "<foo xmlns='" + namespaceURI + "' ";
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(elementPath.Equals(expectedElementPath), 
			              "Incorrect active element path.");
		}		
		
		[Test]
		public void PathTest2()
		{
			string text = "<foo xmlns='" + namespaceURI + "' ><bar ";
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI));
			Assert.IsTrue(expectedElementPath.Equals(elementPath), 
			              "Incorrect active element path.");
		}			
		
		[Test]
		public void PathTest3()
		{
			string text = "<f:foo xmlns:f='" + namespaceURI + "' ><f:bar ";
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI, "f"));
			expectedElementPath.Elements.Add(new QualifiedName("bar", namespaceURI, "f"));
			Assert.IsTrue(expectedElementPath.Equals(elementPath), 
			              "Incorrect active element path.");
		}		
		
		[Test]
		public void PathTest4()
		{
			string text = "<x:foo xmlns:x='" + namespaceURI + "' ";
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI, "x"));
			Assert.IsTrue(expectedElementPath.Equals(elementPath), 
			              "Incorrect active element path.");
		}	
		
		[Test]
		public void PathTest5()
		{
			string text = "<foo xmlns='" + namespaceURI + "'>\r\n"+ 
							"<y\r\n" +
						  	"Id = 'foo' ";
	
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			expectedElementPath.Elements.Add(new QualifiedName("y", namespaceURI));
			Assert.IsTrue(expectedElementPath.Equals(elementPath), 
			              "Incorrect active element path.");
		}
		
		[Test]
		public void PathTest6()
		{
			string text = "<bar xmlns='http://bar'>\r\n" +
							"<foo xmlns='" + namespaceURI + "' ";
	
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			
			expectedElementPath = new XmlElementPath();
			expectedElementPath.Elements.Add(new QualifiedName("foo", namespaceURI));
			Assert.IsTrue(expectedElementPath.Equals(elementPath), 
			              "Incorrect active element path.");
		}
		
		/// <summary>
		/// Tests that we get no path back if we are outside the start
		/// tag.
		/// </summary>
		[Test]
		public void OutOfStartTagPathTest1()
		{
			string text = "<foo xmlns='" + namespaceURI + "'> ";
	
			elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
			Assert.AreEqual(0, elementPath.Elements.Count, "Should have no path.");
		}
	}
}
