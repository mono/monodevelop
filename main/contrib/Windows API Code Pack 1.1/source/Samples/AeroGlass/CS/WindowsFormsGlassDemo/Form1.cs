using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.WindowsAPICodePack.Shell;

namespace WindowsFormsGlassDemo
{
    public partial class Form1 : GlassForm
    {
        public Form1( )
        {
            InitializeComponent( );

            explorerBrowser1.Navigate( (ShellObject)KnownFolders.Desktop );

            AeroGlassCompositionChanged += new EventHandler<AeroGlassCompositionChangedEventArgs>( Form1_AeroGlassCompositionChanged );

            if( AeroGlassCompositionEnabled )
            {
                ExcludeControlFromAeroGlass( panel1 );
            }
            else
            {
                this.BackColor = Color.Teal;
            }

            // set the state of the Desktop Composition check box.
            compositionEnabled.Checked = AeroGlassCompositionEnabled;
        }

        void Form1_AeroGlassCompositionChanged( object sender, AeroGlassCompositionChangedEventArgs e )
        {
            // When the desktop composition mode changes the window exclusion must be changed appropriately.
            if( e.GlassAvailable )
            {
                compositionEnabled.Checked = true;
                ExcludeControlFromAeroGlass( panel1 );
                Invalidate( );
            }
            else
            {
                compositionEnabled.Checked = false;
                this.BackColor = Color.Teal;
            }
        }

        private void Form1_Resize( object sender, EventArgs e )
        {
            Rectangle panelRect = ClientRectangle;
            panelRect.Inflate( -30, -30 );
            panel1.Bounds = panelRect;
            ExcludeControlFromAeroGlass( panel1 );
        }

        private void compositionEnabled_CheckedChanged(object sender, EventArgs e)
        {
            // Toggles the desktop composition mode.
            AeroGlassCompositionEnabled = compositionEnabled.Checked;
        }
    }
}
