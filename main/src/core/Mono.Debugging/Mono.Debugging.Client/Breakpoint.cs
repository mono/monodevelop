// Breakpoint.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Xml;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class Breakpoint: BreakEvent
	{
		string fileName;
		int line;
		
		string conditionExpression;
		bool breakIfConditionChanges;
		
		public Breakpoint (string fileName, int line)
		{
			this.fileName = fileName;
			this.line = line;
		}
		
		internal Breakpoint (XmlElement elem): base (elem)
		{
			fileName = elem.GetAttribute ("file");
			line = int.Parse (elem.GetAttribute ("line"));
			string s = elem.GetAttribute ("conditionExpression");
			if (s.Length > 0)
				conditionExpression = s;
			s = elem.GetAttribute ("breakIfConditionChanges");
			if (s.Length > 0)
				breakIfConditionChanges = bool.Parse (s);
		}
		
		internal override XmlElement ToXml (XmlDocument doc)
		{
			XmlElement elem = base.ToXml (doc);
			elem.SetAttribute ("file", fileName);
			elem.SetAttribute ("line", line.ToString ());
			if (!string.IsNullOrEmpty (conditionExpression)) {
				elem.SetAttribute ("conditionExpression", conditionExpression);
				if (breakIfConditionChanges)
					elem.SetAttribute ("breakIfConditionChanges", "True");
			}
			return elem;
		}

		
		public string FileName {
			get { return fileName; }
		}
		
		public int Line {
			get { return line; }
		}

		public string ConditionExpression {
			get {
				return conditionExpression;
			}
			set {
				conditionExpression = value;
			}
		}

		public bool BreakIfConditionChanges {
			get {
				return breakIfConditionChanges;
			}
			set {
				breakIfConditionChanges = value;
			}
		}
		
		public override void CopyFrom (BreakEvent ev)
		{
			base.CopyFrom (ev);
			Breakpoint bp = (Breakpoint) ev;
			fileName = bp.fileName;
			line = bp.line;
			conditionExpression = bp.conditionExpression;
			breakIfConditionChanges = bp.breakIfConditionChanges;
		}

	}
	
	public enum HitAction
	{
		Break,
		PrintExpression,
		CustomAction
	}
	
	public delegate bool BreakEventHitHandler (string actionId, BreakEvent be);
}
