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
		ImageSource image, imageHovered, imagePressed, imageDisabled;

		ImageSource currentImage;
		public ImageSource CurrentImage
		{
			get { return currentImage; }
			private set { currentImage = value; RaisePropertyChanged (); }
		}

		public ImageSource Image
		{
			get { return image; }
			set { image = value; RaisePropertyChanged (); }
		}

		public ImageSource ImageHovered
		{
			get { return imageHovered; }
			set { imageHovered = value; RaisePropertyChanged (); }
		}

		public ImageSource ImagePressed
		{
			get { return imagePressed; }
			set { imagePressed = value; RaisePropertyChanged (); }
		}

		public ImageSource ImageDisabled
		{
			get { return imageDisabled; }
			set { imageDisabled = value; RaisePropertyChanged (); }
		}

		public IconButtonControl (string imageResource) : this ()
		{
			SetImageFromResource (imageResource);
		}

		public void SetImageFromResource (string imageResource) {
			if (!String.IsNullOrEmpty (imageResource)) {
				var extension = System.IO.Path.GetExtension (imageResource);
				var name = System.IO.Path.GetFileNameWithoutExtension (imageResource);
				Image = CurrentImage = Xwt.Drawing.Image.FromResource (typeof(RunButtonControl), imageResource).WithSize (Xwt.IconSize.Medium).GetImageSource ();
				try {
					ImageHovered = Xwt.Drawing.Image.FromResource (typeof(RunButtonControl), name + "~hover" + extension).WithSize (Xwt.IconSize.Medium).GetImageSource ();
				} catch {
					ImageHovered = null;
				}
				try {
					ImagePressed = Xwt.Drawing.Image.FromResource (typeof(RunButtonControl), name + "~pressed" + extension).WithSize (Xwt.IconSize.Medium).GetImageSource ();
				} catch {
					ImagePressed = null;
				}
				try {
					ImageDisabled = Xwt.Drawing.Image.FromResource (typeof(RunButtonControl), name + "~disabled" + extension).WithSize (Xwt.IconSize.Medium).GetImageSource ();
				} catch {
					ImageDisabled = null;
				}
				CurrentImage = Image;
			} else {
				Image = CurrentImage = null;
				ImageHovered = null;
				ImagePressed = null;
				ImageDisabled = null;
			}
		}

		public IconButtonControl (ImageSource image, ImageSource imageHovered, ImageSource imagePressed, ImageSource imageDisabled) : this ()
		{
			Image = image;
			ImageHovered = imageHovered;
			ImagePressed = imagePressed;
			ImageDisabled = imageDisabled;
		}

		internal IconButtonControl ()
		{
			InitializeComponent ();

			DataContext = this;
			ToolTipService.SetShowOnDisabled (this, true);
			IsEnabled = false;
		}

		void OnClick (object sender, RoutedEventArgs args)
		{
			if (Click != null)
				Click (sender, args);
		}

		protected override void OnPropertyChanged (DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged (e);

			if (e.Property == IsEnabledProperty && ImageDisabled != null)
				CurrentImage = IsEnabled ? Image : ImageDisabled;
			else if (e.Property == IsMouseOverProperty && IsEnabled && ImageHovered != null)
				CurrentImage = IsMouseOver ? ImageHovered : Image;
		}

		void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown (e);
			if (ImagePressed == null)
				return;
			if (IsEnabled)
				CurrentImage = ImagePressed;
		}

		void OnMouseLeftButtonUp (object sender, MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown (e);
			if (Image == null)
				return;
			if (IsEnabled)
				CurrentImage = Image;
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs(propName));
		}

		public event RoutedEventHandler Click;
		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class RunButtonControl : IconButtonControl
	{
		RunButtonControl (OperationIcon icon) : base(GetIconResource(icon))
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
				SetImageFromResource (GetIconResource (icon));
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

		static string GetIconResource (OperationIcon icon)
		{
			switch (icon) {
				case OperationIcon.Stop:
					return "stop.png";
				case OperationIcon.Run:
					return "execute.png";
				case OperationIcon.Build:
					return "build.png";
				default:
					throw new InvalidOperationException ();
			}
		}
	}

	public class ButtonBarButton : IconButtonControl, IDisposable
	{
		IButtonBarButton button;
		public ButtonBarButton (IButtonBarButton button)
		{
			this.button = button;
			if (!button.Image.IsNull) {
				try {
					SetImageFromResource (button.Image + ".png");
				} catch {
					Image = button.Image.GetImageSource (Xwt.IconSize.Medium);
				}
			}

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
			if (!button.Image.IsNull) {
				try {
					SetImageFromResource (button.Image + ".png");
				} catch {
					Image = button.Image.GetImageSource (Xwt.IconSize.Medium);
				}
			}
        }

		void OnButtonClicked (object sender, RoutedEventArgs args)
		{
			button.NotifyPushed ();
		}
	}
}
