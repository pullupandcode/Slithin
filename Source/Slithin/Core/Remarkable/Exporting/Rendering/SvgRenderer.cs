﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Slithin.Core.Remarkable.Models;
using Slithin.Core.Services;
using Svg;
using Svg.Pathing;

namespace Slithin.Core.Remarkable.Exporting.Rendering;

public static class SvgRenderer
{
    public static Stream RenderPage(Page page, int pageIndex, Metadata md, int width = 1404, int height = 1872)
    {
        var svgDoc = new SvgDocument { Width = width, Height = height, ViewBox = new SvgViewBox(0, 0, width, height) };

        var group = new SvgGroup();
        svgDoc.Children.Add(group);
        group.Fill = new SvgColourServer(Color.Transparent);

        var template = GetBase64Template(pageIndex, md);

        if (template != null)
        {
            group.Children.Add(new SvgImage { Href = "data:image/png;base64," + template, X = 0, Y = 0 });
        }

        RenderLayer(page, group);

        var stream = new MemoryStream();
        svgDoc.Write(stream);

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    private static SvgUnit CalculateStrokeWidth(Line line)
    {
        return line.BrushType switch
        {
            Brushes.Highlighter or Brushes.Rubber => new SvgUnit(20 * BaseSizes.GetValue(line.BrushBaseSize)),
            _ => new SvgUnit(BaseSizes.GetValue(line.BrushBaseSize))
        };
    }

    private static SvgColourServer ConvertColor(Line line)
    {
        return line.Color switch
        {
            Colors.Gray => new SvgColourServer(Color.Gray),
            Colors.White => new SvgColourServer(Color.White),
            Colors.Blue => new SvgColourServer(Color.Blue),
            Colors.Green => new SvgColourServer(Color.Green),
            Colors.Pink => new SvgColourServer(Color.Pink),
            Colors.Red => new SvgColourServer(Color.Red),
            Colors.Yellow => new SvgColourServer(Color.Yellow),
            _ => new SvgColourServer(Color.Black)
        };
    }

    private static SvgPathSegmentList GeneratePathData(IReadOnlyList<Point> points)
    {
        var psl = new SvgPathSegmentList { new SvgMoveToSegment(new PointF(points[0].X, points[0].Y)) };

        for (var i = 0; i + 1 < points.Count; i++)
        {
            psl.Add(new SvgLineSegment(
                new PointF(points[i].X, points[i].Y),
                new PointF(points[i + 1].X, points[i + 1].Y))
            );

            i++;
        }

        return psl;
    }

    private static string GetBase64Template(int i, Metadata md)
    {
        if (md.PageData.Data == null)
        {
            return null;
        }

        var filename = i < md.PageData.Data.Length ? md.PageData.Data[i] : "Blank";
        var pathManager = ServiceLocator.Container.Resolve<IPathManager>();
        var buffer = File.ReadAllBytes(Path.Combine(pathManager.TemplatesDir, filename + ".png"));

        return Convert.ToBase64String(buffer);
    }

    private static void RenderLayer(Page page, SvgGroup group)
    {
        foreach (var layer in page.Layers)
        {
            foreach (var line in layer.Lines)
            {
                if (line is not { BrushType: Brushes.Eraseall } && line.BrushType != Brushes.Rubber)
                {
                    RenderLine(line, group);
                }
            }
        }
    }

    private static void RenderLine(Line line, SvgGroup group)
    {
        var path = new SvgPath();

        path.PathData = GeneratePathData(line.Points);

        path.Stroke = ConvertColor(line);

        path.StrokeWidth = CalculateStrokeWidth(line);

        if (line.BrushType == Brushes.Highlighter)
        {
            path.Opacity = 0.25f;
        }

        path.Fill = new SvgColourServer(Color.Transparent);
        path.StrokeLineJoin = SvgStrokeLineJoin.Bevel;
        path.StrokeLineCap = SvgStrokeLineCap.Butt;

        group.Children.Add(path);
    }
}
