//namespace WrikeTimeLogger.Services
//{
//    public class LogoutService
//    {
//        private readonly HttpClient _httpClient;

//        public LogoutService(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }

//        public async Task LogoutAsync()
//        {
//            var response = await _httpClient.PostAsync("/logout", null);
//            response.EnsureSuccessStatusCode();
//        }

//        // Other methods for interacting with your API
//    }
//}
