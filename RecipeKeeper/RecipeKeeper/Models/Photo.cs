using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace RecipeKeeper.Models
{
    public class Photo : INotifyPropertyChanged, IDisposable
    {
        private int _id;
        private int _stepId;
        private string _filePath = string.Empty;
        private int _sortOrder;
        private Bitmap? _bitmap;
        private bool _isLoading;

        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public int StepId
        {
            get => _stepId;
            set { if (_stepId != value) { _stepId = value; OnPropertyChanged(); } }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                    _ = LoadBitmapAsync();
                }
            }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set { if (_sortOrder != value) { _sortOrder = value; OnPropertyChanged(); } }
        }

        public Bitmap? Bitmap
        {
            get => _bitmap;
            set { if (_bitmap != value) { _bitmap = value; OnPropertyChanged(); } }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public Photo() { }

        public Photo(string filePath)
        {
            _filePath = filePath;
            _ = LoadBitmapAsync();
        }

        private async Task LoadBitmapAsync()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                return;

            IsLoading = true;
            try
            {
                await Task.Run(() =>
                {
                    var imageData = File.ReadAllBytes(_filePath);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            using var stream = new MemoryStream(imageData);
                            var newBitmap = new Bitmap(stream);

                            var oldBitmap = _bitmap;
                            Bitmap = newBitmap;
                            oldBitmap?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка создания Bitmap: {ex.Message}");
                        }
                        finally
                        {
                            IsLoading = false;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
                IsLoading = false;
            }
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
