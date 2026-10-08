using System;
using Microsoft.Win32;

namespace PlantSim.Services;

public interface IFileSelectionService
{
    string? SelectOpenFilePath(string filter, string title);
}

public class FileSelectionService : IFileSelectionService
{
    public string? SelectOpenFilePath(string filter, string title)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title
        };

        bool? dialogResult = dialog.ShowDialog();
        return dialogResult == true ? dialog.FileName : null;
    }
}