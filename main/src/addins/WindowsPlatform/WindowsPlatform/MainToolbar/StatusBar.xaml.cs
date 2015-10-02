using MonoDevelop.Ide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Xwt.Drawing;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide.Tasks;

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for StatusBar.xaml
	/// </summary>
	public partial class StatusBarControl : UserControl, StatusBar
	{
		StatusBarContextHandler ctxHandler;
		public StatusBarControl ()
		{
			InitializeComponent ();
			ctxHandler = new StatusBarContextHandler (this);
		}

		public bool AutoPulse {
			get; set;
		}

		public StatusBar MainContext
		{
			get { return ctxHandler.MainContext; }
		}

		public void BeginProgress (string name)
		{
		}

		public void BeginProgress (IconId image, string name)
		{
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void Dispose ()
		{
			//TaskService.Errors.TasksAdded -= updateHandler;
			//TaskService.Errors.TasksRemoved -= updateHandler;
		}

		public void EndProgress ()
		{
		}

		public void Pulse ()
		{
		}

		static Pad sourcePad;
		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
		}

		public void SetProgressFraction (double work)
		{
		}

		public void ShowError (string error)
		{
		}

		public void ShowMessage (string message)
		{
		}

		public void ShowMessage (IconId image, string message)
		{
		}

		public void ShowMessage (string message, bool isMarkup)
		{
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
		}

		public void ShowReady ()
		{
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			return new StatusIcon (this);
		}

		public void ShowWarning (string warning)
		{
		}
	}

	class StatusIcon : StatusBarIcon
	{
		StatusBar bar;

		public StatusIcon (StatusBar bar)
		{
			this.bar = bar;
		}

		public void SetAlertMode (int seconds)
		{
			// Create fade-out fade-in animation.
		}

		public void Dispose ()
		{
		}

		public string ToolTip
		{
			get;
			set;
		}

		Xwt.Drawing.Image image;
		public Xwt.Drawing.Image Image
		{
			get { return image; }
			set
			{
				image = value;
			}
		}

		internal void NotifyClicked (Xwt.PointerButton button)
		{
			if (Clicked != null)
				Clicked (this, new StatusBarIconClickedEventArgs {
					Button = button,
				});
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
	}
}
