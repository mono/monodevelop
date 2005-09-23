using System;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class GuiSyncAbstractService : GuiSyncObject, IService
	{
		[FreeDispatch]
		public virtual void InitializeService()
		{
			OnInitialize(EventArgs.Empty);
		}
		
		
		public virtual void UnloadService()
		{
			OnUnload(EventArgs.Empty);
		}
		
		protected virtual void OnInitialize(EventArgs e)
		{
			if (Initialize != null) {
				Initialize(this, e);
			}
		}
		
		protected virtual void OnUnload(EventArgs e)
		{
			if (Unload != null) {
				Unload(this, e);
			}
		}
		
		public event EventHandler Initialize;
		public event EventHandler Unload;
	}
}
