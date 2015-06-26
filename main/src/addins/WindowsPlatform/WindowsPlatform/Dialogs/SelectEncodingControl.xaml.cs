using MonoDevelop.Projects.Text;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MonoDevelop.Ide;

namespace MonoDevelop.Platform
{
	/// <summary>
	/// Interaction logic for SelectEncodingControl.xaml
	/// </summary>
	public partial class SelectEncodingControl : UserControl
	{
		public SelectEncodingControl ()
		{
			InitializeComponent ();
			DataContext = this;
		}

		readonly ObservableCollection<TextEncoding> AvailableEncodings = new ObservableCollection<TextEncoding> (TextEncoding.SupportedEncodings);
		readonly ObservableCollection<TextEncoding> ShownEncodings = new ObservableCollection<TextEncoding> (TextEncoding.ConversionEncodings);

		void ListBox_SelectionChanged (object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons ();
		}

		void shownLst_Loaded (object sender, RoutedEventArgs e)
		{
			availableLst.ItemsSource = AvailableEncodings;
			UpdateButtons ();
		}

		void availableLst_Loaded (object sender, RoutedEventArgs e)
		{
			shownLst.ItemsSource = ShownEncodings;
			UpdateButtons ();
		}

		void UpdateButtons ()
		{
			btnLeft.IsEnabled = shownLst.SelectedIndex != -1;
			btnRight.IsEnabled = availableLst.SelectedIndex != -1;
			btnUp.IsEnabled = shownLst.SelectedIndex != -1 && shownLst.SelectedIndex != 0;
			btnDown.IsEnabled = shownLst.SelectedIndex != -1 && shownLst.SelectedIndex != ShownEncodings.Count - 1;
		}

		void btnDown_Click (object sender, RoutedEventArgs e)
		{
			ShownEncodings.Move (shownLst.SelectedIndex, shownLst.SelectedIndex + 1);
			UpdateButtons ();
		}

		void btnUp_Click (object sender, RoutedEventArgs e)
		{
			ShownEncodings.Move (shownLst.SelectedIndex, shownLst.SelectedIndex - 1);
			UpdateButtons ();
		}

		void btnRight_Click (object sender, RoutedEventArgs e)
		{
			ShownEncodings.Add ((TextEncoding)availableLst.SelectedItem);
			AvailableEncodings.RemoveAt (availableLst.SelectedIndex);
		}

		void btnLeft_Click (object sender, RoutedEventArgs e)
		{
			AvailableEncodings.Add ((TextEncoding)shownLst.SelectedItem);
			ShownEncodings.RemoveAt (shownLst.SelectedIndex);
		}

		void btnOk_Click (object sender, RoutedEventArgs e)
		{
			TextEncoding.ConversionEncodings = ShownEncodings.ToArray ();
			((Window)Parent).DialogResult = true;
			((Window)Parent).Close ();
		}

		void btnCancel_Click (object sender, RoutedEventArgs e)
		{
			((Window)Parent).DialogResult = false;
			((Window)Parent).Close ();
		}
	}
}
