public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
{
    string? dishType = null;
    string? sortBy = null;
    string? name = null;

    if (request.QueryStringParameters != null)
    {
        request.QueryStringParameters.TryGetValue("dishType", out dishType);
        request.QueryStringParameters.TryGetValue("sortBy", out sortBy);
        request.QueryStringParameters.TryGetValue("name", out name);
    }

    // 1. Отримуємо страви та відсіюємо будь-які null-елементи з DynamoDB
    var rawDishes = await _dbContext
        .ScanAsync<Dish>(new List<ScanCondition>(), _dishesConfig)
        .GetRemainingAsync();

    var dishes = rawDishes?.Where(d => d != null).ToList() ?? new List<Dish>();

    var orders = new List<global::APIHandler.Entities.Order>();
    var popularityMap = new Dictionary<string, int>();

    // 2. Безпечне зчитування популярності зі списку замовлень
    if (sortBy == DishSortingLabels.PopularityAscending ||
        sortBy == DishSortingLabels.PopularityDescending)
    {
        orders = await _dbContext
            .ScanAsync<global::APIHandler.Entities.Order>(new List<ScanCondition>(), new DynamoDBOperationConfig
            {
                OverrideTableName = ResourceNames.OrdersTableName
            })
            .GetRemainingAsync();

        popularityMap = orders?
            .Where(o => o != null && o.Dishes != null)
            .SelectMany(o => o.Dishes)
            .Where(d => d != null && !string.IsNullOrEmpty(d.DishId))
            .GroupBy(item => item.DishId)
            .ToDictionary(g => g.Key, g => g.Count()) 
            ?? new Dictionary<string, int>();
    }

    // 3. Фільтрація за типом страви (dishType)
    if (!string.IsNullOrEmpty(dishType) &&
        Enum.TryParse<DishType>(dishType, ignoreCase: true, out var parsedType))
    {
        dishes = dishes.Where(d => d.DishType == parsedType).ToList();
    }

    // 4. Фільтрація за назвою з захистом від null
    if (!string.IsNullOrEmpty(name))
    {
        dishes = dishes.Where(d => d.Name != null && d.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // 5. Сортування
    dishes = SortDishes(dishes, sortBy, popularityMap);

    // 6. Мапінг у DTO з фільтрацією null-результатів
    var responseList = dishes
        .Select(d => DishMapper.ToDishListResponse(d))
        .Where(res => res != null)
        .ToList();

    // Повертаємо 200 OK з гарантованим масивом (навіть якщо порожнім [])
    return ResponseBuilder.CreateJsonResponse(200, responseList);
}
