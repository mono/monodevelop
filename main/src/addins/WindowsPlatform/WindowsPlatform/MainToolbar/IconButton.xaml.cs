using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for RunButton.xaml
	/// </summary>
	public partial class IconButtonControl : UserControl, INotifyPropertyChanged
	{
		public static DependencyProperty ImageProperty = DependencyProperty.Register (
			"Image", typeof (Xwt.Drawing.Image), typeof (IconButtonControl));

		public Xwt.Drawing.Image Image
		{
			get { return (Xwt.Drawing.Image)GetValue(ImageProperty); }
			set { SetValue (ImageProperty, value); }
		}

		public IconButtonControl (Xwt.Drawing.Image image) : this ()
		{
			Image = image;
		}

		public IconButtonControl ()
		{
			InitializeComponent ();

			DataContext = this;
			ToolTipService.SetShowOnDisabled (this, true);
		}

		void OnClick (object sender, RoutedEventArgs args)
		{
			if (Click != null)
				Click (sender, args);
		}

		protected override void OnPropertyChanged (DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged (e);
			if (e.Property == ImageProperty) {
				RunIcon.Image = Image;
				if (Image != null)
					InvalidateMeasure ();
			}
			if (Image == null)
				return;
			if (e.Property == IsMouseOverProperty && IsEnabled) {
				RunIcon.Image = IsMouseOver ? Image.WithStyles ("hover") : Image;
				InvalidateVisual ();
			}
		}

		void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			OnMouseLeftButtonDown (e);
		}

		protected override void OnMouseLeftButtonDown (MouseButtonEventArgs e)
		{
			if (IsEnabled && Image != null) {
				RunIcon.Image = Image.WithStyles ("pressed");
				Background = Styles.MainToolbarButtonPressedBackgroundBrush;
				BorderBrush = Styles.MainToolbarButtonPressedBorderBrush;
			}
			base.OnMouseLeftButtonDown (e);
		}

		protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e)
		{
			if (Image != null) {
				if (IsMouseOver)
					RunIcon.Image = Image.WithStyles ("hover");
				else
					RunIcon.Image = Image;
			}
			
			Background = Brushes.Transparent;
			BorderBrush = Brushes.Transparent;
			base.OnMouseLeftButtonUp (e);
		}

		protected override void OnMouseLeave (MouseEventArgs e)
		{
			Background = Brushes.Transparent;
			BorderBrush = Brushes.Transparent;
			base.OnMouseLeave (e);
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs(propName));
		}

		protected override Size MeasureOverride (Size constraint)
		{
			if (Image != null)
				return new Size (Image.Width, Image.Width);
			return base.MeasureOverride (constraint);
		}

		public event RoutedEventHandler Click;
		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class RunButtonControl : IconButtonControl
	{
		RunButtonControl (OperationIcon icon) : base(GetIcon(icon))
		{
			this.icon = icon;
			ToolTip = GetTooltip(icon);
		}
		public RunButtonControl () : this (OperationIcon.Run)
		{
		}

		OperationIcon icon;
		public OperationIcon Icon
		{
			get { return icon; }
			set
			{
				if (value == icon)
					return;
				icon = value;
				ToolTip = GetTooltip (icon);
				Image = GetIcon (icon);
			}
		}

		static string GetTooltip (OperationIcon icon)
		{
			switch (icon)
			{
				case OperationIcon.Stop:
					return GettextCatalog.GetString("Stop currently running operation");
				case OperationIcon.Run:
					return GettextCatalog.GetString("Run current startup project");
				case OperationIcon.Build:
					return GettextCatalog.GetString("Build current startup project");
				default:
					throw new InvalidOperationException();
			}
		}

		static Xwt.Drawing.Image stopIcon = Xwt.Drawing.Image.FromResource (typeof (RunButtonControl), "stop.png").WithSize (Xwt.IconSize.Medium);
		static Xwt.Drawing.Image executeIcon = Xwt.Drawing.Image.FromResource (typeof (RunButtonControl), "execute.png").WithSize (Xwt.IconSize.Medium);
		static Xwt.Drawing.Image buildIcon = Xwt.Drawing.Image.FromResource (typeof (RunButtonControl), "build.png").WithSize (Xwt.IconSize.Medium);
		static Xwt.Drawing.Image GetIcon (OperationIcon icon)
		{
			switch (icon) {
			case OperationIcon.Stop:
				return stopIcon;
			case OperationIcon.Run:
				return executeIcon;
			case OperationIcon.Build:
				return buildIcon;
			default:
				throw new InvalidOperationException ();
			}
		}
	}

	public class ButtonBarButton : IconButtonControl, IDisposable
	{
		IButtonBarButton button;
		public ButtonBarButton (IButtonBarButton button)
			: base (button.Image.IsNull ? null : button.Image.GetStockIcon().WithSize(Xwt.IconSize.Medium))
		{
			this.button = button;

			VerticalContentAlignment = VerticalAlignment.Center;
			ToolTip = button.Tooltip;
			IsEnabled = button.Enabled;
			Visibility = button.Visible ? Visibility.Visible : Visibility.Collapsed;
			Margin = new Thickness(3, 0, 3, 0);

			button.EnabledChanged += OnButtonEnabledChanged;
			button.VisibleChanged += OnButtonVisibleChanged;
			button.TooltipChanged += OnButtonTooltipChanged;
			button.ImageChanged += OnButtonImageChanged;

			Click += OnButtonClicked;
		}

		public void Dispose ()
		{
			button.EnabledChanged -= OnButtonEnabledChanged;
			button.VisibleChanged -= OnButtonVisibleChanged;
			button.TooltipChanged -= OnButtonTooltipChanged;
			button.ImageChanged -= OnButtonImageChanged;
		}

		void OnButtonEnabledChanged (object sender, EventArgs args)
		{
			IsEnabled = button.Enabled;
		}

		void OnButtonVisibleChanged (object sender, EventArgs e)
		{
			Visibility = button.Visible ? Visibility.Visible : Visibility.Collapsed;
		}

		void OnButtonTooltipChanged (object sender, EventArgs args)
		{
			ToolTip = button.Tooltip;
		}

		void OnButtonImageChanged (object sender, EventArgs args)
		{
			Image = button.Image.GetStockIcon ().WithSize (Xwt.IconSize.Medium);
        }

		void OnButtonClicked (object sender, RoutedEventArgs args)
		{
			button.NotifyPushed ();
		}
	}
}
