﻿using System.Drawing.Imaging;
using System.IO;
using Svg;

namespace Slithin.Core.Remarkable.Rendering.Exporters
{
    public class PngExporter : IExportProvider
    {
        public bool ExportSingleDocument => false;
        public string Title => "PNG Graphics";

        public bool Export(ExportOptions options, Metadata metadata, string outputPath)
        {
            if (options.Document.IsT1)
            {
                var notebook = options.Document.AsT1;

                for (var i = 0; i < notebook.Pages.Count; i++)
                {
                    var page = notebook.Pages[i];

                    var svgStrm = SvgRenderer.RenderPage(page, i, metadata);

                    var doc = SvgDocument.Open<SvgDocument>(svgStrm);
                    var bitmap = doc.Draw();
                    bitmap.Save(Path.Combine(outputPath, i + ".png"), ImageFormat.Png);

                    svgStrm.Close();
                }

                return true;
            }

            return false;
        }
    }
}
