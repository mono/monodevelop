using System;

namespace Stetic
{
	public delegate void ObjectWrapperEventHandler (object sender, ObjectWrapperEventArgs args);
	
	public class ObjectWrapperEventArgs: EventArgs
	{
		ObjectWrapper objectWrapper;
		
		public ObjectWrapperEventArgs (ObjectWrapper objectWrapper)
		{
			this.objectWrapper = objectWrapper;
		}
		
		public ObjectWrapper Wrapper {
			get { return objectWrapper; }
		}
	}	
}
