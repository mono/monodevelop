﻿//
// AccessibilityUtilities.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 
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
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	internal static class AccessibilityUtilities
	{
		public static Accessibility Minimum(Accessibility accessibility1, Accessibility accessibility2)
		{
			if (accessibility1 == Accessibility.Private || accessibility2 == Accessibility.Private)
			{
				return Accessibility.Private;
			}

			if (accessibility1 == Accessibility.ProtectedAndInternal || accessibility2 == Accessibility.ProtectedAndInternal)
			{
				return Accessibility.ProtectedAndInternal;
			}

			if (accessibility1 == Accessibility.Protected || accessibility2 == Accessibility.Protected)
			{
				return Accessibility.Protected;
			}

			if (accessibility1 == Accessibility.Internal || accessibility2 == Accessibility.Internal)
			{
				return Accessibility.Internal;
			}

			if (accessibility1 == Accessibility.ProtectedOrInternal || accessibility2 == Accessibility.ProtectedOrInternal)
			{
				return Accessibility.ProtectedOrInternal;
			}

			return Accessibility.Public;
		}
	}
}

