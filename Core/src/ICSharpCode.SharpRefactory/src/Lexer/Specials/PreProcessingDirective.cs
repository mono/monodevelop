using System;
using System.Drawing;
using System.Text;
using System.CodeDom;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class PreProcessingDirective
	{
		string cmd;
		string arg;
		Point  start;
		Point  end;
		
		public Point Start {
			get {
				return start;
			}
			set {
				start = value;
			}
		}
		
		public Point End {
			get {
				return end;
			}
			set {
				end = value;
			}
		}
		
		public string Cmd {
			get {
				return cmd;
			}
			set {
				cmd = value;
			}
		}
		
		public string Arg {
			get {
				return arg;
			}
			set {
				arg = value;
			}
		}
		public override string ToString()
		{
			return String.Format("[PreProcessingDirective: Cmd = {0}, Arg = {1}]",
			                     Cmd,
			                     Arg);
		}
		
		public PreProcessingDirective(string cmd, string arg, Point start, Point end)
		{
			this.cmd = cmd;
			this.arg = arg;
			this.start = start;
			this.end = end;
		}
	}
}
