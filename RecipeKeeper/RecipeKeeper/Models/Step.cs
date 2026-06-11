using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RecipeKeeper.Models
{
    public class Step : INotifyPropertyChanged
    {
        private int _id;
        private int _recipeId;
        private int _stepNumber;
        private string _instructionText = string.Empty;
        private ObservableCollection<Photo> _photos = new();

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public int RecipeId
        {
            get => _recipeId;
            set { if (_recipeId != value) { _recipeId = value; OnPropertyChanged(); } }
        }

        public int StepNumber
        {
            get => _stepNumber;
            set { if (_stepNumber != value) { _stepNumber = value; OnPropertyChanged(); } }
        }

        public string InstructionText
        {
            get => _instructionText;
            set { if (_instructionText != value) { _instructionText = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<Photo> Photos
        {
            get => _photos;
            set
            {
                if (_photos != value)
                {
                    foreach (var photo in _photos) { photo.Dispose(); }
                    _photos = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}