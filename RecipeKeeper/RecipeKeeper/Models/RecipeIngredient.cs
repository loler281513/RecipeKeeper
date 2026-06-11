using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace RecipeKeeper.Models
{
    public class RecipeIngredient : INotifyPropertyChanged
    {
        private int _id;
        private int _recipeId;
        private Product _product = null!;
        private double? _quantity;
        private string? _unit = string.Empty;

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

        public Product Product
        {
            get => _product;
            set { if (_product != value) { _product = value; OnPropertyChanged(); } }
        }

        public double? Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } }
        }

        public string? Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(); } }
        }

        public string DisplayText => Quantity.HasValue
            ? $"{Product?.Name} — {Quantity} {Unit}"
            : $"{Product?.Name}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(Product) || propertyName == nameof(Quantity) || propertyName == nameof(Unit))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }
}
