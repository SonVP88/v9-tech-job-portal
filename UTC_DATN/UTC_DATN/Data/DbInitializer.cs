using Microsoft.EntityFrameworkCore;
using UTC_DATN.Entities;

namespace UTC_DATN.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(UTC_DATNContext context)
        {
            await context.Database.EnsureCreatedAsync();

            await SeedDepartmentsAsync(context);
            await SeedRolesAsync(context);
            await SeedUsersAsync(context);
        }

        // ════════════════════════════════════════════════════════════════════════
        // SEED: 7 Phòng ban / Khu vực chuẩn Nhà máy Công nghiệp
        // ════════════════════════════════════════════════════════════════════════
        private static async Task SeedDepartmentsAsync(UTC_DATNContext context)
        {
            if (await context.Departments.AnyAsync()) return;

            var departments = new List<Department>
            {
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000001"),
                    DepartmentCode = "ADMIN_SYS",
                    DepartmentName = "Ban Giám Đốc",
                    Description    = "Cấp quản lý cao nhất – Ban Giám đốc và Quản trị hệ thống ATS.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000002"),
                    DepartmentCode = "HR_TEAM",
                    DepartmentName = "Phòng Nhân Sự (HR)",
                    Description    = "Tuyển dụng, sàng lọc hồ sơ, quản lý pipeline ứng viên toàn nhà máy.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000003"),
                    DepartmentCode = "INT_TEAM",
                    DepartmentName = "Hội Đồng Phỏng Vấn",
                    Description    = "Thực hiện phỏng vấn chuyên môn, chấm điểm và phản hồi ứng viên.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000004"),
                    DepartmentCode = "PROD_LINE",
                    DepartmentName = "Xưởng Sản Xuất & Lắp Ráp",
                    Description    = "Trực tiếp sản xuất, lắp ráp sản phẩm theo dây chuyền công nghiệp.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000005"),
                    DepartmentCode = "QA_QC",
                    DepartmentName = "Phòng Quản Lý Chất Lượng (QA/QC)",
                    Description    = "Kiểm tra, kiểm soát chất lượng sản phẩm đầu vào và đầu ra.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000006"),
                    DepartmentCode = "WH_LOGS",
                    DepartmentName = "Khu Vực Kho & Logistics",
                    Description    = "Quản lý kho hàng, vận chuyển, xuất-nhập kho tại khu công nghiệp.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                },
                new Department
                {
                    DepartmentId   = new Guid("10000000-0000-0000-0000-000000000007"),
                    DepartmentCode = "TECH_MNT",
                    DepartmentName = "Phòng Kỹ Thuật & Bảo Trì",
                    Description    = "Vận hành, bảo dưỡng máy móc, hệ thống điện-cơ trong khu sản xuất.",
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow
                }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Seeded 7 Departments (nhà máy công nghiệp)");
        }

        private static async Task SeedRolesAsync(UTC_DATNContext context)
        {
            if (await context.Roles.AnyAsync()) return;

            var roles = new List<Role>
            {
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000001"), Code = "ADMIN",       Name = "Ban Giám Đốc / Administrator", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000002"), Code = "MANAGER",     Name = "Trưởng Phòng / Quản Lý",       CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000003"), Code = "HR",          Name = "Chuyên Viên Nhân Sự",           CreatedAt = DateTime.UtcNow },
                new Role { RoleId = new Guid("20000000-0000-0000-0000-000000000004"), Code = "INTERVIEWER", Name = "Hội Đồng Phỏng Vấn",            CreatedAt = DateTime.UtcNow }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Seeded 4 Roles: ADMIN, MANAGER, HR, INTERVIEWER");
        }

        // ════════════════════════════════════════════════════════════════════════
        // SEED: Full matrix – Nhân sự đầy đủ cấu trúc phân cấp nhà máy
        // ════════════════════════════════════════════════════════════════════════
        private static async Task SeedUsersAsync(UTC_DATNContext context)
        {
            if (await context.Users.AnyAsync()) return;

            // ─── Định nghĩa các Guid cố định để tham chiếu ManagerId ───────────
            // Ban Giám Đốc
            var uGiamDoc          = new Guid("30000000-0000-0000-0000-000000000001");

            // Trưởng Phòng Nhân Sự
            var uTruongHR         = new Guid("30000000-0000-0000-0000-000000000010");
            // Trưởng Hội Đồng Phỏng Vấn
            var uTruongPV         = new Guid("30000000-0000-0000-0000-000000000020");
            // Trưởng Xưởng Sản Xuất
            var uTruongSX         = new Guid("30000000-0000-0000-0000-000000000030");
            // Trưởng Phòng QA/QC
            var uTruongQA         = new Guid("30000000-0000-0000-0000-000000000040");
            // Trưởng Kho & Logistics
            var uTruongKho        = new Guid("30000000-0000-0000-0000-000000000050");
            // Trưởng Phòng Kỹ Thuật
            var uTruongKT         = new Guid("30000000-0000-0000-0000-000000000060");

            var pw = BCrypt.Net.BCrypt.HashPassword("Password123!");

            // ─── DEPT IDs ─────────────────────────────────────────────────────
            var dAdmin  = new Guid("10000000-0000-0000-0000-000000000001");
            var dHR     = new Guid("10000000-0000-0000-0000-000000000002");
            var dPV     = new Guid("10000000-0000-0000-0000-000000000003");
            var dSX     = new Guid("10000000-0000-0000-0000-000000000004");
            var dQA     = new Guid("10000000-0000-0000-0000-000000000005");
            var dKho    = new Guid("10000000-0000-0000-0000-000000000006");
            var dKT     = new Guid("10000000-0000-0000-0000-000000000007");

            // ─── ROLE IDs ─────────────────────────────────────────────────────
            var rAdmin  = new Guid("20000000-0000-0000-0000-000000000001");
            var rMgr    = new Guid("20000000-0000-0000-0000-000000000002");
            var rHR     = new Guid("20000000-0000-0000-0000-000000000003");
            var rPV     = new Guid("20000000-0000-0000-0000-000000000004");

            // ─── Danh sách Users ──────────────────────────────────────────────
            var users = new List<(User user, Guid roleId)>
            {
                // ===== BAN GIÁM ĐỐC =====
                (new User
                {
                    UserId = uGiamDoc,
                    Email = "giamdoc@factory.com", NormalizedEmail = "GIAMDOC@FACTORY.COM",
                    PasswordHash = pw, FullName = "Nguyễn Văn Giám Đốc", Phone = "0901000001",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dAdmin, ManagerId = null
                }, rAdmin),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000002"),
                    Email = "pho.giamdoc@factory.com", NormalizedEmail = "PHO.GIAMDOC@FACTORY.COM",
                    PasswordHash = pw, FullName = "Trần Thị Phó Giám Đốc", Phone = "0901000002",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dAdmin, ManagerId = uGiamDoc
                }, rAdmin),

                // ===== PHÒNG NHÂN SỰ (HR) =====
                (new User
                {
                    UserId = uTruongHR,
                    Email = "truong.hr@factory.com", NormalizedEmail = "TRUONG.HR@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lê Thị Trưởng HR", Phone = "0901000010",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000011"),
                    Email = "hr1@factory.com", NormalizedEmail = "HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Phạm Văn Nhân Sự 1", Phone = "0901000011",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000012"),
                    Email = "hr2@factory.com", NormalizedEmail = "HR2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Hoàng Thị Nhân Sự 2", Phone = "0901000012",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000013"),
                    Email = "hr3@factory.com", NormalizedEmail = "HR3@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đỗ Văn Nhân Sự 3", Phone = "0901000013",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dHR, ManagerId = uTruongHR
                }, rHR),

                // ===== HỘI ĐỒNG PHỎNG VẤN =====
                (new User
                {
                    UserId = uTruongPV,
                    Email = "truong.pv@factory.com", NormalizedEmail = "TRUONG.PV@FACTORY.COM",
                    PasswordHash = pw, FullName = "Ngô Văn Trưởng Phỏng Vấn", Phone = "0901000020",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000021"),
                    Email = "pv1@factory.com", NormalizedEmail = "PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Vũ Thị Phỏng Vấn Viên 1", Phone = "0901000021",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000022"),
                    Email = "pv2@factory.com", NormalizedEmail = "PV2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Bùi Văn Phỏng Vấn Viên 2", Phone = "0901000022",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000023"),
                    Email = "pv3@factory.com", NormalizedEmail = "PV3@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đinh Thị Phỏng Vấn Viên 3", Phone = "0901000023",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dPV, ManagerId = uTruongPV
                }, rPV),

                // ===== XƯỞNG SẢN XUẤT & LẮP RÁP =====
                (new User
                {
                    UserId = uTruongSX,
                    Email = "truong.sx@factory.com", NormalizedEmail = "TRUONG.SX@FACTORY.COM",
                    PasswordHash = pw, FullName = "Trịnh Văn Trưởng Xưởng", Phone = "0901000030",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000031"),
                    Email = "sx.hr1@factory.com", NormalizedEmail = "SX.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lý Thị HR Xưởng 1", Phone = "0901000031",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uTruongSX
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000032"),
                    Email = "sx.pv1@factory.com", NormalizedEmail = "SX.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Mai Văn PV Xưởng 1", Phone = "0901000032",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dSX, ManagerId = uTruongSX
                }, rPV),

                // ===== PHÒNG QUẢN LÝ CHẤT LƯỢNG (QA/QC) =====
                (new User
                {
                    UserId = uTruongQA,
                    Email = "truong.qa@factory.com", NormalizedEmail = "TRUONG.QA@FACTORY.COM",
                    PasswordHash = pw, FullName = "Phan Thị Trưởng QA", Phone = "0901000040",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000041"),
                    Email = "qa.hr1@factory.com", NormalizedEmail = "QA.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Tô Văn HR Chất Lượng 1", Phone = "0901000041",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uTruongQA
                }, rHR),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000042"),
                    Email = "qa.pv1@factory.com", NormalizedEmail = "QA.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Cao Thị PV Chất Lượng 1", Phone = "0901000042",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dQA, ManagerId = uTruongQA
                }, rPV),

                // ===== KHU VỰC KHO & LOGISTICS =====
                (new User
                {
                    UserId = uTruongKho,
                    Email = "truong.kho@factory.com", NormalizedEmail = "TRUONG.KHO@FACTORY.COM",
                    PasswordHash = pw, FullName = "Lương Văn Trưởng Kho", Phone = "0901000050",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKho, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000051"),
                    Email = "kho.hr1@factory.com", NormalizedEmail = "KHO.HR1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Đặng Thị HR Kho 1", Phone = "0901000051",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKho, ManagerId = uTruongKho
                }, rHR),

                // ===== PHÒNG KỸ THUẬT & BẢO TRÌ =====
                (new User
                {
                    UserId = uTruongKT,
                    Email = "truong.kt@factory.com", NormalizedEmail = "TRUONG.KT@FACTORY.COM",
                    PasswordHash = pw, FullName = "Hứa Văn Trưởng Kỹ Thuật", Phone = "0901000060",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uGiamDoc
                }, rMgr),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000061"),
                    Email = "kt.pv1@factory.com", NormalizedEmail = "KT.PV1@FACTORY.COM",
                    PasswordHash = pw, FullName = "Dương Thị PV Kỹ Thuật 1", Phone = "0901000061",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uTruongKT
                }, rPV),

                (new User
                {
                    UserId = new Guid("30000000-0000-0000-0000-000000000062"),
                    Email = "kt.pv2@factory.com", NormalizedEmail = "KT.PV2@FACTORY.COM",
                    PasswordHash = pw, FullName = "Thái Văn PV Kỹ Thuật 2", Phone = "0901000062",
                    IsActive = true, CreatedAt = DateTime.UtcNow, DepartmentId = dKT, ManagerId = uTruongKT
                }, rPV),
            };

            // Thêm tất cả users vào context
            await context.Users.AddRangeAsync(users.Select(u => u.user));
            await context.SaveChangesAsync();

            // Gán roles
            var userRoles = users.Select(u => new UserRole
            {
                UserId    = u.user.UserId,
                RoleId    = u.roleId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();

            Console.WriteLine($"✓ Seeded {users.Count} Users với đầy đủ cấu trúc phân cấp nhà máy.");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine("  📧 TÀI KHOẢN ĐĂNG NHẬP (mật khẩu: Password123!)");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
            Console.WriteLine("  [ADMIN]    giamdoc@factory.com        → Ban Giám Đốc");
            Console.WriteLine("  [ADMIN]    pho.giamdoc@factory.com    → Ban Giám Đốc");
            Console.WriteLine("  [MANAGER]  truong.hr@factory.com      → Phòng Nhân Sự (HR)");
            Console.WriteLine("  [MANAGER]  truong.pv@factory.com      → Hội Đồng Phỏng Vấn");
            Console.WriteLine("  [MANAGER]  truong.sx@factory.com      → Xưởng Sản Xuất");
            Console.WriteLine("  [MANAGER]  truong.qa@factory.com      → Phòng QA/QC");
            Console.WriteLine("  [MANAGER]  truong.kho@factory.com     → Khu Vực Kho");
            Console.WriteLine("  [MANAGER]  truong.kt@factory.com      → Phòng Kỹ Thuật");
            Console.WriteLine("  [HR]       hr1@factory.com            → Phòng Nhân Sự");
            Console.WriteLine("  [HR]       hr2@factory.com            → Phòng Nhân Sự");
            Console.WriteLine("  [PV]       pv1@factory.com            → Hội Đồng Phỏng Vấn");
            Console.WriteLine("  [PV]       pv2@factory.com            → Hội Đồng Phỏng Vấn");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
        }
    }
}
