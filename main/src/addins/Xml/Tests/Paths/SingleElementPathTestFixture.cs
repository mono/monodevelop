
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Paths
{
	[TestFixture]
	public class SingleElementPathTestFixture
	{
		XmlElementPath path;
		QualifiedName qualifiedName;
		
		[SetUp]
		public void Init()
		{
			path = new XmlElementPath();
			qualifiedName = new QualifiedName("foo", "http://foo");
			path.Elements.Add(qualifiedName);
		}
		
		[Test]
		public void HasOneItem()
		{
			Assert.AreEqual(1, path.Elements.Count,  "Should have 1 element.");
		}
		
		[Test]
		public void Equality()
		{
			XmlElementPath newPath = new XmlElementPath();
			newPath.Elements.Add(new QualifiedName("foo", "http://foo"));
			
			Assert.IsTrue(newPath.Equals(path), "Should be equal.");
		}
		
		[Test]
		public void NotEqual()
		{
			XmlElementPath newPath = new XmlElementPath();
			newPath.Elements.Add(new QualifiedName("Foo", "bar"));
			
			Assert.IsFalse(newPath.Equals(path), "Should not be equal.");
		}	
		
		[Test]
		public void Compact()
		{
			path.Compact();
			Equality();
		}
	}
}
