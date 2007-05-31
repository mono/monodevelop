//
// ProjectReadHelper.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;

namespace MonoDevelop.Ide.Projects
{
	public static class ProjectReadHelper
	{
		public delegate bool ReaderCallback ();
		public delegate bool ReaderCallbackWithData (ReadCallbackData data);
		
		public class ReadCallbackData {
			bool skipNextRead = false;
			public bool SkipNextRead {
				get { return skipNextRead; }
				set { skipNextRead = value; }
			}
		};
		
		public static void ReadList (XmlReader reader, string endNode, ReaderCallback callback)
		{
			ReadList (reader, endNode, delegate(ReadCallbackData data) { 
				return callback ();
			});
		}
			
		public static void ReadList(XmlReader reader, string endNode, ReaderCallbackWithData callback)
		{
			if (reader.IsEmptyElement) {
				return;
			}
			ReadCallbackData data = new ReadCallbackData ();
			while (reader.Read()) {
			 skip:
				data.SkipNextRead = false;
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					if (reader.LocalName == endNode) {
						return;
					}
					// TODO: Logging system ...
					//System.Console.WriteLine ("unknown end node: " + reader.LocalName);
					break;
				case XmlNodeType.Element:
					bool validNode = callback (data);
					if (!validNode) {
						// TODO: Logging system ...
						System.Console.WriteLine ("unknown Node: " + reader.LocalName);
					}
					if (data.SkipNextRead) {
						goto skip;
					}
					break;
				}
			}
		}
	}
}
