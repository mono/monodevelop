// 
// CompletionDataList.cs
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface ICompletionDataList : IList<ICompletionData>
	{
		bool AutoCompleteUniqueMatch { get; }
		string DefaultCompletionString { get; }
		void Sort (Comparison<ICompletionData> comparison);
	}
	
	public interface IMutableCompletionDataList : ICompletionDataList, IDisposable
	{
		ICompletionDataList GetSnapshot ();
		bool IsChanging { get; }
		event EventHandler Changing;
		event EventHandler Changed;
	}
	
	public class CompletionDataList : List<ICompletionData>, ICompletionDataList
	{
		public bool AutoCompleteUniqueMatch { get; set; }
		public string DefaultCompletionString { get; set; }
		
		public CompletionDataList ()
		{
		}
		
		public CompletionDataList (IEnumerable<ICompletionData> data)
			: base (data)
		{
		}
		
		public CompletionData Add (string text)
		{
			CompletionData datum = new CompletionData (text);
			Add (datum);
			return datum;
		}
			
		public CompletionData Add (string text, string icon)
		{
			CompletionData datum = new CompletionData (text, icon);
			Add (datum);
			return datum;
		}
		
		public CompletionData Add (string text, string icon, string description)
		{
			CompletionData datum = new CompletionData (text, icon, description);
			Add (datum);
			return datum;
		}
		
		public CompletionData Add (string displayText, string icon, string description, string completionText)
		{
			CompletionData datum = new CompletionData (displayText, icon, description, completionText);
			Add (datum);
			return datum;
		}
		
		public ICompletionData Find (string name)
		{
			foreach (ICompletionData datum in this)
				if (datum.DisplayText == name)
					return datum;
			return null;
		}
		
		public bool Remove (string name)
		{
			for (int i = 0; i < this.Count; i++) {
				if (this[i].DisplayText == name) {
					this.RemoveAt (i);
					return true;
				}
			}
			return false;
		}
		
		public void RemoveWhere (Func<ICompletionData,bool> shouldRemove)
		{
			for (int i = 0; i < this.Count;) {
				if (shouldRemove (this[i]))
					this.RemoveAt (i);
				else
					i++;
			}
		}
	}
}
