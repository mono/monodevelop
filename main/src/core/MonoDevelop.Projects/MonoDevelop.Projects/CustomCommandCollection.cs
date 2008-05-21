// CustomCommandCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class CustomCommandCollection: List<CustomCommand>
	{
		public CustomCommandCollection Clone ()
		{
			CustomCommandCollection col = new CustomCommandCollection ();
			col.CopyFrom (this);
			return col;
		}
		
		public void CopyFrom (CustomCommandCollection col)
		{
			Clear ();
			foreach (CustomCommand cmd in col)
				Add (cmd.Clone ());
		}
		
		public void ExecuteCommand (IProgressMonitor monitor, SolutionEntityItem entry, CustomCommandType type, string configuration)
		{
			ExecuteCommand (monitor, entry, type, null, configuration);
		}
		
		public void ExecuteCommand (IProgressMonitor monitor, SolutionEntityItem entry, CustomCommandType type, ExecutionContext context, string configuration)
		{
			foreach (CustomCommand cmd in this) {
				if (cmd.Type == type)
					cmd.Execute (monitor, entry, context, configuration);
				if (monitor.IsCancelRequested)
					break;
			}
		}
		
		public bool HasCommands (CustomCommandType type)
		{
			foreach (CustomCommand cmd in this)
				if (cmd.Type == type)
					return true;
			return false;
		}
	}
}
