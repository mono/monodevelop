// 
// ParsingTests.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Ide.Gui.Content;

using NUnit.Framework;

namespace MonoDevelop.Xml.StateEngine
{
	
	[TestFixture]
	public class ParsingTests : UnitTests.TestBase
	{
		
		[Test]
		public void AttributeName ()
		{
			string testString = @"
<doc>
	<tag.a>
		<tag.b id=""foo"" />
	</tag.a>
</doc>
";
			Parser parser = new Parser (new XmlFreeState(), false);
			for (int i = 0; i < 30; i++)
				parser.Push (testString[i]);
			
			Assert.AreEqual (parser.Position, 30);
			Assert.IsTrue (parser.CurrentState is XmlDoubleQuotedAttributeValueState);
			XAttribute attr = parser.Nodes.Peek () as XAttribute;
			Assert.IsNotNull (attr);
			Assert.AreEqual (attr.Name.FullName, "id");
		}
		
		[Test]
		public void Unclosed ()
		{
			string testString = @"
<doc>
	<tag.a>
		<tag.b><tag.b>
	</tag.a>
</doc>
";
			Parser parser = new Parser (new XmlFreeState(), false);
			int errorCount = 0;
			parser.ErrorLogged += delegate { errorCount++; };
			
			MoveParser (parser, testString, 33);
			Assert.IsTrue (parser.CurrentState is XmlFreeState);
			Assert.AreEqual (parser.Nodes.Count, 5);
			AssertName (parser.Nodes.Peek () as XElement, "tag.b");
			
			MoveParser (parser, testString, 10);
			Assert.IsTrue (parser.CurrentState is XmlFreeState);
			Assert.AreEqual (parser.Nodes.Count, 2);
			AssertName (parser.Nodes.Peek () as XElement, "doc");
			
			Assert.AreEqual (errorCount, 2);
		}
		
		void MoveParser (Parser parser, string str, int count)
		{
			int end = parser.Position + count;
			for (int i = parser.Position; i < end; i++)
				parser.Push (str[i]);
			Assert.AreEqual (parser.Position, end);
		}
		
		void AssertName<T> (T node, string name) where T : INamedXObject
		{
			Assert.IsNotNull (node);
			Assert.AreEqual (node.Name.FullName, name);
		}
	}
}
