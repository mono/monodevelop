//
// TestNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.NUnit
{
	public class UnitTestResultsStore
	{
		UnitTest test;
		IResultsStore store;
		
		internal UnitTestResultsStore (UnitTest test, IResultsStore store)
		{
			this.test = test;
			this.store = store;
		}
		
		public UnitTestResult GetLastResult (DateTime date)
		{
			if (store == null) return null;
			return store.GetLastResult (test.ActiveConfiguration, test, date);
		}
		
		public UnitTestResult GetNextResult (DateTime date)
		{
			if (store == null) return null;
			return store.GetNextResult (test.ActiveConfiguration, test, date);
		}
		
		public UnitTestResult GetPreviousResult (DateTime date)
		{
			if (store == null) return null;
			return store.GetPreviousResult (test.ActiveConfiguration, test, date);
		}
		
		public UnitTestResult[] GetResults (DateTime startDate, DateTime endDate)
		{
			if (store == null) return new UnitTestResult [0];
			return store.GetResults (test.ActiveConfiguration, test, startDate, endDate);
		}
		
		public UnitTestResult[] GetResultsToDate (DateTime endDate, int count)
		{
			if (store == null) return new UnitTestResult [0];
			return store.GetResultsToDate (test.ActiveConfiguration, test, endDate, count);
		}
	}
}

