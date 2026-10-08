using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PlantSim.Services;

public interface IDialogueService
{
    void ShowInformation(string message, string title);
    void ShowError(string message, string title);
    bool ShowConfirmation(string message, string title);
}

public class DialogueService : IDialogueService
{
    public void ShowInformation(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title)
    {
        MessageBoxResult result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}