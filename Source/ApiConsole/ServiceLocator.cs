﻿using Slithin.API.Lib;

namespace ApiConsole;

public static class ServiceLocator
{
    public static readonly MarketplaceAPI API = new();
}
