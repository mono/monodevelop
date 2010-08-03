// 
// CompletionMatcher.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeCompletion
{
	public interface ICompletionMatcher
	{
		bool CalcMatchRank (string name, out int matchRank);
		bool IsMatch (string name);
		int[] GetMatch (string text);
	}
	
	static class CompletionMatcher
	{
		static bool useLaneCompletionMatcher;
		
		public static bool UseLaneCompletionMatcher {
			get {
				return useLaneCompletionMatcher;
			}
			set {
				useLaneCompletionMatcher = value;
				PropertyService.Set ("UseLaneCompletionMatcher", useLaneCompletionMatcher);
			}
		}
		
		static CompletionMatcher ()
		{
			useLaneCompletionMatcher = PropertyService.Get ("UseLaneCompletionMatcher", true);
		}
		
		public static ICompletionMatcher CreateCompletionMatcher (string filterText)
		{
			return useLaneCompletionMatcher ? (ICompletionMatcher)new LaneCompletionMatcher (filterText) : new PrefixCompletionMatcher (filterText);
		}
	}

	class LaneCompletionMatcher : ICompletionMatcher
	{
		readonly string filterLowerCase;
		
		readonly List<MatchLane> matchLanes;
		
		public LaneCompletionMatcher (string filter)
		{
			matchLanes = new List<MatchLane> ();
			this.filterLowerCase = filter != null ? filter.ToLower () : "";
		}

		public bool CalcMatchRank (string name, out int matchRank)
		{
			if (filterLowerCase.Length == 0) {
				matchRank = int.MinValue;
				return true;
			}
			MatchLane lane = MatchString (name);
			if (lane != null) {
				matchRank = -(lane.Positions [0] + (name.Length - filterLowerCase.Length));
				return true;
			}
			matchRank = int.MinValue;
			return false;
		}

		public bool IsMatch (string name)
		{
			return filterLowerCase.Length == 0 || MatchString (name) != null;
		}
		
		/// <summary>
		/// Gets the match indices.
		/// </summary>
		/// <returns>
		/// The indices in the text which are matched by our filter.
		/// </returns>
		/// <param name='text'>
		/// The text to match.
		/// </param>
		public int[] GetMatch (string text)
		{
			if (filterLowerCase.Length == 0) 
				return new int[0];
			var lane = MatchString (text);
			if (lane == null)
				return null;
			int cnt = 0;
			for (int i = 0; i < lane.Positions.Length; i++) {
				cnt += lane.Lengths[i];
			}
			int[] result = new int [cnt];
			int x = 0;
			for (int i = 0; i < lane.Positions.Length; i++) {
				int p = lane.Positions[i];
				for (int j = 0 ; j < lane.Lengths[i]; j++) {
					result[x++] = p++;
				}
			}
			return result;
		}

		MatchLane MatchString (string text)
		{
			if (text.Length < filterLowerCase.Length)
				return null;
	
			matchLanes.Clear ();
			bool lastWasSeparator = false;
			int tn = 0;
			char filterStart = filterLowerCase[0];
			
			while (tn < text.Length) {
				char ct = text [tn];
				bool ctIsUpper = char.IsUpper (ct);
				char ctLower = ctIsUpper ? char.ToLower (ct) : ct;
				
				// Keep the lane count in a var because new lanes don't have to be updated
				// until the next iteration
				int laneCount = matchLanes != null ? matchLanes.Count : 0;
	
				if (ctLower == filterStart) {
					matchLanes.Add (new MatchLane (MatchMode.Substring, tn, text.Length - tn));
					if (filterLowerCase.Length == 1)
						return matchLanes[0];
					if (ctIsUpper || lastWasSeparator)
						matchLanes.Add (new MatchLane (MatchMode.Acronym, tn, text.Length - tn));
				}
	
				for (int n=0; n<laneCount; n++) {
					MatchLane lane = matchLanes [n];
					if (lane == null)
						continue;
					char cm = filterLowerCase [lane.MatchIndex];
					bool match = ctLower == cm;
					bool wordStartMatch = match && (tn == 0 || ctIsUpper || lastWasSeparator);
	
					if (lane.MatchMode == MatchMode.Substring) {
						if (wordStartMatch) {
							// Possible acronym match after a substring. Start a new lane.
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Acronym;
							newLane.Index++;
							newLane.Positions [newLane.Index] = tn;
							newLane.Lengths [newLane.Index] = 1;
							newLane.MatchIndex++;
							matchLanes.Add (newLane);
						}
						if (match) {
							// Maybe it is a false substring start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Acronym;
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Lengths [lane.Index]++;
							lane.MatchIndex++;
						} else {
							if (lane.Lengths [lane.Index] > 1)
								lane.MatchMode = MatchMode.Acronym;
							else
								matchLanes [n] = null; // Kill the lane
						}
					}
					else if (lane.MatchMode == MatchMode.Acronym) {
						if (match && lane.Positions [lane.Index] == tn - 1) {
							// Possible substring match after an acronim. Start a new lane.
							MatchLane newLane = lane.Clone ();
							newLane.MatchMode = MatchMode.Substring;
							newLane.Lengths [newLane.Index]++;
							newLane.MatchIndex++;
							matchLanes.Add (newLane);
							if (newLane.MatchIndex == filterLowerCase.Length)
								return newLane;
						}
						if (wordStartMatch || (match && char.IsPunctuation (cm))) {
							// Maybe it is a false acronym start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = lane.Clone ();
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Index++;
							lane.Positions [lane.Index] = tn;
							lane.Lengths [lane.Index] = 1;
							lane.MatchIndex++;
						}
					}
					if (lane.MatchIndex == filterLowerCase.Length)
						return lane;
				}
				lastWasSeparator = (ct == '.' || ct == '_' || ct == '-' || ct == ' ' || ct == '/' || ct == '\\');
				tn++;
			}
			return null;
		}
	
		enum MatchMode {
			Substring,
			Acronym
		}
	
		class MatchLane
		{
			public int[] Positions;
			public int[] Lengths;
			public MatchMode MatchMode;
			public int Index;
			public int MatchIndex;
	
			public MatchLane ()
			{
			}
	
			public MatchLane (MatchMode mode, int pos, int len)
			{
				MatchMode = mode;
				Positions = new int [len];
				Lengths = new int [len];
				Positions [0] = pos;
				Lengths [0] = 1;
				Index = 0;
				MatchIndex = 1;
			}
	
			public MatchLane Clone ()
			{
				MatchLane lane = new MatchLane ();
				lane.Positions = (int[]) Positions.Clone ();
				lane.Lengths = (int[]) Lengths.Clone ();
				lane.MatchMode = MatchMode;
				lane.MatchIndex = MatchIndex;
				lane.Index = Index;
				return lane;
			}
		}
	}
	
	class PrefixCompletionMatcher : ICompletionMatcher
	{
		readonly string filterTextUpperCase;

		readonly bool[] filterTextLowerCaseTable;
		readonly bool[] filterIsNonLetter;

		readonly List<int> matchIndices;

		public PrefixCompletionMatcher (string filterText)
		{
			matchIndices = new List<int> ();
			if (filterText != null) {
				filterTextLowerCaseTable = new bool[filterText.Length];
				filterIsNonLetter        = new bool[filterText.Length];
				for (int  i = 0; i < filterText.Length; i++) {
					filterTextLowerCaseTable[i] = char.IsLower (filterText[i]);
					filterIsNonLetter[i] = !char.IsLetter (filterText[i]);
				}
				
				filterTextUpperCase = filterText.ToUpper ();
			} else {
				filterTextUpperCase = "";
			}
		}

		public bool CalcMatchRank (string name, out int matchRank)
		{
			if (filterTextUpperCase.Length == 0) {
				matchRank = int.MinValue;
				return true;
			}
			var lane = GetMatch (name);
			if (lane != null) {
				matchRank = -(lane[0] + (name.Length - filterTextUpperCase.Length));
				return true;
			}
			matchRank = int.MinValue;
			return false;
		}

		public bool IsMatch (string text)
		{
			return GetMatch (text) != null;
		}
		
		/// <summary>
		/// Gets the match indices.
		/// </summary>
		/// <returns>
		/// The indices in the text which are matched by our filter.
		/// </returns>
		/// <param name='text'>
		/// The text to match.
		/// </param>
		public int[] GetMatch (string text)
		{
			if (string.IsNullOrEmpty (filterTextUpperCase))
				return new int[0];
			if (string.IsNullOrEmpty (text))
				return null;

			matchIndices.Clear ();
			int j = 0;
			
			for (int i = 0; i < filterTextUpperCase.Length; i++) {
				if (j >= text.Length)
					return null;
				bool wasMatch = false;
				char filterChar = filterTextUpperCase[i];
				// filter char is no letter -> search for next exact match
				if (filterIsNonLetter[i]) {
					for (; j < text.Length; j++) {
						if (filterChar == text[j]) {
							matchIndices.Add (j);
							j++;
							wasMatch = true;
							break;
						}
					}
					if (!wasMatch)
						return null;
					continue;
				}
				
				// letter case
				bool textCharIsUpper = char.IsUpper (text[j]);
				if ((textCharIsUpper || filterTextLowerCaseTable[i]) && filterChar == (textCharIsUpper ? text[j] : char.ToUpper (text[j]))) {
					matchIndices.Add (j++);
					continue;
				}

				// no match, try to continue match at the next word start
				j++;
				for (; j < text.Length; j++) {
					if (char.IsUpper (text[j]) && filterChar == text[j]) {
						matchIndices.Add (j);
						j++;
						wasMatch = true;
						break;
					}
				}
				
				if (!wasMatch)
					return null;
			}
			
			return matchIndices.ToArray ();
		}
	}
}

