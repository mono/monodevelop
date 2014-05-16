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

        //     INITIALIZATION
        //_________________________________________________________________________________________

        /// <summary>
        /// Creates a new JavaScriptException instance.
        /// </summary>
        /// <param name="engine"> The script engine used to create the error object. </param>
        /// <param name="name"> The name of the error, e.g "RangeError". </param>
        /// <param name="message"> A description of the error. </param>
        public JavaScriptException(ScriptEngine engine, string name, string message)
            : base(string.Format("{0}: {1}", name, message))
		{
			this.Name = name;
        }

        /// <summary>
        /// Creates a new JavaScriptException instance.
        /// </summary>
        /// <param name="name"> The name of the error, e.g "RangeError". </param>
        /// <param name="message"> A description of the error. </param>
        /// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
        /// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
        public JavaScriptException(ScriptEngine engine, string name, string message, int lineNumber, string sourcePath)
            : base(string.Format("{0}: {1}", name, message))
		{
			this.Name = name;
            this.LineNumber = lineNumber;
            this.SourcePath = sourcePath;
		}

        /// <summary>
        /// Creates a new JavaScriptException instance.
        /// </summary>
        /// <param name="name"> The name of the error, e.g "RangeError". </param>
        /// <param name="message"> A description of the error. </param>
        /// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
        /// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
        /// <param name="functionName"> The name of the function.  Can be <c>null</c>. </param>
        public JavaScriptException(ScriptEngine engine, string name, string message, int lineNumber, string sourcePath, string functionName)
            : base(string.Format("{0}: {1}", name, message))
        {
	        this.Name = name;
            this.LineNumber = lineNumber;
            this.SourcePath = sourcePath;
            this.FunctionName = functionName;
        }



        //     SERIALIZATION
        //_________________________________________________________________________________________

#if !SILVERLIGHT

        /// <summary>
        /// Initializes a new instance of the JavaScriptException class with serialized data.
        /// </summary>
        /// <param name="info"> The SerializationInfo that holds the serialized object data about
        /// the exception being thrown. </param>
        /// <param name="context"> The StreamingContext that contains contextual information about
        /// the source or destination. </param>
        protected JavaScriptException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // State cannot be restored here because there is no way to serialize it without
            // falling afoul of .NET 4 security restrictions (Exception.GetObjectData is marked
            // as SecurityCritical).  The recommended workaround is to implement the
            // ISafeSerializationData interface, but since this library isn't targeting .NET 4
            // that doesn't work either!  Instead, we store the state in the Data dictionary
            // and let the Exception class serialize and deserialize everything.
        }

#endif



        //     .NET ACCESSOR PROPERTIES
        //_________________________________________________________________________________________

        /// <summary>
        /// Gets a reference to the JavaScript Error object.
        /// </summary>
        public object ErrorObject
        {
            get { return this.Data["ErrorObject"]; }
            private set { this.Data["ErrorObject"] = value; }
        }

        /// <summary>
        /// Gets the type of error, e.g. "TypeError" or "SyntaxError".
        /// </summary>
        public string Name
        {
            get { return this.Data["Name"] as string; }
	        set { this.Data["Name"] = value; }
        }

        /// <summary>
        /// Gets the line number in the source file the error occurred on.  Can be <c>0</c> if no
        /// line number information is available.
        /// </summary>
        public int LineNumber
        {
            get
            {
                object line = this.Data["LineNumber"];
                if (line == null)
                    return 0;
                return (int)line;
            }
            internal set { this.Data["LineNumber"] = value; }
        }

        /// <summary>
        /// Gets the path or URL of the source file.  Can be <c>null</c> if no source information
        /// is available.
        /// </summary>
        public string SourcePath
        {
            get { return (string)this.Data["SourcePath"]; }
            internal set { this.Data["SourcePath"] = value; }
        }

        /// <summary>
        /// Gets the name of the function where the exception occurred.  Can be <c>null</c> if no
        /// source information is available.
        /// </summary>
        public string FunctionName
        {
            get { return (string)this.Data["FunctionName"]; }
            internal set { this.Data["FunctionName"] = value; }
        }
    }
}
