using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    internal static class AttachedTextFormatting
    {
        public static readonly DependencyProperty TextRunPropertiesProperty = DependencyProperty.RegisterAttached("TextRunProperties", typeof(TextRunProperties), typeof(AttachedTextFormatting), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnTextRunPropertiesChanged));

        public static TextRunProperties GetTextRunProperties(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (TextRunProperties)obj.GetValue(TextRunPropertiesProperty);
        }

        public static void SetTextRunProperties(DependencyObject obj, TextRunProperties value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(TextRunPropertiesProperty, value);
        }

        private static void OnTextRunPropertiesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextBlock target = obj as TextBlock;
            if (target != null)
            {
                TextRunProperties properties = (TextRunProperties)e.NewValue;
                if (properties == null)
                {
                    ClearValues(target);
                }
                else
                {
                    SetValues(target, properties);
                }
            }
        }

        private static void ClearValues(DependencyObject control)
        {
            control.ClearValue(TextElement.ForegroundProperty);
            control.ClearValue(TextElement.BackgroundProperty);
            control.ClearValue(TextElement.FontFamilyProperty);
            control.ClearValue(TextElement.FontSizeProperty);
            control.ClearValue(TextElement.FontWeightProperty);
            control.ClearValue(TextElement.FontStyleProperty);
            control.ClearValue(TextElement.TextEffectsProperty);
            control.ClearValue(Inline.TextDecorationsProperty);
        }

        private static void SetValues(DependencyObject control, TextRunProperties value)
        {
            control.SetValue(TextElement.ForegroundProperty, value.ForegroundBrush);
            control.SetValue(TextElement.BackgroundProperty, value.BackgroundBrush);
            if (value.Typeface != null)
            {
                control.SetValue(TextElement.FontFamilyProperty, value.Typeface.FontFamily);
                control.SetValue(TextElement.FontWeightProperty, value.Typeface.Weight);
                control.SetValue(TextElement.FontStyleProperty, value.Typeface.Style);
            }
            else
            {
                control.ClearValue(TextElement.FontFamilyProperty);
                control.ClearValue(TextElement.FontWeightProperty);
                control.ClearValue(TextElement.FontStyleProperty);
            }
            control.SetValue(TextElement.FontSizeProperty, value.FontRenderingEmSize);
            control.SetValue(TextElement.TextEffectsProperty, value.TextEffects);
            control.SetValue(Inline.TextDecorationsProperty, value.TextDecorations);
        }
    }
}
