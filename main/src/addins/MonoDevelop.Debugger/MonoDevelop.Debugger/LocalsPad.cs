// LocalsPad.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using Mono.Debugging.Client;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	public class LocalsPad : ObjectValuePad
	{
		Dictionary<string, ObjectValue> lastLookup = new Dictionary<string, ObjectValue> ();
		StackFrame lastFrame;
		
		public LocalsPad ()
		{
			tree.AllowEditing = true;
			tree.AllowAdding = false;
		}

		public override void OnUpdateList ()
		{
			base.OnUpdateList ();

			var frame = DebuggingService.CurrentFrame;
			
			if (frame == null || !FrameEquals (frame, lastFrame)) {
				tree.ClearExpressions ();
				lastLookup = null;
			}

			lastFrame = frame;
			
			if (frame == null)
				return;

			//add expressions not in tree already, remove expressions that are longer valid
			var frameLocals = frame.GetAllLocals ();
			var lookup = new Dictionary<string, ObjectValue> (frameLocals.Length);

			foreach (var local in frameLocals) {
				var variableName = local.Name;

				//not sure if there is a use case for duplicate variable names, or blanks 
				if (string.IsNullOrWhiteSpace (variableName) || variableName == "?" || lookup.ContainsKey (variableName))
					continue;

				lookup.Add (variableName, local);

				if (lastLookup != null) {
					ObjectValue priorValue;
					if (lastLookup.TryGetValue (variableName, out priorValue))
						tree.ReplaceValue (priorValue, local);
					else
						tree.AddValue (local);
				}
			}

			if (lastLookup != null) {
				//get rid of the values that didnt survive from the last refresh
				foreach (var prior in lastLookup) {
					if (!lookup.ContainsKey (prior.Key))
						tree.RemoveValue (prior.Value);
				}
			} else {
				tree.ClearValues ();
				tree.AddValues (lookup.Values);
			}

			lastLookup = lookup;
		}
		
		static bool FrameEquals (StackFrame a, StackFrame z)
		{
			if (null == a || null == z)
				return a == z;

			if (a.SourceLocation == null || z.SourceLocation == null)
				return a.SourceLocation == z.SourceLocation;

			if (a.SourceLocation.FileName == null) {
				if (z.SourceLocation.FileName != null)
					return false;
			} else {
				if (!a.SourceLocation.FileName.Equals (z.SourceLocation.FileName, StringComparison.Ordinal))
					return false;
			}

			if (a.SourceLocation.MethodName == null) {
				if (z.SourceLocation.MethodName != null)
					return false;

				return true;
			}

			return a.SourceLocation.MethodName.Equals (z.SourceLocation.MethodName, StringComparison.Ordinal);
		}
	}
}
