using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlantSim.Infrastructure;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(storage, value))
        {
            storage = value;
            RaisePropertyChanged(propertyName);
        }
    }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}