/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>
	/// Formatter to format timestamps relative to the current time using time units
	/// in the format defined by
	/// <code>git log --relative-date</code>
	/// .
	/// </summary>
	public class RelativeDateFormatter
	{
		internal const long SECOND_IN_MILLIS = 1000;

		internal const long MINUTE_IN_MILLIS = 60 * SECOND_IN_MILLIS;

		internal const long HOUR_IN_MILLIS = 60 * MINUTE_IN_MILLIS;

		internal const long DAY_IN_MILLIS = 24 * HOUR_IN_MILLIS;

		internal const long WEEK_IN_MILLIS = 7 * DAY_IN_MILLIS;

		internal const long MONTH_IN_MILLIS = 30 * DAY_IN_MILLIS;

		internal const long YEAR_IN_MILLIS = 365 * DAY_IN_MILLIS;

		/// <param name="when">
		/// <see cref="System.DateTime">System.DateTime</see>
		/// to format
		/// </param>
		/// <returns>
		/// age of given
		/// <see cref="System.DateTime">System.DateTime</see>
		/// compared to now formatted in the same
		/// relative format as returned by
		/// <code>git log --relative-date</code>
		/// </returns>
		public static string Format(DateTime when)
		{
			long ageMillis = SystemReader.GetInstance().GetCurrentTime() - when.GetTime();
			// shouldn't happen in a perfect world
			if (ageMillis < 0)
			{
				return JGitText.Get().inTheFuture;
			}
			// seconds
			if (ageMillis < UpperLimit(MINUTE_IN_MILLIS))
			{
				return MessageFormat.Format(JGitText.Get().secondsAgo, Round(ageMillis, SECOND_IN_MILLIS
					));
			}
			// minutes
			if (ageMillis < UpperLimit(HOUR_IN_MILLIS))
			{
				return MessageFormat.Format(JGitText.Get().minutesAgo, Round(ageMillis, MINUTE_IN_MILLIS
					));
			}
			// hours
			if (ageMillis < UpperLimit(DAY_IN_MILLIS))
			{
				return MessageFormat.Format(JGitText.Get().hoursAgo, Round(ageMillis, HOUR_IN_MILLIS
					));
			}
			// up to 14 days use days
			if (ageMillis < 14 * DAY_IN_MILLIS)
			{
				return MessageFormat.Format(JGitText.Get().daysAgo, Round(ageMillis, DAY_IN_MILLIS
					));
			}
			// up to 10 weeks use weeks
			if (ageMillis < 10 * WEEK_IN_MILLIS)
			{
				return MessageFormat.Format(JGitText.Get().weeksAgo, Round(ageMillis, WEEK_IN_MILLIS
					));
			}
			// months
			if (ageMillis < YEAR_IN_MILLIS)
			{
				return MessageFormat.Format(JGitText.Get().monthsAgo, Round(ageMillis, MONTH_IN_MILLIS
					));
			}
			// up to 5 years use "year, months" rounded to months
			if (ageMillis < 5 * YEAR_IN_MILLIS)
			{
				long years = ageMillis / YEAR_IN_MILLIS;
				string yearLabel = (years > 1) ? JGitText.Get().years : JGitText.Get().year;
				//
				long months = Round(ageMillis % YEAR_IN_MILLIS, MONTH_IN_MILLIS);
				string monthLabel = (months > 1) ? JGitText.Get().months : (months == 1 ? JGitText
					.Get().month : string.Empty);
				//
				return MessageFormat.Format(months == 0 ? JGitText.Get().years0MonthsAgo : JGitText
					.Get().yearsMonthsAgo, new object[] { years, yearLabel, months, monthLabel });
			}
			// years
			return MessageFormat.Format(JGitText.Get().yearsAgo, Round(ageMillis, YEAR_IN_MILLIS
				));
		}

		private static long UpperLimit(long unit)
		{
			long limit = unit + unit / 2;
			return limit;
		}

		private static long Round(long n, long unit)
		{
			long rounded = (n + unit / 2) / unit;
			return rounded;
		}
	}
}
