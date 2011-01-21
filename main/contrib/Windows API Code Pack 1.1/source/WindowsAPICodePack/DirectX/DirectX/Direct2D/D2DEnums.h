// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

/// <summary>
/// Specifies whether an arc should be greater than 180 degrees.
/// <para>(Also see DirectX SDK: D2D1_ARC_SIZE)</para>
///</summary>
public enum class ArcSize : Int32
{
    /// <summary>
    /// An arc's sweep should be 180 degrees or less.
    ///</summary>
    Small = D2D1_ARC_SIZE_SMALL,
    /// <summary>
    /// An arc's sweep should be 180 degrees or greater.
    ///</summary>
    Large = D2D1_ARC_SIZE_LARGE,
};

/// <summary>
/// Specifies how a geometry is simplified to a SimplifiedGeometrySink.
/// <para>(Also see DirectX SDK: D2D1_GEOMETRY_SIMPLIFICATION_OPTION)</para>
///</summary>
public enum class GeometrySimplificationOption : Int32
{
    /// <summary>
    /// The output can contain cubic Bezier curves and line segments.
    ///</summary>
    CubicsAndLines = D2D1_GEOMETRY_SIMPLIFICATION_OPTION_CUBICS_AND_LINES,
    /// <summary>
    /// The output is flattened so that it contains only line segments. 
    ///</summary>
    Lines = D2D1_GEOMETRY_SIMPLIFICATION_OPTION_LINES,
};

/// <summary>
/// Specifies the algorithm that is used when images are scaled or rotated.
/// <para>(Also see DirectX SDK: D2D1_BITMAP_INTERPOLATION_MODE)</para>
///</summary>
/// <remarks>
/// To stretch an image, each pixel in the original image must be mapped to a group of pixels in the larger image. 
/// To shrink an image, groups of pixels in the original image must be mapped to single pixels in the smaller image. The effectiveness of the algorithms that perform these mappings determines the quality of a scaled image. 
/// Algorithms that produce higher-quality scaled images tend to require more processing time. NearestNeighbor provides faster but lower-quality interpolation, while Linear provides higher-quality interpolation. 
/// </remarks>
public enum class BitmapInterpolationMode : Int32
{

    /// <summary>
    /// Use the exact color of the nearest bitmap pixel to the current rendering pixel.
    /// </summary>
    NearestNeighbor = D2D1_BITMAP_INTERPOLATION_MODE_NEAREST_NEIGHBOR,

    /// <summary>
    /// Interpolate a color from the four bitmap pixels that are the nearest to the rendering pixel.
    /// </summary>
    Linear = D2D1_BITMAP_INTERPOLATION_MODE_LINEAR,
};

/// <summary>
/// This determines what gamma is used for interpolation/blending.
/// <para>(Also see DirectX SDK: D2D1_GAMMA)</para>
///</summary>
public enum class Gamma : Int32
{

    /// <summary>
    /// Interpolation and blending are performed in the standard RGB (sRGB) standard gamma.
    /// </summary>
    StandardRgb = D2D1_GAMMA_2_2,

    /// <summary>
    /// Interpolation and blending are performed in the linear-gamma color space.
    /// </summary>
    Linear = D2D1_GAMMA_1_0,
};

/// <summary>
/// Describes whether an opacity mask contains graphics or text. Direct2D uses this information to determine which gamma space to use when blending the opacity mask.
/// <para>(Also see DirectX SDK: D2D1_OPACITY_MASK_CONTENT)</para>
///</summary>
public enum class OpacityMaskContent : Int32
{

    /// <summary>
    /// The opacity mask contains graphics. The opacity mask is blended in the gamma 2.2 color space.
    /// </summary>
    Graphics = D2D1_OPACITY_MASK_CONTENT_GRAPHICS,

    /// <summary>
    /// The opacity mask contains non-GDI text. 
    /// The gamma space used for blending is obtained from the render target's text rendering parameters (RenderTarget.TextRenderingParams)
    /// </summary>
    TextNatural = D2D1_OPACITY_MASK_CONTENT_TEXT_NATURAL,

    /// <summary>
    /// The opacity mask contains text rendered using the GDI-compatible rendering mode. The opacity mask is blended using the gamma for GDI rendering.
    /// </summary>
    TextGdiCompatible = D2D1_OPACITY_MASK_CONTENT_TEXT_GDI_COMPATIBLE,
};

