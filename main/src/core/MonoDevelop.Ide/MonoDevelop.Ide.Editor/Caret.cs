//
// Caret.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.Editor
{
	public abstract class Caret
	{
		public abstract DocumentLocation Location {
			get;
			set;
		}

		public virtual int Line {
			get {
				return Location.Line;
			}
			set {
				Location = new DocumentLocation (value, Column);
			}
		}

		public virtual int Column {
			get {
				return Location.Column;
			}
			set {
				Location = new DocumentLocation (Line, value);
			}
		}

		public abstract int Offset {
			get;
			set;
		}

		protected virtual void OnPositionChanged (CaretLocationEventArgs args)
		{
			if (PositionChanged != null)
				PositionChanged (this, args);
		}

		public event EventHandler<CaretLocationEventArgs> PositionChanged;
	}

	public enum CaretChangeReason {
		BufferChange,
		Movement
	}

	public class CaretLocationEventArgs : DocumentLocationEventArgs
	{
		public CaretChangeReason CaretChangeReason { get; } 

		public CaretLocationEventArgs (DocumentLocation location, CaretChangeReason reason) : base (location)
		{
			CaretChangeReason = reason;
		}
	}
}