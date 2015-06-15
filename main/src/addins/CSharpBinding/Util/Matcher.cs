// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp
{
	public abstract class Matcher<T>
	{
		// Tries to match this matcher against the provided sequence at the given index.  If the
		// match succeeds, 'true' is returned, and 'index' points to the location after the match
		// ends.  If the match fails, then false it returned and index remains the same.  Note: the
		// matcher does not need to consume to the end of the sequence to succeed.
		public abstract bool TryMatch(IList<T> sequence, ref int index);

		internal static Matcher<T> Repeat(Matcher<T> matcher)
		{
			return new RepeatMatcher(matcher);
		}

		internal static Matcher<T> OneOrMore(Matcher<T> matcher)
		{
			// m+ is the same as (m m*)
			return Sequence(matcher, Repeat(matcher));
		}

		internal static Matcher<T> Choice(Matcher<T> matcher1, Matcher<T> matcher2)
		{
			return new ChoiceMatcher(matcher1, matcher2);
		}

		internal static Matcher<T> Sequence(params Matcher<T>[] matchers)
		{
			return new SequenceMatcher(matchers);
		}

		internal static Matcher<T> Single(Func<T, bool> predicate, string description)
		{
			return new SingleMatcher(predicate, description);
		}
		private class ChoiceMatcher : Matcher<T>
		{
			private readonly Matcher<T> _matcher1;
			private readonly Matcher<T> _matcher2;

			public ChoiceMatcher(Matcher<T> matcher1, Matcher<T> matcher2)
			{
				_matcher1 = matcher1;
				_matcher2 = matcher2;
			}

			public override bool TryMatch(IList<T> sequence, ref int index)
			{
				return
					_matcher1.TryMatch(sequence, ref index) ||
					_matcher2.TryMatch(sequence, ref index);
			}

			public override string ToString()
			{
				return string.Format("({0}|{1})", _matcher1, _matcher2);
			}
		}
		private class RepeatMatcher : Matcher<T>
		{
			private readonly Matcher<T> _matcher;

			public RepeatMatcher(Matcher<T> matcher)
			{
				_matcher = matcher;
			}

			public override bool TryMatch(IList<T> sequence, ref int index)
			{
				while (_matcher.TryMatch(sequence, ref index))
				{
				}

				return true;
			}

			public override string ToString()
			{
				return string.Format("({0}*)", _matcher);
			}
		}
		private class SequenceMatcher : Matcher<T>
		{
			private readonly Matcher<T>[] _matchers;

			public SequenceMatcher(params Matcher<T>[] matchers)
			{
				_matchers = matchers;
			}

			public override bool TryMatch(IList<T> sequence, ref int index)
			{
				var currentIndex = index;
				foreach (var matcher in _matchers)
				{
					if (!matcher.TryMatch(sequence, ref currentIndex))
					{
						return false;
					}
				}

				index = currentIndex;
				return true;
			}

			public override string ToString()
			{
				return string.Format("({0})", string.Join(",", (object[])_matchers));
			}
		}
		private class SingleMatcher : Matcher<T>
		{
			private readonly Func<T, bool> _predicate;
			private readonly string _description;

			public SingleMatcher(Func<T, bool> predicate, string description)
			{
				_predicate = predicate;
				_description = description;
			}

			public override bool TryMatch(IList<T> sequence, ref int index)
			{
				if (index < sequence.Count && _predicate(sequence[index]))
				{
					index++;
					return true;
				}

				return false;
			}

			public override string ToString()
			{
				return _description;
			}
		}
	}

	public class Matcher
	{
		/// <summary>
		/// Matcher equivalent to (m*)
		/// </summary>
		public static Matcher<T> Repeat<T>(Matcher<T> matcher)
		{
			return Matcher<T>.Repeat(matcher);
		}

		/// <summary>
		/// Matcher equivalent to (m+)
		/// </summary>
		public static Matcher<T> OneOrMore<T>(Matcher<T> matcher)
		{
			return Matcher<T>.OneOrMore(matcher);
		}

		/// <summary>
		/// Matcher equivalent to (m_1|m_2)
		/// </summary>
		public static Matcher<T> Choice<T>(Matcher<T> matcher1, Matcher<T> matcher2)
		{
			return Matcher<T>.Choice(matcher1, matcher2);
		}

		/// <summary>
		/// Matcher equivalent to (m_1 ... m_n)
		/// </summary>
		public static Matcher<T> Sequence<T>(params Matcher<T>[] matchers)
		{
			return Matcher<T>.Sequence(matchers);
		}

		/// <summary>
		/// Matcher that matches an element if the provide predicate returns true.
		/// </summary>
		public static Matcher<T> Single<T>(Func<T, bool> predicate, string description)
		{
			return Matcher<T>.Single(predicate, description);
		}
	}
}