/// <summary>
/// This enumeration describes the type of combine operation to be performed.
/// <para>(Also see DirectX SDK: D2D1_COMBINE_MODE)</para>
///</summary>
public enum class CombineMode : Int32
{

    /// <summary>
    /// The two regions are combined by taking the union of both. Given two geometries, A and B, the resulting geometry is <i>geometry A + geometry B</i>.
    /// </summary>
    Union = D2D1_COMBINE_MODE_UNION,

    /// <summary>
    /// The two regions are combined by taking their intersection. The new area consists of the overlapping region between the two geometries.
    /// </summary>
    Intersect = D2D1_COMBINE_MODE_INTERSECT,

    /// <summary>
    /// The two regions are combined by taking the area that exists in the first region but not the second and the area that exists in the second region but not the first. Given two geometries, A and B, the new region consists of (A-B) + (B-A).
    /// </summary>
    Xor = D2D1_COMBINE_MODE_XOR,

    /// <summary>
    /// The second region is excluded from the first. Given two geometries, A and B, the area of geometry B is removed from the area of geometry A, producing a region that is A-B.
    /// </summary>
    Exclude = D2D1_COMBINE_MODE_EXCLUDE,
};

/// <summary>
/// Describes the sequence of dashes and gaps in a stroke.
/// <para>(Also see DirectX SDK: D2D1_DASH_STYLE)</para>
///</summary>
public enum class DashStyle : Int32
{
    /// <summary>
    /// A solid line with no breaks.
    ///</summary>
    Solid = D2D1_DASH_STYLE_SOLID,
    /// <summary>
    /// A dash followed by a gap of equal length. The dash and the gap are each twice as long as the stroke thickness.
    /// <para>The equivalent dash array is {2, 2}.</para>
    ///</summary>
    Dash = D2D1_DASH_STYLE_DASH,
    /// <summary>
    /// A dot followed by a longer gap.
    /// <para>The equivalent dash array is {0, 2}.</para>
    ///</summary>
    Dot = D2D1_DASH_STYLE_DOT,
    /// <summary>
    /// A dash, followed by a gap, followed by a dot, followed by another gap.
    /// <para>The equivalent dash array is {2, 2, 0, 2}.</para>
    ///</summary>
    DashDot = D2D1_DASH_STYLE_DASH_DOT,
    /// <summary>
    /// A dash, followed by a gap, followed by a dot, followed by another gap, followed by another dot, followed by another gap.
    /// <para>The equivalent dash array is {2, 2, 0, 2, 0, 2}.</para>
    ///</summary>
    DashDotDot = D2D1_DASH_STYLE_DASH_DOT_DOT,
    /// <summary>
    /// The dash pattern is specified by an array of floating-point values.
    ///</summary>
    Custom = D2D1_DASH_STYLE_CUSTOM,
};

/// <summary>
/// Indicates the type of information that Direct2D sends to the debug layer.
/// <para>(Also see DirectX SDK: D2D1_DEBUG_LEVEL)</para>
///</summary>
public enum class DebugLevel : Int32
{
    /// <summary>
    /// Direct2D does not produce any debugging output.
    ///</summary>
    None = D2D1_DEBUG_LEVEL_NONE,
    /// <summary>
    /// Direct2D sends error messages to the debug layer.
    ///</summary>
    Error = D2D1_DEBUG_LEVEL_ERROR,
    /// <summary>
    /// Direct2D sends error messages and warnings to the debug layer.
    ///</summary>
    Warning = D2D1_DEBUG_LEVEL_WARNING,
    /// <summary>
    /// Direct2D sends error messages, warnings, and additional diagnostic information to the debug layer.
    ///</summary>
    Information = D2D1_DEBUG_LEVEL_INFORMATION,
};

/// <summary>
/// Specifies whether text snapping is suppressed or clipping to the layout rectangle is enabled. This enumeration allows a bitwise combination of its member values.
/// <para>(Also see DirectX SDK: D2D1_DRAW_TEXT_OPTIONS)</para>
///</summary>
[Flags]
public enum class DrawTextOptions : Int32
{

    /// <summary>
    /// Text is vertically snapped to pixel boundaries and is not clipped to the layout rectangle.
    /// </summary>
    None = D2D1_DRAW_TEXT_OPTIONS_NONE,

