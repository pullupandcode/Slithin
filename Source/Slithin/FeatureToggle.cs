﻿namespace Slithin;

public class FeatureToggle
{
    public void Init()
    {
        Core.FeatureToggle.Features.Collect();
#if DEBUG

        Core.FeatureToggle.Features.EnableAll();
#else

        /*
            Feature<TemplateEditorFeature>.Enable();
            Feature<SharableFeature>.Enable();
            Feature<ExportFeature>.Enable();
            Feature<ExportPdfFeature>.Enable();
            Feature<ExportSvgFeature>.Enable();
            Feature<ExportPngFeature>.Enable();
        */
#endif
    }
}
