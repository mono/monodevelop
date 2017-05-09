//
// Choreographer.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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

namespace Mono.TextEditor.Theatrics
{
    enum Blocking
    {
        Upstage,
        Downstage
    }
    
	enum Easing
    {
        Linear,
        QuadraticIn,
        QuadraticOut,
        QuadraticInOut,
        ExponentialIn,
        ExponentialOut,
        ExponentialInOut,
        Sine,
    }

    static class Choreographer
    {
        public static int PixelCompose (double percent, int size, Easing easing)
        {
            return (int)System.Math.Round (Compose (percent, size, easing));
        }

        public static double Compose (double percent, double scale, Easing easing)
        {
            return scale * Compose (percent, easing);
        }

        public static double Compose (double percent, Easing easing)
        {
            if (percent < 0.0 || percent > 1.0) {
                throw new ArgumentOutOfRangeException ("percent", "must be between 0 and 1 inclusive");
            }

            switch (easing) {
                case Easing.QuadraticIn:
                    return percent * percent;

                case Easing.QuadraticOut:
                    return -1.0 * percent * (percent - 2.0);

                case Easing.QuadraticInOut:
                    percent *= 2.0;
                    return percent < 1.0
                        ? percent * percent * 0.5
                        : -0.5 * (--percent * (percent - 2.0) - 1.0);

                case Easing.ExponentialIn:
                    return System.Math.Pow (2.0, 10.0 * (percent - 1.0));

                case Easing.ExponentialOut:
                    return -System.Math.Pow (2.0, -10.0 * percent) + 1.0;

                case Easing.ExponentialInOut:
                    percent *= 2.0;
                    return percent < 1.0
                        ? 0.5 * System.Math.Pow (2.0, 10.0 * (percent - 1.0))
                        : 0.5 * (-System.Math.Pow (2.0, -10.0 * --percent) + 2.0);

                case Easing.Sine:
                    return System.Math.Sin (percent * System.Math.PI);

                case Easing.Linear:
                default:
                    return percent;
            }
        }
    }
}