    /// <summary>
    /// Text is not vertically snapped to pixel boundaries. This setting is recommended for text that is being animated.
    /// </summary>
    NoSnap = D2D1_DRAW_TEXT_OPTIONS_NO_SNAP,

    /// <summary>
    /// Text is clipped to the layout rectangle.
    /// </summary>
    Clip = 0x00000002

};

/// <summary>
/// Describes how non-text primitives are rendered.
/// <para>(Also see DirectX SDK: D2D1_ANTIALIAS_MODE)</para>
///</summary>
public enum class AntiAliasMode : Int32
{

    /// <summary>
    /// Edges are antialiased using the Direct2D per-primitive method of high-quality antialiasing.
    /// </summary>
    PerPrimitive = D2D1_ANTIALIAS_MODE_PER_PRIMITIVE,

    /// <summary>
    /// Objects are aliased in most cases. Objects are antialiased only when they are drawn to a render target created by the CreateGraphicsSurfaceRenderTarget method and Direct3D multisampling has been enabled on the backing DirectX Graphics Infrastructure (DXGI) surface.
    /// </summary>
    Aliased = D2D1_ANTIALIAS_MODE_ALIASED,
};

/// <summary>
/// Specifies how the alpha value of a bitmap or render target should be treated.
/// <para>(Also see DirectX SDK: D2D1_ALPHA_MODE)</para>
///</summary>
public enum class AlphaMode : Int32
{

    /// <summary>
    /// Alpha mode should be determined implicitly. Some target surfaces do not supply or
    /// imply this information in which case alpha must be specified.
    /// </summary>
    Unknown = D2D1_ALPHA_MODE_UNKNOWN,

    /// <summary>
    /// The alpha value has been premultiplied. Each color is first scaled by the alpha value. If the multiplied format is greater than the alpha channel, the standard Source Over blending math creates an additive blend.
    /// </summary>
    Premultiplied = D2D1_ALPHA_MODE_PREMULTIPLIED,

    /// <summary>
    /// The alpha value has not been premultiplied; ie. opacity is in the 'A' component only.
    /// </summary>
    Straight = D2D1_ALPHA_MODE_STRAIGHT,

    /// <summary>
    /// Ignore any alpha channel information.
    /// </summary>
    Ignore = D2D1_ALPHA_MODE_IGNORE,
};

/// <summary>
/// Specifies how a brush paints areas outside of its normal content area.
/// <para>(Also see DirectX SDK: D2D1_EXTEND_MODE)</para>
///</summary>
public enum class ExtendMode : Int32
{

    /// <summary>
    /// Repeat the edge pixels of the brush's content for all regions outside the normal content area.
    /// </summary>
    Clamp = D2D1_EXTEND_MODE_CLAMP,

    /// <summary>
    /// Repeat the brush's content.
    /// </summary>
    Wrap = D2D1_EXTEND_MODE_WRAP,

    /// <summary>
    /// The same as wrap, except that alternate tiles of the brush's content are flipped. (The brush's normal content is drawn untransformed).
    /// </summary>
    Mirror = D2D1_EXTEND_MODE_MIRROR,
};

/// <summary>
/// Specifies the threading model of the created factory and all of its derived resources.
/// <para>(Also see DirectX SDK: D2D1_FACTORY_TYPE)</para>
///</summary>
public enum class D2DFactoryType : Int32
{

    /// <summary>
    /// The resulting factory and derived resources may only be invoked serially. Reference
    /// counts on resources are interlocked, however, resource and render target state is
    /// not protected from multi-threaded access.
    /// </summary>
    SingleThreaded = D2D1_FACTORY_TYPE_SINGLE_THREADED,

    /// <summary>
    /// The resulting factory may be invoked from multiple threads. Returned resources use
    /// interlocked reference counting and their state is protected.
    /// </summary>
    Multithreaded = D2D1_FACTORY_TYPE_MULTI_THREADED,
};

/// <summary>
/// Indicates whether the given figure is filled or hollow.
/// <para>(Also see DirectX SDK: D2D1_FIGURE_BEGIN)</para>
///</summary>
public enum class FigureBegin : Int32
{
    /// <summary>
    /// The figure is filled.
    /// </summary>
    Filled = D2D1_FIGURE_BEGIN_FILLED,
    /// <summary>
    /// The figure is hollow.
    /// </summary>
    Hollow = D2D1_FIGURE_BEGIN_HOLLOW,
};

