﻿using System;
using System.IO;
using Slithin.Core.ImportExport;
using Slithin.Core.Remarkable.Exporting.Rendering;
using Slithin.Core.Remarkable.Models;

namespace Slithin.Core.Remarkable.Exporting.Exporters;

public class SvgExporter : IExportProvider
{
    public bool ExportSingleDocument => false;

    public string Title => "SVG Graphics";

    public bool CanHandle(Metadata md)
    {
        return md.Content.FileType == "notebook";
    }

    public bool Export(ExportOptions options, Metadata metadata, string outputPath, IProgress<int> progress)
    {
        if (!options.Document.IsT1)
        {
            return false;
        }

        var notebook = options.Document.AsT1;

        //ToDo: May parallize
        for (var i = 0; i < options.PagesIndices.Count; i++)
        {
            var percent = (int)((float)i / (float)options.PagesIndices.Count * 100);

            var page = notebook.Pages[options.PagesIndices[i]];

            var svgStrm = SvgRenderer.RenderPage(page, i, metadata);
            var outputStrm = File.Create(Path.Combine(outputPath, i + ".svg"));

            svgStrm.CopyTo(outputStrm);

            svgStrm.Close();
            outputStrm.Close();

            progress.Report(percent);
        }

        return true;
    }

    public override string ToString() => Title;
}
