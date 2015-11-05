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
using System.Windows.Media.Animation;

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
						BuildResultIcon = Stock.Error.GetStockIcon ().WithSize (Xwt.IconSize.Small).GetImageSource();
					} else if (wc > 0) {
						BuildResultPanelVisibility = Visibility.Visible;
						BuildResultCount = wc;
						BuildResultIcon = Stock.Warning.GetStockIcon ().WithSize (Xwt.IconSize.Small).GetImageSource();
					} else
						BuildResultPanelVisibility = Visibility.Collapsed;
				});
			};
			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
		}

		void OnProgressBarLoaded(object sender, RoutedEventArgs args)
		{
			var p = (ProgressBar)sender;
			p.ApplyTemplate();

			var color = new SolidColorBrush (new System.Windows.Media.Color {
				A = 0xFF,
				R = 0xB3,
				G = 0xE7,
				B = 0x70,
			});

			var target = p.Template.FindName("Animation", p);
			var panel = target as Panel;
			if (panel != null)
				panel.Background = color;

			var rect = target as Rectangle;
			if (rect != null)
				rect.Fill = color;
		}

		public bool AutoPulse {
			get { return ProgressBar.IsIndeterminate; }
			set { ProgressBar.IsIndeterminate = value; }
		}

		public StatusBar MainContext
		{
			get { return ctxHandler.MainContext; }
		}

		public void BeginProgress (string name)
		{
			EndProgress();
			ShowMessage (name);
		}

		public void BeginProgress (IconId image, string name)
		{
			EndProgress();
			ShowMessage(image, name);
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
			oldWork = 0;
            ProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
		}

		public void Pulse ()
		{
			// Nothing to do here.
		}

		static Pad sourcePad;
		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
		}

		double oldWork = 0;
		public void SetProgressFraction (double work)
		{
			var anim = new DoubleAnimation
			{
				From = oldWork,
				To = work,
				Duration = TimeSpan.FromSeconds(0.2),
				FillBehavior = FillBehavior.HoldEnd,
			};
			oldWork = work;

			ProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim, HandoffBehavior.SnapshotAndReplace);
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
			var icon = new StatusIcon (this) {
				Image = pixbuf,
				Margin = new Thickness (5, 5, 5, 5),
				MaxWidth = 14,
				MaxHeight = 14,
			};

			StatusIconsPanel.Children.Add (icon);

			return icon;
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
				Source = value.WithSize (Xwt.IconSize.Small).GetImageSource ();
			}
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
	}
}