/// <summary>
/// Indicates whether the figure is open or closed on its end point.
/// <para>(Also see DirectX SDK: D2D1_FIGURE_END)</para>
///</summary>
public enum class FigureEnd : Int32
{
    /// <summary>
    /// The figure is open.
    /// </summary>
    Open = D2D1_FIGURE_END_OPEN,
    /// <summary>
    /// The figure is closed.
    /// </summary>
    Closed = D2D1_FIGURE_END_CLOSED,
};

/// <summary>
/// <para>(Also see DirectX SDK: D2D1_GEOMETRY_RELATION)</para>
///</summary>
public enum class GeometryRelation : Int32
{

    /// <summary>
    /// The relation between the geometries couldn't be determined. This value is never returned
    /// by any D2D method.
    /// </summary>
    Unknown = D2D1_GEOMETRY_RELATION_UNKNOWN,

    /// <summary>
    /// The two geometries do not intersect at all.
    /// </summary>
    Disjoint = D2D1_GEOMETRY_RELATION_DISJOINT,

    /// <summary>
    /// The passed in geometry is entirely contained by the object.
    /// </summary>
    IsContained = D2D1_GEOMETRY_RELATION_IS_CONTAINED,

    /// <summary>
    /// The object entirely contains the passed in geometry.
    /// </summary>
    Contains = D2D1_GEOMETRY_RELATION_CONTAINS,

    /// <summary>
    /// The two geometries overlap but neither completely contains the other.
    /// </summary>
    Overlap = D2D1_GEOMETRY_RELATION_OVERLAP,
};

/// <summary>
/// Indicates whether the given segment should be stroked, or, if the join between this
/// segment and the previous one should be smooth.
/// <para>(Also see DirectX SDK: D2D1_PATH_SEGMENT)</para>
///</summary>
[Flags]
public enum class PathSegmentOptions : Int32
{
    /// <summary>
    /// The segment is joined as specified by the StrokeStyle class, and it is stroked.
    /// </summary>
    None = D2D1_PATH_SEGMENT_NONE,
    /// <summary>
    /// The segment is not stroked.
    /// </summary>
    ForceUnstroked = D2D1_PATH_SEGMENT_FORCE_UNSTROKED,
    /// <summary>
    /// The segment is always joined with the one preceding it using a round line join, regardless of which LineJoin enumeration is specified 
    /// by the StrokeStyle class. 
    /// If this segment is the first segment and the figure is closed, a round line join is used to connect 
    /// the closing segment with the first segment. 
    /// If the figure is not closed, this setting has no effect on the first segment of the figure. 
    /// </summary>
    ForceRoundLineJoin = D2D1_PATH_SEGMENT_FORCE_ROUND_LINE_JOIN,
};

/// <summary>
/// Enum which descibes the drawing of the ends of a line.
/// <para>(Also see DirectX SDK: D2D1_CAP_STYLE)</para>
///</summary>
public enum class CapStyle : Int32
{

    /// <summary>
    /// A cap that does not extend past the last point of the line. Comparable to cap used for objects other than lines. 
    /// </summary>
    Flat = D2D1_CAP_STYLE_FLAT,

    /// <summary>
    /// Half of a square that has a length equal to the line thickness.
    /// </summary>
    Square = D2D1_CAP_STYLE_SQUARE,

    /// <summary>
    /// A semicircle that has a diameter equal to the line thickness.
    /// </summary>
    Round = D2D1_CAP_STYLE_ROUND,

    /// <summary>
    /// An isosceles right triangle whose hypotenuse is equal in length to the thickness of the line.
    /// </summary>
    Triangle = D2D1_CAP_STYLE_TRIANGLE,
};

/// <summary>
/// Specified options that can be applied when a layer resource is applied to create
/// a layer.
/// <para>(Also see DirectX SDK: D2D1_LAYER_OPTIONS)</para>
///</summary>
[Flags]
public enum class LayerOptions : Int32
{
    /// <summary>
    /// The text in this layer does not use ClearType antialiasing.
    /// </summary>
    None = D2D1_LAYER_OPTIONS_NONE,

