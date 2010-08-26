/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Globalization;

namespace GitSharp.Core.Util
{
    public static class DateTimeExtensions
    {
    	private static readonly long EPOCH_TICKS = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public static long ToMillisecondsSinceEpoch(this DateTimeOffset dateTimeOffset)
        {
            return ((dateTimeOffset.Ticks - dateTimeOffset.Offset.Ticks - EPOCH_TICKS) / TimeSpan.TicksPerMillisecond);
        }

        public static long ToMillisecondsSinceEpoch(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("dateTime is expected to be expressed as a UTC DateTime", "dateTime");
            }

            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TimeSpan.Zero).ToMillisecondsSinceEpoch();
        }

        public static DateTime MillisToUtcDateTime(this long milliSecondsSinceEpoch)
        {
            return milliSecondsSinceEpoch.MillisToDateTimeOffset(0).UtcDateTime;
        }

        public static DateTimeOffset MillisToDateTimeOffset(this long milliSecondsSinceEpoch, long offsetMinutes)
        {
            var offset = TimeSpan.FromMinutes(offsetMinutes);
            var utcTicks = EPOCH_TICKS + milliSecondsSinceEpoch * TimeSpan.TicksPerMillisecond;
            return new DateTimeOffset(utcTicks + offset.Ticks, offset);
        }

		/// <summary>
		/// Gets the DateTime in the sortable ISO format.
		/// </summary>
		/// <param name="when"></param>
		/// <returns></returns>
		public static string ToIsoDateFormat(this DateTime when)
		{
			return when.ToString("s", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Gets the DateTimeOffset in the sortable ISO format.
		/// </summary>
		/// <param name="when"></param>
		/// <returns></returns>
		public static string ToIsoDateFormat(this DateTimeOffset when)
		{
			return when.ToString("s", CultureInfo.InvariantCulture);
		}
    }
}
