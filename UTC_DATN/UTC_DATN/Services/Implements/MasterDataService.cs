using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using UTC_DATN.Data;
using UTC_DATN.Entities;
using UTC_DATN.Models;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class MasterDataService : IMasterDataService
    {
        private readonly UTC_DATNContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MasterDataService> _logger;

        // Cache key constants
        private const string PROVINCES_CACHE_KEY = "provinces_list";
        private const int CACHE_DURATION_MINUTES = 15;

        public MasterDataService(
            UTC_DATNContext context,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<MasterDataService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách Skills từ database
        /// </summary>
        /// <returns>Danh sách Skills</returns>
        public async Task<List<Skill>> GetAllSkillsAsync()
        {
            return await _context.Skills
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy toàn bộ danh sách JobTypes từ database
        /// </summary>
        /// <returns>Danh sách JobTypes</returns>
        public async Task<List<JobType>> GetAllJobTypesAsync()
        {
            return await _context.JobTypes
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách Tỉnh/Thành phố từ API công khai provinces.open-api.vn
        /// Có caching 15 phút để giảm tải
        /// </summary>
        /// <returns>Danh sách Tỉnh/Thành phố</returns>
        public async Task<List<ProvinceDto>> GetProvincesAsync()
        {
            // Kiểm tra cache trước
            if (_cache.TryGetValue(PROVINCES_CACHE_KEY, out List<ProvinceDto>? cachedProvinces))
            {
                return cachedProvinces!;
            }

            try
            {

                var httpClient = _httpClientFactory.CreateClient();
                // Sử dụng API v2 - dữ liệu sau khi sáp nhập tỉnh tháng 7/2025
                var response = await httpClient.GetAsync("https://provinces.open-api.vn/api/v2/p/");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(" API trả về lỗi: {StatusCode}", response.StatusCode);
                    return new List<ProvinceDto>();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<List<ProvinceApiResponse>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse == null || !apiResponse.Any())
                {
                    _logger.LogWarning("API trả về dữ liệu rỗng");
                    return new List<ProvinceDto>();
                }

                // Map từ API response sang DTO
                var provinces = apiResponse.Select(p => new ProvinceDto
                {
                    Code = p.Code,
                    Name = p.Name,
                    FullName = p.Name, // API chỉ trả về "name", dùng làm fullName luôn
                    NameEn = p.Name_en
                }).ToList();

                // Cache kết quả 15 phút
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                _cache.Set(PROVINCES_CACHE_KEY, provinces, cacheOptions);

                _logger.LogInformation("✅ Đã lấy {Count} tỉnh/thành phố từ API và lưu cache", provinces.Count);
                return provinces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi gọi API provinces.open-api.vn");
                throw new Exception("Không thể lấy danh sách tỉnh/thành phố", ex);
            }
        }

        /// <summary>
        /// Lấy danh sách Phường/Xã theo mã tỉnh từ API v2
        /// API v2 bỏ cấp huyện, chỉ còn Tỉnh -> Phường/Xã
        /// </summary>
        /// <param name="provinceCode">Mã tỉnh</param>
        /// <returns>Danh sách Phường/Xã</returns>
        public async Task<List<WardDto>> GetWardsAsync(int provinceCode)
        {
            var cacheKey = $"wards_province_{provinceCode}";

            // Kiểm tra cache
            if (_cache.TryGetValue(cacheKey, out List<WardDto>? cachedWards))
            {
                _logger.LogInformation("✅ Lấy danh sách phường/xã từ cache cho tỉnh {ProvinceCode}", provinceCode);
                return cachedWards!;
            }

            try
            {
                _logger.LogInformation("🌐 Gọi API để lấy wards của tỉnh {ProvinceCode}", provinceCode);

                var httpClient = _httpClientFactory.CreateClient();
                // API v2: /api/v2/w/?province={provinceCode} trả về danh sách wards
                var response = await httpClient.GetAsync($"https://provinces.open-api.vn/api/v2/w/?province={provinceCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ API trả về lỗi: {StatusCode}", response.StatusCode);
                    return new List<WardDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📝 Raw Wards API response: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);

                // Parse response - API trả về array of wards
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var wards = new List<WardDto>();

                // V2 API trả về array trực tiếp
                foreach (var ward in root.EnumerateArray())
                {
                    wards.Add(new WardDto
                    {
                        Code = ward.GetProperty("code").GetInt32(),
                        Name = ward.GetProperty("name").GetString() ?? "",
                        ProvinceCode = ward.GetProperty("province_code").GetInt32()
                    });
                }

                _logger.LogInformation("✅ Found {Count} wards for province {ProvinceCode}", wards.Count, provinceCode);

                // Cache 15 phút
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                _cache.Set(cacheKey, wards, cacheOptions);

                return wards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi lấy danh sách phường/xã");
                return new List<WardDto>();
            }
        }
    }
}
