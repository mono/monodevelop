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
using System.Threading.Tasks;
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
		CompletionSelectionStatus FindMatchedEntry (ICompletionDataList completionDataList, MruCache cache, string partialWord, List<int> filteredItems);
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
			return DefaultFilterItems (this, input.FilteredItems, input.OldCompletionString, input.CompletionString, GetCompletionDataMatcher (input.CompletionString));
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

		public virtual CompletionSelectionStatus FindMatchedEntry (ICompletionDataList completionDataList, MruCache cache, string partialWord, List<int> filteredItems)
		{
			// default - word with highest match rating in the list.
			int idx = -1;
			if (DefaultCompletionString != null && DefaultCompletionString.StartsWith (partialWord, StringComparison.OrdinalIgnoreCase)) {
				partialWord = DefaultCompletionString;
			}
			CompletionDataMatcher matcher = null;
			if (!string.IsNullOrEmpty (partialWord)) {
				matcher = GetCompletionDataMatcher (partialWord);
				string bestWord = null;
				int bestRank = int.MinValue;
				int bestIndex = 0;
				int bestIndexPriority = int.MinValue;
				for (int i = 0; i < filteredItems.Count; i++) {
					int index = filteredItems [i];
					var data = completionDataList [index];
					if (bestIndexPriority > data.PriorityGroup)
						continue;
					int rank;
					if (!matcher.CalcMatchRank (data, out rank))
						continue;
					if (rank > bestRank || data.PriorityGroup > bestIndexPriority) {
						bestWord = data.DisplayText;
						bestRank = rank;
						bestIndex = i;
						bestIndexPriority = data.PriorityGroup;
					}
				}

				if (bestWord != null) {
					idx = bestIndex;
					// exact match found.
					if (string.Compare (bestWord, partialWord ?? "", true) == 0)
						return new CompletionSelectionStatus (idx);
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
					if (currentData == null || matcher != null && matcher.CalcMatchRank (mruData, out r1) && matcher.CalcMatchRank (currentData, out r2)) {
						if (r1 >= r2 || partialWord.Length == 0 || partialWord.Length == 1 && mruData.DisplayText [0] == partialWord [0]) {
							bestMruIndex = curMruIndex;
							idx = i;
							currentData = mruData;
						}
					}
				}
			}
			return new CompletionSelectionStatus(idx);
		}

		public virtual int [] GetHighlightedIndices (CompletionData completionData, string completionString)
		{
			var matcher = GetCompletionDataMatcher (completionString);
			return matcher.StringMatcher.GetMatch (completionData.DisplayText);
		}

		public event EventHandler CompletionListClosed;

		CompletionDataMatcher lastMatcher;
		int lastMatcherId;

		/// <summary>
		/// Gets a code completion matcher for the provided string. It will try to reuse an existing one.
		/// </summary>
		CompletionDataMatcher GetCompletionDataMatcher (string partialWord)
		{
			// It keeps the last used matcher in a field, and will reuse it if the requested word doesn't change.
			// 'partialWord' doesn't usually change during the lifetime of CompletionDataList, so in general
			// the same matcher will be used by all calculations required for a key press

			if (lastMatcher != null && partialWord == lastMatcher.MatchString)
				return lastMatcher;

			return lastMatcher = new CompletionDataMatcher {
				MatcherId = lastMatcherId++, 
				StringMatcher = CompletionMatcher.CreateCompletionMatcher (partialWord), 
				MatchString = partialWord
			};
		}

		internal static CompletionListFilterResult DefaultFilterItems (ICompletionDataList dataList, IReadOnlyList<int> currentFilteredItems, string oldCompletionString, string completionString, CompletionDataMatcher matcher = null)
		{
			List<int> filteredItems;
			var newCategories = new List<CategorizedCompletionItems> ();

			matcher = matcher ?? new CompletionDataMatcher { StringMatcher = CompletionMatcher.CreateCompletionMatcher (completionString), MatcherId = -1, MatchString = completionString };

			if (oldCompletionString == null || !completionString.StartsWith (oldCompletionString, StringComparison.Ordinal)) {

				// We are filtering the list from scratch (not reusing previous results)
				// This is the most slow operation since we may have to filter a very large amount of items.
				// To improve performance, when there are many items, we try to parallelize the filtering

				// Use multiple threads when we can split the work in chunks of at least 4000 items.
				int numFilterThreads = Math.Max (Math.Min (dataList.Count / 4000, Environment.ProcessorCount / 2), 1);

				// When completion string is empty the matching algorithm is not executed, so there is no need to parallelize
				if (string.IsNullOrEmpty (completionString))
					numFilterThreads = 1;
				
				filteredItems = new List<int> ();

				int slice = dataList.Count / numFilterThreads;
				var results = new(Task, List<int>) [numFilterThreads - 1];

				Counters.ProcessCodeCompletion.Trace ("Begin initial item filtering (" + dataList.Count + " items) (" + (numFilterThreads - 1) + " threads)");

				// Start additional threads
				for (int n = 0; n < numFilterThreads - 1; n++) {
					var items = new List<int> (slice / 2);
					var start = slice * n;
					var end = slice * (n + 1);
					var task = Task.Run (() => FilterItems (dataList, completionString, items, matcher.Clone (), start, end));
					results [n] = (task, items);
				}

				// Filter the chunk of items for the current thread
				FilterItems (dataList, completionString, filteredItems, matcher, slice * (numFilterThreads - 1), dataList.Count);

				foreach (var t in results) {
					t.Item1.Wait ();
					filteredItems.AddRange (t.Item2);
				}

				Counters.ProcessCodeCompletion.Trace ("End initial item filtering");
			} else {
				// We are filtering a list that was already filtered, there shouldn't be that many items,
				// so let's not parallelize.

				Counters.ProcessCodeCompletion.Trace ("Begin item filtering (" + dataList.Count + " items)");

				var oldItems = currentFilteredItems;
				filteredItems = new List<int> ();
				foreach (int newSelection in oldItems) {
					if (string.IsNullOrEmpty (completionString) || matcher.IsMatch (dataList [newSelection]))
						filteredItems.Add (newSelection);
				}
				Counters.ProcessCodeCompletion.Trace ("End item filtering");
			}
			try {
				// The list of items is filtered. Now sort by rank.

				Counters.ProcessCodeCompletion.Trace ("Begin sorting items (" + filteredItems.Count + " items)");
				filteredItems.Sort (delegate (int left, int right) {
					var data1 = dataList [left];
					var data2 = dataList [right];
					if (data1 != null && data2 == null)
						return -1;
					if (data1 == null && data2 != null)
						return 1;
					if (data1 == null && data2 == null)
						return left.CompareTo (right);

					if (data1.PriorityGroup != data2.PriorityGroup)
						return data2.PriorityGroup.CompareTo (data1.PriorityGroup);

					if (string.IsNullOrEmpty (completionString))
						return CompareTo (dataList, left, right);

					int rank1, rank2;
					var hasRank1 = matcher.CalcMatchRank (data1, out rank1);
					var hasRank2 = matcher.CalcMatchRank (data2, out rank2);

					if (!hasRank1 && hasRank2)
						return 1;
					if (hasRank1 && !hasRank2)
						return -1;

					if (rank1 != rank2)
						return rank2.CompareTo (rank1);

					return CompareTo (dataList, left, right);
				});
				Counters.ProcessCodeCompletion.Trace ("End sorting items");
			} catch (Exception e) {
				LoggingService.LogError ("Error while filtering completion items.", e);
			}

			// Make a list of items for each category. This is done after sorting so that the list of items
			// of a category is also sorted.

			var categories = new List<CategorizedCompletionItems> (5);
			for (int n = 0; n < filteredItems.Count; n++) {
				var itemIndex = filteredItems [n];
				var item = dataList [itemIndex];
				var completionCategory = item.CompletionCategory;
				GetCategory (newCategories, item.CompletionCategory).Items.Add (itemIndex);
			}

			// Put the item from a lower priority group with the highest match rank always to position #2

			if (filteredItems.Count > 0) {
				int idx = 0;
				int rank;
				var data = dataList [filteredItems [0]];
				int firstGrp = data.PriorityGroup;
				matcher.CalcMatchRank (data, out rank);
				for (int i = 1; i < filteredItems.Count; i++) {
					var curData = dataList [filteredItems [i]];
					if (curData.PriorityGroup == firstGrp)
						continue;
					int curRank;
					matcher.CalcMatchRank (curData, out curRank);
					if (curRank > rank) {
						idx = i;
						rank = curRank;
					}
				}

				if (idx != 0) {
					var tmp = filteredItems [idx];
					for (int i = idx; i > 1; i--) {
						filteredItems [i] = filteredItems [i - 1];
					}
					filteredItems [1] = tmp;
				}
			}

			newCategories.Sort (delegate (CategorizedCompletionItems left, CategorizedCompletionItems right) {
				if (left.CompletionCategory == null)
					return 1;
				if (right.CompletionCategory == null)
					return -1;

				return left.CompletionCategory.CompareTo (right.CompletionCategory);
			});

			return new CompletionListFilterResult (filteredItems, newCategories);
		}

		static void FilterItems (IList<CompletionData> dataList, string completionString, List<int> items, CompletionDataMatcher matcher, int start, int end)
		{
			var tt = System.Diagnostics.Stopwatch.StartNew ();
			for (int newSelection = start; newSelection < end; newSelection++) {
				var item = dataList [newSelection];
				if (string.IsNullOrEmpty (completionString) || matcher.IsMatch (item))
					items.Add (newSelection);
			}
		}

		static CategorizedCompletionItems GetCategory (List<CategorizedCompletionItems> categories, CompletionCategory completionCategory)
		{
			for (int n = 0; n < categories.Count; n++) {
				var cat = categories [n];
				if (cat.CompletionCategory == completionCategory)
					return cat;
			}
			var result = new CategorizedCompletionItems ();
			result.CompletionCategory = completionCategory;
			if (completionCategory == null) {
				categories.Add (result);
			} else {
				categories.Insert (0, result);
			}

			return result;
		}

		internal static int CompareTo (ICompletionDataList completionDataList, int n, int m)
		{
			var item1 = completionDataList [n];
			var item2 = completionDataList [m];
			return (defaultComparer ?? (defaultComparer = GetComparerForCompletionList (completionDataList))).Compare (item1, item2);
		}

		internal static IComparer<CompletionData> GetComparerForCompletionList (ICompletionDataList dataList)
		{
			var concrete = dataList as CompletionDataList;
			return concrete != null && concrete.Comparer != null ? concrete.Comparer : CompletionData.Comparer;
		}

		internal static readonly IComparer<CompletionData> overloadComparer = CompletionData.Comparer;
		internal static IComparer<CompletionData> defaultComparer;
	}
}
