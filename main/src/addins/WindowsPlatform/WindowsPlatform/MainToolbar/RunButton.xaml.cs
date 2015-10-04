using MonoDevelop.Components.MainToolbar;
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
	public partial class RunButtonControl : UserControl, INotifyPropertyChanged
	{
		ImageSource currentImage;
		public ImageSource CurrentImage {
			get { return currentImage; }
			set { currentImage = value; RaisePropertyChanged (); }
		}

		public RunButtonControl ()
		{
			InitializeComponent ();

			DataContext = this;
			icon = OperationIcon.Run;
			CurrentImage = GetIcon ();
			IsEnabled = false;
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
				CurrentImage = GetIcon ();
			}
		}

		ImageSource GetIcon ()
		{
			string img;
			switch (icon) {
				case OperationIcon.Stop:
					img = "ico-stop-normal-32.png";
					break;
				case OperationIcon.Run:
					img = "ico-execute-normal-32.png";
					break;
				case OperationIcon.Build:
					img = "ico-build-normal-32.png";
					break;
				default:
					throw new InvalidOperationException ();
			}

			return (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (Xwt.Drawing.Image.FromResource (typeof (RoundButton), img));
		}

		void OnClick (object sender, RoutedEventArgs args)
		{
			if (Click != null)
				Click (sender, args);
		}

		void RaisePropertyChanged([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propName));
		}

		public event RoutedEventHandler Click;
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
