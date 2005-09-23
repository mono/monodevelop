//
// IntegerAxis.cs
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

namespace MonoDevelop.Gui.Widgets.Chart
{
	public class IntegerAxis: Axis
	{
		public IntegerAxis ()
		{
		}
		
		public IntegerAxis (bool showLabels): base (showLabels)
		{
		}
		
		protected override TickEnumerator CreateTickEnumerator (double minTickStep)
		{
			long val = (long) minTickStep;
			long red = 10;
			while (val > red)
				red = red * 10;
			
			long scale;
			
			if (val <= 1)
				scale = 1;
			else if (val > red / 2)
				scale = red;
			else if (val > red / 4)
				scale = red / 2;
			else
				scale = red / 4;
			return new IntegerTickEnumerator (scale);
		}
	}
	
	internal class IntegerTickEnumerator: TickEnumerator
	{
		long scale;
		long current;
		
		public IntegerTickEnumerator (long scale)
		{
			this.scale = scale;
		}
		
		public override void Init (double startValue)
		{
			current = (((long)startValue) / scale) * scale;
		}
		
		public override void MoveNext ()
		{
			current += scale;
		}
		
		public override void MovePrevious ()
		{
			current -= scale;
		}
		
		public override double CurrentValue {
			get { return (double) current; }
		}
	}
}
