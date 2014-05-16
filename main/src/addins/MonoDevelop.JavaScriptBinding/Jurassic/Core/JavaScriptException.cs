using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Jurassic
{
	/// <summary>
	/// Represents a wrapper for javascript error objects.
	/// </summary>
	[Serializable]
	public class JavaScriptException : Exception
	{
		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The script engine used to create the error object. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		public JavaScriptException (ScriptEngine engine, string name, string message)
			: base (string.Format ("{0}: {1}", name, message))
		{
			Name = name;
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		public JavaScriptException (ScriptEngine engine, string name, string message, int lineNumber, string sourcePath)
			: base (string.Format ("{0}: {1}", name, message))
		{
			Name = name;
			LineNumber = lineNumber;
			SourcePath = sourcePath;
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		/// <param name="functionName"> The name of the function.  Can be <c>null</c>. </param>
		public JavaScriptException (ScriptEngine engine, string name, string message, int lineNumber, string sourcePath, string functionName)
			: base (string.Format ("{0}: {1}", name, message))
		{
			Name = name;
			LineNumber = lineNumber;
			SourcePath = sourcePath;
			FunctionName = functionName;
		}

		/// <summary>
		/// Gets a reference to the JavaScript Error object.
		/// </summary>
		public object ErrorObject {
			get { return Data ["ErrorObject"]; }
			private set { Data ["ErrorObject"] = value; }
		}

		/// <summary>
		/// Gets the type of error, e.g. "TypeError" or "SyntaxError".
		/// </summary>
		public string Name {
			get { return Data ["Name"] as string; }
			set { Data ["Name"] = value; }
		}

		/// <summary>
		/// Gets the line number in the source file the error occurred on.  Can be <c>0</c> if no
		/// line number information is available.
		/// </summary>
		public int LineNumber {
			get {
				object line = Data ["LineNumber"];
				if (line == null)
					return 0;
				return (int)line;
			}
			internal set { Data ["LineNumber"] = value; }
		}

		/// <summary>
		/// Gets the path or URL of the source file.  Can be <c>null</c> if no source information
		/// is available.
		/// </summary>
		public string SourcePath {
			get { return (string)Data ["SourcePath"]; }
			internal set { Data ["SourcePath"] = value; }
		}

		/// <summary>
		/// Gets the name of the function where the exception occurred.  Can be <c>null</c> if no
		/// source information is available.
		/// </summary>
		public string FunctionName {
			get { return (string)Data ["FunctionName"]; }
			internal set { Data ["FunctionName"] = value; }
		}
	}
}
