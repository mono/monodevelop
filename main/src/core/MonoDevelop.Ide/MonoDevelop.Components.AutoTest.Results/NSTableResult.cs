//
// NSTableResult.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc.
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
#if MAC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using AppKit;
using Foundation;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class NSTableResult : NSObjectResult
	{
		NSTableView table;
		nint row = -1;
		string columnName;
		NSTableColumn column;
		
		internal NSTableResult (NSObject resultObject) : base (resultObject)
		{
			table = resultObject as NSTableView;
		}
		
		internal NSTableResult (NSTableView resultObject, nint row) : base (resultObject)
		{
			table = resultObject;
			this.row = row;
		}
		
		internal NSTableResult (NSObject resultObject, string columnName) : base (resultObject)
		{
			table = resultObject as NSTableView;
			this.columnName = columnName;
			this.column = table.TableColumns ().FirstOrDefault (x => x.Identifier == columnName || x.Title == columnName);
		}

		public override List<AppResult> Children (bool recursive = true)
		{
			var list = new List<AppResult> ();
			for (int i = 0; i < table.RowCount;i ++)
				list.Add (new NSTableResult(table, i));
			return list;
		}

		public override bool Select ()
		{
			base.Select ();
			if (row > -1) {
				table.SelectRow (row, true);
				return true;
			}
			return false;
		}
	}
}
#endif
