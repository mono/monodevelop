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
		bool hasDebugInfo;
		
		[NonSerialized]
		DebuggerSession session;

		public StackFrame (long address, SourceLocation location, string language, bool isExternalCode, bool hasDebugInfo)
		{
			this.address = address;
			this.location = location;
			this.language = language;
			this.isExternalCode = isExternalCode;
			this.hasDebugInfo = hasDebugInfo;
		}
		
		public StackFrame (long address, SourceLocation location, string language)
			: this (address, location, language, string.IsNullOrEmpty (location.Filename), true)
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
		
		public bool HasDebugInfo {
			get { return this.hasDebugInfo; }
		}
		
		public ObjectValue[] GetLocalVariables ()
		{
			return GetLocalVariables (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetLocalVariables (EvaluationOptions options)
		{
			if (!hasDebugInfo)
				return new ObjectValue [0];
			ObjectValue[] values = sourceBacktrace.GetLocalVariables (index, options);
			ObjectValue.ConnectCallbacks (this, values);
			return values;
		}
		
		public ObjectValue[] GetParameters ()
		{
			return GetParameters (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetParameters (EvaluationOptions options)
		{
			if (!hasDebugInfo)
				return new ObjectValue [0];
			ObjectValue[] values = sourceBacktrace.GetParameters (index, options);
			ObjectValue.ConnectCallbacks (this, values);
			return values;
		}
		
		public ObjectValue[] GetAllLocals ()
		{
			if (!hasDebugInfo)
				return new ObjectValue [0];
			IExpressionEvaluator evaluator = session.FindExpressionEvaluator (this);
			if (evaluator != null)
				return evaluator.GetLocals (this);
			return GetAllLocals (session.EvaluationOptions);
		}
		
		public ObjectValue[] GetAllLocals (EvaluationOptions options)
		{
			if (!hasDebugInfo)
				return new ObjectValue [0];
			ObjectValue[] values = sourceBacktrace.GetAllLocals (index, options);
			ObjectValue.ConnectCallbacks (this, values);
			return values;
		}
		
		public ObjectValue GetThisReference ()
		{
			return GetThisReference (session.EvaluationOptions);
		}
		
		public ObjectValue GetThisReference (EvaluationOptions options)
		{
			if (!hasDebugInfo)
				return null;
			ObjectValue value = sourceBacktrace.GetThisReference (index, options);
			ObjectValue.ConnectCallbacks (this, value);
			return value;
		}
		
		public ObjectValue GetException ()
		{
			return GetException (session.EvaluationOptions);
		}
		
		public ObjectValue GetException (EvaluationOptions options)
		{
			if (!hasDebugInfo)
				return null;
			ObjectValue value = sourceBacktrace.GetException (index, options);
			if (value != null)
				ObjectValue.ConnectCallbacks (this, value);
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
			if (!hasDebugInfo) {
				ObjectValue[] vals = new ObjectValue [expressions.Length];
				for (int n=0; n<expressions.Length; n++)
					vals [n] = ObjectValue.CreateUnknown (expressions [n]);
				return vals;
			}
			if (options.UseExternalTypeResolver) {
				string[] resolved = new string [expressions.Length];
				for (int n=0; n<expressions.Length; n++)
					resolved [n] = ResolveExpression (expressions [n]);
				expressions = resolved;
			}
			ObjectValue[] values = sourceBacktrace.GetExpressionValues (index, expressions, options);
			ObjectValue.ConnectCallbacks (this, values);
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
			if (!hasDebugInfo)
				return ObjectValue.CreateUnknown (expression);
			if (options.UseExternalTypeResolver)
				expression = ResolveExpression (expression);
			ObjectValue[] values = sourceBacktrace.GetExpressionValues (index, new string[] { expression }, options);
			ObjectValue.ConnectCallbacks (this, values);
			return values [0];
		}
		
		/// <summary>
		/// Returns True if the expression is valid and can be evaluated for this frame.
		/// </summary>
		public bool ValidateExpression (string expression)
		{
			return ValidateExpression (expression, session.EvaluationOptions);
		}
		
		/// <summary>
		/// Returns True if the expression is valid and can be evaluated for this frame.
		/// </summary>
		public ValidationResult ValidateExpression (string expression, EvaluationOptions options)
		{
			if (options.UseExternalTypeResolver)
				expression = ResolveExpression (expression);
			return sourceBacktrace.ValidateExpression (index, expression, options);
		}
		
		public CompletionData GetExpressionCompletionData (string exp)
		{
			if (!hasDebugInfo)
				return null;
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
	
	[Serializable]
	public struct ValidationResult
	{
		bool isValid;
		string message;
		
		public ValidationResult (bool isValid, string message)
		{
			this.isValid = isValid;
			this.message = message;
		}
		
		public bool IsValid { get { return isValid; } }
		public string Message { get { return message; } }
		
		public static implicit operator bool (ValidationResult result)
		{
			return result.isValid;
		}
	}
}
