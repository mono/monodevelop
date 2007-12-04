using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Mono.Debugger;
using Mono.Debugger.Languages;

namespace MonoDevelop.DebuggerVisualizers
{

	public class TargetObjectProvider : IVisualizerObjectProvider
	{
		public TargetObjectProvider (ITargetObject target, Process thread, string sourceType)
		{
			this.target = target;
			this.thread = thread;
			CreateVisualizerObjectSource (sourceType);
		}

		// Create the debuggee-side object that we'll communicate with
		void CreateVisualizerObjectSource (string sourceType)
		{
			Console.WriteLine ("Creating Debuggee-side object (of type {0})", sourceType);

			Mono.Debugger.StackFrame frame = thread.CurrentFrame;

			// shouldn't be hardcoded - it comes from the attribute 
			objectSourceType = frame.Language.LookupType (frame, sourceType) as ITargetStructType;
			if (objectSourceType == null)
				throw new Exception ("couldn't find type for object source");

			ITargetMethodInfo method = null;
			foreach (ITargetMethodInfo m in objectSourceType.Constructors) {
				if (m.FullName == ".ctor()") {
					method = m;
					break;
				}
			}

			if (method == null)
				throw new Exception ("couldn't find applicable constructor for object source");

			ITargetFunctionObject ctor = objectSourceType.GetConstructor (frame, method.Index);
			ITargetObject[] args = new ITargetObject[0];

			objectSource = ctor.Type.InvokeStatic (frame, args, false) as ITargetStructObject;
			if (objectSource == null)
				throw new Exception ("unable to create instance of object source");

			Console.WriteLine ("succeeded in creating debuggee-side object source");
		}

#region IVisualizerObjectProvider implementation

		public bool IsObjectReplaceable
		{
			get {
				return false;
			}
		}
	  

		public Stream GetData()
		{
			TargetMemoryStream tms = new TargetMemoryStream (thread);

			ITargetMethodInfo method = null;
			foreach (ITargetMethodInfo m in objectSourceType.Methods) {
				if (m.FullName == "GetData(System.Object,System.IO.Stream)") {
					method = m;
					break;
				}
			}

			if (method == null)
				throw new Exception ("couldn't find VisualizerObjectSource.GetData implementation in object source");

			ITargetFunctionObject GetData = objectSource.GetMethod (method.Index);

			ITargetObject[] args = new ITargetObject[2];
			args[0] = target;
			args[1] = tms.TargetStream;

			GetData.Invoke (args, false);

			return new MemoryStream (tms.ToArray());
		}

		public object GetObject ()
		{
			Stream s = GetData();
			BinaryFormatter f = new BinaryFormatter ();

			return f.Deserialize (s);
		}

		public void ReplaceData (Stream newObjectData)
		{
			throw new NotImplementedException ();
		}

		public void ReplaceObject (object newObject)
		{
			BinaryFormatter f = new BinaryFormatter();
			MemoryStream stream = new MemoryStream ();

			f.Serialize (stream, newObject);
			ReplaceData (stream);
		}

		public Stream TransferData (Stream outgoingData)
		{
			throw new NotImplementedException ();
		}

		public object TransferObject (object outgoingObject)
		{
			BinaryFormatter f = new BinaryFormatter();
			Stream outgoingStream = new MemoryStream ();
			Stream incomingStream;

			f.Serialize (outgoingStream, outgoingObject);
			incomingStream = TransferData (outgoingStream);

			return f.Deserialize (incomingStream);
		}
#endregion

		Process thread;
		ITargetObject target;
		ITargetStructType objectSourceType;
		ITargetStructObject objectSource;
	}

}
