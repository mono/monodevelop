#if WINDOWS
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Ide.Composition
{
    [Export(typeof(CodeLensPresenterStyle))]
    [Name("MonoDevelopCodeLensPresenterStyle")]
    [Order(Before = "default")]
    class MonoDevelopCodeLensPresenterStyle : CodeLensPresenterStyle
    {
        public MonoDevelopCodeLensPresenterStyle()
        {
            var blackBrush = new SolidColorBrush(Colors.Black);
            var whiteBrush = new SolidColorBrush(Colors.White);

            var typeface = new Typeface("Calibri");
            var size = 8.0 * 96.0 / 72.0;

            var textRunProperties = TextFormattingRunProperties.CreateTextFormattingRunProperties(
                foreground: blackBrush,
                background: null,
                typeface: typeface,
                size: size,
                textDecorations: null,
                textEffects: null,
                hintingSize: null,
                cultureInfo: null);

            this.IndicatorTextRunProperties = textRunProperties;
            this.IndicatorHoveredTextRunProperties = textRunProperties;
            this.IndicatorDisabledTextRunProperties = textRunProperties;
            this.IndicatorSeparatorBrush = blackBrush;
            this.PopupBackgroundBrush = whiteBrush;
            this.PopupTextBrush = blackBrush;
            this.PopupBorderBrush = blackBrush;
        }
    }
}
#endif