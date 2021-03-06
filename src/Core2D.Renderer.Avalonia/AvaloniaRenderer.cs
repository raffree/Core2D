﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Core2D.Containers;
using Core2D.Data;
using Core2D.Interfaces;
using Core2D.Shapes;
using Core2D.Style;
using Spatial;
using Spatial.Arc;
using A = Avalonia;
using AM = Avalonia.Media;
using AMI = Avalonia.Media.Imaging;
using AME = Avalonia.MatrixExtensions;

namespace Core2D.Renderer.Avalonia
{
    /// <summary>
    /// Native Avalonia shape renderer.
    /// </summary>
    public class AvaloniaRenderer : ObservableObject, IShapeRenderer
    {
        private readonly IServiceProvider _serviceProvider;
        private IShapeRendererState _state;
        private ICache<IShapeStyle, (AM.IBrush, AM.Pen)> _styleCache;
        private ICache<IArrowStyle, (AM.IBrush, AM.Pen)> _arrowStyleCache;
        // TODO: Add LineShape cache.
        // TODO: Add EllipseShape cache.
        // TODO: Add ArcShape cache.
        // TODO: Add CubicBezierShape cache.
        // TODO: Add QuadraticBezierShape cache.
        private ICache<ITextShape, (string, AM.FormattedText, IShapeStyle)> _textCache;
        private ICache<string, AMI.Bitmap> _biCache;
        // TODO: Add PathShape cache.
        private readonly Func<double, float> _scaleToPage;
        private readonly double _textScaleFactor;
        private readonly PathGeometryConverter _converter;

