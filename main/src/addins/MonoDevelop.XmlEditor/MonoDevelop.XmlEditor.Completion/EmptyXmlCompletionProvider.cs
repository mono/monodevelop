// 
// EmptyXmlCompletionProvider.cs
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
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.XmlEditor.Completion
{
	
	
	public class EmptyXmlCompletionProvider : IXmlCompletionProvider
	{
		static CompletionData[] emptyData = new CompletionData [] {};

		public CompletionData[] GetElementCompletionData ()
		{
			return emptyData;
		}

		public CompletionData[] GetElementCompletionData (string namespacePrefix)
		{
			return emptyData;
		}

		public CompletionData[] GetChildElementCompletionData (MonoDevelop.XmlEditor.XmlElementPath path)
		{
			return emptyData;
		}

		public CompletionData[] GetAttributeCompletionData (MonoDevelop.XmlEditor.XmlElementPath path)
		{
			return emptyData;
		}

		public CompletionData[] GetAttributeValueCompletionData (MonoDevelop.XmlEditor.XmlElementPath path, string name)
		{
			return emptyData;
		}

		public CompletionData[] GetChildElementCompletionData (string tagName)
		{
			return emptyData;
		}

		public CompletionData[] GetAttributeCompletionData (string tagName)
		{
			return emptyData;
		}

		public CompletionData[] GetAttributeValueCompletionData (string tagName, string name)
		{
			return emptyData;
		}
	}
}
