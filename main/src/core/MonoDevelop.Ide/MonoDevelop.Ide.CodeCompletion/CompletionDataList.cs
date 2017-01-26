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
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.CodeCompletion
{
	public interface ICompletionDataList : IList<CompletionData>
	{
		int TriggerWordStart { get; }
		int TriggerWordLength { get; }

		bool IsSorted { get; }
		bool AutoCompleteUniqueMatch { get; }
		bool AutoCompleteEmptyMatch { get; }
		bool AutoCompleteEmptyMatchOnCurlyBrace { get; }
		bool CloseOnSquareBrackets { get; }
		bool AutoSelect { get; }
		string DefaultCompletionString { get; }
		CompletionSelectionMode CompletionSelectionMode { get; }
		void Sort (Comparison<CompletionData> comparison);
		void Sort (IComparer<CompletionData> comparison);
		
		IEnumerable<ICompletionKeyHandler> KeyHandler { get; }
		
		void OnCompletionListClosed (EventArgs e);
		event EventHandler CompletionListClosed;

		/// <summary>
		/// Gives the abilit to override the custom filtering
		/// </summary>
		/// <returns>The filtered completion list, or null if the default list should be taken.</returns>
		/// <param name="input">Contains all information needed to filter the list.</param>
		CompletionListFilterResult FilterCompletionList (CompletionListFilterInput input);
		int FindMatchedEntry (ICompletionDataList completionDataList, MruCache cache, string partialWord, List<int> filteredItems);
		int [] GetHighlightedIndices (CompletionData completionData, string completionString);
	}
	
	
	public interface ICompletionKeyHandler
	{
		bool PreProcessKey (CompletionListWindow listWindow, KeyDescriptor descriptor, out KeyActions keyAction);
		bool PostProcessKey (CompletionListWindow listWindow, KeyDescriptor descriptor, out KeyActions keyAction);
	}
	
	public enum CompletionSelectionMode {
		InsideTextEditor,
		OwnTextField
	}
	
	public class CompletionDataList : List<CompletionData>, ICompletionDataList
	{
		public int TriggerWordStart { get; set; } = -1;
		public int TriggerWordLength { get; set; }

		public bool IsSorted { get; set; }
		public IComparer<CompletionData> Comparer { get; set; }
		
		public bool AutoCompleteUniqueMatch { get; set; }
		public string DefaultCompletionString { get; set; }
		public bool AutoSelect { get; set; }
		public bool AutoCompleteEmptyMatch { get; set; }
		public bool AutoCompleteEmptyMatchOnCurlyBrace { get; set; }
		public CompletionSelectionMode CompletionSelectionMode { get; set; }
		public bool CloseOnSquareBrackets { get; set; }

		public virtual CompletionListFilterResult FilterCompletionList (CompletionListFilterInput input)
		{
			return null;
		}

		List<ICompletionKeyHandler> keyHandler = new List<ICompletionKeyHandler> ();
		public IEnumerable<ICompletionKeyHandler> KeyHandler {
			get { return keyHandler; }
		}
		public CompletionDataList ()
		{
			this.AutoSelect = true;
		}
		
		public CompletionDataList (IEnumerable<CompletionData> data) : base(data)
		{
			this.AutoSelect = true;
		}
		
		public void AddKeyHandler (ICompletionKeyHandler keyHandler)
		{
			this.keyHandler.Add (keyHandler);
		}
		
		public CompletionData Add (string text)
		{
			CompletionData datum = new CompletionData (text);
			Add (datum);
			return datum;
		}
			
		public CompletionData Add (string text, IconId icon)
		{
			CompletionData datum = new CompletionData (text, icon);
			Add (datum);
			return datum;
		}
		
		public CompletionData Add (string text, IconId icon, string description)
		{
			CompletionData datum = new CompletionData (text, icon, description);
			Add (datum);
			return datum;
		}
		
		public CompletionData Add (string displayText, IconId icon, string description, string completionText)
		{
			CompletionData datum = new CompletionData (displayText, icon, description, completionText);
			Add (datum);
			return datum;
		}
		
		public CompletionData Find (string name)
		{
			foreach (CompletionData datum in this)
				if (datum.CompletionText == name)
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
		
		public void RemoveWhere (Func<CompletionData,bool> shouldRemove)
		{
			for (int i = 0; i < this.Count;) {
				if (shouldRemove (this[i]))
					this.RemoveAt (i);
				else
					i++;
			}
		}
		
		public void AddRange (IEnumerable<string> vals)
		{
			AddRange (from s in vals select new CompletionData (s));
		}
		
		public void OnCompletionListClosed (EventArgs e)
		{
			EventHandler handler = this.CompletionListClosed;
			if (handler != null)
				handler (this, e);
		}

		public virtual int FindMatchedEntry (ICompletionDataList completionDataList, MruCache cache, string partialWord, List<int> filteredItems)
		{
			                  // default - word with highest match rating in the list.
			int idx = -1;

			StringMatcher matcher = null;
			if (!string.IsNullOrEmpty (partialWord)) {
				matcher = CompletionMatcher.CreateCompletionMatcher (partialWord);
				string bestWord = null;
				int bestRank = int.MinValue;
				int bestIndex = 0;
				int bestIndexPriority = int.MinValue;
				for (int i = 0; i < filteredItems.Count; i++) {
					int index = filteredItems [i];
					var data = completionDataList [index];
					if (bestIndexPriority > data.PriorityGroup)
						continue;
					string text = data.DisplayText;
					int rank;
					if (!matcher.CalcMatchRank (text, out rank))
						continue;
					if (rank > bestRank || data.PriorityGroup > bestIndexPriority) {
						bestWord = text;
						bestRank = rank;
						bestIndex = i;
						bestIndexPriority = data.PriorityGroup;
					}
				}

				if (bestWord != null) {
					idx = bestIndex;
					// exact match found.
					if (string.Compare (bestWord, partialWord ?? "", true) == 0)
						return idx;
				}
			}

			CompletionData currentData;
			int bestMruIndex;
			if (idx >= 0) {
				currentData = completionDataList [filteredItems [idx]];
				bestMruIndex = cache.GetIndex (currentData);
			} else {
				bestMruIndex = int.MaxValue;
				currentData = null;
			}
			for (int i = 0; i < filteredItems.Count; i++) {
				var mruData = completionDataList [filteredItems [i]];
				int curMruIndex = cache.GetIndex (mruData);
				if (curMruIndex == 1)
					continue;
				if (curMruIndex < bestMruIndex) {
					int r1 = 0, r2 = 0;
					if (currentData == null || matcher != null && matcher.CalcMatchRank (mruData.DisplayText, out r1) && matcher.CalcMatchRank (currentData.DisplayText, out r2)) {
						if (r1 >= r2 || partialWord.Length == 0 || partialWord.Length == 1 && mruData.DisplayText [0] == partialWord [0]) {
							bestMruIndex = curMruIndex;
							idx = i;
							currentData = mruData;
						}
					}
				}
			}
			return idx;
		}

		public virtual int [] GetHighlightedIndices (CompletionData completionData, string completionString)
		{
			var matcher = CompletionMatcher.CreateCompletionMatcher (completionString);
			return matcher.GetMatch (completionData.DisplayText);
		}

		public event EventHandler CompletionListClosed;
	}
}
