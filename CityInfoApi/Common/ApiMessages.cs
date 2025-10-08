namespace CityInfoApi.Common
{
    public static class ApiMessages
    {

        public const string CityCreated = "City created successfully.";
        public const string CityUpdated = "City updated successfully.";
        public const string CityDeleted = "City deleted successfully.";
        public const string CityNotFound = "City not found.";
        public const string DuplicateCity = "City already exists.";

        public const string InvalidRequest = "Request body cannot be null or invalid.";
        public const string SearchEmpty = "City name must be provided.";
        public const string NoResults = "No cities found matching the search criteria.";

        public const string WeatherApiError = "Unable to retrieve weather information.";
        public const string CountryApiError = "Unable to retrieve country information.";
    }
}
