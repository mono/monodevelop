//
// Schema/SequenceSchema.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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

namespace MonoDevelop.Database.Sql
{
	public class SequenceSchema : AbstractSchema
	{
		protected string minValue;
		protected string maxValue;
		protected string currentValue;
		protected string increment;
		
		public SequenceSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public SequenceSchema (SequenceSchema seq)
			: base (seq)
		{
			this.minValue = seq.minValue;
			this.maxValue = seq.maxValue;
			this.currentValue = seq.currentValue;
			this.increment = seq.increment;
		}
		
		public string MinValue {
			get { return minValue; }
			set {
				if (minValue != value) {
					minValue = value;
					OnChanged();
				}
			}
		}
		
		public string MaxValue {
			get { return maxValue; }
			set {
				if (maxValue != value) {
					maxValue = value;
					OnChanged();
				}
			}
		}
		
		public string CurrentValue {
			get { return currentValue; }
			set {
				if (currentValue != value) {
					currentValue = value;
					OnChanged();
				}
			}
		}
		
		public string Increment {
			get { return increment; }
			set {
				if (increment != value) {
					increment = value;
					OnChanged();
				}
			}
		}
		
		public override object Clone ()
		{
			return new SequenceSchema (this);
		}
	}
}
