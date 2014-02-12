// 
// PinnedWatch.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger
{
	public class PinnedWatch
	{
		[ProjectPathItemProperty]
		FilePath file;
		
		[ItemProperty]
		int line;
		
		[ItemProperty (DefaultValue = 0)]
		int offsetX;
		
		[ItemProperty (DefaultValue = 0)]
		int offsetY;
		
		[ItemProperty]
		string expression;
		
		[ItemProperty]
		bool liveUpdate;
		
		ObjectValue value;
		bool evaluated;
		
		internal Breakpoint BoundTracer;
		
		public PinnedWatch ()
		{
		}
		
		internal PinnedWatchStore Store {
			get; set;
		}
		
		internal void Evaluate (bool notify)
		{
			if (DebuggingService.CurrentFrame != null) {
				evaluated = true;
				value = DebuggingService.CurrentFrame.GetExpressionValue (expression, true);
				value.Name = expression;
				if (notify)
					NotifyChanged ();
			}
		}
		
		internal void UpdateFromTrace (string trace)
		{
			EvaluationResult res = new EvaluationResult (trace);
			ObjectValueFlags flags = ObjectValueFlags.Primitive | ObjectValueFlags.Field;
			string type = "";
			if (value != null) {
				flags = value.Flags;
				type = value.TypeName;
			}
			value = ObjectValue.CreatePrimitive (null, new ObjectPath (Expression), type, res, flags);
			evaluated = true;
			NotifyChanged ();
		}
		
		internal void Invalidate ()
		{
			value = null;
			evaluated = false;
			NotifyChanged ();
		}
		
		public FilePath File {
			get { return file; }
			set { file = value; NotifyChanged (); }
		}
		
		public bool LiveUpdate {
			get { return liveUpdate; }
			internal set { liveUpdate = value; }
		}

		public int Line {
			get { return line; }
			set { line = value; NotifyChanged (); }
		}

		public int OffsetX {
			get { return offsetX; }
			set { offsetX = value; NotifyChanged (); }
		}

		public int OffsetY {
			get { return offsetY; }
			set { offsetY = value; NotifyChanged (); }
		}
		
		internal void LoadValue (ObjectValue val)
		{
			value = val;
			value.Name = expression;
			evaluated = true;
		}
		
		public ObjectValue Value {
			get {
				if (!evaluated)
					Evaluate (false);
				return value; 
			}
			set {
				evaluated = true;
				this.value = value;
				value.Name = expression;
				NotifyChanged (); 
			}
		}
		
		public string Expression {
			get { return expression; }
			set {
				if (expression != value) {
					evaluated = false;
					expression = value;
					NotifyChanged (); 
				}
			}
		}
		
		void NotifyChanged ()
		{
			if (Store != null)
				Store.NotifyWatchChanged (this);
		}
	}
}

