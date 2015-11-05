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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for StatusBar.xaml
	/// </summary>
	public partial class StatusBarControl : UserControl, StatusBar, INotifyPropertyChanged
	{
		StatusBarContextHandler ctxHandler;
		TaskEventHandler updateHandler;
		public StatusBarControl ()
		{
			InitializeComponent ();
			DataContext = this;
			Background = Styles.StatusBarBackgroundBrush;

			ctxHandler = new StatusBarContextHandler (this);

			ShowReady ();

			updateHandler = delegate {
				int ec = 0, wc = 0;

				foreach (MonoDevelop.Ide.Tasks.Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}

				DispatchService.GuiDispatch (delegate {
					if (ec > 0) {
						BuildResultPanelVisibility = Visibility.Visible;
						BuildResultCount = ec;
					} else if (wc > 0) {
						BuildResultPanelVisibility = Visibility.Visible;
						BuildResultCount = wc;
					} else
						BuildResultPanelVisibility = Visibility.Collapsed;
				});
			};
			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
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
			ShowMessage (name);
		}

		public void BeginProgress (IconId image, string name)
		{
			ShowMessage (image, name);
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void Dispose ()
		{
			TaskService.Errors.TasksAdded -= updateHandler;
			TaskService.Errors.TasksRemoved -= updateHandler;
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
			TextBrush = Styles.StatusBarErrorTextBrush;
			ShowMessage (error);
		}

		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false);
		}

		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, false);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, true);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			if (image == Stock.StatusSteady || image.IsNull)
				StatusImage = null;
			else
				StatusImage = image.GetStockIcon ().WithSize (Xwt.IconSize.Small).GetImageSource ();
			Message = message;
		}

		public void ShowReady ()
		{
			textBrush = Styles.StatusBarReadyTextBrush;
			ShowMessage (BrandingService.StatusSteadyIconId, BrandingService.ApplicationName);
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			// No icons on Windows
			return null;
        }

		public void ShowWarning (string warning)
		{
			TextBrush = Styles.StatusBarWarningTextBrush;
			ShowMessage (warning);
		}

		string message;
		public string Message
		{
			get { return message; }
			set { message = value; RaisePropertyChanged (); }
		}

		Brush textBrush = Styles.StatusBarTextBrush;
		public Brush TextBrush
		{
			get { return textBrush; }
			set { textBrush = value; RaisePropertyChanged (); }
		}

		ImageSource statusImage;
		public ImageSource StatusImage
		{
			get { return statusImage; }
			set { statusImage = value; RaisePropertyChanged (); }
		}

		int buildResultCount;
		public int BuildResultCount
		{
			get { return buildResultCount; }
			set { buildResultCount = value; RaisePropertyChanged (); }
		}

		Visibility buildResultPanelVisibility = Visibility.Collapsed;
		public Visibility BuildResultPanelVisibility
		{
            get { return buildResultPanelVisibility; }
			set { buildResultPanelVisibility = value; RaisePropertyChanged (); }
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs (propName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
