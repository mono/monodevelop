 /* 
 * CommandManager.cs - The C# side of the JSCall Gecko#/C# glue layer
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005-2007 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Gecko;
using System.Text;

namespace AspNetEdit.JSCall
{
	public class CommandManager
	{
		private const char delimiter = (char)234;
		
		private Hashtable functions = new Hashtable ();
		private WebControl webControl;

		public CommandManager (WebControl control)
		{
			if (control == null)
				throw new ArgumentNullException ("The Command Manager must be bound to a WebControl instance.", "control");
			
			webControl = control;
			webControl.TitleChange += new EventHandler (webControl_ECMAStatus);	
		}
		
		private void webControl_ECMAStatus (object sender, EventArgs e)
		{
			if (!webControl.Title.StartsWith ("JSCall"))
				return;
			
			string[] call = webControl.Title.Split (delimiter);
			if (call.Length < 2)
				throw new Exception ("Too few parameters in call from JavaScript.");
				
			string function = call[1];
			string returnTo = call[2];

			string[] args = (string[]) System.Array.CreateInstance (typeof(String), (call.Length - 3));
			System.Array.Copy (call, 3, args, 0, (call.Length - 3));
			
			if (!functions.Contains (function))
				throw new Exception ("Unknown function name called from JavaScript.");
			
			ClrCall clrCall = (ClrCall) functions[function];
			

			if (returnTo.Length == 0)
			{
				clrCall (args);
			}
			else
			{
				string[] result = { clrCall (args) };
				JSCall(returnTo, null, result);
			}
		}
		
		public void JSEval (string script)
		{
			if ((script == null) || (script.Length < 1))
				throw new ArgumentNullException ("A null or empty script cannot be executed.", "script");
			
			webControl.LoadUrl ("javascript:" + script);
		}
		
		public void JSCall (string function, string returnTo, params string[] args)
		{
			if ((function==null) || (function.Length < 1))
				throw new ArgumentException ("A function name must be specified.", "function");
			
			StringBuilder sb = new StringBuilder ();
			sb.Append ("javascript: ");
			
			//wrap the call in a function to handle the return value
			if (returnTo != null)
				sb.AppendFormat ("JSCallPlaceClrCall(\"{0}\", \"\", new Array( ", returnTo);
			
			//call the function
			sb.AppendFormat ("{0} (", function);
			
			//add the arguments
			if (args != null) {
				for (int i = 0; i < args.Length - 1; i++)				
					sb.AppendFormat ("\"{0}\", ", escapeJSString (args[i]));
				sb.AppendFormat ("\"{0}\"", escapeJSString (args[args.Length-1]));
			}
			
			//close the function call			
			sb.Append (")");
			
			//end return wrapper
			if (returnTo != null)
				sb.Append (") )");
			sb.Append(";");
			
			System.Diagnostics.Trace.WriteLine(sb.ToString ());
			
			webControl.LoadUrl (sb.ToString ());
		}
		
		private string escapeJSString (string s)
		{
			return s.Replace ("\\", "\\\\")
			        .Replace ("\"", "\\\"")
			        .Replace ("\n", "\\n");
		}

		public void RegisterJSHandler (string name, ClrCall handler)
		{
			if (!functions.Contains (name))
			{
				functions.Add (name, handler);
			}
			else
			{
				throw new Exception ("A handler with this name already exists.");
			}

		}

		public void UnregisterJSHandler (string name)
		{
			if (functions.Contains (name))
			{
				functions.Remove (name);
			}
			else
			{
				throw new IndexOutOfRangeException ("A function with this name has not been registered.");
			}
		}
	}

	public delegate string ClrCall (string[] args);
}