    /// <summary>
    /// The layer will render correctly for ClearType text. If the render target was set
    /// to ClearType previously, the layer will continue to render ClearType. If the render
    /// target was set to ClearType and this option is not specified, the render target
    /// will be set to render gray-scale until the layer is popped. The caller can override
    /// this default by calling SetTextAntiAliasMode while within the layer. This flag is
    /// slightly slower than the default.
    /// </summary>
    InitializeForClearType = D2D1_LAYER_OPTIONS_INITIALIZE_FOR_CLEARTYPE,
};

/// <summary>
/// Describes the shape that joins two lines or segments. 
/// <para>(Also see DirectX SDK: D2D1_LINE_JOIN)</para>
///</summary>
/// <remarks>
/// A miter limit affects how sharp miter joins are allowed to be. 
/// If the line join style MiterOrBevel, then the join will be mitered with regular angular vertices 
/// if it doesn't extend beyond the miter limit; otherwise, the line join will be beveled.
/// </remarks>
public enum class LineJoin : Int32
{
    /// <summary>
    /// Regular angular vertices. 
    /// </summary>
    Miter = D2D1_LINE_JOIN_MITER,

    /// <summary>
    /// Beveled vertices. 
    /// </summary>
    Bevel = D2D1_LINE_JOIN_BEVEL,

    /// <summary>
    /// Rounded vertices.
    /// </summary>
    Round = D2D1_LINE_JOIN_ROUND,

    /// <summary>
    /// Regular angular vertices unless the join would extend beyond the miter limit; otherwise, beveled vertices. 
    /// </summary>
    MiterOrBevel = D2D1_LINE_JOIN_MITER_OR_BEVEL,
};

/// <summary>
/// Describes how a render target behaves when it presents its content. This enumeration allows a bitwise combination of its member values.
/// <para>(Also see DirectX SDK: D2D1_PRESENT_OPTIONS)</para>
///</summary>
[Flags]
public enum class PresentOptions : Int32
{    
    /// <summary>
    /// The render target waits until the display refreshes to present and discards the frame upon presenting.
    /// </summary>
    None = D2D1_PRESENT_OPTIONS_NONE,

    /// <summary>
    /// The render target does not discard the frame upon presenting.
    /// </summary>
    RetainContents = D2D1_PRESENT_OPTIONS_RETAIN_CONTENTS,

    /// <summary>
    /// The render target does not wait until the display refreshes to present.
    /// </summary>
    Immediately = D2D1_PRESENT_OPTIONS_IMMEDIATELY,
};

/// <summary>
/// Describes how a render target is remoted and whether it should be GDI-compatible. This enumeration allows a bitwise combination of its member values.
/// <para>(Also see DirectX SDK: D2D1_RENDER_TARGET_USAGE)</para>
///</summary>
[Flags]
public enum class RenderTargetUsages : Int32
{
    /// <summary>
    /// The render target attempts to use Direct3D command-stream remoting and uses bitmap remoting if stream remoting fails. The render target is not GDI-compatible.
    /// </summary>
    None = D2D1_RENDER_TARGET_USAGE_NONE,

    /// <summary>
    /// The render target renders content locally and sends it to the terminal services client as a bitmap. 
    /// </summary>
    ForceBitmapRemoting = D2D1_RENDER_TARGET_USAGE_FORCE_BITMAP_REMOTING,

    /// <summary>
    /// The render target can be used efficiently with GDI.
    /// </summary>
    GdiCompatible = D2D1_RENDER_TARGET_USAGE_GDI_COMPATIBLE,
};

/// <summary>
/// Describes whether a render target uses hardware or software rendering, or if Direct2D should select the rendering mode.
/// <para>(Also see DirectX SDK: D2D1_RENDER_TARGET_TYPE)</para>
///</summary>
public enum class RenderTargetType : Int32
{

    /// <summary>
    /// The render target uses hardware rendering, if available; otherwise, it uses software rendering.
    /// </summary>
    Default = D2D1_RENDER_TARGET_TYPE_DEFAULT,

    /// <summary>
    /// The render target uses software rendering only.
    /// </summary>
    Software = D2D1_RENDER_TARGET_TYPE_SOFTWARE,

    /// <summary>
    /// The render target uses hardware rendering only. 
    /// </summary>
    Hardware = D2D1_RENDER_TARGET_TYPE_HARDWARE,
};

