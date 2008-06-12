using System;
using System.IO;

namespace MonoDevelop.DebuggerVisualizers
{
	public interface IVisualizerObjectProvider {
		bool IsObjectReplaceable { get; }

		Stream GetData();
		// causes the debuggee to serialize the object, then we
		// deserialize it into the debugger's address space.
		object GetObject();
		void ReplaceData (Stream newObjectData);
		void ReplaceObject (object newObject);
		Stream TransferData (Stream outgoingData);
		object TransferObject (object outgoingObject);
	}
}

