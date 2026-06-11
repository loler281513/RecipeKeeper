using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RecipeKeeper.Models
{
    public class Product : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;

        // Оставляем для совместимости с текущим UI фильтра
        private bool _includeFilter;
        private bool _excludeFilter;

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        public bool IncludeFilter
        {
            get => _includeFilter;
            set { if (_includeFilter != value) { _includeFilter = value; OnPropertyChanged(); } }
        }

        public bool ExcludeFilter
        {
            get => _excludeFilter;
            set { if (_excludeFilter != value) { _excludeFilter = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
