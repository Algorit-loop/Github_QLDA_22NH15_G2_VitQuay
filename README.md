# 🎓 AppEL - Nền tảng quản lý và bán khóa học trực tuyến

> Một hệ thống ASP.NET Core MVC cho phép quản lý khóa học, người dùng và bán khóa học online. Hỗ trợ 3 loại tài khoản: **Admin**, **Instructor**, và **Student**.

---

## 🚀 Tính năng chính

- 🔐 Xác thực & phân quyền người dùng theo 3 vai trò:
  - **Admin**: Quản lý danh mục, người dùng, và toàn bộ hệ thống
  - **Instructor**: Tạo và quản lý khóa học của mình
  - **Student**: Đăng ký, theo dõi và học khóa học đã đăng ký

- 📚 Quản lý khóa học:
  - Tạo, sửa, xoá khóa học
  - Quản lý danh mục (category)
  - Đăng ký / huỷ đăng ký khóa học

- 💾 Lưu trữ dữ liệu bằng JSON (các file: `courses.json`, `users.json`, `enrollments.json`, ...)

- 🌐 Giao diện Razor (CSHTML) thân thiện, chia theo vai trò

---

## 🧰 Công nghệ sử dụng

- ⚙️ **ASP.NET Core MVC (.NET 8.0)**
- 💾 Lưu trữ dữ liệu bằng file JSON (thay vì database)
- 🖥 Razor Views (CSHTML)
- 🎨 CSS thuần (file `site.css`)
- 👤 Hệ thống phân quyền theo role
- 📂 Cấu trúc thư mục rõ ràng: Controllers, Models, Services, ViewModels, Views

---

## 📁 Cấu trúc thư mục

```bash
├── Controllers/              # Controller cho Account, Admin, Course, Lesson...
├── Data/                     # Dữ liệu JSON: users, courses, categories...
├── Models/                   # Class mô hình: Course, Category, User, Enrollment...
├── Services/                 # Các service xử lý logic dữ liệu
├── ViewModels/              # Các lớp hỗ trợ dữ liệu cho view
├── Views/                    # Razor view, chia theo thư mục: Account, Admin, Course...
├── wwwroot/                  # Tài nguyên tĩnh: CSS, JS, ảnh khóa học
├── appsettings.json          # Cấu hình ứng dụng
└── AppEL.csproj              # File cấu hình dự án .NET
```

---

## 📦 Cài đặt & chạy ứng dụng

### 🔧 Yêu cầu

- .NET SDK 8.0+
- Visual Studio 2022 hoặc VS Code

### ▶️ Chạy ứng dụng

```bash
# 1. Mở terminal/cmd tại thư mục dự án
# 2. Chạy lệnh:
dotnet run
```

Truy cập trình duyệt tại: `http://localhost:xxxx` (port sẽ được in ra khi chạy)

---

## 🧪 Dữ liệu mẫu

- Dữ liệu người dùng, khóa học, danh mục và đăng ký được lưu trong:
  - `Data/users.json`
  - `Data/courses.json`
  - `Data/categories.json`
  - `Data/enrollments.json`

> 🔑 Bạn có thể chỉnh sửa các file này để tạo tài khoản hoặc khóa học mẫu nếu cần.

---

## 👤 Vai trò người dùng (Roles)

| Role        | Quyền hạn chính                            |
|-------------|--------------------------------------------|
| Admin       | Quản lý toàn bộ hệ thống, người dùng, danh mục |
| Instructor  | Tạo & chỉnh sửa khóa học của mình           |
| Student     | Xem, đăng ký và học khóa học                |

---

## 📄 Giấy phép

Dự án mang tính học tập. Có thể sử dụng và sửa đổi cho mục đích cá nhân hoặc trong tổ chức.

---

## ✍️ Tác giả

