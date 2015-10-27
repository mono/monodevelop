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
		static Brush ErrorTextBrush = Brushes.Red;
		static Brush WarningTextBrush = Brushes.Orange;
		static Brush NormalTextBrush = Brushes.Black;
		static Brush ReadyTextBrush = Brushes.Gray;

		StatusBarContextHandler ctxHandler;
		TaskEventHandler updateHandler;
		public StatusBarControl ()
		{
			InitializeComponent ();
			DataContext = this;

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
						BuildResultIcon = (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (ImageService.GetIcon (Stock.Error));
					} else if (wc > 0) {
						BuildResultPanelVisibility = Visibility.Visible;
						BuildResultCount = wc;
						BuildResultIcon = (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (ImageService.GetIcon (Stock.Warning));
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
			TextBrush = ErrorTextBrush;
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
			if (image == Stock.StatusSteady)
				StatusImage = null;
			else
				StatusImage = (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (ImageService.GetIcon (image));
			Message = message;
		}

		public void ShowReady ()
		{
			textBrush = ReadyTextBrush;
			ShowMessage (BrandingService.StatusSteadyIconId, BrandingService.ApplicationName);
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			var icon = new StatusIcon (this) {
				Image = pixbuf,
				Margin = new Thickness (0, 1, 0, 1),
				MaxWidth = 14,
				MaxHeight = 14,
			};

			StatusIconsPanel.Children.Add (icon);

			return icon;
        }

		public void ShowWarning (string warning)
		{
			TextBrush = WarningTextBrush;
			ShowMessage (warning);
		}

		string message;
		public string Message
		{
			get { return message; }
			set { message = value; RaisePropertyChanged (); }
		}

		Brush textBrush = Brushes.Black;
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

		ImageSource buildResultIcon;
		public ImageSource BuildResultIcon
		{
			get { return buildResultIcon; }
			set { buildResultIcon = value; RaisePropertyChanged (); }
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

	class StatusIcon : System.Windows.Controls.Image, StatusBarIcon
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
			((StackPanel)Parent).Children.Remove (this);
		}

		public new string ToolTip
		{
			get { return (string)base.ToolTip; }
			set { base.ToolTip = value; }
		}

		protected override void OnMouseUp (MouseButtonEventArgs e)
		{
			base.OnMouseUp (e);

			Xwt.PointerButton button;
			switch (e.ChangedButton) {
				case MouseButton.Left:
					button = Xwt.PointerButton.Left;
					break;
				case MouseButton.Middle:
					button = Xwt.PointerButton.Middle;
					break;
				case MouseButton.Right:
					button = Xwt.PointerButton.Right;
					break;
				case MouseButton.XButton1:
					button = Xwt.PointerButton.ExtendedButton1;
					break;
				case MouseButton.XButton2:
					button = Xwt.PointerButton.ExtendedButton2;
					break;
				default:
					throw new NotSupportedException ();
			}

			if (Clicked != null)
				Clicked (this, new StatusBarIconClickedEventArgs {
					Button = button,
				});
		}

		Xwt.Drawing.Image image;
		public Xwt.Drawing.Image Image
		{
			get { return image; }
			set
			{
				image = value;
				Source = (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (value);
			}
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
	}
}
