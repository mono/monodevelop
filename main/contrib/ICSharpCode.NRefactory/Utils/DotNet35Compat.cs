// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

internal static class DotNet35Compat
{
	public static string StringJoin<T>(string separator, IEnumerable<T> elements)
	{
		#if DOTNET35
		return string.Join(separator, elements.Select(e => e != null ? e.ToString() : null).ToArray());
		#else
		return string.Join(separator, elements);
		#endif
	}
	
	public static IEnumerable<U> SafeCast<T, U>(this IEnumerable<T> elements) where T : class, U where U : class
	{
		#if DOTNET35
		foreach (T item in elements)
			yield return item;
		#else
		return elements;
		#endif
	}
	
	public static Predicate<U> SafeCast<T, U>(this Predicate<T> predicate) where U : class, T where T : class
	{
		#if DOTNET35
		return e => predicate(e);
		#else
		return predicate;
		#endif
	}
	
	#if DOTNET35
	public static IEnumerable<R> Zip<T1, T2, R>(this IEnumerable<T1> input1, IEnumerable<T2> input2, Func<T1, T2, R> f)
	{
		using (var e1 = input1.GetEnumerator())
			using (var e2 = input2.GetEnumerator())
				while (e1.MoveNext() && e2.MoveNext())
					yield return f(e1.Current, e2.Current);
	}
	#endif
}

#if DOTNET35
namespace System.Diagnostics.Contracts { }
namespace System.Threading
{
	internal struct CancellationToken
	{
		public void ThrowIfCancellationRequested() {}
	}
}
#endif
