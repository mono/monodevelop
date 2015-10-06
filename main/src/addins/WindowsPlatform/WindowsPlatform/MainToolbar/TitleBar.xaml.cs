using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using MonoDevelop.Ide;

namespace WindowsPlatform.MainToolbar
{
    /// <summary>
    /// Interaction logic for TitleBar.xaml
    /// </summary>
	public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

		void AlwaysCanExecute (object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}

		void CloseExecuted (object sender, ExecutedRoutedEventArgs e)
		{
			IdeApp.Exit ();
		}

		void MaximizeExecuted (object sender, ExecutedRoutedEventArgs e)
		{
			if (IdeApp.Workbench.RootWindow.GdkWindow.State == Gdk.WindowState.Maximized)
				IdeApp.Workbench.RootWindow.Unmaximize ();
			else {
				IdeApp.Workbench.RootWindow.Maximize ();
				IdeApp.Workbench.RootWindow.Resize ((int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
				IdeApp.Workbench.RootWindow.Decorated = false;
			}
		}

		void MinimizeExecuted (object sender, ExecutedRoutedEventArgs e)
		{
			IdeApp.Workbench.RootWindow.Iconify ();
		}

		protected override void OnVisualParentChanged (DependencyObject oldParent)
		{
			base.OnVisualParentChanged (oldParent);

			window = Window.GetWindow (this);
		}

		int initX, initY;
		bool inDrag;
		Window window;

		void OnDragStart (object sender, MouseButtonEventArgs e)
		{
			if (e.MouseDevice.DirectlyOver.GetType () != typeof (Border))
				return;

			if (e.ClickCount == 2)
				MaximizeExecuted (this, null);
			else {
				int wndX, wndY;
				IdeApp.Workbench.RootWindow.GetPosition (out wndX, out wndY);
				IdeApp.Workbench.RootWindow.BeginMoveDrag (1, wndX, wndY, (uint)e.Timestamp);
			}
		}
	}
}
