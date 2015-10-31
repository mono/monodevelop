using MonoDevelop.Components.MainToolbar;
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

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for Toolbar.xaml
	/// </summary>
	public partial class ToolBar : UserControl
	{
		public ToolBar ()
		{
			InitializeComponent ();
			Background = Styles.MainToolbarBackgroundBrush;
		}
	}
}
