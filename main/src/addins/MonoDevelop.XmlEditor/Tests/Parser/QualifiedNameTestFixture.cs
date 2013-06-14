
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	/// <summary>
	/// Tests the comparison of <see cref="QualifiedName"/> items.
	/// </summary>
	[TestFixture]
	public class QualifiedNameTestFixture
	{
		[Test]
		public void EqualsTest1()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com");
			QualifiedName name2 = new QualifiedName("foo", "http://foo.com");
			
			Assert.AreEqual(name1, name2, "Should be the same.");
		}
		
		[Test]
		public void EqualsTest2()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com", "f");
			QualifiedName name2 = new QualifiedName("foo", "http://foo.com", "f");
			
			Assert.AreEqual(name1, name2, "Should be the same.");
		}		
		
		[Test]
		public void EqualsTest3()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com", "f");
			QualifiedName name2 = new QualifiedName("foo", "http://foo.com", "ggg");
			
			Assert.IsTrue(name1 == name2, "Should be the same.");
		}	
		
		[Test]
		public void NotEqualsTest1()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com", "f");
			QualifiedName name2 = new QualifiedName("foo", "http://bar.com", "f");
			
			Assert.IsFalse(name1 == name2, "Should not be the same.");
		}		
		
		[Test]
		public void NotEqualsTest2()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com", "f");
			QualifiedName name2 = null; 
			
			Assert.IsFalse(name1 == name2, "Should not be the same.");
		}
		
		[Test]
		public void HashCodeTest()
		{
			QualifiedName name1 = new QualifiedName("foo", "http://foo.com", "f");
			XmlQualifiedName xmlQualifiedName = new XmlQualifiedName("foo", "http://foo.com");

			Assert.AreEqual(name1.GetHashCode(), xmlQualifiedName.GetHashCode());
		}		
	}
}
