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

namespace MonoDevelop.IPhone.InterfaceBuilder
{

	public class IBConnectionRecord : IBObject
	{
		public int ConnectionId { get; set; }
		public IBCocoaTouchOutletConnection Connection { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "connectionID")
				ConnectionId = (int) value;
			else if (name == "connection")
				Connection = (IBCocoaTouchOutletConnection) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}

	public class IBCocoaTouchOutletConnection : IBObject
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
	
	public class IBCocoaTouchEventConnection : IBCocoaTouchOutletConnection
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
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "className")
				ClassName = (string) value;
			else if (name == "superclassName")
				SuperclassName = (string) value;
			else if (name == "actions")
				Actions = (NSMutableDictionary) value;
			else if (name == "outlets")
				Outlets = (NSMutableDictionary) value;
			else if (name == "sourceIdentifier")
				SourceIdentifier = new Unref<IBClassDescriptionSource> (value);
			else
				base.OnPropertyDeserialized (name, value);
		}
	}
}
