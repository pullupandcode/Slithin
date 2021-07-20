﻿using System.Linq;
using System.Windows.Input;
using Slithin.Core;
using Slithin.Core.Commands;
using Slithin.Core.Remarkable;

namespace Slithin.ViewModels
{
    public class NotebooksPageViewModel : BaseViewModel
    {
        private Metadata _selectedNotebook;

        public NotebooksPageViewModel()
        {
            //ImportCommand = DialogService.CreateOpenCommand<ImportNotebookModal>(new AddTemplateModalViewModel());
            RemoveTemplateCommand = new RemoveNotebookCommand(this);

            //ToDo: Remove after UI is working
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.DocumentType, VisibleName = "My Document d" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.DocumentType, VisibleName = "My Document re" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.CollectionType, VisibleName = "Folder 1" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.CollectionType, VisibleName = "Folder 2" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.DocumentType, VisibleName = "My Document fed" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.DocumentType, VisibleName = "My Document fd" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.DocumentType, VisibleName = "My Document fd" });
            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.CollectionType, VisibleName = "Folder 3" });

            SyncService.NotebooksFilter.Documents.Add(new Metadata() { Type = MetadataType.CollectionType, VisibleName = "Folder 4" });

            var ordered = SyncService.NotebooksFilter.Documents.OrderBy(_ => _.Type != MetadataType.CollectionType);

            SyncService.NotebooksFilter.Documents = new System.Collections.ObjectModel.ObservableCollection<Metadata>(ordered);
        }

        public ICommand ExportCommand { get; set; }
        public ICommand ImportCommand { get; set; }
        public ICommand RemoveTemplateCommand { get; set; }

        public Metadata SelectedNotebook
        {
            get { return _selectedNotebook; }
            set { SetValue(ref _selectedNotebook, value); }
        }
    }
}
