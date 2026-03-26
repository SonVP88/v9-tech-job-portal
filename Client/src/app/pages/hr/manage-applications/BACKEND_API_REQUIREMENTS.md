# Backend API Requirements

## Vietnam Holidays API Endpoint

Để tính năng **tư động cập nhật danh sách ngày lễ Việt Nam** hoạt động, backend cần implement endpoint sau:

### Endpoint

```
GET /api/master-data/vietnam-holidays/{year}
```

### Headers

```
Authorization: Bearer {token}
Content-Type: application/json
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `year` | number | Yes | Năm cần lấy danh sách ngày lễ |

### Response

**Status Code:** `200 OK`

```json
{
  "holidays": [
    "01-01",
    "17-02",
    "18-02",
    "19-02",
    "20-02",
    "21-02",
    "22-02",
    "23-02",
    "30-04",
    "01-05",
    "02-09"
  ],
  "year": 2026,
  "count": 11
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `holidays` | Array<string> | Danh sách ngày lễ theo format `DD-MM` |
| `year` | number | Năm được yêu cầu |
| `count` | number | Số lượng ngày lễ |

### Data Format

- **DD-MM Format**: Ngày-Tháng (không có năm)
- **Examples**:
  - `01-01` = 1 tháng 1 (Tết Dương Lịch)
  - `17-02` = 17 tháng 2 (Tết Nguyên Đán 2026)
  - `30-04` = 30 tháng 4 (Ngày Giải phóng)
  - `02-09` = 2 tháng 9 (Quốc khánh)

### Danh Sách Ngày Lễ Việt Nam (Tham Khảo)

| Ngày Lễ | Date |
|---------|------|
| Tết Dương Lịch | 01-01 |
| Tết Nguyên Đán 2026 | 17-02 đến 23-02 |
| Tết Nguyên Đán 2027 | 06-02 đến 12-02 |
| Ngày Giải phóng | 30-04 |
| Quốc tế Lao động | 01-05 |
| Quốc khánh | 02-09 |

### Error Response

**Status Code:** `404 Not Found` (hoặc `500 Internal Server Error`)

```json
{
  "message": "Không tìm thấy dữ liệu ngày lễ cho năm này",
  "statusCode": 404
}
```

### Frontend Behavior

- **Khi Thành Công**: Cập nhật danh sách `vietnameseHolidays` từ API
- **Khi Thất Bại**: Sử dụng danh sách ngày lễ mặc định (hardcode trong component)
- **Fallback**: Luôn có danh sách mặc định để đảm bảo UX không bị gián đoạn

### Implementation Notes

1. **Cache**: Frontend cache danh sách ngày lễ cho năm hiện tại
2. **Update**: Mỗi khi bộ lọc năm thay đổi, cần gọi lại API
3. **Performance**: Có thể implement caching trên backend với TTL 24 giờ

### C# Backend Implementation Example

```csharp
[HttpGet("vietnam-holidays/{year}")]
public async Task<IActionResult> GetVietnamHolidays(int year)
{
    try
    {
        // Lấy danh sách ngày lễ từ database hoặc config
        var holidays = await _masterDataService.GetHolidaysByYear(year);
        
        return Ok(new
        {
            holidays = holidays.Select(h => h.DateString).ToList(), // Format: DD-MM
            year = year,
            count = holidays.Count()
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error loading holidays" });
    }
}
```

---

**Last Updated**: March 25, 2026  
**Frontend File**: `manage-applications.component.ts`  
**Method**: `loadVietnamHolidays()`
