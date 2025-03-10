﻿namespace Slithin.Core.ItemContext;

public interface IContextCommand
{
    public object ParentViewModel { get; set; }
    public string Titel { get; }

    bool CanExecute(object data);

    void Execute(object data);
}
