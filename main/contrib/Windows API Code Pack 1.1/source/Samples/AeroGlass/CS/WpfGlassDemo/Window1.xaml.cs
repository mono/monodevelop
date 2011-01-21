using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;

using System.Drawing;

using Microsoft.WindowsAPICodePack.Shell;

namespace WpfGlassDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : GlassWindow
    {
        public Window1( )
        {
            InitializeComponent( );
        }

        private void GlassWindow_Loaded( object sender, RoutedEventArgs e )
        {
            // update GlassRegion on window size change
            SizeChanged += new SizeChangedEventHandler( Window1_SizeChanged );

            // update background color on change of desktop composition mode
            AeroGlassCompositionChanged += new EventHandler<AeroGlassCompositionChangedEventArgs>(Window1_AeroGlassCompositionChanged);

            // Set the window background color
            if( AeroGlassCompositionEnabled )
            {
                // exclude the GDI rendered controls from the initial GlassRegion
                ExcludeElementFromAeroGlass( eb1 );
                SetAeroGlassTransparency( );
            }
            else
            {
                this.Background = System.Windows.Media.Brushes.Teal;
            }

            // initialize the explorer browser control
            eb1.NavigationTarget = (ShellObject)KnownFolders.Computer;

            // set the state of the Desktop Composition check box.
            EnableCompositionCheck.IsChecked = AeroGlassCompositionEnabled;
        }

        void Window1_AeroGlassCompositionChanged( object sender, AeroGlassCompositionChangedEventArgs e )
        {
            // When the desktop composition mode changes the background color  and window exclusion must be changed appropriately.
            if( e.GlassAvailable )
            {
                this.EnableCompositionCheck.IsChecked = true;
                SetAeroGlassTransparency( );
                ExcludeElementFromAeroGlass( eb1 );
                InvalidateVisual( );
            }
            else
            {
                this.EnableCompositionCheck.IsChecked = false;
                this.Background = System.Windows.Media.Brushes.Teal;
            }
        }

        void Window1_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            ExcludeElementFromAeroGlass( eb1 );
        }

        private void CheckBox_Click( object sender, RoutedEventArgs e )
        {
            // Toggles the desktop composition mode.
            AeroGlassCompositionEnabled = EnableCompositionCheck.IsChecked.Value;
        }

    }
}
