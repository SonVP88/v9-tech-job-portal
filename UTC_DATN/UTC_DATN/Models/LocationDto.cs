namespace UTC_DATN.Models
{
    /// <summary>
    /// DTO cho Tỉnh/Thành phố từ API provinces.open-api.vn
    /// </summary>
    public class ProvinceDto
    {
        /// <summary>
        /// Mã tỉnh/thành phố
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Tên tỉnh/thành phố
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tên đầy đủ (có chữ "Tỉnh" hoặc "Thành phố")
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Tên tiếng Anh
        /// </summary>
        public string? NameEn { get; set; }
    }

    /// <summary>
    /// Response từ external API provinces.open-api.vn
    /// </summary>
    public class ProvinceApiResponse
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;

        // API trả về "name_en" với underscore
        public string? Name_en { get; set; }
    }

    /// <summary>
    /// DTO cho Quận/Huyện từ API provinces.open-api.vn
    /// </summary>
    public class DistrictDto
    {
        /// <summary>
        /// Mã quận/huyện
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Tên quận/huyện
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mã tỉnh/thành phố
        /// </summary>
        public int ProvinceCode { get; set; }
    }

    /// <summary>
    /// DTO cho Phường/Xã từ API provinces.open-api.vn v2
    /// V2 bỏ cấp huyện, wards thuộc trực tiếp province
    /// </summary>
    public class WardDto
    {
        /// <summary>
        /// Mã phường/xã
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Tên phường/xã
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mã tỉnh/thành phố  
        /// </summary>
        public int ProvinceCode { get; set; }
    }
}
