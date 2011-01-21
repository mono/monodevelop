// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AccelerationMeasurement
{
    public partial class AccelerationBar : Control
    {
        public AccelerationBar( )
        {
            InitializeComponent( );
            this.BackColor = Color.White;
        }

        
        // length og the ticks in pixels
        private const int tickLength = 5;
        
        // total number of ticks on each side of 'zero'
        private const int ticks = 5;

        protected override void OnPaint( PaintEventArgs pe )
        {
            base.OnPaint( pe );
            Graphics g = pe.Graphics;

            // draw gauge
            Rectangle gaugeBox = new Rectangle(
                ClientRectangle.Left, ClientRectangle.Top + tickLength,
                ClientRectangle.Width - 2, ClientRectangle.Height - tickLength * 2 );
            g.DrawRectangle( Pens.Black, gaugeBox );

            // draw ticks
            g.DrawLine( Pens.Black, ClientRectangle.Width / 2, 0, ClientRectangle.Width / 2, ClientRectangle.Height );

            int totalTicks = (ticks * 2) + 1;
            float tickSpacing = (float)ClientRectangle.Width / ((float)totalTicks - 1);
            for( int n = 1; n < totalTicks; n++ )
            {

                g.DrawLine( Pens.Black, 
                    tickSpacing * (float)n, (float)gaugeBox.Bottom,
                    tickSpacing * (float)n, (float)ClientRectangle.Bottom );
            }

            // draw indicator
            float pixelsPerUnit = (float)gaugeBox.Width / (float)totalTicks;
            float gaugeCenter = (float)gaugeBox.Width / 2f + gaugeBox.Left;
            float gaugeMiddle = (float)gaugeBox.Height / 2f + gaugeBox.Top;
            float indicatedPosition = gaugeCenter + (pixelsPerUnit * acceleration);
            float indicatorOffset = Math.Max( Math.Min( indicatedPosition, gaugeBox.Width ), gaugeBox.Left );
            PointF[ ] indicator = new PointF[ ]
            {
                new PointF ( indicatorOffset, gaugeBox.Top ),
                new PointF ( indicatorOffset + tickLength, gaugeMiddle ),
                new PointF ( indicatorOffset, gaugeBox.Bottom ),
                new PointF ( indicatorOffset - tickLength, gaugeMiddle ),
                new PointF ( indicatorOffset, gaugeBox.Top )
            };

            Brush fill = ( indicatorOffset == indicatedPosition ) ? new SolidBrush( Color.Red ) : new SolidBrush( Color.Gray );
            g.FillPolygon( fill, indicator, FillMode.Winding );
        }

        public float Acceleration
        {
            set
            {
                acceleration = value;
                Invalidate( );
            }
            get
            {
                return acceleration;
            }
        }
        private float acceleration = 0;
    }
}
