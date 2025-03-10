﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Slithin.Core.MVVM;
using Slithin.Core.Tools;

namespace Slithin.Core.Sync;

public class ToolsFilter : NotifyObject
{
    private List<ITool> _allTools;
    private string _selectedCategory;

    private ObservableCollection<ITool> _tools;

    public ToolsFilter()
    {
        Categories = new();

        Tools = new();
    }

    public List<ITool> AllTools
    {
        get => _allTools;
        set
        {
            _allTools = value;
            RefreshTools();
        }
    }

    public ObservableCollection<string> Categories { get; set; }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set { SetValue(ref _selectedCategory, value); RefreshTools(); }
    }

    public ObservableCollection<ITool> Tools
    {
        get => _tools;
        set { SetValue(ref _tools, value); }
    }

    private void RefreshTools()
    {
        Tools = new(AllTools.Where(_ => _.Info.Category == SelectedCategory));
    }
}