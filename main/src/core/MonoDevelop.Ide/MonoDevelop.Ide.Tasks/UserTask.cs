//
// UserTask.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 David Makovský
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
using System.Runtime.Serialization;
using System.Xml.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Tasks
{
	public class UserTask
	{
		TaskPriority priority = TaskPriority.Normal;
  		bool completed = false;
  		string description;
		Solution solution;
  		
		public TaskPriority Priority
		{
			get { return priority; }
			set { priority = value; }
		}
		
		public bool Completed
		{
			get { return completed; }
			set { completed = value; }
		}
		
		public string Description
		{
			get { return description; }
			set { description = value; }
		}

		[System.Xml.Serialization.XmlIgnoreAttribute]
		public Solution Solution {
			get {
				return solution;
			}
			internal set {
				solution = value;
			}
		}
		
		public override string ToString ()
		{
			return String.Format ("[UserTask: Priority={0}, Completed={1}, Description={2}]",
			                      Enum.GetName (typeof (TaskPriority), priority),
			                      completed, description);
		}
	}	
}
