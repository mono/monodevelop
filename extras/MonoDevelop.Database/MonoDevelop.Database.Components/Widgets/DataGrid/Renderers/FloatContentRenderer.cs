//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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
//

using Gtk;
using System;
using System.Collections;
using System.Data;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Components
{
	public class FloatContentRenderer : IDataGridContentRenderer
	{
		public Type[] DataTypes {
			get { return new Type[]{ typeof(float) }; }
		}

		public void SetContent (CellRendererText cell, object dataObject)
		{
			cell.Text = dataObject.ToString ();
		}

		public int Compare (object x, object y)
		{
			if (x == null && y == null) return 0;
			else if (x == null) return -1;
			else if (y == null) return 1;
			
			float fx = (float)x;
			return fx.CompareTo (y);
		}
	}
}