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
		bool isExternalCode;
		
		[NonSerialized]
		DebuggerSession session;

		public StackFrame (long address, SourceLocation location, string language, bool isExternalCode)
		{
			this.address = address;
			this.location = location;
			this.language = language;
			this.isExternalCode = isExternalCode;
		}
		
		public StackFrame (long address, SourceLocation location, string language)
			: this (address, location, language, string.IsNullOrEmpty (location.Filename))
		{
		}

		public StackFrame (long address, string module, string method, string filename, int line, string language)
			: this (address, new SourceLocation (method, filename, line), language)
		{
		}

		internal void Attach (DebuggerSession session)
		{
			this.session = session;
		}
		
		public DebuggerSession DebuggerSession {
			get { return session; }
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
		
		public bool IsExternalCode {
			get { return isExternalCode; }
		}
		
		public ObjectValue[] GetLocalVariables ()
		{
			return GetLocalVariables (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetLocalVariables (EvaluationOptions options)
		{
			ObjectValue[] values = sourceBacktrace.GetLocalVariables (index, options);
			ObjectValue.ConnectCallbacks (values);
			return values;
		}
		
		public ObjectValue[] GetParameters ()
		{
			return GetParameters (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetParameters (EvaluationOptions options)
		{
			ObjectValue[] values = sourceBacktrace.GetParameters (index, options);
			ObjectValue.ConnectCallbacks (values);
			return values;
		}
		
		public ObjectValue[] GetAllLocals ()
		{
			return GetAllLocals (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetAllLocals (EvaluationOptions options)
		{
			ObjectValue[] values = sourceBacktrace.GetAllLocals (index, options);
			ObjectValue.ConnectCallbacks (values);
			return values;
		}
		
		public ObjectValue GetThisReference ()
		{
			return GetThisReference (session.EvaluationOptions);
		}
		
		public ObjectValue GetThisReference (EvaluationOptions options)
		{
			ObjectValue value = sourceBacktrace.GetThisReference (index, options);
			ObjectValue.ConnectCallbacks (value);
			return value;
		}
		
		public string ResolveExpression (string exp)
		{
			return session.ResolveExpression (exp, location);
		}
		
		public ObjectValue[] GetExpressionValues (string[] expressions, bool evaluateMethods)
		{
			EvaluationOptions options = session.EvaluationOptions;
			options.AllowMethodEvaluation = evaluateMethods;
			return GetExpressionValues (expressions, options);
		}
		
		public ObjectValue[] GetExpressionValues (string[] expressions, EvaluationOptions options)
		{
			if (options.UseExternalTypeResolver) {
				string[] resolved = new string [expressions.Length];
				for (int n=0; n<expressions.Length; n++)
					resolved [n] = ResolveExpression (expressions [n]);
				expressions = resolved;
			}
			ObjectValue[] values = sourceBacktrace.GetExpressionValues (index, expressions, options);
			ObjectValue.ConnectCallbacks (values);
			return values;
		}
		
		public ObjectValue GetExpressionValue (string expression, bool evaluateMethods)
		{
			EvaluationOptions options = session.EvaluationOptions;
			options.AllowMethodEvaluation = evaluateMethods;
			return GetExpressionValue (expression, options);
		}
		
		public ObjectValue GetExpressionValue (string expression, EvaluationOptions options)
		{
			if (options.UseExternalTypeResolver)
				expression = ResolveExpression (expression);
			ObjectValue[] values = sourceBacktrace.GetExpressionValues (index, new string[] { expression }, options);
			ObjectValue.ConnectCallbacks (values);
			return values [0];
		}
		
		public CompletionData GetExpressionCompletionData (string exp)
		{
			return sourceBacktrace.GetExpressionCompletionData (index, exp);
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
