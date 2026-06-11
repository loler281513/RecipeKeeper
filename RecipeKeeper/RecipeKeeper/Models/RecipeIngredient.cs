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

        // Ссылка на сам продукт (из него берем Name)
        public Product Product
        {
            get => _product;
            set { if (_product != value) { _product = value; OnPropertyChanged(); } }
        }

        // FLOAT в БД соответствует double в C#
        public double? Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } }
        }

        // Например: "г", "шт.", "мл"
        public string? Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value; OnPropertyChanged(); } }
        }

        // Удобное свойство для вывода в интерфейс (строка вида "Помидоры - 2 шт.")
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
