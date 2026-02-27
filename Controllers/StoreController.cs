using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.ViewModels;
using JohnHenryFashionWeb.Services;
using JohnHenryFashionWeb.Helpers;

namespace JohnHenryFashionWeb.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeoService _seoService;

        public StoreController(ApplicationDbContext context, SeoService seoService)
        {
            _context = context;
            _seoService = seoService;
        }

        // GET: Store
        public async Task<IActionResult> Index(string? province, string? city, string? brand, string? storeType, string? searchTerm)
        {
            // SEO and meta data
            ViewData["Title"] = "Hệ Thống Cửa Hàng John Henry & Freelancer";
            ViewData["Description"] = "Tìm kiếm cửa hàng John Henry và Freelancer gần bạn. Hệ thống cửa hàng trên toàn quốc với sản phẩm thời trang chất lượng.";
            
            // Generate breadcrumbs
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Name = "Trang chủ", Url = Url.Action("Index", "Home") ?? "/" },
                new BreadcrumbItem { Name = "Cửa hàng", Url = "" }
            };
            
            ViewBag.Breadcrumbs = breadcrumbs;

            // Initialize stores with seed data if empty
            await InitializeStoresIfEmpty();

            // Build query
            var query = _context.Stores.Where(s => s.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(province))
                query = query.Where(s => s.Province.Contains(province));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(s => s.City.Contains(city));

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(s => s.Brand.Contains(brand));

            if (!string.IsNullOrEmpty(storeType))
                query = query.Where(s => s.StoreType.Contains(storeType));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => 
                    s.Name.Contains(searchTerm) ||
                    s.Address.Contains(searchTerm) ||
                    s.City.Contains(searchTerm) ||
                    s.Province.Contains(searchTerm));
            }

            // Get stores
            var stores = await query
                .OrderBy(s => s.Province)
                .ThenBy(s => s.City)
                .ThenBy(s => s.Name)
                .Select(s => new StoreItemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Phone = s.Phone,
                    City = s.City,
                    Province = s.Province,
                    Brand = s.Brand,
                    StoreType = s.StoreType,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Description = s.Description,
                    ImageUrl = s.ImageUrl,
                    WorkingHours = s.WorkingHours,
                    Email = s.Email,
                    GoogleMapIframe = GoogleMapsHelper.GetGoogleMapIframe(s.Address, s.Province, s.City)
                })
                .ToListAsync();

            // Get filter options
            var allStores = await _context.Stores.Where(s => s.IsActive).ToListAsync();
            
            var viewModel = new StoreLocatorViewModel
            {
                Stores = stores,
                SelectedProvince = province,
                SelectedCity = city,
                SelectedBrand = brand,
                SelectedStoreType = storeType,
                SearchTerm = searchTerm,
                Provinces = allStores.Select(s => s.Province).Distinct().OrderBy(p => p).ToList(),
                Cities = allStores.Select(s => s.City).Distinct().OrderBy(c => c).ToList(),
                Brands = allStores.Select(s => s.Brand).Distinct().OrderBy(b => b).ToList(),
                StoreTypes = allStores.Select(s => s.StoreType).Distinct().OrderBy(st => st).ToList()
            };

            return View(viewModel);
        }

        // GET: Store/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (store == null)
            {
                return NotFound();
            }

            // Get nearby stores (same province)
            var nearbyStores = await _context.Stores
                .Where(s => s.IsActive && s.Id != id && s.Province == store.Province)
                .Take(5)
                .Select(s => new StoreItemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Phone = s.Phone,
                    City = s.City,
                    Province = s.Province,
                    Brand = s.Brand,
                    StoreType = s.StoreType,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    WorkingHours = s.WorkingHours,
                    GoogleMapIframe = GoogleMapsHelper.GetGoogleMapIframe(s.Address, s.Province, s.City)
                })
                .ToListAsync();

            var storeViewModel = new StoreItemViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address,
                Phone = store.Phone,
                City = store.City,
                Province = store.Province,
                Brand = store.Brand,
                StoreType = store.StoreType,
                Latitude = store.Latitude,
                Longitude = store.Longitude,
                Description = store.Description,
                ImageUrl = store.ImageUrl,
                WorkingHours = store.WorkingHours,
                Email = store.Email,
                GoogleMapIframe = GoogleMapsHelper.GetGoogleMapIframe(store.Address, store.Province, store.City)
            };

            var viewModel = new StoreDetailsViewModel
            {
                Store = storeViewModel,
                NearbyStores = nearbyStores
            };

            ViewData["Title"] = $"{store.Name} - Cửa hàng John Henry";
            ViewData["Description"] = $"Thông tin chi tiết cửa hàng {store.Name} tại {store.Address}, {store.City}, {store.Province}";

            return View(viewModel);
        }

        // AJAX: Search stores
        [HttpGet]
        public async Task<IActionResult> Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new StoreSearchResultViewModel());
            }

            var searchTerm = query.ToLower();
            var stores = await _context.Stores
                .Where(s => s.IsActive && 
                           (s.Name.ToLower().Contains(searchTerm) ||
                            s.Address.ToLower().Contains(searchTerm) ||
                            s.City.ToLower().Contains(searchTerm) ||
                            s.Province.ToLower().Contains(searchTerm)))
                .Take(10)
                .Select(s => new StoreItemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Phone = s.Phone,
                    City = s.City,
                    Province = s.Province,
                    Brand = s.Brand,
                    StoreType = s.StoreType,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    GoogleMapIframe = GoogleMapsHelper.GetGoogleMapIframe(s.Address, s.Province, s.City)
                })
                .ToListAsync();

            var result = new StoreSearchResultViewModel
            {
                Results = stores,
                TotalCount = stores.Count,
                SearchTerm = query
            };

            return Json(result);
        }

        // GET: All stores as JSON for map
        [HttpGet]
        public async Task<IActionResult> GetStoresJson()
        {
            var stores = await _context.Stores
                .Where(s => s.IsActive && s.Latitude.HasValue && s.Longitude.HasValue)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    address = s.Address,
                    phone = s.Phone,
                    city = s.City,
                    province = s.Province,
                    brand = s.Brand,
                    storeType = s.StoreType,
                    latitude = s.Latitude,
                    longitude = s.Longitude,
                    workingHours = s.WorkingHours ?? "8:30 - 22:00",
                    googleMapIframe = GoogleMapsHelper.GetGoogleMapIframe(s.Address, s.Province, s.City)
                })
                .ToListAsync();

            return Json(stores);
        }

        #region Private Methods

        private async Task InitializeStoresIfEmpty()
        {
            try
            {
                if (await _context.Stores.AnyAsync())
                    return;
            }
            catch (Exception)
            {
                // If table doesn't exist, ensure it's created via EF migrations
                try
                {
                    await _context.Database.EnsureCreatedAsync();
                }
                catch
                {
                    // If that also fails, skip initialization
                    return;
                }
            }

            // Parse CSV data and seed stores
            var stores = ParseStoreDataFromCsv();
            
            if (stores.Any())
            {
                _context.Stores.AddRange(stores);
                await _context.SaveChangesAsync();
            }
        }

        private List<Store> ParseStoreDataFromCsv()
        {
            var stores = new List<Store>();
            
            // Enhanced data structure based on actual CSV file
            var storeData = new[]
            {
                // Bình Phước
                new { Province = "Bình Phước", Address = "G14, Lê Thị Riêng, P. Tân Bình, TP. Đồng Xoài", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 11.5269, Lng = 106.9033 },
                new { Province = "Bình Phước", Address = "Đường DT 759, P. Phước Bình, Thị Xã Phước Long", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 11.7831, Lng = 106.9998 },
                
                // Bình Dương
                new { Province = "Bình Dương", Address = "53D Nguyễn Văn Tiết, P. Lái Thiêu, TP. Thuận An", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 10.9053, Lng = 106.7089 },
                new { Province = "Bình Dương", Address = "223 Nguyễn An Ninh, TP. Dĩ An", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0562.008.072", Lat = 10.9115, Lng = 106.7717 },
                
                // Đà Nẵng
                new { Province = "Đà Nẵng", Address = "26 Ngô Văn Sở, P. Hòa Khánh Bắc, Q. Liên Chiểu", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0947.967.346", Lat = 16.0763, Lng = 108.1600 },
                new { Province = "Đà Nẵng", Address = "267 Ông Ích Đường, P. Khuê Trung, Q. Cẩm Lệ", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0905.867.808", Lat = 16.0267, Lng = 108.1716 },
                new { Province = "Đà Nẵng", Address = "17 Nguyễn Văn Thoại, Q. Sơn Trà", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0989.357.594", Lat = 16.0678, Lng = 108.2442 },
                
                // Đăk Lăk
                new { Province = "Đăk Lăk", Address = "255 Lê Duẩn, P. Ea Tam, TP. Ban Mê Thuột", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 12.6844, Lng = 108.0380 },
                new { Province = "Đăk Lăk", Address = "21A Lê Duẩn, Thị Trấn Phước An, H. Krông Pắc", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0949.476.060", Lat = 12.9247, Lng = 108.6792 },
                new { Province = "Đăk Lăk", Address = "542 Hùng Vương, P. Buôn Hồ", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 12.9211, Lng = 108.2669 },
                
                // Đăk Nông
                new { Province = "Đăk Nông", Address = "20 Tôn Đức Thắng, P. Nghĩa Thành, TP. Gia Nghĩa", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0968.888.279", Lat = 12.0023, Lng = 107.6925 },
                new { Province = "Đăk Nông", Address = "Trục quốc lộ 28, Thôn 4, X. Quảng Khê, H. Đắk Glong", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 12.0625, Lng = 107.4067 },
                new { Province = "Đăk Nông", Address = "Thôn 10, X. Nam Bình, H. Đăk Song", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0365.960.979", Lat = 12.1389, Lng = 107.4889 },
                new { Province = "Đăk Nông", Address = "55A Lê Duẩn, Thị trấn Đắk Mil", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 12.3067, Lng = 107.5989 },
                
                // Đồng Nai
                new { Province = "Đồng Nai", Address = "14 Nguyễn Thị Minh Khai, P. Xuân An, TP. Long Khánh", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0343.888.914", Lat = 10.9467, Lng = 107.2267 },
                new { Province = "Đồng Nai", Address = "290 Lê Duẩn, Khu Văn Hải, Thị Trấn Long Thành, Huyện Long Thành", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 10.8189, Lng = 107.0244 },
                
                // Điện Biên
                new { Province = "Điện Biên", Address = "61 Phố 4, P. Mường Thanh, TP. Điện Biên Phủ", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 21.3844, Lng = 103.0169 },
                
                // Hà Tĩnh
                new { Province = "Hà Tĩnh", Address = "97 Phan Đình Phùng, P. Nam Hà, TP. Hà Tĩnh", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 18.3403, Lng = 105.9022 },
                
                // Hồ Chí Minh
                new { Province = "Hồ Chí Minh", Address = "372A Nguyễn Ảnh Thủ, P. Trung Mỹ Tây, Q.12", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0522.188.983", Lat = 10.8578, Lng = 106.6267 },
                
                // Kiên Giang
                new { Province = "Kiên Giang", Address = "289 Nguyễn Trung Trực, KP. 5, P. Đông Dương, TP. Phú Quốc", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 10.2267, Lng = 103.9675 },
                
                // Kon Tum
                new { Province = "Kon Tum", Address = "152 -154 Lê Hồng Phong, TP. Kon Tum", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0935.997.337", Lat = 14.3497, Lng = 107.9889 },
                
                // Long An
                new { Province = "Long An", Address = "69 Nguyễn Trung Trực, P. 2, TP. Tân An", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 10.5356, Lng = 106.4169 },
                
                // Ninh Bình
                new { Province = "Ninh Bình", Address = "209 Lương Văn Thăng, P. Đông Thành, TP. Ninh Bình", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 20.2600, Lng = 105.9744 },
                
                // Phú Yên
                new { Province = "Phú Yên", Address = "185 Phan Đình Phùng, P. 2, TP. Tuy Hòa", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0984.010.864", Lat = 13.0956, Lng = 109.2956 },
                
                // Quảng Bình
                new { Province = "Quảng Bình", Address = "39 Trần Hưng Đạo, TP. Đồng Hới", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 17.4822, Lng = 106.5989 },
                
                // Quảng Nam
                new { Province = "Quảng Nam", Address = "169 Lý Thường Kiệt, TP Hội An", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0931.919.522", Lat = 15.8797, Lng = 108.3350 },
                new { Province = "Quảng Nam", Address = "216 Trần Nhân Tông, P Vĩnh Điện, TX Điện Bàn", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0905.788.535", Lat = 15.9544, Lng = 108.2567 },
                new { Province = "Quảng Nam", Address = "90 Nguyễn Thành Hãn, TT. Nam Phước, H. Duy Xuyên", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 15.8033, Lng = 108.1867 },
                
                // Quảng Trị
                new { Province = "Quảng Trị", Address = "19 Hùng Vương, Khu phố 7, P. 1, TP. Đông Hà", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0982.850.027", Lat = 16.8156, Lng = 107.1022 },
                
                // Vĩnh Long
                new { Province = "Vĩnh Long", Address = "58 Phạm Thái Bường, P.4, TP. Vĩnh Long", Brand = "John Henry - Freelancer", Type = "Cửa hàng nhượng quyền", Phone = "0916.588.001", Lat = 10.2389, Lng = 105.9722 },
                
                // Vĩnh Phúc
                new { Province = "Vĩnh Phúc", Address = "200 Mê Linh, P. Liên Bảo, TP. Vĩnh Yên", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0585.661.857", Lat = 21.3089, Lng = 105.6056 },
                
                // Vũng Tàu
                new { Province = "Vũng Tàu", Address = "99 Đường 30 Tháng 4, P. Rạch Dừa, TP. Vũng Tàu", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "", Lat = 10.3467, Lng = 107.0844 },
                new { Province = "Vũng Tàu", Address = "346 Cách Mạng Tháng Tám, P.Phước Trung, TP. Bà Rịa", Brand = "John Henry", Type = "Cửa hàng nhượng quyền", Phone = "0978.796.046", Lat = 10.5133, Lng = 107.1833 },
                
                // Đối tác phân phối
                new { Province = "Điện Biên", Address = "65 Nguyễn Chí Thanh, Tổ 11, P. Mường Thanh, TP. Điện Biên Phủ", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 21.3844, Lng = 103.0169 },
                new { Province = "Điện Biên", Address = "988 Tổ 6, P. Mường Thanh, TP. Điện Biên Phủ", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 21.3844, Lng = 103.0169 },
                new { Province = "Hà Nội", Address = "117 Tổ 1 Miếu Thờ Tiền Dược, H. Sóc Sơn", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 21.2467, Lng = 105.8333 },
                new { Province = "Hà Nội", Address = "73 Cao Lỗ, Thị trấn Đông Anh", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 21.1389, Lng = 105.8333 },
                new { Province = "Thái Bình", Address = "220 Lê Quý Đôn, P. Bồ Xuyên, TP. Thái Bình", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 20.4467, Lng = 106.3367 },
                new { Province = "Đồng Nai", Address = "Km 117, Quốc Lộ 20, Ấp 5, Xã Phú Vinh, H. Ðịnh Quán", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 11.0500, Lng = 107.1333 },
                new { Province = "Hồ Chí Minh", Address = "1016 Phạm Văn Đồng, P.Hiệp Bình Chánh, TP. Thủ Đức", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 10.8578, Lng = 106.7833 },
                new { Province = "Quảng Bình", Address = "Thôn Thanh Gianh, X. Thanh Trạch, H. Bố Trạch", Brand = "John Henry", Type = "Đối tác phân phối", Phone = "", Lat = 17.4822, Lng = 106.5989 }
            };

            foreach (var item in storeData)
            {
                var store = new Store
                {
                    Id = Guid.NewGuid(),
                    Name = GenerateStoreName(item.Brand, item.Province, item.Type),
                    Address = item.Address,
                    Phone = string.IsNullOrEmpty(item.Phone) ? null : item.Phone,
                    City = ExtractCityFromAddress(item.Address),
                    Province = item.Province,
                    Brand = item.Brand,
                    StoreType = item.Type,
                    Latitude = item.Lat,
                    Longitude = item.Lng,
                    IsActive = true,
                    WorkingHours = "8:30 - 22:00",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                stores.Add(store);
            }

            return stores;
        }

        private string GenerateStoreName(string brand, string province, string type)
        {
            var storeTypeName = type.Contains("Đối tác") ? "Đại lý" : "Cửa hàng";
            return $"{storeTypeName} {brand} {province}";
        }

        private string ExtractCityFromAddress(string address)
        {
            // Simple extraction logic - could be improved
            var parts = address.Split(',');
            if (parts.Length >= 2)
            {
                var lastPart = parts.Last().Trim();
                if (lastPart.StartsWith("TP.") || lastPart.StartsWith("Q.") || lastPart.StartsWith("H."))
                    return lastPart;
                
                var secondLastPart = parts[parts.Length - 2].Trim();
                if (secondLastPart.StartsWith("TP.") || secondLastPart.StartsWith("Q.") || secondLastPart.StartsWith("H."))
                    return secondLastPart;
            }
            return "Chưa xác định";
        }

        #endregion
    }
}