using System;
using Mono.Debugging.Backend;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class StackFrame
	{
		long address;
		SourceLocation location;
		IBacktrace sourceBacktrace;
		string language;
		int index;

		public StackFrame (long address, SourceLocation location, string language)
		{
			this.address = address;
			this.location = location;
			this.language = language;
		}

		public StackFrame(long address, string module, string method, string filename, int line, string language)
		{
			this.location = new SourceLocation(method, filename, line);
			this.address = address;
			this.language = language;
		}

		public SourceLocation SourceLocation
		{
			get { return location; }
		}

		public long Address
		{
			get { return address; }
		}

		internal IBacktrace SourceBacktrace {
			get { return sourceBacktrace; }
			set { sourceBacktrace = value; }
		}

		internal int Index {
			get { return index; }
			set { index = value; }
		}

		public string Language {
			get {
				return language;
			}
		}
		
		public ObjectValue[] GetLocalVariables ()
		{
			return sourceBacktrace.GetLocalVariables (index);
		}
		
		public ObjectValue[] GetParameters ()
		{
			return sourceBacktrace.GetParameters (index);
		}
		
		public ObjectValue GetThisReference ()
		{
			return sourceBacktrace.GetThisReference (index);
		}
		
		public ObjectValue[] GetExpressionValues (string[] expressions, bool evaluateMethods)
		{
			return sourceBacktrace.GetExpressionValues (index, expressions, evaluateMethods);
		}
		
		public ObjectValue GetExpressionValue (string expression, bool evaluateMethods)
		{
			ObjectValue[] values = sourceBacktrace.GetExpressionValues (index, new string[] { expression }, evaluateMethods);
			return values [0];
		}
		
		// Returns disassembled code for this stack frame.
		// firstLine is the relative code line. It can be negative.
		public AssemblyLine[] Disassemble (int firstLine, int count)
		{
			return sourceBacktrace.Disassemble (index, firstLine, count);
		}

		public override string ToString()
		{
			string loc;
			if (location.Line != -1 && !string.IsNullOrEmpty (location.Filename))
				loc = " at " + location.Filename + ":" + location.Line;
			else if (!string.IsNullOrEmpty (location.Filename))
				loc = " at " + location.Filename;
			else
				loc = "";
			return String.Format("0x{0:X} in {1}{2}", address, location.Method, loc);
		}
	}
}
