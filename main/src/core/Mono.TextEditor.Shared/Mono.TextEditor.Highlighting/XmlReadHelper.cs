//
// XmlReadHelper.cs
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
using System.Collections.Generic;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	internal static class XmlReadHelper
	{
		public delegate bool ReaderCallback ();
		public delegate bool ReaderCallbackWithData (ReadCallbackData data);
		
		internal class ReadCallbackData {
			bool skipNextRead = false;
			public bool SkipNextRead {
				get { return skipNextRead; }
				set { skipNextRead = value; }
			}
		};
		
		public static void ReadList (XmlReader reader, string endNode, ReaderCallback callback)
		{
			ReadList (reader, new string[] { endNode }, callback);
		}
		public static void ReadList (XmlReader reader, ICollection<string> endNodes, ReaderCallback callback)
		{
			ReadList (reader, endNodes, delegate(ReadCallbackData data) { 
				return callback ();
			});
		}
			
		public static void ReadList (XmlReader reader, string endNode, ReaderCallbackWithData callback)
		{
			ReadList (reader, new string[] { endNode }, callback);		
		}
		
		static string ConcatString (ICollection<string> strings)
		{
			string[] stringArray = new string [strings.Count];
			strings.CopyTo (stringArray, 0);
			return String.Join (",", stringArray);
		}
		
		public static void ReadList (XmlReader reader, ICollection<string> endNodes, ReaderCallbackWithData callback)
		{
			if (reader.IsEmptyElement) 
				return;
			ReadCallbackData data = new ReadCallbackData ();
			bool didReadStartNode = false;
			while (reader.Read()) {
			 skip:
				data.SkipNextRead = false;
				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					if (endNodes.Contains (reader.LocalName)) 
						return;
					Console.WriteLine ("Unknown end node: " + reader.LocalName + " valid end nodes are: " + ConcatString (endNodes));
					break;
				case XmlNodeType.Element:
					if (!didReadStartNode && endNodes.Contains (reader.LocalName)) {
						didReadStartNode = true;
//						break;
					}
					bool validNode = callback (data);
					if (!validNode) 
						Console.WriteLine ("Unknown node: " + reader.LocalName);
					if (data.SkipNextRead) 
						goto skip;
					break;
				}
			}
		}
	}
}
