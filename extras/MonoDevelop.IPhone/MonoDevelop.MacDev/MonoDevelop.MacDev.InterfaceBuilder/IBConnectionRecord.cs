// 
// IBConnectionRecord.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace MonoDevelop.MacDev.InterfaceBuilder
{

	public class IBConnectionRecord : IBObject
	{
		public int ConnectionId { get; set; }
		public IBObject Connection { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "connectionID")
				ConnectionId = (int) value;
			else if (name == "connection")
				Connection = (IBObject) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}

	public abstract class IBConnection : IBObject
	{
		public string Label { get; set; }
		public IBReference Source { get; set; }
		public IBReference Destination { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "label")
				Label = (string) value;
			else if (name == "source")
				Source = (IBReference) value;
			else if (name == "destination")
				Destination = (IBReference) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}
	
	public class IBOutletConnection : IBConnection
	{
	}
	
	public class IBCocoaTouchOutletConnection : IBOutletConnection
	{
	}
	
	public class IBCocoaTouchEventConnection : IBActionConnection
	{
		public int IBEventType { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "IBEventType")
				IBEventType = (int) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}
	
	public class IBActionConnection : IBOutletConnection
	{
	}
	
	public class IBClassDescriptionSource : IBObject
	{
		public string MajorKey { get; set; }
		public string MinorKey { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "majorKey")
				MajorKey = (string) value;
			else if (name == "minorKey")
				MinorKey = (string) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}
	
	public class IBPartialClassDescription : IBObject
	{
		public string ClassName { get; set; }
		public string SuperclassName { get; set; }
		public NSMutableDictionary Actions { get; set; }
		public NSMutableDictionary Outlets { get; set; }
		public Unref<IBClassDescriptionSource> SourceIdentifier { get; set; }
		public NSMutableDictionary ToOneOutletInfosByName { get; set; }
		public NSMutableDictionary ActionInfosByName { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			switch (name) {
			case "className":
				ClassName = (string) value;
				break;
			case "superclassName":
				SuperclassName = (string) value;
				break;
			case "actions":
				Actions = (NSMutableDictionary) value;
				break;
			case "outlets":
				Outlets = (NSMutableDictionary) value;
				break;
			case "sourceIdentifier":
				SourceIdentifier = new Unref<IBClassDescriptionSource> (value);
				break;
			case "toOneOutletInfosByName":
				ToOneOutletInfosByName = (NSMutableDictionary) value;
				break;
			case "actionInfosByName":
				ActionInfosByName = (NSMutableDictionary) value;
				break;
			default:
				base.OnPropertyDeserialized (name, value);
				break;
			}
		}
	}
}
