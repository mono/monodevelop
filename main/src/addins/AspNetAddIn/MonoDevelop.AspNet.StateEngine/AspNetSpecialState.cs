// 
// AspNetSpecialState.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	
	public class AspNetSpecialState : State
	{
		
		Kind mode = Kind.Undetermined;
		
		public AspNetSpecialState (State parent, int position)
			: base (parent, position)
		{
		}
		
		public override State PushChar (char c, int position, out bool reject)
		{
			reject = true;
			int index = position - StartLocation;
			
			if (c == '<' || c == '>')
				return Parent;
			
			return new XmlMalformedTagState (this, position);
		}
		
		enum Kind {
			Undetermined,
			RenderExpression,
			RenderBlock,
			DatabindingExpression,
			ResourceExpression,
			ServerComment
		}

		public override string ToString ()
		{
			return string.Format ("[AspNetSpecial]");
		}
		
		#region Cloning API
		
		public override State ShallowCopy ()
		{
			return new AspNetSpecialState (this);
		}
		
		protected AspNetSpecialState (AspNetSpecialState copyFrom) : base (copyFrom)
		{
			mode = copyFrom.mode;
		}
		
		#endregion
	}
}
