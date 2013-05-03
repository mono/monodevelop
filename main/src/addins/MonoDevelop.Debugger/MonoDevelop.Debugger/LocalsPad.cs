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
using System.Linq;
using Mono.Debugging.Client;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	public class LocalsPad: ObjectValuePad
	{
		StackFrame lastFrame;
		HashSet<string> lastExpressions = new HashSet<string> ();
		
		public LocalsPad ()
		{
			tree.AllowEditing = true;
			tree.AllowAdding = false;
		}

		public override void OnUpdateList ()
		{
			base.OnUpdateList ();
			StackFrame frame = DebuggingService.CurrentFrame;
			
			if (frame == null || !FrameEquals (frame, lastFrame)) {
				tree.ClearExpressions ();
				lastExpressions = null;
			}
			lastFrame = frame;
			
			if (frame == null)
				return;
			
			//FIXME: tree should use the local refs rather than expressions. ATM we exclude items without names
			var expr = new HashSet<string> (frame.GetAllLocals ().Select (i => i.Name)
				.Where (n => !string.IsNullOrEmpty (n) && n != "?"));
			
			//add expressions not in tree already, remove expressions that are longer valid
			if (lastExpressions != null) {
				foreach (string rem in lastExpressions.Except (expr))
					tree.RemoveExpression (rem);
				foreach (string rem in expr.Except (lastExpressions))
					tree.AddExpression (rem);
			} else {
				tree.AddExpressions (expr);
			}
			
			lastExpressions = expr;
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
			} else {
				return a.SourceLocation.MethodName.Equals (z.SourceLocation.MethodName, StringComparison.Ordinal);
			}
		}
	}
}
