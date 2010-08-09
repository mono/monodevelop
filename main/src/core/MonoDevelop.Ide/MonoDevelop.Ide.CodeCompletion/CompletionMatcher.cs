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
		public static ICompletionMatcher CreateCompletionMatcher (string filterText)
		{
			return new LaneCompletionMatcher (filterText);
		}
	}
	
	class LaneCompletionMatcher : ICompletionMatcher
	{
		readonly string filter;
		readonly string filterLowerCase;
		readonly List<MatchLane> matchLanes;
		
		public LaneCompletionMatcher (string filter)
		{
			matchLanes = new List<MatchLane> ();
			this.filter = filter;
			this.filterLowerCase = filter != null ? filter.ToLowerInvariant () : "";
		}

		public bool CalcMatchRank (string name, out int matchRank)
		{
			if (filterLowerCase.Length == 0) {
				matchRank = int.MinValue;
				return true;
			}
			MatchLane lane = MatchString (name);
			if (lane != null) {
				// Favor matches with less splits. That is, 'abc def' is better than 'ab c def'.
				int baseRank = (filter.Length - lane.Index - 1) * 5000;

				// First matching letter close to the begining is better
				// The more matched letters the better
				matchRank = baseRank - (lane.Positions [0] + (name.Length - filterLowerCase.Length));
				
				matchRank += lane.ExactCaseMatches * 10;
				
				// rank up matches which start with a filter substring
				if (lane.Positions [0] == 0)
					matchRank += lane.Lengths[0] * 50;
				
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
			if (string.IsNullOrEmpty (text))
				return null;
			var lane = MatchString (text);
			if (lane == null)
				return null;
			int cnt = 0;
			for (int i = 0; i <= lane.Index; i++) {
				cnt += lane.Lengths[i];
			}
			int[] result = new int [cnt];
			int x = 0;
			for (int i = 0; i <= lane.Index; i++) {
				int p = lane.Positions[i];
				for (int j = 0 ; j < lane.Lengths[i]; j++) {
					result[x++] = p++;
				}
			}
			return result;
		}
		
		MatchLane MatchString (string text)
		{
			if (text == null || text.Length < filterLowerCase.Length)
				return null;
			
			// Pre-match check
			
			string textLowerCase = text.ToLowerInvariant ();
	
			int firstMatchPos = -1;
			int j = 0;
			int tlen = text.Length;
			int flen = filterLowerCase.Length;
			for (int n=0; n<tlen && j < flen; n++) {
				char ctLower = textLowerCase[n];
				char cfLower = filterLowerCase [j];
				if (ctLower == cfLower && !(cfLower != filter[j] && ctLower == text[n])) {
					bool exactMatch = filter[j] == text[n];
					j++;
					if (firstMatchPos == -1)
						firstMatchPos = n;
					if (flen == 1) {
						MatchLane lane = CreateLane (MatchMode.Substring, n);
						if (exactMatch)
							lane.ExactCaseMatches++;
						return lane;
					}
				}
			}

			if (j < flen)
				return null;
			
			ResetLanePool ();
			
			// Full match check
			
			matchLanes.Clear ();
			bool lastWasSeparator = false;
			int tn = firstMatchPos;
			char filterStartLower = filterLowerCase[0];
			bool filterStartIsUpper = filterStartLower != filter[0];
			
			while (tn < text.Length) {
				char ct = text [tn];
				char ctLower = textLowerCase [tn];
				bool ctIsUpper = ct != ctLower;
				
				// Keep the lane count in a var because new lanes don't have to be updated
				// until the next iteration
				int laneCount = matchLanes != null ? matchLanes.Count : 0;
	
				if (ctLower == filterStartLower && !(filterStartIsUpper && !ctIsUpper)) {
					MatchLane lane = CreateLane (MatchMode.Substring, tn);
					if (filterStartIsUpper == ctIsUpper)
						lane.ExactCaseMatches++;
					matchLanes.Add (lane);
					if (filterLowerCase.Length == 1)
						return matchLanes[0];
					if (ctIsUpper || lastWasSeparator)
						matchLanes.Add (CreateLane (MatchMode.Acronym, tn));
				}
	
				for (int n=0; n<laneCount; n++) {
					MatchLane lane = matchLanes [n];
					if (lane == null)
						continue;
					char cfLower = filterLowerCase [lane.MatchIndex];
					bool cfIsUpper = cfLower != filter [lane.MatchIndex];
					bool match = ctLower == cfLower && !(cfIsUpper && !ctIsUpper);
					bool exactMatch = match && (cfIsUpper == ctIsUpper);
					bool wordStartMatch = match && (tn == 0 || ctIsUpper || lastWasSeparator);
	
					if (lane.MatchMode == MatchMode.Substring) {
						if (wordStartMatch) {
							// Possible acronym match after a substring. Start a new lane.
							MatchLane newLane = CloneLane (lane);
							newLane.MatchMode = MatchMode.Acronym;
							newLane.Index++;
							newLane.Positions [newLane.Index] = tn;
							newLane.Lengths [newLane.Index] = 1;
							newLane.MatchIndex++;
							if (exactMatch)
								newLane.ExactCaseMatches++;
							matchLanes.Add (newLane);
						}
						if (match) {
							// Maybe it is a false substring start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = CloneLane (lane);
							newLane.MatchMode = MatchMode.Acronym;
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Lengths [lane.Index]++;
							lane.MatchIndex++;
							if (exactMatch)
								newLane.ExactCaseMatches++;
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
							MatchLane newLane = CloneLane (lane);
							newLane.MatchMode = MatchMode.Substring;
							newLane.Lengths [newLane.Index]++;
							newLane.MatchIndex++;
							if (exactMatch)
								newLane.ExactCaseMatches++;
							matchLanes.Add (newLane);
							if (newLane.MatchIndex == filterLowerCase.Length)
								return newLane;
						}
						if (wordStartMatch || (match && char.IsPunctuation (cfLower))) {
							// Maybe it is a false acronym start, so add a new lane to keep
							// track of the old lane
							MatchLane newLane = CloneLane (lane);
							matchLanes.Add (newLane);
	
							// Update the current lane
							lane.Index++;
							lane.Positions [lane.Index] = tn;
							lane.Lengths [lane.Index] = 1;
							lane.MatchIndex++;
							if (exactMatch)
								newLane.ExactCaseMatches++;
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
		
		void ResetLanePool ()
		{
			lanePoolIndex = 0;
		}
		
		MatchLane GetPoolLane ()
		{
			if (lanePoolIndex < lanePool.Count)
				return lanePool [lanePoolIndex++];
			
			MatchLane lane = new MatchLane (filterLowerCase.Length * 2);
			lanePool.Add (lane);
			lanePoolIndex++;
			return lane;
		}
		
		MatchLane CreateLane (MatchMode mode, int pos)
		{
			MatchLane lane = GetPoolLane ();
			lane.Initialize (mode, pos);
			return lane;
		}
		
		MatchLane CloneLane (MatchLane other)
		{
			MatchLane lane = GetPoolLane ();
			lane.Initialize (other);
			return lane;
		}
		
		int lanePoolIndex = 0;
		List<MatchLane> lanePool = new List<MatchLane> ();
	
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
			public int ExactCaseMatches;
	
			public MatchLane (int maxlen)
			{
				Positions = new int [maxlen];
				Lengths = new int [maxlen];
			}
			
			public void Initialize (MatchMode mode, int pos)
			{
				MatchMode = mode;
				Positions [0] = pos;
				Lengths [0] = 1;
				Index = 0;
				MatchIndex = 1;
				ExactCaseMatches = 0;
			}
	
			public void Initialize (MatchLane other)
			{
				for (int n=0; n<=other.Index; n++) {
					Positions [n] = other.Positions [n];
					Lengths [n] = other.Lengths [n];
				}
				MatchMode = other.MatchMode;
				MatchIndex = other.MatchIndex;
				Index = other.Index;
				ExactCaseMatches = other.ExactCaseMatches;
			}
		}
	}
	
/*	class BacktrackingCompletionMatcher : ICompletionMatcher
	{
		readonly string filterTextUpperCase;

		readonly bool[] filterTextLowerCaseTable;
		readonly bool[] filterIsNonLetter;

		readonly List<int> matchIndices;
		int[] cachedResult;

		public BacktrackingCompletionMatcher (string filterText)
		{
			matchIndices = new List<int> ();
			if (filterText != null) {
				filterTextLowerCaseTable = new bool[filterText.Length];
				filterIsNonLetter = new bool[filterText.Length];
				for (int i = 0; i < filterText.Length; i++) {
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
			int[] match = GetMatch (text);
			// no need to clear the cache
			cachedResult = cachedResult ?? match;
			return match != null;
		}

		int GetMatchChar (string text, int i, int j, bool onlyWordStart)
		{
			char filterChar = filterTextUpperCase[i];
			// filter char is no letter -> search for next exact match
			if (filterIsNonLetter[i]) {
				for (; j < text.Length; j++) {
					if (filterChar == text[j])
						return j;
				}
				return -1;
			}
			
			// letter case
			bool textCharIsUpper = char.IsUpper (text[j]);
			if (!onlyWordStart && (textCharIsUpper || filterTextLowerCaseTable[i]) && filterChar == (textCharIsUpper ? text[j] : char.ToUpper (text[j]))) {
				return j;
			}
			
			// no match, try to continue match at the next word start
			j++;
			for (; j < text.Length; j++) {
				if (char.IsUpper (text[j]) && filterChar == text[j])
					return j;
			}
			return -1;
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
			int[] result;
			if (cachedResult != null) {
				result = cachedResult;
			} else {
				cachedResult = result = new int[filterTextUpperCase.Length];
			}
			int j = 0;
			int i = 0;
			bool onlyWordStart = false;
			while (i < filterTextUpperCase.Length) {
				if (j >= text.Length) {
					if (i > 0) {
						j = result[--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				}
				j = GetMatchChar (text, i, j, onlyWordStart);
				onlyWordStart = false;
				if (j == -1) {
					if (i > 0) {
						j = result[--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				} else {
					result[i] = j++;
				}
				i++;
			}
			cachedResult = null;
			// clear cache
			return result;
		}
	}*/

}