/// <summary>
/// Specifies additional features supportable by a compatible render target when it is created. This enumeration allows a bitwise combination of its member values.
/// <para>(Also see DirectX SDK: D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS)</para>
///</summary>
[Flags]
public enum class CompatibleRenderTargetOptions : Int32
{
    /// <summary>
    /// The render target supports no additional features.
    /// </summary>
    None = D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS_NONE,

    /// <summary>
    /// The render target supports interoperability with the Windows Graphics Device Interface (GDI). 
    /// </summary>
    GdiCompatible = D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS_GDI_COMPATIBLE,
};

/// <summary>
/// Defines the direction that an elliptical arc is drawn. 
/// <para>(Also see DirectX SDK: D2D1_SWEEP_DIRECTION)</para>
///</summary>
public enum class SweepDirection : Int32
{
    /// <summary>
    /// Arcs are drawn in a counterclockwise (negative-angle) direction. 
    /// </summary>
    Counterclockwise = D2D1_SWEEP_DIRECTION_COUNTER_CLOCKWISE,
    /// <summary>
    /// Arcs are drawn in a clockwise (positive-angle) direction.
    /// </summary>
    Clockwise = D2D1_SWEEP_DIRECTION_CLOCKWISE,
};

/// <summary>
/// Describes the antialiasing mode used for drawing text.
/// <para>(Also see DirectX SDK: D2D1_TEXT_ANTIALIAS_MODE)</para>
///</summary>
public enum class TextAntiAliasMode : Int32
{

    /// <summary>
    /// Render text using the current system setting.
    /// </summary>
    Default = D2D1_TEXT_ANTIALIAS_MODE_DEFAULT,

    /// <summary>
    /// Render text using ClearType antialiasing.
    /// </summary>
    ClearType = D2D1_TEXT_ANTIALIAS_MODE_CLEARTYPE,

    /// <summary>
    /// Render text using gray-scale antialiasing.
    /// </summary>
    Grayscale = D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE,

    /// <summary>
    /// Do not use antialiasing.
    /// </summary>
    Aliased = D2D1_TEXT_ANTIALIAS_MODE_ALIASED,
};

/// <summary>
/// Specifies how the intersecting areas of geometries are combined to form the area of the composite geometry. 
/// <para>(Also see DirectX SDK: D2D1_FILL_MODE)</para>
///</summary>
public enum class FillMode : Int32
{
    /// <summary>
    /// Determines whether a point is in the fill region by drawing a ray from that point to infinity 
    /// in any direction, and then counting the number of path segments within the given shape 
    /// that the ray crosses. If this number is odd, the point is in the fill region; 
    /// if even, the point is outside the fill region.
    /// </summary>
    Alternate = D2D1_FILL_MODE_ALTERNATE,


    /// <summary>
    /// Determines whether a point is in the fill region of the path by drawing a ray from that point 
    /// to infinity in any direction, and then examining the places where a segment of the shape 
    /// crosses the ray. 
    /// Starting with a count of zero, add one each time a segment crosses the ray from left to 
    /// right and subtract one each time a path segment crosses the ray from right to left, as long as 
    /// left and right are seen from the perspective of the ray. 
    /// After counting the crossings, if the result is zero, then the point is outside the path. 
    /// Otherwise, it is inside the path.
    ///</summary>    
    Winding = D2D1_FILL_MODE_WINDING,
};

/// <summary>
/// Describes whether a window is occluded.
/// <para>(Also see DirectX SDK: D2D1_WINDOW_STATE)</para>
///</summary>
public enum class WindowState : Int32
{
    /// <summary>
    /// The window is not occluded
    /// </summary>
    None = D2D1_WINDOW_STATE_NONE,

    /// <summary>
    /// The window is occluded.
    /// </summary>
    Occluded = D2D1_WINDOW_STATE_OCCLUDED,
};

/// <summary>
/// Specifies how a device context is initialized for GDI rendering when it is retrieved from the render target.
/// <para>(Also see DirectX SDK: D2D1_DC_INITIALIZE_MODE)</para>
///</summary>
public enum class DCInitializeMode : Int32
{

    /// <summary>
    /// The current contents of the render target are copied to the device context when it is initialized.
    /// </summary>
    Copy = D2D1_DC_INITIALIZE_MODE_COPY,

    /// <summary>
    /// The device context is cleared to transparent black when it is initialized.
    /// </summary>
    Clear = D2D1_DC_INITIALIZE_MODE_CLEAR,
};

} } } }

