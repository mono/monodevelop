// 
// ExceptionInfo.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class ExceptionInfo
	{
		ObjectValue exception;
		ObjectValue messageObject;

		[NonSerialized]
		ExceptionStackFrame[] frames;
		
		[NonSerialized]
		ExceptionInfo innerException;
		
		/// <summary>
		/// The provided value can have the following members:
		/// Type of the object: type of the exception
		/// Message: Message of the exception
		/// Instance: Raw instance of the exception
		/// StackTrace: an array of frames. Each frame must have:
		///     Value of the object: display text of the frame
		///     File: name of the file
		///     Line: line
		///     Col: column
		/// InnerException: inner exception, following the same format described above.
		/// </summary>
		public ExceptionInfo (ObjectValue exception)
		{
			this.exception = exception;
			if (exception.IsEvaluating || exception.IsEvaluatingGroup)
				exception.ValueChanged += HandleExceptionValueChanged;
		}

		void LoadMessage ()
		{
			if (messageObject == null) {
				messageObject = exception.GetChild ("Message");
				if (messageObject != null && messageObject.IsEvaluating)
					messageObject.ValueChanged += HandleMessageValueChanged;
			}
		}

		void HandleMessageValueChanged (object sender, EventArgs e)
		{
			frames = null;
			NotifyChanged ();
		}

		void HandleExceptionValueChanged (object sender, EventArgs e)
		{
			frames = null;
			if (exception.IsEvaluatingGroup)
				exception = exception.GetArrayItem (0);
			LoadMessage ();
			NotifyChanged ();
		}

		void NotifyChanged ()
		{
			EventHandler evnt = Changed;
			if (evnt != null)
				evnt (this, EventArgs.Empty);
		}
		
		public string Type {
			get { return exception.TypeName; }
		}

		public string Message {
			get {
				LoadMessage ();
				if (messageObject != null && messageObject.IsEvaluating)
					return "Loading...";
				return messageObject != null ? messageObject.Value : null;
			}
		}

		public ObjectValue Instance {
			get {
				return exception.GetChild ("Instance");
			}
		}
		
		public bool IsEvaluating {
			get { return exception.IsEvaluating || exception.IsEvaluatingGroup; }
		}

		public bool StackIsEvaluating {
			get {
				ObjectValue stackTrace = exception.GetChild ("StackTrace");
				if (stackTrace != null)
					return stackTrace.IsEvaluating;
				else
					return false;
			}
		}

		public ExceptionStackFrame[] StackTrace {
			get {
				if (frames != null)
					return frames;
				
				ObjectValue stackTrace = exception.GetChild ("StackTrace");
				if (stackTrace == null)
					return frames = new ExceptionStackFrame [0];
				
				if (stackTrace.IsEvaluating) {
					frames = new ExceptionStackFrame [0];
					stackTrace.ValueChanged += HandleExceptionValueChanged;
					return frames;
				}
				List<ExceptionStackFrame> list = new List<ExceptionStackFrame> ();
				foreach (ObjectValue val in stackTrace.GetAllChildren ())
					list.Add (new ExceptionStackFrame (val));
				frames = list.ToArray ();
				return frames;
			}
		}
		
		public ExceptionInfo InnerException {
			get {
				if (innerException == null) {
					ObjectValue innerVal = exception.GetChild ("InnerException");
					if (innerVal == null || innerVal.IsError || innerVal.IsUnknown)
						return null;
					if (innerVal.IsEvaluating) {
						innerVal.ValueChanged += delegate { NotifyChanged (); };
						return null;
					}
					innerException = new ExceptionInfo (innerVal);
					innerException.Changed += delegate {
						NotifyChanged ();
					};
				}
				return innerException;
			}
		}
		
		public event EventHandler Changed;
		
		internal void ConnectCallback (StackFrame parentFrame)
		{
			ObjectValue.ConnectCallbacks (parentFrame, exception);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			var chain = new List<ExceptionInfo> ();
			ExceptionInfo e = this;
			while (e != null) {
				chain.Insert (0, e);
				if (sb.Length > 0)
					sb.Append (" ---> ");
				sb.Append (e.Type).Append (": ").Append (e.Message);
				e = e.InnerException;
			}
			sb.AppendLine ();
			foreach (var ex in chain) {
				if (ex != chain[0])
					sb.AppendLine ("  --- End of inner exception stack trace ---");
				foreach (var f in ex.StackTrace) {
					sb.Append ("  at ").Append (f.DisplayText);
					if (!string.IsNullOrEmpty (f.File))
						sb.Append (" in ").Append (f.File).Append (":").Append (f.Line);
					sb.AppendLine ();
				}
			}
			return sb.ToString ();
		}
	}
	
	public class ExceptionStackFrame
	{
		ObjectValue frame;
		
		/// <summary>
		/// The provided value must have a specific structure.
		/// The Value property is the display text.
		/// A child "File" member must be the name of the file.
		/// A child "Line" member must be the line.
		/// A child "Col" member must be the column.
		/// </summary>
		public ExceptionStackFrame (ObjectValue value)
		{
			frame = value;
		}

		public string File {
			get {
				ObjectValue file = frame.GetChild ("File");
				if (file != null)
					return file.Value;
				else
					return null;
			}
		}

		public int Line {
			get {
				ObjectValue val = frame.GetChild ("Line");
				if (val != null)
					return int.Parse (val.Value);
				else
					return 0;
			}
		}

		public int Column {
			get {
				ObjectValue val = frame.GetChild ("Column");
				if (val != null)
					return int.Parse (val.Value);
				else
					return 0;
			}
		}

		public string DisplayText {
			get { return frame.Value; }
		}
	}
}

