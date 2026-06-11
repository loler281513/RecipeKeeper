using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RecipeKeeper.Models;
using RecipeKeeper.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeKeeper.Views
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Recipe> _allRecipes;
        private ObservableCollection<Recipe> _filteredRecipes;
        private ObservableCollection<Product> _products;
        private bool _isFilterVisible = false;
        private Recipe? _selectedRecipe;
        private int _nextNewRecipeNumber = 1;
        private int _currentStepIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();

            // Загружаем продукты из БД
            var loadedProducts = db.GetProducts();
            _products = new ObservableCollection<Product>(loadedProducts);

            // Загружаем рецепты с учетом всех связей
            var loadedRecipes = db.GetAllRecipes(loadedProducts);
            _allRecipes = new ObservableCollection<Recipe>(loadedRecipes);
            _filteredRecipes = new ObservableCollection<Recipe>(_allRecipes);

            // Если рецептов нет - показываем сообщение
            if (!_allRecipes.Any())
            {
                // Находим TextBlock внутри DefaultMessage StackPanel
                var defaultTextBlock = DefaultMessage.Children.OfType<TextBlock>().FirstOrDefault();
                if (defaultTextBlock != null)
                {
                    defaultTextBlock.Text = "Нет рецептов. Нажмите \"+\" чтобы добавить первый рецепт!";
                }
                DefaultMessage.IsVisible = true;
            }

            // Привязка данных
            RecipesList.ItemsSource = _filteredRecipes;
            ProductsList.ItemsSource = _products;

            // Обработчики событий
            SearchBox.TextChanged += OnSearchTextChanged;
            FilterButton.Click += OnFilterButtonClick;
            AddRecipeButton.Click += OnAddRecipeClick;

            RecipesList.SelectionChanged += (s, e) => {
                var hasSelection = RecipesList.SelectedItem != null;
                ViewRecipeButton.IsEnabled = hasSelection;
                EditRecipeButton.IsEnabled = hasSelection;
                DeleteRecipeButton.IsEnabled = hasSelection;
            };

            ViewRecipeButton.Click += OnViewRecipeClick;
            EditRecipeButton.Click += OnEditRecipeClick;
            DeleteRecipeButton.Click += OnDeleteRecipeClick;
            SaveRecipeButton.Click += OnSaveRecipeClick;
            AddStepButton.Click += OnAddStepClick;

            // Скрываем панели по умолчанию
            EditPanel.IsVisible = false;
            FilterContainer.IsVisible = _isFilterVisible;
            ViewPanel.IsVisible = false;

            // Подписываемся на изменения фильтров продуктов
            foreach (var product in _products)
            {
                product.PropertyChanged += OnProductFilterChanged;
            }
        }

        private void OnProductFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Product.IncludeFilter) || e.PropertyName == nameof(Product.ExcludeFilter))
            {
                ApplyFilters();
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void OnFilterButtonClick(object? sender, RoutedEventArgs e)
        {
            _isFilterVisible = !_isFilterVisible;
            FilterContainer.IsVisible = _isFilterVisible;
        }

        private void ApplyFilters()
        {
            string searchText = SearchBox.Text?.Trim().ToLower() ?? "";

            var includeProducts = _products.Where(p => p.IncludeFilter).Select(p => p.Name.ToLower()).ToList();
            var excludeProducts = _products.Where(p => p.ExcludeFilter).Select(p => p.Name.ToLower()).ToList();

            var filtered = _allRecipes.Where(recipe =>
            {
                if (!string.IsNullOrEmpty(searchText) && !recipe.Title.ToLower().Contains(searchText))
                    return false;

                if (includeProducts.Any())
                {
                    if (!includeProducts.All(inc => recipe.Ingredients.Any(ing => ing.Product != null && ing.Product.Name.ToLower().Contains(inc))))
                        return false;
                }

                if (excludeProducts.Any() && excludeProducts.Any(exc => recipe.Ingredients.Any(ing => ing.Product != null && ing.Product.Name.ToLower().Contains(exc))))
                    return false;

                return true;
            });

            _filteredRecipes.Clear();
            foreach (var recipe in filtered)
            {
                _filteredRecipes.Add(recipe);
            }
        }

        private void OnAddRecipeClick(object? sender, RoutedEventArgs e)
        {
            _selectedRecipe = new Recipe
            {
                Id = 0,
                Title = $"Новый рецепт {_nextNewRecipeNumber++}",
                Description = string.Empty,
                CookingTimeMinutes = 30,
                Servings = 4,
                CategoryId = 1,
                Steps = new ObservableCollection<Step>()
            };

            ViewPanel.IsVisible = false;
            DefaultMessage.IsVisible = false;
            EditPanel.IsVisible = true;

            EditRecipeName.Text = _selectedRecipe.Title;
            EditDescription.Text = string.Empty;
            EditIngredients.Text = string.Empty;
            EditCookingTime.Text = "30";
            EditServings.Text = "4";

            EditStepsList.ItemsSource = _selectedRecipe.Steps;
        }

        private void OnViewRecipeClick(object? sender, RoutedEventArgs e)
        {
            var recipe = RecipesList.SelectedItem as Recipe;

            if (recipe != null)
            {
                _selectedRecipe = recipe;
                EditPanel.IsVisible = false;
                DefaultMessage.IsVisible = false;
                ViewPanel.IsVisible = true;

                ViewRecipeName.Text = recipe.Title;
                ViewIngredientsList.ItemsSource = recipe.Ingredients.Select(i => i.DisplayText).ToList();

                if (recipe.Steps.Any())
                {
                    _currentStepIndex = 0;
                    UpdateStepView();
                }
                else
                {
                    StepIndicator.Text = "Шаги отсутствуют";
                    CurrentStepDescription.Text = string.Empty;
                    CurrentStepImage.Source = null;
                    PrevStepButton.IsEnabled = false;
                    NextStepButton.IsEnabled = false;
                    NoPhotoOverlay.IsVisible = true;
                }
            }
        }

        private void UpdateStepView()
        {
            if (_selectedRecipe == null || !_selectedRecipe.Steps.Any()) return;

            var step = _selectedRecipe.Steps[_currentStepIndex];
            StepIndicator.Text = $"Шаг {step.StepNumber} из {_selectedRecipe.Steps.Count}";
            CurrentStepDescription.Text = step.InstructionText;

            PrevStepButton.IsEnabled = _currentStepIndex > 0;
            NextStepButton.IsEnabled = _currentStepIndex < _selectedRecipe.Steps.Count - 1;

            if (step.Photos.Any() && step.Photos[0].Bitmap != null)
            {
                CurrentStepImage.Source = step.Photos[0].Bitmap;
                NoPhotoOverlay.IsVisible = false;
            }
            else
            {
                CurrentStepImage.Source = null;
                NoPhotoOverlay.IsVisible = true;
            }
        }

        private void OnPrevStepClick(object? sender, RoutedEventArgs e)
        {
            if (_currentStepIndex > 0)
            {
                _currentStepIndex--;
                UpdateStepView();
            }
        }

        private void OnNextStepClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedRecipe != null && _currentStepIndex < _selectedRecipe.Steps.Count - 1)
            {
                _currentStepIndex++;
                UpdateStepView();
            }
        }

        private void OnEditRecipeClick(object? sender, RoutedEventArgs e)
        {
            var recipe = RecipesList.SelectedItem as Recipe;
            if (recipe != null)
            {
                _selectedRecipe = recipe;
                ViewPanel.IsVisible = false;
                DefaultMessage.IsVisible = false;
                EditPanel.IsVisible = true;

                EditRecipeName.Text = recipe.Title;
                EditDescription.Text = recipe.Description ?? string.Empty;
                EditCookingTime.Text = recipe.CookingTimeMinutes.ToString();
                EditServings.Text = recipe.Servings.ToString();
                EditIngredients.Text = string.Join("\n", recipe.Ingredients.Select(i => i.DisplayText));
                EditStepsList.ItemsSource = recipe.Steps;
            }
        }

        private async void OnSaveRecipeClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedRecipe == null) return;

                string title = EditRecipeName.Text?.Trim() ?? "Без названия";
                string description = EditDescription.Text?.Trim() ?? "";
                int cookingTime = int.TryParse(EditCookingTime.Text, out int ct) ? ct : 30;
                int servings = int.TryParse(EditServings.Text, out int sv) ? sv : 2;

                _selectedRecipe.Title = title;
                _selectedRecipe.Description = description;
                _selectedRecipe.CookingTimeMinutes = cookingTime;
                _selectedRecipe.Servings = servings;

                ParseIngredientsFromText(_selectedRecipe, EditIngredients.Text);

                var db = new DatabaseService();
                int savedId = db.SaveRecipe(_selectedRecipe);
                _selectedRecipe.Id = savedId;

                if (!_allRecipes.Contains(_selectedRecipe))
                {
                    _allRecipes.Add(_selectedRecipe);
                }

                ApplyFilters();

                EditPanel.IsVisible = false;
                ViewPanel.IsVisible = true;
                ViewRecipeName.Text = _selectedRecipe.Title;
                ViewIngredientsList.ItemsSource = _selectedRecipe.Ingredients.Select(i => i.DisplayText).ToList();

                if (_selectedRecipe.Steps.Any())
                {
                    _currentStepIndex = 0;
                    UpdateStepView();
                }

                await ShowStatusMessage("Рецепт сохранен!");
            }
            catch (Exception ex)
            {
                await ShowStatusMessage($"Ошибка сохранения: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Save error: {ex}");
            }
        }

        private void ParseIngredientsFromText(Recipe recipe, string? ingredientsText)
        {
            if (string.IsNullOrWhiteSpace(ingredientsText)) return;

            recipe.Ingredients.Clear();
            var db = new DatabaseService();
            var lines = ingredientsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string productName;
                double? quantity = null;
                string? unit = null;

                string[] separators = { " — ", " - ", ": " };
                string[] parts = null;

                foreach (var sep in separators)
                {
                    if (trimmed.Contains(sep))
                    {
                        parts = trimmed.Split(new[] { sep }, StringSplitOptions.None);
                        break;
                    }
                }

                if (parts != null && parts.Length >= 2)
                {
                    productName = parts[0].Trim();
                    string qtyStr = parts[1].Trim();

                    var qtyParts = qtyStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (qtyParts.Length >= 1 && double.TryParse(qtyParts[0], out double qty))
                    {
                        quantity = qty;
                        unit = qtyParts.Length > 1 ? qtyParts[1] : null;
                    }
                }
                else
                {
                    productName = trimmed;
                }

                var product = db.GetOrCreateProduct(productName);

                if (!_products.Any(p => p.Id == product.Id))
                {
                    _products.Add(product);
                    product.PropertyChanged += OnProductFilterChanged;
                }

                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Product = product,
                    Quantity = quantity,
                    Unit = unit
                });
            }
        }

        private async Task ShowStatusMessage(string message)
        {
            var statusBlock = this.FindControl<TextBlock>("StatusTextBlock");
            if (statusBlock != null)
            {
                statusBlock.Text = message;
                statusBlock.IsVisible = true;
                await Task.Delay(3000);
                statusBlock.IsVisible = false;
            }
        }

        private async void OnDeleteRecipeClick(object? sender, RoutedEventArgs e)
        {
            var recipe = RecipesList.SelectedItem as Recipe;
            if (recipe == null) return;

            var dialog = new Window
            {
                Title = "Подтверждение удаления",
                Width = 300,
                Height = 150,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(10),
                    Children =
            {
                new TextBlock { Text = $"Удалить рецепт \"{recipe.Title}\"?", Margin = new Avalonia.Thickness(0, 0, 0, 10) },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new Button { Content = "Да", Tag = true, Width = 60 },
                        new Button { Content = "Нет", Tag = false, Width = 60 }
                    }
                }
            }
                }
            };

            bool? result = null;
            foreach (var child in ((StackPanel)dialog.Content).Children)
            {
                if (child is StackPanel buttonPanel)
                {
                    foreach (var btn in buttonPanel.Children)
                    {
                        if (btn is Button button)
                        {
                            button.Click += (s, args) =>
                            {
                                result = (bool?)button.Tag;
                                dialog.Close();
                            };
                        }
                    }
                }
            }

            await dialog.ShowDialog(this);

            if (result == true)
            {
                try
                {
                    var db = new DatabaseService();
                    db.DeleteRecipe(recipe.Id);

                    _allRecipes.Remove(recipe);
                    ApplyFilters();

                    if (_selectedRecipe == recipe)
                    {
                        _selectedRecipe = null;
                        ViewPanel.IsVisible = false;
                        EditPanel.IsVisible = false;

                        // Исправленный блок для установки текста
                        var defaultTextBlock = DefaultMessage.Children.OfType<TextBlock>().FirstOrDefault();
                        if (defaultTextBlock != null)
                        {
                            defaultTextBlock.Text = "Нет рецептов. Нажмите \"+\" чтобы добавить первый рецепт!";
                        }
                        DefaultMessage.IsVisible = true;
                    }

                    await ShowStatusMessage("Рецепт удален");
                }
                catch (Exception ex)
                {
                    await ShowStatusMessage($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void OnDeleteStepClick(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var step = button?.DataContext as Step;

            if (step != null && _selectedRecipe != null)
            {
                _selectedRecipe.Steps.Remove(step);

                for (int i = 0; i < _selectedRecipe.Steps.Count; i++)
                {
                    _selectedRecipe.Steps[i].StepNumber = i + 1;
                }

                EditStepsList.ItemsSource = null;
                EditStepsList.ItemsSource = _selectedRecipe.Steps;
            }
        }

        private async void OnAddPhotoToStepClick(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var step = button?.DataContext as Step;

            if (step != null)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите изображение для шага",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                });

                if (files.Any())
                {
                    var filePath = files[0].Path.LocalPath;
                    var newPhoto = new Photo(filePath)
                    {
                        StepId = step.Id,
                        SortOrder = step.Photos.Count
                    };
                    step.Photos.Add(newPhoto);
                }
            }
        }

        private void OnDeletePhotoFromStepClick(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var photo = button?.DataContext as Photo;

            if (photo != null && _selectedRecipe != null)
            {
                foreach (var step in _selectedRecipe.Steps)
                {
                    if (step.Photos.Contains(photo))
                    {
                        step.Photos.Remove(photo);
                        photo.Dispose();
                        break;
                    }
                }
            }
        }

        private void OnAddStepClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedRecipe != null)
            {
                var newStep = new Step
                {
                    Id = 0,
                    RecipeId = _selectedRecipe.Id,
                    StepNumber = _selectedRecipe.Steps.Count + 1,
                    InstructionText = "Новый шаг",
                    Photos = new ObservableCollection<Photo>()
                };
                _selectedRecipe.Steps.Add(newStep);

                EditStepsList.ItemsSource = null;
                EditStepsList.ItemsSource = _selectedRecipe.Steps;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            foreach (var recipe in _allRecipes)
            {
                foreach (var step in recipe.Steps)
                {
                    foreach (var photo in step.Photos)
                    {
                        photo.Dispose();
                    }
                }
            }
        }
    }
}