        /// <inheritdoc/>
        public IShapeRendererState State
        {
            get => _state;
            set => Update(ref _state, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaRenderer"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="textScaleFactor">The text scale factor.</param>
        public AvaloniaRenderer(IServiceProvider serviceProvider, double textScaleFactor = 1.0)
        {
            _serviceProvider = serviceProvider;
            _state = _serviceProvider.GetService<IFactory>().CreateShapeRendererState();
            _styleCache = _serviceProvider.GetService<IFactory>().CreateCache<IShapeStyle, (AM.IBrush, AM.Pen)>();
            _arrowStyleCache = _serviceProvider.GetService<IFactory>().CreateCache<IArrowStyle, (AM.IBrush, AM.Pen)>();
            _textCache = _serviceProvider.GetService<IFactory>().CreateCache<ITextShape, (string, AM.FormattedText, IShapeStyle)>();
            _biCache = _serviceProvider.GetService<IFactory>().CreateCache<string, AMI.Bitmap>(bi => bi.Dispose());
            _textScaleFactor = textScaleFactor;
            _scaleToPage = (value) => (float)(value);
            _converter = new PathGeometryConverter(_serviceProvider);
            ClearCache(isZooming: false);
        }

        /// <inheritdoc/>
        public override object Copy(IDictionary<object, object> shared)
        {
            throw new NotImplementedException();
        }

        private A.Point GetTextOrigin(IShapeStyle style, ref Rect2 rect, ref A.Size size)
        {
            double ox, oy;

            switch (style.TextStyle.TextHAlignment)
            {
                case TextHAlignment.Left:
                    ox = rect.X;
                    break;
                case TextHAlignment.Right:
                    ox = rect.Right - size.Width;
                    break;
                case TextHAlignment.Center:
                default:
                    ox = (rect.Left + rect.Width / 2.0) - (size.Width / 2.0);
                    break;
            }

            switch (style.TextStyle.TextVAlignment)
            {
                case TextVAlignment.Top:
                    oy = rect.Y;
                    break;
                case TextVAlignment.Bottom:
                    oy = rect.Bottom - size.Height;
                    break;
                case TextVAlignment.Center:
                default:
                    oy = (rect.Bottom - rect.Height / 2f) - (size.Height / 2f);
                    break;
            }

            return new A.Point(ox, oy);
        }

        private static AM.Color ToColor(IColor color)
        {
            switch (color)
            {
                case IArgbColor argbColor:
                    return AM.Color.FromArgb(argbColor.A, argbColor.R, argbColor.G, argbColor.B);
                default:
                    throw new NotSupportedException($"The {color.GetType()} color type is not supported.");
            }
        }

        private AM.IBrush ToBrush(IColor color)
        {
            switch (color)
            {
                case IArgbColor argbColor:
                    return new AM.SolidColorBrush(ToColor(argbColor));
                default:
                    throw new NotSupportedException($"The {color.GetType()} color type is not supported.");
            }
        }

        private AM.Pen ToPen(IBaseStyle style, Func<double, float> scale)
        {
            var lineCap = default(AM.PenLineCap);
            var dashStyle = default(AM.DashStyle);

            switch (style.LineCap)
            {
                case LineCap.Flat:
                    lineCap = AM.PenLineCap.Flat;
                    break;
                case LineCap.Square:
                    lineCap = AM.PenLineCap.Square;
                    break;
                case LineCap.Round:
                    lineCap = AM.PenLineCap.Round;
                    break;
            }

            if (style.Dashes != null)
            {
                dashStyle = new AM.DashStyle(
                    StyleHelper.ConvertDashesToDoubleArray(style.Dashes),
                    style.DashOffset);
            }

            var pen = new AM.Pen(
                ToBrush(style.Stroke),
                scale(style.Thickness / State.ZoomX),
                dashStyle, lineCap,
                lineCap, lineCap);

            return pen;
        }

        private static Rect2 CreateRect(IPointShape tl, IPointShape br, double dx, double dy)
        {
            return Rect2.FromPoints(tl.X, tl.Y, br.X, br.Y, dx, dy);
        }

        private static void DrawLineInternal(AM.DrawingContext dc, AM.Pen pen, bool isStroked, ref A.Point p0, ref A.Point p1)
        {
            if (isStroked)
            {
                dc.DrawLine(pen, p0, p1);
            }
        }

        private static void DrawLineCurveInternal(AM.DrawingContext _dc, AM.Pen pen, bool isStroked, ref A.Point pt1, ref A.Point pt2, double curvature, CurveOrientation orientation, PointAlignment pt1a, PointAlignment pt2a)
        {
            if (isStroked)
            {
                var sg = new AM.StreamGeometry();
                using (var sgc = sg.Open())
                {
                    sgc.BeginFigure(new A.Point(pt1.X, pt1.Y), false);
                    double p1x = pt1.X;
                    double p1y = pt1.Y;
                    double p2x = pt2.X;
                    double p2y = pt2.Y;
                    LineShapeExtensions.GetCurvedLineBezierControlPoints(orientation, curvature, pt1a, pt2a, ref p1x, ref p1y, ref p2x, ref p2y);
                    sgc.CubicBezierTo(
                        new A.Point(p1x, p1y),
                        new A.Point(p2x, p2y),
                        new A.Point(pt2.X, pt2.Y));
                    sgc.EndFigure(false);
                }
                _dc.DrawGeometry(null, pen, sg);
            }
        }

        private void DrawLineArrowsInternal(AM.DrawingContext dc, ILineShape line, IShapeStyle style, double dx, double dy, out A.Point pt1, out A.Point pt2)
        {
            // Start arrow style.
            GetCached(style.StartArrowStyle, out var fillStartArrow, out var strokeStartArrow);

            // End arrow style.
            GetCached(style.EndArrowStyle, out var fillEndArrow, out var strokeEndArrow);

            // Line max length.
            double _x1 = line.Start.X + dx;
            double _y1 = line.Start.Y + dy;
            double _x2 = line.End.X + dx;
            double _y2 = line.End.Y + dy;

            line.GetMaxLength(ref _x1, ref _y1, ref _x2, ref _y2);

            float x1 = _scaleToPage(_x1);
            float y1 = _scaleToPage(_y1);
            float x2 = _scaleToPage(_x2);
            float y2 = _scaleToPage(_y2);

            // Arrow transforms.
            var sas = style.StartArrowStyle;
            var eas = style.EndArrowStyle;
            double a1 = Math.Atan2(y1 - y2, x1 - x2);
            double a2 = Math.Atan2(y2 - y1, x2 - x1);

            // Draw start arrow.
            pt1 = DrawLineArrowInternal(dc, strokeStartArrow, fillStartArrow, x1, y1, a1, sas);

            // Draw end arrow.
            pt2 = DrawLineArrowInternal(dc, strokeEndArrow, fillEndArrow, x2, y2, a2, eas);
        }

        private static A.Point DrawLineArrowInternal(AM.DrawingContext dc, AM.Pen pen, AM.IBrush brush, float x, float y, double angle, IArrowStyle style)
        {
            A.Point pt = default;
            var rt = AME.MatrixHelper.Rotation(angle, new A.Vector(x, y));
            double rx = style.RadiusX;
            double ry = style.RadiusY;
            double sx = 2.0 * rx;
            double sy = 2.0 * ry;

            switch (style.ArrowType)
            {
                default:
                case ArrowType.None:
                    {
                        pt = new A.Point(x, y);
                    }
                    break;
                case ArrowType.Rectangle:
                    {
                        pt = AME.MatrixHelper.TransformPoint(rt, new A.Point(x - (float)sx, y));
                        var rect = new Rect2(x - sx, y - ry, sx, sy);
                        using (var d = dc.PushPreTransform(rt))
                        {
                            DrawRectangleInternal(dc, brush, pen, style.IsStroked, style.IsFilled, ref rect);
                        }
                    }
                    break;
                case ArrowType.Ellipse:
                    {
                        pt = AME.MatrixHelper.TransformPoint(rt, new A.Point(x - (float)sx, y));
                        using (var d = dc.PushPreTransform(rt))
                        {
                            var rect = new Rect2(x - sx, y - ry, sx, sy);
                            DrawEllipseInternal(dc, brush, pen, style.IsStroked, style.IsFilled, ref rect);
                        }
                    }
                    break;
                case ArrowType.Arrow:
                    {
                        var pts = new A.Point[]
                        {
                            new A.Point(x, y),
                            new A.Point(x - (float)sx, y + (float)sy),
                            new A.Point(x, y),
                            new A.Point(x - (float)sx, y - (float)sy),
                            new A.Point(x, y)
                        };
                        pt = AME.MatrixHelper.TransformPoint(rt, pts[0]);
                        var p11 = AME.MatrixHelper.TransformPoint(rt, pts[1]);
                        var p21 = AME.MatrixHelper.TransformPoint(rt, pts[2]);
                        var p12 = AME.MatrixHelper.TransformPoint(rt, pts[3]);
                        var p22 = AME.MatrixHelper.TransformPoint(rt, pts[4]);
                        DrawLineInternal(dc, pen, style.IsStroked, ref p11, ref p21);
                        DrawLineInternal(dc, pen, style.IsStroked, ref p12, ref p22);
                    }
                    break;
            }

            return pt;
        }

        private static void DrawRectangleInternal(AM.DrawingContext dc, AM.IBrush brush, AM.Pen pen, bool isStroked, bool isFilled, ref Rect2 rect)
        {
            if (!isStroked && !isFilled)
                return;

            var r = new A.Rect(rect.X, rect.Y, rect.Width, rect.Height);

            if (isFilled)
            {
                dc.FillRectangle(brush, r);
            }

            if (isStroked)
            {
                dc.DrawRectangle(pen, r);
            }
        }

        private static void DrawEllipseInternal(AM.DrawingContext dc, AM.IBrush brush, AM.Pen pen, bool isStroked, bool isFilled, ref Rect2 rect)
        {
            if (!isFilled && !isStroked)
                return;

            var r = new A.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            var g = new AM.EllipseGeometry(r);

            dc.DrawGeometry(
                isFilled ? brush : null,
                isStroked ? pen : null,
                g);
        }

        private void DrawGridInternal(AM.DrawingContext dc, AM.Pen stroke, ref Rect2 rect, double offsetX, double offsetY, double cellWidth, double cellHeight, bool isStroked)
        {
            double ox = rect.X;
            double oy = rect.Y;
            double sx = ox + offsetX;
            double sy = oy + offsetY;
            double ex = ox + rect.Width;
            double ey = oy + rect.Height;

            for (double x = sx; x < ex; x += cellWidth)
            {
                var p0 = new A.Point(_scaleToPage(x), _scaleToPage(oy));
                var p1 = new A.Point(_scaleToPage(x), _scaleToPage(ey));
                DrawLineInternal(dc, stroke, isStroked, ref p0, ref p1);
            }

            for (double y = sy; y < ey; y += cellHeight)
            {
                var p0 = new A.Point(_scaleToPage(ox), _scaleToPage(y));
                var p1 = new A.Point(_scaleToPage(ex), _scaleToPage(y));
                DrawLineInternal(dc, stroke, isStroked, ref p0, ref p1);
            }
        }

        private static AM.StreamGeometry ToStreamGeometry(IArcShape arc, double dx, double dy)
        {
            var sg = new AM.StreamGeometry();

            using (var sgc = sg.Open())
            {
                var a = new WpfArc(
                    Point2.FromXY(arc.Point1.X, arc.Point1.Y),
                    Point2.FromXY(arc.Point2.X, arc.Point2.Y),
                    Point2.FromXY(arc.Point3.X, arc.Point3.Y),
                    Point2.FromXY(arc.Point4.X, arc.Point4.Y));

                sgc.BeginFigure(
                    new A.Point(a.Start.X + dx, a.Start.Y + dy),
                    arc.IsFilled);

                sgc.ArcTo(
                    new A.Point(a.End.X + dx, a.End.Y + dy),
                    new A.Size(a.Radius.Width, a.Radius.Height),
                    0.0,
                    a.IsLargeArc,
                    AM.SweepDirection.Clockwise);

                sgc.EndFigure(false);
            }

            return sg;
        }

        private static AM.StreamGeometry ToStreamGeometry(ICubicBezierShape cubicBezier, double dx, double dy)
        {
            var sg = new AM.StreamGeometry();

            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(
                    new A.Point(cubicBezier.Point1.X + dx, cubicBezier.Point1.Y + dy),
                    cubicBezier.IsFilled);

                sgc.CubicBezierTo(
                    new A.Point(cubicBezier.Point2.X + dx, cubicBezier.Point2.Y + dy),
                    new A.Point(cubicBezier.Point3.X + dx, cubicBezier.Point3.Y + dy),
                    new A.Point(cubicBezier.Point4.X + dx, cubicBezier.Point4.Y + dy));

                sgc.EndFigure(false);
            }

            return sg;
        }

        private static AM.StreamGeometry ToStreamGeometry(IQuadraticBezierShape quadraticBezier, double dx, double dy)
        {
            var sg = new AM.StreamGeometry();

            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(
                    new A.Point(quadraticBezier.Point1.X + dx, quadraticBezier.Point1.Y + dy),
                    quadraticBezier.IsFilled);

                sgc.QuadraticBezierTo(
                    new A.Point(quadraticBezier.Point2.X + dx, quadraticBezier.Point2.Y + dy),
                    new A.Point(quadraticBezier.Point3.X + dx, quadraticBezier.Point3.Y + dy));

                sgc.EndFigure(false);
            }

            return sg;
        }

