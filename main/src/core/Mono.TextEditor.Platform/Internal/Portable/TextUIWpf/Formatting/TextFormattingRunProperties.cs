// Copyright (c) Microsoft Corporation
// All rights reserved

#pragma warning disable 1634, 1691

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Media.TextFormatting;

    /// <summary>
    /// Holds text formatting property information. This class derives from the abstract WPF <see cref="TextRunProperties"/> class. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is used to hold all information about the text formatting properties. Once created,
    /// it is immutable and all operations return different objects. For each unique set of
    /// TextFormattingRunProperties there exists exactly one object instance. If a TextFormattingRunProperties
    /// has reference equality to another, their properties are identical. Conversely, if a TextFormattingRunProperties
    /// object has reference inequality, the properties are distinct.
    /// </para>
    /// <para>
    /// Checking reference equality is the only way to determine whether two TextFormattingRunProperties are distinct.
    /// Checking the equality of each property of the object may indicate the two are identical, but that may or may
    /// not be the case.
    /// </para>
    /// <para>
    /// TextFormattingRunProperties may have empty properties. An empty property 
    /// inherits the empty properties from some additional text. The TextFormattingRunProperties object can
    /// determine whether a property is empty or not: [PropertyName]Empty property. TextFormattingRunProperties
    /// also contains a facility for emptying a property: Clear[PropertyName]().
    /// </para>
    /// <para>
    /// All freezable fields of the TextFormattingRunProperties object are frozen on creation.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class TextFormattingRunProperties : TextRunProperties, ISerializable, IObjectReference
    {
        #region Private Members

        [NonSerialized]
        private Typeface _typeface;
        [NonSerialized]
        private double? _size, _hintingSize, _foregroundOpacity, _backgroundOpacity;
        [NonSerialized]
        private Brush _foregroundBrush, _backgroundBrush;
        [NonSerialized]
        private TextDecorationCollection _textDecorations;
        [NonSerialized]
        private TextEffectCollection _textEffects;
        [NonSerialized]
        private System.Globalization.CultureInfo _cultureInfo;
        [NonSerialized]
        private bool? _bold, _italic;

        private const double DefaultEmSize = (12 * 96 / 72);

        // List of all TextFormattingRunProperties created
        [NonSerialized]
        private static List<TextFormattingRunProperties> ExistingProperties = new List<TextFormattingRunProperties>();

        // The standard empty properties that contain no specific formatting information
        [NonSerialized]
        private static TextFormattingRunProperties EmptyProperties = new TextFormattingRunProperties();

        // An empty TextEffectsCollection so it doesn't need to be reallocated
        [NonSerialized]
        private static TextEffectCollection EmptyTextEffectCollection = new TextEffectCollection();

        // An empty TextDecorationCollection so it doesn't need to be reallocated
        [NonSerialized]
        private static TextDecorationCollection EmptyTextDecorationCollection = new TextDecorationCollection();

        /// <summary>
        /// Static constructor to freeze static empty collections.
        /// </summary>
        static TextFormattingRunProperties()
        {
            EmptyTextEffectCollection.Freeze();
            EmptyTextDecorationCollection.Freeze();
        }
        #endregion // Private Members

        #region Constructors
        /// <summary>
        /// Initializes a new, empty instance of <see cref="TextFormattingRunProperties"/>.
        /// </summary>
        /// <remarks>The properties return the default values, but the
        /// internal structure is not altered.
        /// </remarks>
        internal TextFormattingRunProperties() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/> for deserialization.
        /// </summary>
        /// <param name="info">The serialization information provided by the deserialization mechanism.</param>
        /// <param name="context">The serialization context.</param>
        internal TextFormattingRunProperties(SerializationInfo info, StreamingContext context)
        {
            _foregroundBrush = (Brush)GetObjectFromSerializationInfo("ForegroundBrush", info);
            _backgroundBrush = (Brush)GetObjectFromSerializationInfo("BackgroundBrush", info);
            _size = (double?)GetObjectFromSerializationInfo("FontRenderingSize", info);
            _hintingSize = (double?)GetObjectFromSerializationInfo("FontHintingSize", info);
            _foregroundOpacity = (double?)GetObjectFromSerializationInfo("ForegroundOpacity", info);
            _backgroundOpacity = (double?)GetObjectFromSerializationInfo("BackgroundOpacity", info);
            _italic = (bool?)GetObjectFromSerializationInfo("Italic", info);
            _bold = (bool?)GetObjectFromSerializationInfo("Bold", info);
            _textDecorations = (TextDecorationCollection)GetObjectFromSerializationInfo("TextDecorations", info);
            _textEffects = (TextEffectCollection)GetObjectFromSerializationInfo("TextEffects", info);
            string cultureName = (string)GetObjectFromSerializationInfo("CultureInfoName", info);
            _cultureInfo = cultureName == null ? (CultureInfo)null : (new CultureInfo(cultureName));

            FontFamily fontFamily = GetObjectFromSerializationInfo("FontFamily", info) as FontFamily;
            if (fontFamily != null)
            {
                FontStyle fontStyle = (FontStyle)GetObjectFromSerializationInfo("Typeface.Style", info);
                FontWeight fontWeight = (FontWeight)GetObjectFromSerializationInfo("Typeface.Weight", info);
                FontStretch fontStretch = (FontStretch)GetObjectFromSerializationInfo("Typeface.Stretch", info);
                _typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
            }

            if (_size.HasValue && _size.Value <= 0)
            {
                // Non-positive text sizes make WPF text formatting throw.
                Debug.Fail("Deserializing a non-positive text size.");
                _size = DefaultEmSize;
            }
            if (_hintingSize.HasValue && _hintingSize.Value <= 0)
            {
                Debug.Fail("Deserializing a non-positive text hint size.");
                _hintingSize = DefaultEmSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/>.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="background">The background brush.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="size">The size.</param>
        /// <param name="hintingSize">The hinting size.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="textEffects">The text effects.</param>
        /// <param name="cultureInfo">The culture info.</param>
        internal TextFormattingRunProperties(Brush foreground, Brush background, Typeface typeface, double? size, double? hintingSize, TextDecorationCollection textDecorations, TextEffectCollection textEffects, CultureInfo cultureInfo)
        {
            if (size.HasValue && size.Value <= 0)
                throw new ArgumentOutOfRangeException("size", "size should be positive or null");
            if (hintingSize.HasValue && hintingSize.Value <= 0)
                throw new ArgumentOutOfRangeException("hintingSize", "hintingSize should be positive or null");

            _foregroundBrush = foreground;
            _backgroundBrush = background;
            _typeface = typeface;
            _size = size;
            _hintingSize = hintingSize;
            _textDecorations = textDecorations;
            _textEffects = textEffects;
            _cultureInfo = cultureInfo;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/> from a second instance.
        /// </summary>
        /// <param name="toCopy">The TextFormattingRunProperties to copy.</param>
        internal TextFormattingRunProperties(TextFormattingRunProperties toCopy)
        {
            _foregroundBrush = toCopy._foregroundBrush;
            _backgroundBrush = toCopy._backgroundBrush;
            _typeface = toCopy._typeface;
            _size = toCopy._size;
            _hintingSize = toCopy._hintingSize;
            _textDecorations = toCopy._textDecorations;
            _textEffects = toCopy._textEffects;
            _cultureInfo = toCopy._cultureInfo;
            _backgroundOpacity = toCopy._backgroundOpacity;
            _foregroundOpacity = toCopy._foregroundOpacity;
            _italic = toCopy._italic;
            _bold = toCopy._bold;

            if (_size.HasValue && _size.Value <= 0)
            {
                // Non-positive text sizes make WPF text formatting throw.
                Debug.Fail("Copying a non-positive text size.");
                _size = DefaultEmSize;
            }
            if (_hintingSize.HasValue && _hintingSize.Value <= 0)
            {
                Debug.Fail("Copying a non-positive text hint size.");
                _hintingSize = DefaultEmSize;
            }
        }

        #endregion // Constructors

        #region Public Factory Methods

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/>.
        /// </summary>
        /// <returns>The default TextFormattingRunProperties for the system.</returns>
        [EditorBrowsable(EditorBrowsableState.Always)]
        public static TextFormattingRunProperties CreateTextFormattingRunProperties()
        {
            return FindOrCreateProperties(EmptyProperties);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/> with the specified options.
        /// </summary>
        /// <param name="typeface">The typeface of the text.</param>
        /// <param name="size">The size of the text.</param>
        /// <param name="foreground">The foreground color of the text.</param>
        /// <returns>A TextFormattingRunProperties that has the requested properties.</returns>
        [EditorBrowsable(EditorBrowsableState.Always)]
        public static TextFormattingRunProperties CreateTextFormattingRunProperties(Typeface typeface, double size, Color foreground)
        {
            return FindOrCreateProperties(new TextFormattingRunProperties(new SolidColorBrush(foreground), null, typeface, size, null, null, null, null));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextFormattingRunProperties"/> with the specified options.
        /// </summary>
        /// <param name="foreground">The foreground brush of the text.</param>
        /// <param name="background">The background brush of the text.</param>
        /// <param name="typeface">The typeface of the text.</param>
        /// <param name="size">The size of the text.</param>
        /// <param name="hintingSize">The hinting size of the text.</param>
        /// <param name="textDecorations">The text decorations on the text.</param>
        /// <param name="textEffects">The text effects on the text.</param>
        /// <param name="cultureInfo">The culture info.</param>
        /// <returns>A TextFormattingRunProperties object that has the requested properties.</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TextFormattingRunProperties CreateTextFormattingRunProperties(Brush foreground, Brush background, Typeface typeface, double? size, double? hintingSize, TextDecorationCollection textDecorations, TextEffectCollection textEffects, CultureInfo cultureInfo)
        {
            return FindOrCreateProperties(new TextFormattingRunProperties(foreground, background, typeface, size, hintingSize, textDecorations, textEffects, cultureInfo));
        }

        #endregion // Public Factory Methods

        #region Exposed Properties

        /// <summary>
        /// Gets the background brush.
        /// </summary>
        /// <remarks>
        /// This property gets a transparent brush if the background brush is not currently set.
        /// </remarks>
        public override Brush BackgroundBrush
        {
            get
            {
                return _backgroundBrush ?? Brushes.Transparent;
            }
        }

        /// <summary>
        /// Gets the culture information.
        /// </summary>
        /// <remarks>
        /// Returns the current culture if no culture is currently set.
        /// </remarks>
        public override System.Globalization.CultureInfo CultureInfo
        {
            get
            {
                return _cultureInfo ?? System.Globalization.CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Gets the font hinting size.
        /// </summary>
        /// <remarks>
        /// Returns zero if no hinting size is currently set.
        /// </remarks>
        public override double FontHintingEmSize
        {
            get
            {
                return _hintingSize ?? DefaultEmSize;
            }
        }

        /// <summary>
        /// Gets the font rendering size.
        /// </summary>
        /// <remarks>
        /// Returns zero if no rendering size is currently set.
        /// </remarks>
        public override double FontRenderingEmSize
        {
            get
            {
                return _size ?? DefaultEmSize;
            }
        }

        /// <summary>
        /// Gets the foreground brush.
        /// </summary>
        /// <remarks>
        /// Returns a transparent brush if the foreground brush is not currently set.
        /// </remarks>
        public override Brush ForegroundBrush
        {
            get
            {
                return _foregroundBrush ?? Brushes.Transparent;
            }
        }

        /// <summary>
        /// Returns true if the formatting is made explicitly italic.
        /// </summary>
        /// <remarks>
        /// Returns false if <see cref="ItalicEmpty"/> is true.
        /// </remarks>
        public bool Italic
        {
            get
            {
                return _italic.HasValue ? _italic.Value : false;
            }
        }

        /// <summary>
        /// Returns true if the formatting is made explicitly bold.
        /// </summary>
        /// <remarks>
        /// Returns false if <see cref="BoldEmpty"/> returns true.
        /// </remarks>
        public bool Bold
        {
            get
            {
                return _bold.HasValue ? _bold.Value : false;
            }
        }

        /// <summary>
        /// Returns the opacity of the foreground.
        /// </summary>
        /// <remarks>
        /// Returns 1.0 if <see cref="ForegroundOpacityEmpty"/> is true.
        /// </remarks>
        public double ForegroundOpacity
        {
            get
            {
                return _foregroundOpacity.HasValue ? _foregroundOpacity.Value : 1.00;
            }
        }

        /// <summary>
        /// Returns the opacity of the background.
        /// </summary>
        /// <remarks>
        /// Returns 1.0 if <see cref="BackgroundOpacityEmpty"/> is true.
        /// </remarks>
        public double BackgroundOpacity
        {
            get
            {
                return _backgroundOpacity.HasValue ? _backgroundOpacity.Value : 1.00;
            }
        }

        /// <summary>
        /// Gets the decorations for the text.
        /// </summary>
        /// <remarks>
        /// Returns an empty <see cref="TextDecorationCollection"/> if no collection is currently set.
        /// </remarks>
        public override TextDecorationCollection TextDecorations
        {
            get
            {
                return _textDecorations ?? EmptyTextDecorationCollection;
            }
        }

        /// <summary>
        /// Gets the text effects for the text.
        /// </summary>
        /// <remarks>
        /// Returns an empty TextEffectCollection if no collection is currently set.
        /// </remarks>
        public override TextEffectCollection TextEffects
        {
            get
            {
                return _textEffects ?? EmptyTextEffectCollection;
            }
        }

        /// <summary>
        /// Gets the Typeface for the text.
        /// </summary>
        /// <remarks>
        /// Returns the system default Typeface if no typeface is currently set.
        /// </remarks>
        public override Typeface Typeface
        {
            get
            {
                return _typeface;
            }
        }

        /// <summary>
        /// Determines whether the background brush is empty.
        /// </summary>
        /// <returns><c>true</c> if the background brush is empty, <c>false</c>otherwise.</returns>
        public bool BackgroundBrushEmpty
        {
            get
            {
                return _backgroundBrush == null;
            }
        }

        /// <summary>
        /// Determines whether any custom opacity is explicitly set for the background.
        /// </summary>
        /// <returns><c>true</c> if the there is no custom opacity set, <c>false</c>otherwise.</returns>
        public bool BackgroundOpacityEmpty
        {
            get
            {
                return !_backgroundOpacity.HasValue;
            }
        }

        /// <summary>
        /// Determines whether any custom opacity is explicitly set for the foreground.
        /// </summary>
        /// <returns><c>true</c> if the there is no custom opacity set, <c>false</c> otherwise.</returns>
        public bool ForegroundOpacityEmpty
        {
            get
            {
                return !_foregroundOpacity.HasValue;
            }
        }

        /// <summary>
        /// Determines whether the bold property is set.
        /// </summary>
        /// <returns><c>true</c> if the bold property is not set, <c>false</c> otherwise.</returns>
        public bool BoldEmpty
        {
            get
            {
                return !_bold.HasValue;
            }
        }

        /// <summary>
        /// Determines whether the italic property is set.
        /// </summary>
        /// <returns><c>true</c> if the italic property is not set, <c>false</c> otherwise.</returns>
        public bool ItalicEmpty
        {
            get
            {
                return !_italic.HasValue;
            }
        }

        /// <summary>
        /// Determines whether the culture info is empty.
        /// </summary>
        /// <returns><c>true</c> if the culture info is empty, <c>false</c> otherwise.</returns>
        public bool CultureInfoEmpty
        {
            get
            {
                return _cultureInfo == null;
            }
        }

        /// <summary>
        /// Determines whether the font hinting size is empty.
        /// </summary>
        /// <returns><c>true</c> if the font hinting is empty, <c>false</c> otherwise.</returns>
        public bool FontHintingEmSizeEmpty
        {
            get
            {
                return _hintingSize == null;
            }
        }


        /// <summary>
        /// Determines whether the size is empty.
        /// </summary>
        /// <returns><c>true</c> if the size is empty, <c>false</c> otherwise.</returns>
        public bool FontRenderingEmSizeEmpty
        {
            get
            {
                return _size == null;
            }
        }

        /// <summary>
        /// Determines whether the foreground brush is empty.
        /// </summary>
        /// <returns><c>true</c> if the foreground brush is empty, <c>false</c> otherwise.</returns>
        public bool ForegroundBrushEmpty
        {
            get
            {
                return _foregroundBrush == null;
            }
        }

        /// <summary>
        /// Determines whether the text decorations collection is empty.
        /// </summary>
        /// <returns><c>true</c> if the text decorations collection is empty, <c>false</c> otherwise.</returns>
        public bool TextDecorationsEmpty
        {
            get
            {
                return _textDecorations == null;
            }
        }

        /// <summary>
        /// Determines whether the text effects collection is empty.
        /// </summary>
        /// <returns><c>true</c> if the text effects collection is empty, <c>false</c> otherwise.</returns>
        public bool TextEffectsEmpty
        {
            get
            {
                return _textEffects == null;
            }
        }

        /// <summary>
        /// Determines whether the typeface is empty.
        /// </summary>
        /// <returns><c>true</c> if the typeface is empty, <c>false</c> otherwise.</returns>
        public bool TypefaceEmpty
        {
            get
            {
                return _typeface == null;
            }
        }

        #endregion // Exposed Properties

        #region Public Setters

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same, but clears the <see cref="Bold"/> property.
        /// </summary>
        public TextFormattingRunProperties ClearBold()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._bold = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same, but clears the <see cref="Italic"/> property.
        /// </summary>
        public TextFormattingRunProperties ClearItalic()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._italic = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same, but clears the <see cref="ForegroundOpacity"/> property.
        /// </summary>
        public TextFormattingRunProperties ClearForegroundOpacity()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._foregroundOpacity = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same, but clears the <see cref="BackgroundOpacity"/> property.
        /// </summary>
        public TextFormattingRunProperties ClearBackgroundOpacity()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._backgroundOpacity = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the background brush.
        /// </summary>
        public TextFormattingRunProperties ClearBackgroundBrush()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._backgroundBrush = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> ith all properties the same except for the culture info.
        /// </summary>
        public TextFormattingRunProperties ClearCultureInfo()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._cultureInfo = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the font hinting size.
        /// </summary>
        public TextFormattingRunProperties ClearFontHintingEmSize()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._hintingSize = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the rendering size.
        /// </summary>
        public TextFormattingRunProperties ClearFontRenderingEmSize()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._size = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the foreground brush.
        /// </summary>
        public TextFormattingRunProperties ClearForegroundBrush()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._foregroundBrush = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the text decorations.
        /// </summary>
        public TextFormattingRunProperties ClearTextDecorations()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._textDecorations = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the text effects.
        /// </summary>
        public TextFormattingRunProperties ClearTextEffects()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._textEffects = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with all properties the same except for the typeface.
        /// </summary>
        public TextFormattingRunProperties ClearTypeface()
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._typeface = null;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but 
        /// with the background brush set to <paramref name="brush"/>.
        /// </summary>
        /// <param name="brush">The new background brush.</param>
        /// <remarks>
        /// The brush is frozen by this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="brush"/> is null.</exception>
        public TextFormattingRunProperties SetBackgroundBrush(Brush brush)
        {
            // Validate
            if (brush == null)
                throw new ArgumentNullException("brush");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._backgroundBrush = brush;
            copy.FreezeBackgroundBrush();

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the background set to <paramref name="background"/>.
        /// </summary>
        /// <param name="background">The new background color.</param>
        /// <remarks>
        /// The background brush is changed by this method.
        /// </remarks>
        public TextFormattingRunProperties SetBackground(Color background)
        {
            return SetBackgroundBrush(new SolidColorBrush(background));
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the culture set to <paramref name="cultureInfo"/>.
        /// </summary>
        /// <param name="cultureInfo">The new culture information.</param>
        public TextFormattingRunProperties SetCultureInfo(System.Globalization.CultureInfo cultureInfo)
        {
            // Validate
            if (cultureInfo == null)
                throw new ArgumentNullException("cultureInfo");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._cultureInfo = cultureInfo;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the font hinting size set to <paramref name="hintingSize"/>.
        /// </summary>
        /// <param name="hintingSize">The new font hinting size.</param>
        public TextFormattingRunProperties SetFontHintingEmSize(double hintingSize)
        {
            // Validate
            if (hintingSize <= 0)
                throw new ArgumentOutOfRangeException("hintingSize");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._hintingSize = hintingSize;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the font rendering size set to <paramref name="renderingSize"/>.
        /// </summary>
        /// <param name="renderingSize">The new rendering size.</param>
        public TextFormattingRunProperties SetFontRenderingEmSize(double renderingSize)
        {
            // Validate
            if (renderingSize <= 0)
                throw new ArgumentOutOfRangeException("renderingSize");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._size = renderingSize;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one 
        /// but with the new foreground <see cref="Brush"/> set to <paramref name="brush"/>.
        /// </summary>
        /// <param name="brush">The new foreground brush.</param>
        /// <remarks>
        /// The brush is frozen by this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="brush"/>is null.</exception>
        public TextFormattingRunProperties SetForegroundBrush(Brush brush)
        {
            // Validate
            if (brush == null)
                throw new ArgumentNullException("brush");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._foregroundBrush = brush;
            copy.FreezeForegroundBrush();

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the foreground set to <paramref name="foreground"/>.
        /// </summary>
        /// <param name="foreground">The new foreground color.</param>
        /// <remarks>
        /// The foreground brush is changed by this method.
        /// </remarks>
        public TextFormattingRunProperties SetForeground(Color foreground)
        {
            return SetForegroundBrush(new SolidColorBrush(foreground));
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the text decorations set to <paramref name="textDecorations"/>.
        /// </summary>
        /// <param name="textDecorations">The new text decoration collection.</param>
        /// <remarks>
        /// The <paramref name="textDecorations"/> is frozen by this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textDecorations"/> is null.</exception>
        public TextFormattingRunProperties SetTextDecorations(TextDecorationCollection textDecorations)
        {
            // Validate
            if (textDecorations == null)
                throw new ArgumentNullException("textDecorations");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._textDecorations = textDecorations;
            copy.FreezeTextDecorations();

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the text effects set to <paramref name="textEffects"/>.
        /// </summary>
        /// <param name="textEffects">The new text effect collection.</param>
        /// <remarks>
        /// The <paramref name="textEffects"/> is frozen by this method.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textEffects"/> is null.</exception>
        public TextFormattingRunProperties SetTextEffects(TextEffectCollection textEffects)
        {
            // Validate
            if (textEffects == null)
                throw new ArgumentNullException("textEffects");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._textEffects = textEffects;
            copy.FreezeTextEffects();

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the typeface set to <paramref name="typeface"/>.
        /// </summary>
        /// <param name="typeface">The new typeface.</param>
        /// <exception cref="ArgumentNullException"><paramref name="typeface"/> is null.</exception>
        /// <remarks>
        /// If you wish to only make the formatting either italic or bold, instead of setting a typeface, please use the 
        /// <see cref="SetBold"/> and <see cref="SetItalic"/> methods.
        /// </remarks>
        public TextFormattingRunProperties SetTypeface(Typeface typeface)
        {
            // Validate
            if (typeface == null)
                throw new ArgumentNullException("typeface");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._typeface = typeface;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the <see cref="ForegroundOpacity"/> property set to <paramref name="opacity"/>.
        /// </summary>
        /// <param name="opacity">The foreground opacity.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="opacity"/> is less than zero or bigger than 1</exception>
        public TextFormattingRunProperties SetForegroundOpacity(double opacity)
        {
            if (opacity < 0.0 || opacity > 1.0)
                throw new ArgumentOutOfRangeException("opacity");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._foregroundOpacity = opacity;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the <see cref="BackgroundOpacity"/> property set to <paramref name="opacity"/>.
        /// </summary>
        /// <param name="opacity">The background opacity.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="opacity"/> is less than zero or bigger than 1</exception>
        public TextFormattingRunProperties SetBackgroundOpacity(double opacity)
        {
            if (opacity < 0.0 || opacity > 1.0)
                throw new ArgumentOutOfRangeException("opacity");

            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._backgroundOpacity = opacity;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the <see cref="Bold"/> property set to <paramref name="isBold"/>.
        /// </summary>
        /// <param name="isBold">Should be set to true if text formatting is to be bold.</param>
        public TextFormattingRunProperties SetBold(bool isBold)
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._bold = isBold;

            return FindOrCreateProperties(copy);
        }

        /// <summary>
        /// Gets a new <see cref="TextFormattingRunProperties"/> with the properties of this one but
        /// with the <see cref="Italic"/> property set to <paramref name="isItalic"/>.
        /// </summary>
        /// <param name="isItalic">Should be set to true if text formatting is to be italic.</param>
        public TextFormattingRunProperties SetItalic(bool isItalic)
        {
            TextFormattingRunProperties copy = new TextFormattingRunProperties(this);
            copy._italic = isItalic;

            return FindOrCreateProperties(copy);
        }

        #endregion // Public Setters

        #region Public Methods

        /// <summary>
        /// Determines whether the foreground brush for this <see cref="TextFormattingRunProperties"/> is the same as <paramref name="brush"/>.
        /// </summary>
        /// <param name="brush">The other <see cref="Brush"/>.</param>
        /// <returns><c>true</c> if the foreground brushes are the same, <c>false</c> if they are not.</returns>
        public bool ForegroundBrushSame(Brush brush)
        {
            return BrushesEqual(this._foregroundBrush, brush);
        }

        /// <summary>
        /// Determines whether the background brush for this <see cref="TextFormattingRunProperties"/> is the same as <paramref name="brush"/>.
        /// </summary>
        /// <param name="brush">The other <see cref="Brush"/>.</param>
        /// <returns><c>true</c> if the background brushes are the same, <c>false</c> if they are not.</returns>
        public bool BackgroundBrushSame(Brush brush)
        {
            return BrushesEqual(this._backgroundBrush, brush);
        }

        /// <summary>
        /// Determines whether font sizes for two <see cref="TextFormattingRunProperties"/> are the same.
        /// </summary>
        /// <param name="other">The other <see cref="TextFormattingRunProperties"/>.</param>
        /// <returns><c>true</c> if the sizes are the same, <c>false</c> if they are not.</returns>
        public bool SameSize(TextFormattingRunProperties other)
        {
            if (other == null)
                return false;

            return _size == other._size;
        }

        #endregion // Public methods

        #region Private Methods

        /// <summary>
        /// Determine whether two brushes are equal.
        /// </summary>
        /// <param name="brush">The first brush.</param>
        /// <param name="other">The second brush.</param>
        /// <returns><c>true</c> if the two are equal, <c>false</c> otherwise.</returns>
        /// <remarks>internal for testability</remarks>
        internal static bool BrushesEqual(Brush brush, Brush other)
        {
            if (object.ReferenceEquals(brush, other))
            {
                return true;
            }
            else if (object.ReferenceEquals(brush, null) || object.ReferenceEquals(other, null))
            {
                return false;
            }
            else
            {
                if (brush.Opacity == 0 && other.Opacity == 0)
                    return true;

                SolidColorBrush colorBrush1 = brush as SolidColorBrush;
                SolidColorBrush colorBrush2 = other as SolidColorBrush;

                // If both brushes are SolidColorBrushes check the color of each
                if (colorBrush1 != null && colorBrush2 != null)
                {
                    if (colorBrush1.Color.A == 0 && colorBrush2.Color.A == 0)
                        return true;

                    return colorBrush1.Color == colorBrush2.Color &&
                           Math.Abs(colorBrush1.Opacity - colorBrush2.Opacity) < 0.01;
                }

                // as a last resort try brush.Equals (which pretty much is the equivalent of returning false here since
                // it doesn't compare any of the core properties of brushes)
                return brush.Equals(other);
            }
        }

        private static bool TypefacesEqual(Typeface typeface, Typeface other)
        {
            if (typeface == null)
                return (other == null);
            else
                return typeface.Equals(other);  // .Equals is crucial here! We won't see reference equality.
        }

        /// <summary>
        /// Return either the existing TextFormattingRunProperties that matches the requested
        /// properties, or add the new one to our list of existing properties and return it.
        /// </summary>
        /// <param name="properties">The properties to find the unique instance of.</param>
        /// <returns>The unique instance satisfying the properties passed in.</returns>
        internal static TextFormattingRunProperties FindOrCreateProperties(TextFormattingRunProperties properties)
        {
            // See if properties matching the one passed in exist.
            TextFormattingRunProperties existing = ExistingProperties.Find(delegate(TextFormattingRunProperties other)
            {
                return properties.IsEqual(other);
            });

            // If no properties matching the requested ones exist, use the passed in properties as our unique instance
            if (existing == null)
            {
                // Freeze everything first to ensure all properties are frozen
                properties.FreezeEverything();

                // Add to our list of existing items so this set of properties can be returned in the future
                // if requested again.
                ExistingProperties.Add(properties);

                // Return the properties
                return properties;
            }

            // Return the existing properties that match the properties passed in
            return existing;
        }

        /// <summary>
        /// Determine whether two TextFormattingRunProperties have the same formatting effects.
        /// </summary>
        /// <param name="other">The other set of properties to check against.</param>
        /// <returns>true if the two TextFormattingRunProperties have the same formatting effects, false otherwise.</returns>
        private bool IsEqual(TextFormattingRunProperties other)
        {
            return _size == other._size &&
                   _hintingSize == other._hintingSize &&
                   TypefacesEqual(_typeface, other._typeface) &&
                   _cultureInfo == other._cultureInfo &&
                   _textDecorations == other._textDecorations &&
                   _textEffects == other._textEffects &&
                   _italic == other._italic &&
                   _bold == other._bold &&
                   _foregroundOpacity == other._foregroundOpacity &&
                   _backgroundOpacity == other._backgroundOpacity && 
                   BrushesEqual(_foregroundBrush, other._foregroundBrush) &&
                   BrushesEqual(_backgroundBrush, other._backgroundBrush);
        }

        /// <summary>
        /// Freeze the background brush.
        /// </summary>
        private void FreezeBackgroundBrush()
        {
            if (_backgroundBrush != null)
            {
                if (_backgroundBrush.CanFreeze)
                    _backgroundBrush.Freeze();
            }
        }

        /// <summary>
        /// Freeze all available freezable members.
        /// </summary>
        private void FreezeEverything()
        {
            FreezeForegroundBrush();
            FreezeBackgroundBrush();
            FreezeTextEffects();
            FreezeTextDecorations();
        }

        /// <summary>
        /// Freeze the foreground brush.
        /// </summary>
        private void FreezeForegroundBrush()
        {
            if (_foregroundBrush != null)
            {
                if (_foregroundBrush.CanFreeze)
                    _foregroundBrush.Freeze();
            }
        }

        /// <summary>
        /// Freeze the text decorations object.
        /// </summary>
        private void FreezeTextDecorations()
        {
            if (_textDecorations != null)
            {
                if (_textDecorations.CanFreeze)
                    _textDecorations.Freeze();
            }
        }

        /// <summary>
        /// Freeze the text effects object.
        /// </summary>
        private void FreezeTextEffects()
        {
            if (_textEffects != null)
            {
                if (_textEffects.CanFreeze)
                    _textEffects.Freeze();
            }
        }

        #endregion // Private Methods

        #region ISerializable Members

        /// <summary>
        /// Serializes the <see cref="TextFormattingRunProperties"/> object using a XamlWriter.
        /// </summary>
        /// <param name="info">The SerializationInfo used for serialization.</param>
        /// <param name="context">The serialization context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            // Add in all members to the SerializationInfo object
            info.AddValue("BackgroundBrush", BackgroundBrushEmpty ? "null" : XamlWriter.Save(BackgroundBrush));
            info.AddValue("ForegroundBrush", ForegroundBrushEmpty ? "null" : XamlWriter.Save(ForegroundBrush));
            info.AddValue("FontHintingSize", FontHintingEmSizeEmpty ? "null" : XamlWriter.Save(FontHintingEmSize));
            info.AddValue("FontRenderingSize", FontRenderingEmSizeEmpty ? "null" : XamlWriter.Save(FontRenderingEmSize));
            info.AddValue("TextDecorations", TextDecorationsEmpty ? "null" : XamlWriter.Save(TextDecorations));
            info.AddValue("TextEffects", TextEffectsEmpty ? "null" : XamlWriter.Save(TextEffects));
            info.AddValue("CultureInfoName", CultureInfoEmpty ? "null" : XamlWriter.Save(CultureInfo.Name));
            info.AddValue("FontFamily", TypefaceEmpty ? "null" : XamlWriter.Save(Typeface.FontFamily));
            info.AddValue("Italic", ItalicEmpty ? "null" : XamlWriter.Save(Italic));
            info.AddValue("Bold", BoldEmpty ? "null" : XamlWriter.Save(Bold));
            info.AddValue("ForegroundOpacity", ForegroundOpacityEmpty ? "null" : XamlWriter.Save(ForegroundOpacity));
            info.AddValue("BackgroundOpacity", BackgroundOpacityEmpty ? "null" : XamlWriter.Save(BackgroundOpacity));
            
            // Only add typeface info if we have a typeface.
            if (!TypefaceEmpty)
            {
                info.AddValue("Typeface.Style", XamlWriter.Save(Typeface.Style));
                info.AddValue("Typeface.Weight", XamlWriter.Save(Typeface.Weight));
                info.AddValue("Typeface.Stretch", XamlWriter.Save(Typeface.Stretch));
            }
        }

        /// <summary>
        /// Deserializes an object from the SerializationInfo struct using a XamlReader.
        /// </summary>
        /// <param name="name">The name of the object to deserialize.</param>
        /// <param name="info">The SerializationInfo used to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        private object GetObjectFromSerializationInfo(string name, SerializationInfo info)
        {
            string serializedObject = info.GetString(name);

            // A null value was stored
            if (serializedObject == "null")
            {
                return null;
            }

            // Get the object using the XamlReader
            return XamlReader.Parse(serializedObject);
        }

        #endregion

        #region IObjectReference Members
        /// <summary>
        /// Gets the interned <see cref="TextFormattingRunProperties"/> object.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The interned <see cref="TextFormattingRunProperties"/> object.</returns>
        public object GetRealObject(StreamingContext context)
        {
            return TextFormattingRunProperties.FindOrCreateProperties(this);
        }

        #endregion
    }
}
