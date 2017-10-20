// 
// UpdateLevel.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core.Setup
{
	public class UpdateLevel
	{
		public static readonly UpdateLevel Stable = new UpdateLevel ("Stable", "Stable", "", 0);
		public static readonly UpdateLevel Beta = new UpdateLevel ("Beta", "Beta", "", 1);
		public static readonly UpdateLevel Alpha = new UpdateLevel ("Alpha", "Alpha", "", 2);
		public static readonly UpdateLevel Test = new UpdateLevel ("Test", "Test", "", 100);
		public static readonly UpdateLevel [] DefaultLevels = { Stable, Beta, Alpha };

		public string Id { get; set; }
		public string Name { get; set; }
		public string BannerMessage { get; set; }
		public int Idx { get; set; }

		public UpdateLevel (string id, string name, string bannerMessage, int idx)
		{
			this.Id = id;
			this.Name = name;
			this.BannerMessage = bannerMessage;
			this.Idx = idx;
		}

		public UpdateLevel () { }

		public override bool Equals (System.Object obj)
		{
			if (obj == null) {
				return false;
			}

			UpdateLevel a = obj as UpdateLevel;
			if ((System.Object)a == null) {
				return false;
			}

			return a.Idx == Idx;
		}

		public bool Equals (UpdateLevel a)
		{
			if ((object)a == null) {
				return false;
			}
			return (a.Idx == Idx);
		}

		public static bool operator == (UpdateLevel a, UpdateLevel b)
		{
			if (Object.ReferenceEquals (a, null) && Object.ReferenceEquals (b, null)) {
				return true;
			}
			if (Object.ReferenceEquals (a, null) || Object.ReferenceEquals (b, null)) {
				return false;
			}
			return a.Idx == b.Idx;
		}

		public static bool operator != (UpdateLevel a, UpdateLevel b)
		{
			if (Object.ReferenceEquals (a, null) && Object.ReferenceEquals (b, null)) {
				return false;
			}
			if (Object.ReferenceEquals (a, null) || Object.ReferenceEquals (b, null)) {
				return true;
			}
			return a.Idx != b.Idx;
		}

		public static bool operator <= (UpdateLevel a, UpdateLevel b)
		{
			if (Object.ReferenceEquals (a, null)) {
				return true;
			}
			if (Object.ReferenceEquals (b, null)) {
				return false;
			}
			return a.Idx <= b.Idx;
		}

		public static bool operator >= (UpdateLevel a, UpdateLevel b)
		{
			if (Object.ReferenceEquals (a, null))
				return false;
			if (Object.ReferenceEquals (b, null))
				return true;
			return a.Idx >= b.Idx;
		}


		public override int GetHashCode ()
		{
			return Idx;
		}

		public override string ToString ()
		{
			return Id;
		}

		public int ToInt32 ()
		{
			return Idx;
		}

		public static implicit operator int (UpdateLevel a)
		{
			return a.ToInt32 ();
		}

		public static implicit operator string (UpdateLevel a)
		{
			return a.ToString ();
		}
	}
}