        private A.Matrix ToMatrix(IMatrixObject m)
        {
            return new A.Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY);
        }

        private void GetCached(IArrowStyle style, out AM.IBrush fill, out AM.Pen stroke)
        {
            (fill, stroke) = _arrowStyleCache.Get(style);
            if (fill == null || stroke == null)
            {
                fill = ToBrush(style.Fill);
                stroke = ToPen(style, _scaleToPage);
                _arrowStyleCache.Set(style, (fill, stroke));
            }
        }

        private void GetCached(IShapeStyle style, out AM.IBrush fill, out AM.Pen stroke)
        {
            (fill, stroke) = _styleCache.Get(style);
            if (fill == null || stroke == null)
            {
                fill = ToBrush(style.Fill);
                stroke = ToPen(style, _scaleToPage);
                _styleCache.Set(style, (fill, stroke));
            }
        }

        /// <inheritdoc/>
        public void InvalidateCache(IShapeStyle style)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void InvalidateCache(IMatrixObject matrix)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void InvalidateCache(IBaseShape shape, IShapeStyle style, double dx, double dy)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void ClearCache(bool isZooming)
        {
            _styleCache.Reset();
            _arrowStyleCache.Reset();

            if (!isZooming)
            {
                _textCache.Reset();
                _biCache.Reset();
            }
        }

