//
// Utilities.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
//
//using System;
//
//namespace Hyena
//{
//    public class DateTimeUtil
//    {
//        public static readonly DateTime LocalUnixEpoch = new DateTime (1970, 1, 1).ToLocalTime ();
//
//        public static DateTime ToDateTime (long time)
//        {
//            return FromTimeT (time);
//        }
//
//        public static long FromDateTime (DateTime time)
//        {
//            return ToTimeT (time);
//        }
//
//        private static long super_ugly_min_hack = -15768000000; // 500 yrs before epoch...ewww
//        public static DateTime FromTimeT (long time)
//        {
//            return (time <= super_ugly_min_hack) ? DateTime.MinValue : LocalUnixEpoch.AddSeconds (time);
//        }
//
//        public static long ToTimeT (DateTime time)
//        {
//            return (long)time.Subtract (LocalUnixEpoch).TotalSeconds;
//        }
//
//        public static string FormatDuration (long time) {
//            return (time > 3600 ? 
//                    String.Format ("{0}:{1:00}:{2:00}", time / 3600, (time / 60) % 60, time % 60) :
//                    String.Format ("{0}:{1:00}", time / 60, time % 60));
//        }
//    }
//}