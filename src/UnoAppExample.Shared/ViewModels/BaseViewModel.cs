using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoAppExample.ViewModels
{
    public abstract partial class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Raise the PropertyChanged event, passing the name of the property whose value has changed.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}
