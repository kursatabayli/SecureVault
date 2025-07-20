//using SecureVault.App.Services.Service.Contracts;

//namespace SecureVault.App.Services.Service.Implementations
//{
//    public class NetworkService : INetworkService
//    {
//        private readonly HttpClient _httpClient;
//        private static string _cachedIpAddress = null;
//        private static DateTime _cacheTime;
//        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

//        public NetworkService()
//        {
//            _httpClient = new HttpClient();
//        }

//        public async Task<string> GetPublicIpAddressAsync()
//        {
//            if (!string.IsNullOrEmpty(_cachedIpAddress) && DateTime.UtcNow - _cacheTime < _cacheDuration)
//            {
//                return _cachedIpAddress;
//            }

//            try
//            {
//                var ipAddress = await _httpClient.GetStringAsync("https://api64.ipify.org");

//                if (!string.IsNullOrWhiteSpace(ipAddress))
//                {
//                    _cachedIpAddress = ipAddress.Trim();
//                    _cacheTime = DateTime.UtcNow;
//                    return _cachedIpAddress;
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Public IP adresi alınamadı: {ex.Message}");
//                return null;
//            }
//        }
//    }
//}
