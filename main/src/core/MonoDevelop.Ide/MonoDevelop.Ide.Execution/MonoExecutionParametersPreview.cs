// 
// MonoExecutionParametersPreview.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Ide.Execution;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Execution
{
	partial class MonoExecutionParametersPreview : Gtk.Dialog
	{

		public MonoExecutionParametersPreview (MonoExecutionParameters options)
		{
			this.Build ();
			
			string cmd;
			Dictionary<string,string> vars = new Dictionary<string, string> ();
			options.GenerateOptions (vars, out cmd);
			
			StringBuilder sb = StringBuilderCache.Allocate ();
			
			if (cmd.Length == 0 && vars.Count == 0) {
				sb.AppendLine (GLib.Markup.EscapeText (GettextCatalog.GetString ("No options have been specified.")));
			}
			
			if (cmd.Length > 0) {
				sb.Append ("<b>").Append (GettextCatalog.GetString ("Command Line Options")).Append ("</b>\n");
				sb.AppendLine ();
				sb.AppendLine (GLib.Markup.EscapeText (cmd));
				sb.AppendLine ();
			}
			
			if (vars.Count > 0) {
				sb.Append ("<b>").Append (GettextCatalog.GetString ("Environment Variables")).Append ("</b>\n");
				sb.AppendLine ();
				List<string> svars = new List<string> ();
				foreach (KeyValuePair<string,string> var in vars)
					svars.Add (GLib.Markup.EscapeText (var.Key) + " = " + GLib.Markup.EscapeText (var.Value));
				svars.Sort ();
				foreach (string svar in svars)
					sb.AppendLine (svar);
			}
			
			labelOps.Markup = StringBuilderCache.ReturnAndFree (sb);
		}
	}
}
