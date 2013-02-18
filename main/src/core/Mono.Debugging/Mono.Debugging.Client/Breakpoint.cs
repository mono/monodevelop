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
		bool breakIfConditionChanges;
		string conditionExpression;
		string lastConditionValue;
		int adjustedColumn = -1;
		int adjustedLine = -1;
		string fileName;
		int column;
		int line;
		
		public Breakpoint (string fileName, int line, int column)
		{
			FileName = fileName;
			Column = column;
			Line = line;
		}

		public Breakpoint (string fileName, int line) : this (fileName, line, 1)
		{
		}
		
		internal Breakpoint (XmlElement elem): base (elem)
		{
			string s = elem.GetAttribute ("file");
			if (!string.IsNullOrEmpty (s))
				fileName = s;
			
			s = elem.GetAttribute ("line");
			if (string.IsNullOrEmpty (s) || !int.TryParse (s, out line))
				line = 1;
			
			s = elem.GetAttribute ("column");
			if (string.IsNullOrEmpty (s) || !int.TryParse (s, out column))
				column = 1;
			
			s = elem.GetAttribute ("conditionExpression");
			if (!string.IsNullOrEmpty (s))
				conditionExpression = s;
			
			s = elem.GetAttribute ("breakIfConditionChanges");
			if (!string.IsNullOrEmpty (s) && !bool.TryParse (s, out breakIfConditionChanges))
				breakIfConditionChanges = false;
		}
		
		internal override XmlElement ToXml (XmlDocument doc)
		{
			XmlElement elem = base.ToXml (doc);
			
			if (!string.IsNullOrEmpty (fileName))
				elem.SetAttribute ("file", fileName);
			
			elem.SetAttribute ("line", line.ToString ());
			elem.SetAttribute ("column", column.ToString ());
			
			if (!string.IsNullOrEmpty (conditionExpression)) {
				elem.SetAttribute ("conditionExpression", conditionExpression);
				if (breakIfConditionChanges)
					elem.SetAttribute ("breakIfConditionChanges", "True");
			}
			
			return elem;
		}
		
		public string FileName {
			get { return fileName; }
			protected set { fileName = value; }
		}

		public int OriginalColumn {
			get { return column; }
		}
		
		public int Column {
			get { return adjustedColumn == -1 ? column : adjustedColumn; }
			protected set { column = value; }
		}
		
		public int OriginalLine {
			get { return line; }
		}
		
		public int Line {
			get { return adjustedLine == -1 ? line : adjustedLine; }
			protected set { line = value; }
		}
		
		public void SetColumn (int newColumn)
		{
			ResetAdjustedColumn ();
			column = newColumn;
		}
		
		public void SetLine (int newLine)
		{
			ResetAdjustedLine ();
			line = newLine;
		}
		
		internal void SetAdjustedColumn (int newColumn)
		{
			adjustedColumn = newColumn;
		}
		
		internal void SetAdjustedLine (int newLine)
		{
			adjustedLine = newLine;
		}
		
		internal void ResetAdjustedColumn ()
		{
			adjustedColumn = -1;
		}
		
		internal void ResetAdjustedLine ()
		{
			adjustedLine = -1;
		}
		
		internal bool HasAdjustedColumn {
			get { return adjustedColumn != -1; }
		}

		internal bool HasAdjustedLine {
			get { return adjustedLine != -1; }
		}

		public string ConditionExpression {
			get {
				return conditionExpression;
			}
			set {
				conditionExpression = value;
			}
		}
		
		public string LastConditionValue {
			get {
				return lastConditionValue;
			}
			set {
				lastConditionValue = value;
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
			
			breakIfConditionChanges = bp.breakIfConditionChanges;
			conditionExpression = bp.conditionExpression;
			fileName = bp.fileName;
			column = bp.column;
			line = bp.line;
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
