
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Paths
{
	[TestFixture]
	public class NoElementPathTestFixture
	{
		XmlElementPath path;
		
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			path = new XmlElementPath();
		}
		
		[Test]
		public void HasNoItems()
		{
			Assert.AreEqual(0, path.Elements.Count, 
			                "Should not be any elements.");
		}
		
		[Test]
		public void Equality()
		{
			XmlElementPath newPath = new XmlElementPath();
			
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
