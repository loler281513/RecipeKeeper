using Microsoft.Data.Sqlite;
using RecipeKeeper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace RecipeKeeper.Services
{
    public class DatabaseService
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            // База данных будет создана в папке приложения
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dataDirectory = Path.Combine(appDirectory, "Data");

            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);

            _databasePath = Path.Combine(dataDirectory, "RecipeKeeper.db");
            _connectionString = $"Data Source={_databasePath}";

            // Создаем базу данных и таблицы при первом запуске
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool isNewDatabase = !File.Exists(_databasePath);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Создание таблиц
            string createTablesSql = @"
                -- Таблица категорий
                CREATE TABLE IF NOT EXISTS CATEGORY (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );

                -- Таблица ингредиентов/продуктов
                CREATE TABLE IF NOT EXISTS INGREDIENT (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );

                -- Таблица рецептов
                CREATE TABLE IF NOT EXISTS RECIPE (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    description TEXT,
                    cooking_time_minutes INTEGER NOT NULL DEFAULT 0,
                    servings INTEGER NOT NULL DEFAULT 1,
                    main_photo_path TEXT,
                    category_id INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (category_id) REFERENCES CATEGORY(id)
                );

                -- Таблица связи рецептов с ингредиентами
                CREATE TABLE IF NOT EXISTS RECIPE_INGREDIENT (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    recipe_id INTEGER NOT NULL,
                    ingredient_id INTEGER NOT NULL,
                    quantity REAL,
                    unit TEXT,
                    FOREIGN KEY (recipe_id) REFERENCES RECIPE(id) ON DELETE CASCADE,
                    FOREIGN KEY (ingredient_id) REFERENCES INGREDIENT(id)
                );

                -- Таблица шагов приготовления
                CREATE TABLE IF NOT EXISTS STEP (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    recipe_id INTEGER NOT NULL,
                    step_number INTEGER NOT NULL,
                    instruction_text TEXT NOT NULL,
                    FOREIGN KEY (recipe_id) REFERENCES RECIPE(id) ON DELETE CASCADE
                );

                -- Таблица фото для шагов
                CREATE TABLE IF NOT EXISTS STEP_PHOTO (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    step_id INTEGER NOT NULL,
                    photo_path TEXT NOT NULL,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (step_id) REFERENCES STEP(id) ON DELETE CASCADE
                );

                -- Индексы для производительности
                CREATE INDEX IF NOT EXISTS idx_recipe_ingredient_recipe ON RECIPE_INGREDIENT(recipe_id);
                CREATE INDEX IF NOT EXISTS idx_recipe_ingredient_ingredient ON RECIPE_INGREDIENT(ingredient_id);
                CREATE INDEX IF NOT EXISTS idx_step_recipe ON STEP(recipe_id);
                CREATE INDEX IF NOT EXISTS idx_step_photo_step ON STEP_PHOTO(step_id);
            ";

            using var command = new SqliteCommand(createTablesSql, connection);
            command.ExecuteNonQuery();

            // Если база данных новая - заполняем начальными данными
            if (isNewDatabase)
            {
                SeedInitialData(connection);
            }
        }

        private void SeedInitialData(SqliteConnection connection)
        {
            // Добавляем категории
            var categories = new[] { "Завтраки", "Обеды", "Ужины", "Десерты", "Салаты", "Супы" };
            foreach (var category in categories)
            {
                using var cmd = new SqliteCommand("INSERT INTO CATEGORY (name) VALUES (@name)", connection);
                cmd.Parameters.AddWithValue("@name", category);
                cmd.ExecuteNonQuery();
            }

            // Добавляем продукты
            var products = new[]
            {
                "Мука", "Молоко", "Яйца", "Сахар", "Мясо", "Картофель",
                "Соль", "Перец", "Масло сливочное", "Масло растительное",
                "Лук", "Чеснок", "Помидоры", "Огурцы", "Сыр", "Сметана"
            };

            foreach (var product in products)
            {
                using var cmd = new SqliteCommand("INSERT INTO INGREDIENT (name) VALUES (@name)", connection);
                cmd.Parameters.AddWithValue("@name", product);
                cmd.ExecuteNonQuery();
            }

            // Получаем ID категорий и продуктов
            var categoryIds = new Dictionary<string, int>();
            using (var cmd = new SqliteCommand("SELECT id, name FROM CATEGORY", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    categoryIds[reader.GetString(1)] = reader.GetInt32(0);
            }

            var productIds = new Dictionary<string, int>();
            using (var cmd = new SqliteCommand("SELECT id, name FROM INGREDIENT", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    productIds[reader.GetString(1)] = reader.GetInt32(0);
            }

            // Добавляем тестовые рецепты
            AddSampleRecipe(connection, productIds, categoryIds);
        }

        private void AddSampleRecipe(SqliteConnection connection, Dictionary<string, int> productIds, Dictionary<string, int> categoryIds)
        {
            // Рецепт 1: Классические блины
            using var insertRecipe = new SqliteCommand(@"
                INSERT INTO RECIPE (title, description, cooking_time_minutes, servings, category_id, created_at, updated_at)
                VALUES (@title, @description, @cooking_time, @servings, @category_id, datetime('now'), datetime('now'));
                SELECT last_insert_rowid();", connection);

            insertRecipe.Parameters.AddWithValue("@title", "Классические блины");
            insertRecipe.Parameters.AddWithValue("@description", "Вкусные домашние блины на молоке");
            insertRecipe.Parameters.AddWithValue("@cooking_time", 30);
            insertRecipe.Parameters.AddWithValue("@servings", 4);
            insertRecipe.Parameters.AddWithValue("@category_id", categoryIds.GetValueOrDefault("Завтраки", 1));

            int recipeId1 = Convert.ToInt32(insertRecipe.ExecuteScalar());

            // Ингредиенты для блинов
            AddRecipeIngredient(connection, recipeId1, productIds["Мука"], 200, "г");
            AddRecipeIngredient(connection, recipeId1, productIds["Молоко"], 500, "мл");
            AddRecipeIngredient(connection, recipeId1, productIds["Яйца"], 3, "шт.");
            AddRecipeIngredient(connection, recipeId1, productIds["Сахар"], 2, "ст.л.");
            AddRecipeIngredient(connection, recipeId1, productIds["Соль"], 1, "щепотка");
            AddRecipeIngredient(connection, recipeId1, productIds["Масло растительное"], 30, "мл");

            // Шаги для блинов
            AddStep(connection, recipeId1, 1, "Взбейте яйца с сахаром и солью в глубокой миске.");
            AddStep(connection, recipeId1, 2, "Добавьте молоко и постепенно всыпайте муку, непрерывно помешивая, чтобы не было комков.");
            AddStep(connection, recipeId1, 3, "Добавьте растительное масло, перемешайте.");
            AddStep(connection, recipeId1, 4, "Выпекайте блины на раскаленной сковороде с двух сторон до золотистого цвета.");

            // Рецепт 2: Жареное мясо с картофелем
            using var insertRecipe2 = new SqliteCommand(@"
                INSERT INTO RECIPE (title, description, cooking_time_minutes, servings, category_id, created_at, updated_at)
                VALUES (@title, @description, @cooking_time, @servings, @category_id, datetime('now'), datetime('now'));
                SELECT last_insert_rowid();", connection);

            insertRecipe2.Parameters.AddWithValue("@title", "Жареное мясо с картофелем");
            insertRecipe2.Parameters.AddWithValue("@description", "Сытный и простой ужин для всей семьи");
            insertRecipe2.Parameters.AddWithValue("@cooking_time", 45);
            insertRecipe2.Parameters.AddWithValue("@servings", 2);
            insertRecipe2.Parameters.AddWithValue("@category_id", categoryIds.GetValueOrDefault("Ужины", 3));

            int recipeId2 = Convert.ToInt32(insertRecipe2.ExecuteScalar());

            AddRecipeIngredient(connection, recipeId2, productIds["Мясо"], 400, "г");
            AddRecipeIngredient(connection, recipeId2, productIds["Картофель"], 600, "г");
            AddRecipeIngredient(connection, recipeId2, productIds["Лук"], 1, "шт.");
            AddRecipeIngredient(connection, recipeId2, productIds["Соль"], 1, "по вкусу");
            AddRecipeIngredient(connection, recipeId2, productIds["Перец"], 1, "по вкусу");
            AddRecipeIngredient(connection, recipeId2, productIds["Масло растительное"], 50, "мл");

            AddStep(connection, recipeId2, 1, "Нарежьте мясо кусочками 2-3 см.");
            AddStep(connection, recipeId2, 2, "Нарежьте картофель крупными кусочками, лук полукольцами.");
            AddStep(connection, recipeId2, 3, "Обжарьте мясо на сковороде до золотистой корочки (5-7 минут).");
            AddStep(connection, recipeId2, 4, "Добавьте лук, жарьте еще 2-3 минуты.");
            AddStep(connection, recipeId2, 5, "Добавьте картофель, посолите, поперчите и жарьте до готовности картофеля (15-20 минут).");
        }

        private void AddRecipeIngredient(SqliteConnection connection, int recipeId, int ingredientId, double quantity, string unit)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO RECIPE_INGREDIENT (recipe_id, ingredient_id, quantity, unit)
                VALUES (@recipe_id, @ingredient_id, @quantity, @unit)", connection);

            cmd.Parameters.AddWithValue("@recipe_id", recipeId);
            cmd.Parameters.AddWithValue("@ingredient_id", ingredientId);
            cmd.Parameters.AddWithValue("@quantity", quantity);
            cmd.Parameters.AddWithValue("@unit", unit);
            cmd.ExecuteNonQuery();
        }

        private void AddRecipeIngredient(SqliteConnection connection, int recipeId, int ingredientId, string quantity, string unit)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO RECIPE_INGREDIENT (recipe_id, ingredient_id, quantity, unit)
                VALUES (@recipe_id, @ingredient_id, @quantity, @unit)", connection);

            cmd.Parameters.AddWithValue("@recipe_id", recipeId);
            cmd.Parameters.AddWithValue("@ingredient_id", ingredientId);
            cmd.Parameters.AddWithValue("@quantity", quantity);
            cmd.Parameters.AddWithValue("@unit", unit);
            cmd.ExecuteNonQuery();
        }

        private void AddStep(SqliteConnection connection, int recipeId, int stepNumber, string instructionText)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO STEP (recipe_id, step_number, instruction_text)
                VALUES (@recipe_id, @step_number, @instruction_text)", connection);

            cmd.Parameters.AddWithValue("@recipe_id", recipeId);
            cmd.Parameters.AddWithValue("@step_number", stepNumber);
            cmd.Parameters.AddWithValue("@instruction_text", instructionText);
            cmd.ExecuteNonQuery();
        }

        // ==================== МЕТОДЫ ПОЛУЧЕНИЯ ДАННЫХ ====================

        public List<Product> GetProducts()
        {
            var products = new List<Product>();
            string query = "SELECT id, name FROM INGREDIENT ORDER BY name";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return products;
        }

        public List<Category> GetCategories()
        {
            var categories = new List<Category>();
            string query = "SELECT id, name FROM CATEGORY ORDER BY name";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return categories;
        }

        public List<Recipe> GetAllRecipes(List<Product> allProducts)
        {
            var recipes = new List<Recipe>();
            string recipeQuery = "SELECT id, title, description, cooking_time_minutes, servings, main_photo_path, category_id FROM RECIPE ORDER BY title";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using (var command = new SqliteCommand(recipeQuery, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    recipes.Add(new Recipe
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CookingTimeMinutes = reader.GetInt32(3),
                        Servings = reader.GetInt32(4),
                        MainPhotoPath = reader.IsDBNull(5) ? null : reader.GetString(5),
                        CategoryId = reader.GetInt32(6)
                    });
                }
            }

            foreach (var recipe in recipes)
            {
                LoadRecipeIngredients(connection, recipe, allProducts);
                LoadRecipeSteps(connection, recipe);
            }

            return recipes;
        }

        private void LoadRecipeIngredients(SqliteConnection connection, Recipe recipe, List<Product> allProducts)
        {
            string query = @"
                SELECT ri.id, ri.ingredient_id, ri.quantity, ri.unit 
                FROM RECIPE_INGREDIENT ri 
                WHERE ri.recipe_id = @RecipeId";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@RecipeId", recipe.Id);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int ingredientId = reader.GetInt32(1);
                var product = allProducts.FirstOrDefault(p => p.Id == ingredientId);

                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Id = reader.GetInt32(0),
                    RecipeId = recipe.Id,
                    Product = product ?? new Product { Id = ingredientId, Name = "Неизвестный продукт" },
                    Quantity = reader.IsDBNull(2) ? null : (double?)reader.GetDouble(2),
                    Unit = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }

        private void LoadRecipeSteps(SqliteConnection connection, Recipe recipe)
        {
            string stepQuery = "SELECT id, step_number, instruction_text FROM STEP WHERE recipe_id = @RecipeId ORDER BY step_number";
            using var command = new SqliteCommand(stepQuery, connection);
            command.Parameters.AddWithValue("@RecipeId", recipe.Id);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var step = new Step
                {
                    Id = reader.GetInt32(0),
                    RecipeId = recipe.Id,
                    StepNumber = reader.GetInt32(1),
                    InstructionText = reader.GetString(2)
                };
                recipe.Steps.Add(step);
            }

            foreach (var step in recipe.Steps)
            {
                LoadStepPhotos(connection, step);
            }
        }

        private void LoadStepPhotos(SqliteConnection connection, Step step)
        {
            string photoQuery = "SELECT id, photo_path, sort_order FROM STEP_PHOTO WHERE step_id = @StepId ORDER BY sort_order";
            using var command = new SqliteCommand(photoQuery, connection);
            command.Parameters.AddWithValue("@StepId", step.Id);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                step.Photos.Add(new Photo
                {
                    Id = reader.GetInt32(0),
                    StepId = step.Id,
                    FilePath = reader.GetString(1),
                    SortOrder = reader.GetInt32(2)
                });
            }
        }

        // ==================== МЕТОДЫ СОХРАНЕНИЯ ====================

        public int SaveRecipe(Recipe recipe)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                int recipeId;

                if (recipe.Id == 0)
                {
                    string insertQuery = @"
                        INSERT INTO RECIPE (title, description, cooking_time_minutes, servings, main_photo_path, category_id, created_at, updated_at)
                        VALUES (@Title, @Description, @CookingTime, @Servings, @MainPhotoPath, @CategoryId, datetime('now'), datetime('now'));
                        SELECT last_insert_rowid();";

                    using var command = new SqliteCommand(insertQuery, connection, transaction);
                    AddRecipeParameters(command, recipe);
                    recipeId = Convert.ToInt32(command.ExecuteScalar());
                }
                else
                {
                    string updateQuery = @"
                        UPDATE RECIPE 
                        SET title = @Title, 
                            description = @Description, 
                            cooking_time_minutes = @CookingTime, 
                            servings = @Servings, 
                            main_photo_path = @MainPhotoPath, 
                            category_id = @CategoryId,
                            updated_at = datetime('now')
                        WHERE id = @Id";

                    using var command = new SqliteCommand(updateQuery, connection, transaction);
                    command.Parameters.AddWithValue("@Id", recipe.Id);
                    AddRecipeParameters(command, recipe);
                    command.ExecuteNonQuery();
                    recipeId = recipe.Id;
                }

                SaveRecipeIngredients(connection, transaction, recipeId, recipe.Ingredients);
                SaveRecipeSteps(connection, transaction, recipeId, recipe.Steps);

                transaction.Commit();
                return recipeId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void AddRecipeParameters(SqliteCommand command, Recipe recipe)
        {
            command.Parameters.AddWithValue("@Title", recipe.Title);
            command.Parameters.AddWithValue("@Description", (object?)recipe.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@CookingTime", recipe.CookingTimeMinutes);
            command.Parameters.AddWithValue("@Servings", recipe.Servings);
            command.Parameters.AddWithValue("@MainPhotoPath", (object?)recipe.MainPhotoPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", recipe.CategoryId == 0 ? 1 : recipe.CategoryId);
        }

        private void SaveRecipeIngredients(SqliteConnection connection, SqliteTransaction transaction, int recipeId, ObservableCollection<RecipeIngredient> ingredients)
        {
            string deleteQuery = "DELETE FROM RECIPE_INGREDIENT WHERE recipe_id = @RecipeId";
            using var deleteCmd = new SqliteCommand(deleteQuery, connection, transaction);
            deleteCmd.Parameters.AddWithValue("@RecipeId", recipeId);
            deleteCmd.ExecuteNonQuery();

            string insertQuery = @"
                INSERT INTO RECIPE_INGREDIENT (recipe_id, ingredient_id, quantity, unit)
                VALUES (@RecipeId, @IngredientId, @Quantity, @Unit)";

            foreach (var ingredient in ingredients.Where(i => i.Product != null && i.Product.Id > 0))
            {
                using var insertCmd = new SqliteCommand(insertQuery, connection, transaction);
                insertCmd.Parameters.AddWithValue("@RecipeId", recipeId);
                insertCmd.Parameters.AddWithValue("@IngredientId", ingredient.Product.Id);
                insertCmd.Parameters.AddWithValue("@Quantity", (object?)ingredient.Quantity ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Unit", (object?)ingredient.Unit ?? DBNull.Value);
                insertCmd.ExecuteNonQuery();
            }
        }

        private void SaveRecipeSteps(SqliteConnection connection, SqliteTransaction transaction, int recipeId, ObservableCollection<Step> steps)
        {
            string deleteQuery = "DELETE FROM STEP WHERE recipe_id = @RecipeId";
            using var deleteCmd = new SqliteCommand(deleteQuery, connection, transaction);
            deleteCmd.Parameters.AddWithValue("@RecipeId", recipeId);
            deleteCmd.ExecuteNonQuery();

            string insertStepQuery = @"
                INSERT INTO STEP (recipe_id, step_number, instruction_text)
                VALUES (@RecipeId, @StepNumber, @InstructionText);
                SELECT last_insert_rowid();";

            foreach (var step in steps)
            {
                int stepId;
                using var insertCmd = new SqliteCommand(insertStepQuery, connection, transaction);
                insertCmd.Parameters.AddWithValue("@RecipeId", recipeId);
                insertCmd.Parameters.AddWithValue("@StepNumber", step.StepNumber);
                insertCmd.Parameters.AddWithValue("@InstructionText", step.InstructionText);
                stepId = Convert.ToInt32(insertCmd.ExecuteScalar());

                SaveStepPhotos(connection, transaction, stepId, step.Photos);
            }
        }

        private void SaveStepPhotos(SqliteConnection connection, SqliteTransaction transaction, int stepId, ObservableCollection<Photo> photos)
        {
            string insertPhotoQuery = @"
                INSERT INTO STEP_PHOTO (step_id, photo_path, sort_order)
                VALUES (@StepId, @PhotoPath, @SortOrder)";

            for (int i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                using var insertCmd = new SqliteCommand(insertPhotoQuery, connection, transaction);
                insertCmd.Parameters.AddWithValue("@StepId", stepId);
                insertCmd.Parameters.AddWithValue("@PhotoPath", photo.FilePath);
                insertCmd.Parameters.AddWithValue("@SortOrder", i);
                insertCmd.ExecuteNonQuery();
            }
        }

        // ==================== МЕТОДЫ УДАЛЕНИЯ ====================

        public void DeleteRecipe(int recipeId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string query = "DELETE FROM RECIPE WHERE id = @Id";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", recipeId);
            command.ExecuteNonQuery();
        }

        public void DeleteStep(int stepId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string query = "DELETE FROM STEP WHERE id = @Id";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", stepId);
            command.ExecuteNonQuery();
        }

        public void DeletePhoto(int photoId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string query = "DELETE FROM STEP_PHOTO WHERE id = @Id";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", photoId);
            command.ExecuteNonQuery();
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        public Product GetOrCreateProduct(string productName)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string selectQuery = "SELECT id FROM INGREDIENT WHERE name = @Name";
            using var selectCmd = new SqliteCommand(selectQuery, connection);
            selectCmd.Parameters.AddWithValue("@Name", productName);
            var result = selectCmd.ExecuteScalar();

            if (result != null)
            {
                return new Product { Id = Convert.ToInt32(result), Name = productName };
            }

            string insertQuery = "INSERT INTO INGREDIENT (name) VALUES (@Name); SELECT last_insert_rowid();";
            using var insertCmd = new SqliteCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Name", productName);
            int newId = Convert.ToInt32(insertCmd.ExecuteScalar());
            return new Product { Id = newId, Name = productName };
        }

        public bool TestConnection()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}