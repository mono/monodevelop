#if NET_2_0
using System;

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using Mono.Debugger;
using Mono.Debugger.Languages;

using RefParse = ICSharpCode.SharpRefactory.Parser;
using AST = ICSharpCode.SharpRefactory.Parser.AST;

namespace MonoDevelop.Debugger {

	public class DebugAttributeHandler
	{
		public void ScanDirectory (string dir)
		{
			DirectoryInfo info = new DirectoryInfo (dir);
			FileInfo[] dlls = info.GetFiles ("*.dll");

			foreach (FileInfo dll_info in dlls) {
				Assembly a = Assembly.LoadFile (dll_info.FullName);
		
				DebuggerDisplayAttribute[] display_attrs = (DebuggerDisplayAttribute[])a.GetCustomAttributes(typeof (DebuggerDisplayAttribute),
															     false);
				DebuggerTypeProxyAttribute[] proxy_attrs = (DebuggerTypeProxyAttribute[])a.GetCustomAttributes(typeof (DebuggerTypeProxyAttribute),
															       false);
				DebuggerVisualizerAttribute[] viz_attrs = (DebuggerVisualizerAttribute[])a.GetCustomAttributes(typeof (DebuggerVisualizerAttribute),
															       false);

				foreach (DebuggerDisplayAttribute da in display_attrs) {
					if (display_by_type_name.ContainsKey (da.TargetTypeName))
						continue;

					display_by_type_name.Add (da.TargetTypeName, da);
				}

				foreach (DebuggerTypeProxyAttribute pa in proxy_attrs) {
					if (proxy_by_type_name.ContainsKey (pa.TargetTypeName))
						continue;

					proxy_by_type_name.Add (pa.TargetTypeName, pa);
				}

				foreach (DebuggerVisualizerAttribute va in viz_attrs) {
					ArrayList vas = (ArrayList)visualizers_by_type_name[va.TargetTypeName];
					if (vas == null) {
						vas = new ArrayList ();
						visualizers_by_type_name.Add (va.TargetTypeName, vas);
					}

					vas.Add (va);

					Console.WriteLine ("VISUALIZER ATTRIBUTE for type {0}", va.TargetTypeName);
				}
			}
		}

		public void Rescan () {

			display_by_type_name = new Hashtable ();
			proxy_by_type_name = new Hashtable ();
			visualizers_by_type_name = new Hashtable ();

			ScanDirectory (DebuggerPaths.VisualizerPath);
		}

		public string EvaluateDebuggerDisplay (ITargetObject obj, string display)
		{
			StringBuilder sb = new StringBuilder ("");
			DebuggingService dbgr = (DebuggingService)Runtime.DebuggingService;
			EvaluationContext ctx = new EvaluationContext (obj);

			ctx.CurrentProcess = new ProcessHandle (dbgr.MainThread);

			/* break up the string into runs of {...} and
			 * normal text.  treat the {...} as C#
			 * expressions, and evaluate them */
			int start_idx = 0;

			while (true) {
				int left_idx;
				int right_idx;
				left_idx = display.IndexOf ('{', start_idx);

				if (left_idx == -1) {
					/* we're done. */
					sb.Append (display.Substring (start_idx));
					break;
				}
				if (left_idx != start_idx) {
					sb.Append (display.Substring (start_idx, left_idx - start_idx));
				}
				right_idx = display.IndexOf ('}', left_idx + 1);
				if (right_idx == -1) {
					// '{...\0'.  ignore the '{', append the rest, and break out */
					sb.Append (display.Substring (left_idx + 1));
					break;
				}

				if (right_idx - left_idx > 1) {
					/* there's enough space for an
					 * expression.  parse it and see
					 * what we get. */
					RefParse.Parser parser;
					AST.Expression ast_expr;
					Expression dbgr_expr;
					DebuggerASTVisitor visitor;
					string snippet;
					object retval;

					/* parse the snippet to build up MD's AST */
					parser = new RefParse.Parser();

					snippet = display.Substring (left_idx + 1, right_idx - left_idx - 1);
					ast_expr = parser.ParseExpression (new RefParse.Lexer (new RefParse.StringReader (snippet)));

					/* use our visitor to convert from MD's AST to types that
					 * facilitate evaluation by the debugger */
					visitor = new DebuggerASTVisitor ();
					dbgr_expr = (Expression)ast_expr.AcceptVisitor (visitor, null);

					/* finally, resolve and evaluate the expression */
					dbgr_expr = dbgr_expr.Resolve (ctx);
					retval = dbgr_expr.Evaluate (ctx);

#region "c&p'ed from debugger/frontend/Style.cs"
					if (retval is long) {
						sb.Append (String.Format ("0x{0:x}", (long) retval));
					}
					else if (retval is string) {
						sb.Append ('"' + (string) retval + '"');
					}
					else if (retval is ITargetObject) {
						ITargetObject tobj = (ITargetObject) retval;
						sb.Append (tobj.Print ());
					}
					else {
						sb.Append (retval.ToString ());
					}
#endregion
				}

				start_idx = right_idx + 1;
			}

			return sb.ToString ();
		}

		public DebuggerVisualizerAttribute[] GetDebuggerVisualizerAttributes (Type t)
		{
			DebuggerVisualizerAttribute[] vas;

	  		object[] attrs = t.GetCustomAttributes (typeof (DebuggerVisualizerAttribute), false);

			if (attrs != null && attrs.Length > 0) {
				vas = new DebuggerVisualizerAttribute[attrs.Length];
				attrs.CopyTo (vas, 0);
				return vas;
			}

			ArrayList varray = (ArrayList)visualizers_by_type_name[t.AssemblyQualifiedName];
			if (varray == null)
				return null;

			vas = new DebuggerVisualizerAttribute[varray.Count];
			varray.CopyTo (vas);

			return vas;
		}

		public DebuggerTypeProxyAttribute GetDebuggerTypeProxyAttribute (Type t)
		{
	  		object[] attrs = t.GetCustomAttributes (typeof (DebuggerTypeProxyAttribute), false);

			if (attrs != null && attrs.Length > 0)
				return (DebuggerTypeProxyAttribute)attrs[0];

			return proxy_by_type_name[t.AssemblyQualifiedName] as DebuggerTypeProxyAttribute;
		}

		public DebuggerDisplayAttribute GetDebuggerDisplayAttribute (Type t)
		{
	  		object[] attrs = t.GetCustomAttributes (typeof (DebuggerDisplayAttribute), false);

			if (attrs != null && attrs.Length > 0)
				return (DebuggerDisplayAttribute)attrs[0];

			return display_by_type_name[t.AssemblyQualifiedName] as DebuggerDisplayAttribute;
		
		}

		Hashtable display_by_type_name;
		Hashtable proxy_by_type_name;
		Hashtable visualizers_by_type_name;
	}
}
#endif
