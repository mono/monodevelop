using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoDevelop.DebuggerVisualizers
{
	public class VisualizerObjectSource
	{
		public VisualizerObjectSource ()
		{
		}

		public virtual object CreateReplacementObject (object target, Stream incomingData)
		{
			/* do we do anything with @target here? */
			return VisualizerObjectSource.Deserialize (incomingData);
		}

		public static object Deserialize (Stream serializationStream)
		{
			BinaryFormatter f=new BinaryFormatter();
			return f.Deserialize (serializationStream);
		}

		public virtual void GetData (object target, Stream outgoingData)
		{
			VisualizerObjectSource.Serialize (outgoingData, target);
		}

		public static void Serialize (Stream serializationStream, object target)
		{
			BinaryFormatter f=new BinaryFormatter();
			f.Serialize (serializationStream, target);
		}

		public static void TransferData (object target, Stream incomingData, Stream outgoingData)
		{
			throw new NotImplementedException ();
		}
	}
}
