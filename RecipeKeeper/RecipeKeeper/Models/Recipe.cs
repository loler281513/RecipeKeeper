using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RecipeKeeper.Models
{
    public class Recipe : INotifyPropertyChanged
    {
        private int _id;
        private string _title = string.Empty; // Синхронизировано с [title] в БД
        private string? _description;
        private int _cookingTimeMinutes;
        private int _servings = 1;
        private string? _mainPhotoPath;
        private int _categoryId;

        private ObservableCollection<RecipeIngredient> _ingredients = new();
        private ObservableCollection<Step> _steps = new();

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        public string? Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(); } }
        }

        public int CookingTimeMinutes
        {
            get => _cookingTimeMinutes;
            set { if (_cookingTimeMinutes != value) { _cookingTimeMinutes = value; OnPropertyChanged(); } }
        }

        public int Servings
        {
            get => _servings;
            set { if (_servings != value) { _servings = value; OnPropertyChanged(); } }
        }

        public string? MainPhotoPath
        {
            get => _mainPhotoPath;
            set { if (_mainPhotoPath != value) { _mainPhotoPath = value; OnPropertyChanged(); } }
        }

        public int CategoryId
        {
            get => _categoryId;
            set { if (_categoryId != value) { _categoryId = value; OnPropertyChanged(); } }
        }

        // Теперь храним объекты RecipeIngredient вместо строк
        public ObservableCollection<RecipeIngredient> Ingredients
        {
            get => _ingredients;
            set { if (_ingredients != value) { _ingredients = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<Step> Steps
        {
            get => _steps;
            set { if (_steps != value) { _steps = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}