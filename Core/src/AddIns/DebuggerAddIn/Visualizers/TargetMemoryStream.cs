using System;
using System.IO;

using Mono.Debugger;
using Mono.Debugger.Languages;

namespace MonoDevelop.DebuggerVisualizers
{
	internal class TargetMemoryStream
	{
		public TargetMemoryStream (Process thread)
		{
			this.thread = thread;
			CreateDebugeeStream ();
		}

		void CreateDebugeeStream ()
		{
			Console.WriteLine ("Creating Debuggee-side memory stream");

			Mono.Debugger.StackFrame frame = thread.CurrentFrame;

			streamType = frame.Language.LookupType (frame, "System.IO.MemoryStream") as ITargetStructType;
			if (streamType == null)
				throw new Exception ("couldn't find type `System.IO.MemoryStream'");

			ITargetMethodInfo method = null;
			foreach (ITargetMethodInfo m in streamType.Constructors) {
				if (m.FullName == ".ctor()") {
					method = m;
					break;
				}
			}

			if (method == null)
				throw new Exception ("couldn't find applicable constructor for memory stream");

			ITargetFunctionObject ctor = streamType.GetConstructor (frame, method.Index);
			ITargetObject[] args = new ITargetObject[0];

			debugeeStream = ctor.Type.InvokeStatic (frame, args, false) as ITargetStructObject;
			if (debugeeStream == null)
				throw new Exception ("unable to create instance of memory stream");

			Console.WriteLine ("succeeded in creating debuggee-side memory stream");
		}

		public ITargetObject TargetStream {
			get { return debugeeStream; }
		}

		public byte[] ToArray ()
		{
			ITargetMethodInfo method = null;
			foreach (ITargetMethodInfo m in streamType.Methods) {
				if (m.FullName == "ToArray()") {
					method = m;
					break;
				}
			}

			if (method == null)
				throw new Exception ("couldn't find MemoryStream.ToArray()");

			ITargetFunctionObject ToArray = debugeeStream.GetMethod (method.Index);
			ITargetObject[] args = new ITargetObject[0];

			ITargetArrayObject target_array = ToArray.Invoke (args, false) as ITargetArrayObject;
			if (target_array == null)
				throw new Exception ("MemoryStream.ToArray returned null");
			ITargetArrayType array_type = (ITargetArrayType)target_array.TypeInfo.Type;

			ITargetFundamentalType fund = array_type.ElementType as ITargetFundamentalType;
			if (fund == null || fund.Type != typeof (System.Byte))
				throw new Exception (String.Format ("Array is not of type byte[] (element type is {0})",
								    array_type.ElementType));

			byte[] rv = new byte[target_array.UpperBound - target_array.LowerBound];
			for (int i = 0; i < rv.Length; i ++) {
				ITargetObject el = target_array[target_array.LowerBound + i];
				rv[i] = (byte)((ITargetFundamentalObject)el).Object;
			}

			return rv;
		}

		Process thread;

		ITargetStructType streamType;
		ITargetStructObject debugeeStream;
	}
  
}
