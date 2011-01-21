// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.WindowsAPICodePack.Samples.PicturePropertiesEditor
{
    public class ZOrderStackPanel : Panel
    {

        #region Private Members

        private Random rnd = new Random();

        #endregion

        #region Public Constructors

        public ZOrderStackPanel()
            : base()
        {
        }

        #endregion

        #region MaxOffset Property

        public static readonly DependencyProperty MaxOffsetProperty =
            DependencyProperty.Register("MaxOffset", typeof(int), typeof(ZOrderStackPanel));

        public int MaxOffset
        {
            get { return (int)GetValue(MaxOffsetProperty); }
            set { SetValue(MaxOffsetProperty, value); }
        }

        #endregion

        #region MaxRotation Property

        public static readonly DependencyProperty MaxRotationProperty =
            DependencyProperty.Register("MaxRotation", typeof(double), typeof(ZOrderStackPanel));

        public double MaxRotation
        {
            get { return (double)GetValue(MaxRotationProperty); }
            set { SetValue(MaxRotationProperty, value); }
        }

        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            Size resultSize = new Size(0, 0);

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
                resultSize.Width = Math.Max(resultSize.Width, child.DesiredSize.Width);
                resultSize.Height = Math.Max(resultSize.Height, child.DesiredSize.Height);
            }

            resultSize.Width = double.IsPositiveInfinity(availableSize.Width) ? resultSize.Width : availableSize.Width;
            resultSize.Height = double.IsPositiveInfinity(availableSize.Height) ? resultSize.Height : availableSize.Height;

            return resultSize;

        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                double childX = finalSize.Width / 2 - child.DesiredSize.Width / 2;
                double childY = finalSize.Height / 2 - child.DesiredSize.Height / 2;
                child.Arrange(new Rect(childX, childY, child.DesiredSize.Width, child.DesiredSize.Height));

                RotateAndOffsetChild(child);
            }


            return finalSize;
        }

        private void RotateAndOffsetChild(UIElement child)
        {
            if (MaxRotation != 0 && MaxOffset != 0)
            {
                // create a new Random var called rnd

                double randomNumber = rnd.NextDouble();

                double xOffset = MaxOffset * (2 * rnd.NextDouble() - 1);
                double yOffset = MaxOffset * (2 * rnd.NextDouble() - 1);
                double angle = MaxRotation * (2 * rnd.NextDouble() - 1);

                TranslateTransform offsetTF = new TranslateTransform(xOffset, yOffset);
                RotateTransform rotateRT = new RotateTransform(angle, child.DesiredSize.Width / 2, child.DesiredSize.Height / 2);

                TransformGroup tfg = new TransformGroup();
                tfg.Children.Add(offsetTF);
                tfg.Children.Add(rotateRT);
                child.RenderTransform = tfg;
            }
        }
    }
}
