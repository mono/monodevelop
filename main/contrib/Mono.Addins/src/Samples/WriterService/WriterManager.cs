
using System;
using Mono.Addins;

namespace WriterService
{
	public class WriterManager
	{
		ExtensionContext ctx;
		public event EventHandler Changed;
		
		public WriterManager (string[] flags)
		{
			// Create a new extension context
			ctx = AddinManager.CreateExtensionContext ();
			
			// Register the flags condition in the new context
			FlagsCondition condition = new FlagsCondition (flags);
			ctx.RegisterCondition ("HasFlag", condition);
			
			ctx.AddExtensionNodeHandler ("/WriterService/Writers", delegate {
				if (Changed != null)
					Changed (this, EventArgs.Empty);
			});
		}
		
		public IWriter[] GetWriters ()
		{
			// Returns the IWriter objects registered in the Writers path
			return (IWriter[]) ctx.GetExtensionObjects ("/WriterService/Writers", typeof(IWriter));
		}
	}
}