        /// <inheritdoc/>
        public void Fill(object dc, double x, double y, double width, double height, IColor color)
        {
            var _dc = dc as AM.DrawingContext;
            var brush = ToBrush(color);
            var rect = new A.Rect(x, y, width, height);
            _dc.FillRectangle(brush, rect);
        }

        /// <inheritdoc/>
        public object PushMatrix(object dc, IMatrixObject matrix)
        {
            var _dc = dc as AM.DrawingContext;
            return _dc.PushPreTransform(ToMatrix(matrix));
        }

        /// <inheritdoc/>
        public void PopMatrix(object dc, object state)
        {
            var _state = (AM.DrawingContext.PushedState)state;
            _state.Dispose();
        }

        /// <inheritdoc/>
        public void Draw(object dc, IPageContainer container, double dx, double dy)
        {
            foreach (var layer in container.Layers)
            {
                if (layer.IsVisible)
                {
                    Draw(dc, layer, dx, dy);
                }
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, ILayerContainer layer, double dx, double dy)
        {
            foreach (var shape in layer.Shapes)
            {
                if (shape.State.Flags.HasFlag(State.DrawShapeState.Flags))
                {
                    shape.Draw(dc, this, dx, dy);
                }
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, ILineShape line, double dx, double dy)
        {
            var _dc = dc as AM.DrawingContext;

            var style = line.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            DrawLineArrowsInternal(_dc, line, style, dx, dy, out var pt1, out var pt2);

            if (style.LineStyle.IsCurved)
            {
                DrawLineCurveInternal(
                    _dc,
                    stroke, line.IsStroked,
                    ref pt1, ref pt2,
                    style.LineStyle.Curvature,
                    style.LineStyle.CurveOrientation,
                    line.Start.Alignment,
                    line.End.Alignment);
            }
            else
            {
                DrawLineInternal(_dc, stroke, line.IsStroked, ref pt1, ref pt2);
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, IRectangleShape rectangle, double dx, double dy)
        {
            var _dc = dc as AM.DrawingContext;

            var style = rectangle.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var rect = CreateRect(rectangle.TopLeft, rectangle.BottomRight, dx, dy);

            DrawRectangleInternal(
                _dc,
                fill,
                stroke,
                rectangle.IsStroked,
                rectangle.IsFilled,
                ref rect);

            if (rectangle.IsGrid)
            {
                DrawGridInternal(
                    _dc,
                    stroke,
                    ref rect,
                    rectangle.OffsetX, rectangle.OffsetY,
                    rectangle.CellWidth, rectangle.CellHeight,
                    true);
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, IEllipseShape ellipse, double dx, double dy)
        {
            var _dc = dc as AM.DrawingContext;

            var style = ellipse.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var rect = CreateRect(ellipse.TopLeft, ellipse.BottomRight, dx, dy);

            DrawEllipseInternal(
                _dc,
                fill,
                stroke,
                ellipse.IsStroked,
                ellipse.IsFilled,
                ref rect);
        }

        /// <inheritdoc/>
        public void Draw(object dc, IArcShape arc, double dx, double dy)
        {
            if (!arc.IsFilled && !arc.IsStroked)
                return;

            var _dc = dc as AM.DrawingContext;

            var style = arc.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var sg = ToStreamGeometry(arc, dx, dy);

            _dc.DrawGeometry(
                arc.IsFilled ? fill : null,
                arc.IsStroked ? stroke : null,
                sg);
        }

        /// <inheritdoc/>
        public void Draw(object dc, ICubicBezierShape cubicBezier, double dx, double dy)
        {
            if (!cubicBezier.IsFilled && !cubicBezier.IsStroked)
                return;

            var _dc = dc as AM.DrawingContext;

            var style = cubicBezier.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var sg = ToStreamGeometry(cubicBezier, dx, dy);

            _dc.DrawGeometry(
                cubicBezier.IsFilled ? fill : null,
                cubicBezier.IsStroked ? stroke : null,
                sg);
        }

        /// <inheritdoc/>
        public void Draw(object dc, IQuadraticBezierShape quadraticBezier, double dx, double dy)
        {
            if (!quadraticBezier.IsFilled && !quadraticBezier.IsStroked)
                return;

            var _dc = dc as AM.DrawingContext;

            var style = quadraticBezier.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var sg = ToStreamGeometry(quadraticBezier, dx, dy);

            _dc.DrawGeometry(
                quadraticBezier.IsFilled ? fill : null,
                quadraticBezier.IsStroked ? stroke : null,
                sg);
        }

        /// <inheritdoc/>
        public void Draw(object dc, ITextShape text, double dx, double dy)
        {
            var _dc = dc as AM.DrawingContext;

            var style = text.Style;
            if (style == null)
                return;

            if (!(text.GetProperty(nameof(ITextShape.Text)) is string tbind))
            {
                tbind = text.Text;
            }

            if (tbind == null)
            {
                return;
            }

            GetCached(style, out var fill, out var stroke);

            var rect = CreateRect(text.TopLeft, text.BottomRight, dx, dy);

            (string ct, var ft, var cs) = _textCache.Get(text);
            if (string.Compare(ct, tbind) == 0 && cs == style)
            {
                var size = ft.Bounds.Size;
                var origin = GetTextOrigin(style, ref rect, ref size);
                _dc.DrawText(stroke.Brush, origin, ft);
            }
            else
            {
                var fontStyle = AM.FontStyle.Normal;
                var fontWeight = AM.FontWeight.Normal;
                //var fontDecoration = AM.FontDecoration.None;

                if (style.TextStyle.FontStyle != null)
                {
                    if (style.TextStyle.FontStyle.Flags.HasFlag(FontStyleFlags.Italic))
                    {
                        fontStyle |= AM.FontStyle.Italic;
                    }

                    if (style.TextStyle.FontStyle.Flags.HasFlag(FontStyleFlags.Bold))
                    {
                        fontWeight |= AM.FontWeight.Bold;
                    }

                    // TODO: Implement font decoration after Avalonia adds support.
                    /*
                    if (style.TextStyle.FontStyle.Flags.HasFlag(FontStyleFlags.Underline))
                    {
                        fontDecoration |= AM.FontDecoration.Underline;
                    }

                    if (style.TextStyle.FontStyle.Flags.HasFlag(FontStyleFlags.Strikeout))
                    {
                        fontDecoration |= AM.FontDecoration.Strikethrough;
                    }
                    */
                }

                if (style.TextStyle.FontSize >= 0.0)
                {
                    var tf = new AM.Typeface(
                        style.TextStyle.FontName,
                        style.TextStyle.FontSize * _textScaleFactor,
                        fontStyle,
                        fontWeight);

                    ft = new AM.FormattedText()
                    {
                        Typeface = tf,
                        Text = tbind,
                        TextAlignment = AM.TextAlignment.Left,
                        Wrapping = AM.TextWrapping.NoWrap
                    };

                    var size = ft.Bounds.Size;
                    var origin = GetTextOrigin(style, ref rect, ref size);

                    _textCache.Set(text, (tbind, ft, style));

                    _dc.DrawText(stroke.Brush, origin, ft);
                }
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, IImageShape image, double dx, double dy)
        {
            if (image.Key == null)
                return;

            var _dc = dc as AM.DrawingContext;
            var style = image.Style;
            var rect = CreateRect(image.TopLeft, image.BottomRight, dx, dy);

            if ((image.IsStroked || image.IsFilled) && style != null)
            {
                GetCached(style, out var fill, out var stroke);

                DrawRectangleInternal(
                    _dc,
                    fill,
                    stroke,
                    image.IsStroked,
                    image.IsFilled,
                    ref rect);
            }

            var imageCached = _biCache.Get(image.Key);
            if (imageCached != null)
            {
                try
                {
                    _dc.DrawImage(
                        imageCached,
                        1.0,
                        new A.Rect(0, 0, imageCached.PixelSize.Width, imageCached.PixelSize.Height),
                        new A.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                }
                catch (Exception ex)
                {
                    _serviceProvider.GetService<ILog>()?.LogException(ex);
                }
            }
            else
            {
                if (State.ImageCache == null || string.IsNullOrEmpty(image.Key))
                    return;

                try
                {
                    var bytes = State.ImageCache.GetImage(image.Key);
                    if (bytes != null)
                    {
                        using (var ms = new System.IO.MemoryStream(bytes))
                        {
                            var bi = new AMI.Bitmap(ms);

                            _biCache.Set(image.Key, bi);

                            _dc.DrawImage(
                                bi,
                                1.0,
                                new A.Rect(0, 0, bi.PixelSize.Width, bi.PixelSize.Height),
                                new A.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _serviceProvider.GetService<ILog>()?.LogException(ex);
                }
            }
        }

        /// <inheritdoc/>
        public void Draw(object dc, IPathShape path, double dx, double dy)
        {
            if (path.Geometry == null)
                return;

            if (!path.IsFilled && !path.IsStroked)
                return;

            var _dc = dc as AM.DrawingContext;

            var style = path.Style;
            if (style == null)
                return;

            GetCached(style, out var fill, out var stroke);

            var g = _converter.ToGeometry(path.Geometry, dx, dy);

            _dc.DrawGeometry(
                path.IsFilled ? fill : null,
                path.IsStroked ? stroke : null,
                g);
        }
    }
}
