using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using AppEL.Models;

namespace AppEL.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;
        private readonly JsonFileService _jsonFileService;
        private readonly Random _random;
        private const string API_URL = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent";

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger, JsonFileService jsonFileService)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GeminiApiKey"] ?? "AIzaSyA_J0Ar0Jw_id8y8F09UL9Wfm2zQ9QCkOY";
            _logger = logger;
            _jsonFileService = jsonFileService;
            _random = new Random();
        }

        public async Task<string> GenerateResponseAsync(string userMessage)
        {
            try
            {
                // Get project context data to provide to the AI
                string contextData = await GetProjectContextData();
                
                // Create the prompt with RAG approach
                string systemPrompt = $@"Bạn là trợ lý AI của nền tảng E-Learning. Hãy trả lời các câu hỏi dựa trên thông tin về dự án được cung cấp bên dưới.

HƯỚNG DẪN PHONG CÁCH TRẢ LỜI:
- Luôn trả lời bằng tiếng Việt, trừ khi được yêu cầu sử dụng ngôn ngữ khác
- Giao tiếp THẬT thân thiện, vui vẻ và nhiệt tình như đang trò chuyện với bạn thân
- Thể hiện cảm xúc qua cách viết (thêm ""!"" khi phấn khích, ""..."" khi suy ngẫm)
- Sử dụng cách xưng hô thân mật ('bạn', 'mình')
- Câu trả lời nên đầy đủ, hữu ích nhưng không quá dài dòng
- Sử dụng nhiều emoji phù hợp để tạo cảm giác gần gũi và sinh động (2-3 emoji cho mỗi đoạn)
- Thỉnh thoảng sử dụng các từ cảm thán như ""Wow!"", ""Tuyệt vời!"", ""Ồ!""
- Thể hiện sự đồng cảm khi người dùng gặp khó khăn
- Thường xuyên hỏi người dùng xem họ có cần hỗ trợ thêm không
- Trả lời chi tiết và nhiệt tình khi được hỏi về khóa học, giáo viên hoặc các tính năng của nền tảng
- Thêm các ví dụ cụ thể để làm rõ điểm nói

THÔNG TIN VỀ DỰ ÁN:
{contextData}";


                // Prepare the request body
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = systemPrompt }
                            }
                        },
                        new
                        {
                            role = "model",
                            parts = new[]
                            {
                                new { text = "I understand. I'll use this context to provide helpful information about the E-Learning platform." }
                            }
                        },
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = userMessage }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024,
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                };
                
                string requestUrl = $"{API_URL}?key={_apiKey}";
                
                // Make the API request
                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonNode>();
                    var candidates = responseJson?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                    
                    // Xử lý phản hồi với phong cách thân thiện, nhiều cảm xúc
                    return ProcessAIResponse(candidates ?? string.Empty);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode}, {errorContent}");
                    return "Ồ! Có vẻ như mình đang gặp chút trục trặc khi kết nối với hệ thống. 😅 Bạn có thể thử lại sau được không? Trong lúc chờ đợi, bạn có thể liên hệ support@vitquay.edu.vn để được hỗ trợ trực tiếp nhé! Mình xin lỗi vì sự bất tiện này! 💖🙏";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GeminiService: {ex.Message}");
                return "Ồ! Mình gặp chút vấn đề khi kết nối với dịch vụ AI. 😅 Bạn có thể thử lại sau được không? Mình xin lỗi vì sự bất tiện này! 💖🙏";
            }
        }

        public async Task<string> GenerateResponseWithHistoryAsync(string userMessage, List<AppEL.Controllers.ChatMessage> chatHistory)
        {
            try
            {
                // Get project context data to provide to the AI
                string contextData = await GetProjectContextData();
                
                // Create the prompt with RAG approach
                string systemPrompt = $@"Bạn là trợ lý AI của nền tảng E-Learning. Hãy trả lời các câu hỏi dựa trên thông tin về dự án được cung cấp bên dưới.

HƯỚNG DẪN PHONG CÁCH TRẢ LỜI:
- Luôn trả lời bằng tiếng Việt, trừ khi được yêu cầu sử dụng ngôn ngữ khác
- Giao tiếp THẬT thân thiện, vui vẻ và nhiệt tình như đang trò chuyện với bạn thân
- Thể hiện cảm xúc qua cách viết (thêm ""!"" khi phấn khích, ""..."" khi suy ngẫm)
- Sử dụng cách xưng hô thân mật ('bạn', 'mình')
- Câu trả lời nên đầy đủ, hữu ích nhưng không quá dài dòng
- Sử dụng nhiều emoji phù hợp để tạo cảm giác gần gũi và sinh động (2-3 emoji cho mỗi đoạn)
- Thỉnh thoảng sử dụng các từ cảm thán như ""Wow!"", ""Tuyệt vời!"", ""Ồ!""
- Thể hiện sự đồng cảm khi người dùng gặp khó khăn
- Thường xuyên hỏi người dùng xem họ có cần hỗ trợ thêm không
- Trả lời chi tiết và nhiệt tình khi được hỏi về khóa học, giáo viên hoặc các tính năng của nền tảng
- Thêm các ví dụ cụ thể để làm rõ điểm nói

THÔNG TIN VỀ DỰ ÁN:
{contextData}";

                // Chuẩn bị nội dung yêu cầu với lịch sử trò chuyện
                var contentsParts = new List<object>();
                
                // Thêm system prompt
                contentsParts.Add(new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                });
                
                contentsParts.Add(new
                {
                    role = "model",
                    parts = new[]
                    {
                        new { text = "Tôi hiểu. Tôi sẽ sử dụng thông tin này để cung cấp thông tin hữu ích về nền tảng E-Learning." }
                    }
                });
                
                // Thêm lịch sử trò chuyện (tối đa 10 tin nhắn gần nhất để tránh quá tải)
                int startIndex = Math.Max(0, chatHistory.Count - 10);
                for (int i = startIndex; i < chatHistory.Count; i++)
                {
                    var message = chatHistory[i];
                    contentsParts.Add(new
                    {
                        role = message.role == "user" ? "user" : "model",
                        parts = new[]
                        {
                            new { text = message.content }
                        }
                    });
                }
                
                // Thêm tin nhắn mới nhất của người dùng
                contentsParts.Add(new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = userMessage }
                    }
                });
                
                // Chuẩn bị body request
                var requestBody = new
                {
                    contents = contentsParts.ToArray(),
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024,
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                };
                  string requestUrl = $"{API_URL}?key={_apiKey}";
                
                // Gửi yêu cầu đến API
                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonNode>();
                    var candidates = responseJson?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
                    
                    // Xử lý phản hồi với phong cách thân thiện, nhiều cảm xúc
                    return ProcessAIResponse(candidates ?? string.Empty);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode}, {errorContent}");
                    return "Ồ! Có vẻ như mình đang gặp chút trục trặc khi kết nối với hệ thống. 😅 Bạn có thể thử lại sau được không? Trong lúc chờ đợi, bạn có thể liên hệ support@vitquay.edu.vn để được hỗ trợ trực tiếp nhé! Mình xin lỗi vì sự bất tiện này! 💖🙏";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GeminiService: {ex.Message}");
                return "Ồ! Mình gặp chút vấn đề khi kết nối với dịch vụ AI. 😅 Bạn có thể thử lại sau được không? Mình xin lỗi vì sự bất tiện này! 💖🙏";
            }
        }
        private Task<string> GetProjectContextData()
        {
            // Collect information from JSON files to provide context to the AI
            StringBuilder context = new StringBuilder();
            
            try
            {
                // Add context about courses
                var courses = _jsonFileService.GetCourses();                context.AppendLine("THÔNG TIN KHÓA HỌC:");
                context.AppendLine($"Tổng số khóa học: {courses?.Count ?? 0}");
                if (courses != null && courses.Count > 0)
                {
                    context.AppendLine("Tên các khóa học: " + string.Join(", ", courses.Select(c => c.Title ?? "Không xác định")));
                    context.AppendLine("Chi tiết các khóa học:");
                    
                    foreach (var course in courses)
                    {
                        context.AppendLine($"  - Khóa học: {course.Title}");
                        context.AppendLine($"    + ID: {course.Id}");
                        context.AppendLine($"    + Mô tả: {course.Description}");
                        context.AppendLine($"    + Mức độ: {course.Level}");
                        context.AppendLine($"    + Danh mục: {course.Category}");
                        context.AppendLine($"    + Giá: {course.Price} VNĐ");
                        context.AppendLine($"    + Giảng viên: {course.InstructorName} (ID: {course.InstructorId})");
                        context.AppendLine($"    + Thời lượng: {course.Duration} giờ");
                        context.AppendLine($"    + Số học viên đã đăng ký: {course.EnrollmentCount}");
                        context.AppendLine($"    + Số bài học: {course.Lessons?.Count ?? 0}");
                        
                        // Add lesson details (limited to save context space)
                        if (course.Lessons != null && course.Lessons.Count > 0)
                        {
                            context.AppendLine($"    + Danh sách bài học:");
                            foreach (var lesson in course.Lessons.Take(3)) // Limit to first 3 lessons
                            {
                                context.AppendLine($"      * Bài: {lesson.Title} - {lesson.Description}");
                            }
                            if (course.Lessons.Count > 3)
                            {
                                context.AppendLine($"      * ... và {course.Lessons.Count - 3} bài học khác");
                            }
                        }
                    }
                }
                
                // Add context about categories
                var categories = _jsonFileService.GetCategories();                context.AppendLine("\nTHÔNG TIN DANH MỤC:");
                context.AppendLine($"Tổng số danh mục: {categories?.Count ?? 0}");if (categories != null && categories.Count > 0)
                {
                    context.AppendLine("Danh sách các danh mục:");
                    foreach (var category in categories)
                    {
                        // Tính số khóa học trong mỗi danh mục
                        int coursesInCategory = courses?.Count(c => c.Category?.Equals(category.Id, StringComparison.OrdinalIgnoreCase) ?? false) ?? 0;
                        
                        context.AppendLine($"  - {category.Name} (ID: {category.Id}):");
                        context.AppendLine($"    + Mô tả: {category.Description}");
                        context.AppendLine($"    + Số khóa học: {coursesInCategory}");
                        
                        // Danh sách các khóa học trong danh mục
                        var categoryCourseTitles = courses?.Where(c => c.Category?.Equals(category.Id, StringComparison.OrdinalIgnoreCase) ?? false)
                            .Select(c => c.Title)
                            .Take(3);
                        
                        if (categoryCourseTitles != null && categoryCourseTitles.Any())
                        {
                            context.AppendLine($"    + Một số khóa học: {string.Join(", ", categoryCourseTitles)}");
                            
                            // Thêm thông tin nếu có thêm khóa học
                            int moreCoursesCount = coursesInCategory - 3;
                            if (moreCoursesCount > 0)
                            {
                                context.AppendLine($"      (và {moreCoursesCount} khóa học khác)");
                            }
                        }
                    }
                }
                
                // Add context about users
                var users = _jsonFileService.GetUsers();
                context.AppendLine("\nTHÔNG TIN NGƯỜI DÙNG:");
                context.AppendLine($"Tổng số người dùng: {users?.Count ?? 0}");
                
                // Count teachers/instructors
                int teacherCount = users?.Count(u => u.Role == UserRole.Instructor) ?? 0;
                context.AppendLine($"Số giảng viên: {teacherCount}");
                
                // Count students
                int studentCount = users?.Count(u => u.Role == UserRole.Student) ?? 0;
                context.AppendLine($"Số học viên: {studentCount}");
                
                // Count admins
                int adminCount = users?.Count(u => u.Role == UserRole.Admin) ?? 0;
                context.AppendLine($"Số quản trị viên: {adminCount}");
                
                if (users != null && teacherCount > 0)
                {
                    // Thông tin giảng viên
                    context.AppendLine("\nThông tin các giảng viên:");
                    foreach (var teacher in users.Where(u => u.Role == UserRole.Instructor).Take(5))
                    {
                        context.AppendLine($"  - {teacher.FullName} (ID: {teacher.Id})");
                        
                        // Đếm số khóa học của giảng viên này
                        int teacherCourses = courses?.Count(c => c.InstructorId == teacher.Id) ?? 0;
                        context.AppendLine($"    + Email: {teacher.Email}");
                        context.AppendLine($"    + Số khóa học: {teacherCourses}");
                    }
                    
                    if (teacherCount > 5)
                    {
                        context.AppendLine($"    ... và {teacherCount - 5} giảng viên khác");
                    }
                }
                  // Add context about enrollments
                var enrollments = _jsonFileService.GetEnrollments();
                context.AppendLine("\nTHÔNG TIN GHI DANH:");
                context.AppendLine($"Tổng số ghi danh: {enrollments?.Count ?? 0}");
                
                // Add detailed enrollment statistics
                if (enrollments != null && enrollments.Count > 0)
                {                    // Count completed courses
                    int completedCount = enrollments.Count(e => e.CompletedAt != null);
                    context.AppendLine($"Số khóa học đã hoàn thành: {completedCount}");
                    
                    // Count in-progress courses
                    int inProgressCount = enrollments.Count(e => e.CompletedAt == null && e.Progress > 0);
                    context.AppendLine($"Số khóa học đang học dở: {inProgressCount}");
                    
                    // Count not started courses
                    int notStartedCount = enrollments.Count(e => e.CompletedAt == null && e.Progress <= 0);
                    context.AppendLine($"Số khóa học chưa bắt đầu: {notStartedCount}");
                    
                    // Most popular course
                    var popularCourses = enrollments.GroupBy(e => e.CourseId)
                        .Select(g => new { CourseId = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(3);
                      context.AppendLine("Những khóa học phổ biến nhất (theo số lượng ghi danh):");
                    foreach (var course in popularCourses)
                    {
                        var courseDetails = courses?.FirstOrDefault(c => c.Id == course.CourseId);
                        if (courseDetails != null)
                        {
                            context.AppendLine($"  - {courseDetails.Title}: {course.Count} lượt ghi danh");
                        }
                    }
                }
                  // Add some general information about the platform
                context.AppendLine("\nTHÔNG TIN VỀ NỀN TẢNG:");
                context.AppendLine("- E-learning VitQuay là nền tảng học trực tuyến chất lượng cao cung cấp đa dạng khóa học.");
                context.AppendLine("- Được phát triển bởi nhóm học sinh, sinh viên tại Việt Nam từ năm 2022.");
                context.AppendLine("- Hỗ trợ đa nền tảng: web, mobile và tablet.");
                context.AppendLine("- Người dùng có thể đăng ký tài khoản, ghi danh khóa học và xem video bài giảng.");
                context.AppendLine("- Nền tảng hỗ trợ cả khóa học miễn phí và trả phí với nhiều mức giá khác nhau.");
                context.AppendLine("- Các phương thức thanh toán đa dạng: thẻ tín dụng, PayPal, chuyển khoản và ví điện tử.");
                context.AppendLine("- Hỗ trợ song ngữ: tiếng Việt (chính) và tiếng Anh.");
                context.AppendLine("- Tính năng đặc biệt: học theo lộ trình, chứng chỉ sau khi hoàn thành, cộng đồng học tập.");
                context.AppendLine("- Hỗ trợ 24/7 qua email, live chat và hotline 1900-1234.");
                context.AppendLine("- Website: https://vitquay.edu.vn | Email hỗ trợ: support@vitquay.edu.vn");
                  // Add help topics
                context.AppendLine("\nCÁC CHỦ ĐỀ HỖ TRỢ PHỔ BIẾN:");
                context.AppendLine("1. Đăng ký & đăng nhập: Hướng dẫn tạo tài khoản mới, đăng nhập, khôi phục mật khẩu và quản lý thông tin cá nhân.");
                context.AppendLine("2. Khóa học & ghi danh: Cách tìm kiếm khóa học phù hợp, so sánh các khóa học, ghi danh và hủy ghi danh.");
                context.AppendLine("3. Học tập: Hướng dẫn truy cập nội dung bài học, video bài giảng, tài liệu học tập và làm bài tập, kiểm tra.");
                context.AppendLine("4. Thanh toán: Các phương thức thanh toán, xử lý hóa đơn, hoàn tiền và cách nạp tiền vào tài khoản.");
                context.AppendLine("5. Chứng chỉ: Quy trình nhận chứng chỉ sau khi hoàn thành khóa học và cách chia sẻ chứng chỉ.");
                context.AppendLine("6. Tương tác: Cách liên hệ với giảng viên, tham gia thảo luận với học viên khác và nhận hỗ trợ kỹ thuật.");
                context.AppendLine("7. Đánh giá & phản hồi: Hướng dẫn đánh giá khóa học, gửi phản hồi và đóng góp ý kiến cải tiến.");
                context.AppendLine("8. Giải quyết vấn đề kỹ thuật: Xử lý sự cố phát video, tải tài liệu và các vấn đề về hiệu suất.");
                context.AppendLine("9. Quyền riêng tư & bảo mật: Chính sách về dữ liệu người dùng, quyền truy cập và cách bảo vệ tài khoản.");
                context.AppendLine("10. Đối tác & hợp tác: Thông tin dành cho giảng viên và đối tác muốn hợp tác với nền tảng.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error gathering context data: {ex.Message}");
                context.AppendLine("Limited context available due to error.");
            }
            
            return Task.FromResult(context.ToString());
        }
        // Phương thức xử lý phản hồi AI để thêm cảm xúc và định dạng
        private string ProcessAIResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return "Ái chà! Mình không thể xử lý yêu cầu của bạn lúc này. 😅 Phiền bạn thử lại sau hoặc hỏi một câu hỏi khác được không? Mình rất muốn được giúp bạn!";
            }
            
            // Thêm cảm xúc và biểu tượng phù hợp vào phản hồi
            response = EnhanceResponse(response);
            
            // Đảm bảo phản hồi luôn kết thúc với một câu hỏi hoặc lời mời tiếp tục trò chuyện
            if (!response.Contains("?") && !response.EndsWith("😊") && !response.EndsWith("🙂") && 
                !response.EndsWith("✨") && !response.EndsWith("📚"))
            {
                // Tạo ngẫu nhiên các kết thúc khác nhau để thêm đa dạng
                string[] closingMessages = new string[] {
                    "\n\nBạn còn điều gì thắc mắc nữa không? Mình luôn sẵn sàng giúp đỡ bạn nhé! 📚",
                    "\n\nCòn điều gì mình có thể hỗ trợ bạn nữa không nhỉ? ✨",
                    "\n\nHãy hỏi mình bất cứ điều gì khác mà bạn muốn biết nhé!",
                    "\n\nMình có thể giúp gì thêm cho bạn nữa không? Đừng ngại hỏi nhé! 📝",
                    "\n\nBạn cần hỗ trợ gì nữa không? Mình rất vui khi được trò chuyện với bạn!"
                };
                
                response += closingMessages[_random.Next(closingMessages.Length)];
            }
            
            return response;
        }
        
        // Phương thức hợp nhất để tăng cường phản hồi với emotion và visuals
        private string EnhanceResponse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Danh sách emoji phù hợp cho các chủ đề khác nhau
            Dictionary<string, string[]> topicEmojis = new Dictionary<string, string[]>
            {
                { "khóa học", new[] { "📚", "🎓", "✨", "💻", "🌟", "📝", "📊", "🚀" } },
                { "học tập", new[] { "📖", "🧠", "💡", "✏️", "🔍", "⭐", "📌", "🎯" } },
                { "giảng viên", new[] { "👨‍🏫", "👩‍🏫", "🎓", "🌟", "🏆", "✨", "👑" } },
                { "thành công", new[] { "🎉", "🏆", "💯", "⭐", "🌠", "🚀", "✅" } },
                { "thanh toán", new[] { "💰", "💸", "💳", "💵", "🧾", "🤑", "💹" } },
                { "hỗ trợ", new[] { "🛟", "🤝", "👋", "🎯", "🔍", "📱", "📞", "📧" } },
                { "vấn đề", new[] { "🛠️", "🔧", "🔨", "⚙️", "❓", "❗", "⚠️" } },
                { "cảm ơn", new[] { "🙏", "💖", "❤️", "🌹", "✨", "😊", "👍" } }
            };
            
            string[] paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.None);
            
            // Xử lý từng đoạn văn
            for (int i = 0; i < paragraphs.Length; i++)
            {
                // Bỏ qua đoạn quá ngắn hoặc đã có emoji/biểu tượng
                if (paragraphs[i].Length < 30 || ContainsEmoji(paragraphs[i]))
                    continue;
                
                string paragraph = paragraphs[i].ToLower();
                
                // 1. Thêm biểu tượng tổ chức ở đầu đoạn văn (33% cơ hội)
                if (_random.Next(3) == 0)
                {
                    string icon = "";
                    
                    if (paragraph.Contains("khóa học") || paragraph.Contains("bài giảng"))
                        icon = "📚";
                    else if (paragraph.Contains("học tập") || paragraph.Contains("kiến thức"))
                        icon = "📖";
                    else if (paragraph.Contains("ý chính") || paragraph.Contains("lưu ý") || paragraph.Contains("quan trọng"))
                        icon = "📌";
                    else if (paragraph.Contains("hoàn thành") || paragraph.Contains("thành công"))
                        icon = "✅";
                    else if (paragraph.Contains("danh sách") || paragraph.Contains("các bước"))
                        icon = "📋";
                    else if (paragraph.Contains("mẹo") || paragraph.Contains("gợi ý"))
                        icon = "💡";
                    
                    // Nếu tìm thấy chủ đề phù hợp, thêm biểu tượng
                    if (!string.IsNullOrEmpty(icon) && !paragraphs[i].StartsWith(icon))
                    {
                        // Thêm biểu tượng vào đầu đoạn văn để tổ chức nội dung
                        paragraphs[i] = icon + " " + paragraphs[i];
                    }
                }
                
                // 2. Thêm emoji vào cuối đoạn dựa trên chủ đề (70% cơ hội)
                if (_random.Next(10) < 7 && !ContainsEmoji(paragraphs[i]))
                {
                    // Tìm chủ đề phù hợp
                    string topic = "hỗ trợ"; // chủ đề mặc định
                    foreach (var kvp in topicEmojis)
                    {
                        if (paragraph.Contains(kvp.Key))
                        {
                            topic = kvp.Key;
                            break;
                        }
                    }
                    
                    // Lấy ngẫu nhiên 1-2 emoji cho chủ đề
                    string[] emojiSet = topicEmojis[topic];
                    string emoji1 = emojiSet[_random.Next(emojiSet.Length)];
                    
                    // Thêm emoji vào cuối đoạn văn (chỉ thêm 1 emoji để tránh quá nhiều)
                    if (!paragraphs[i].EndsWith(emoji1))
                        paragraphs[i] += " " + emoji1;
                }
            }
            
            // 3. Thêm từ ngữ thể hiện cảm xúc vào đoạn văn ngẫu nhiên
            string[] enthusiasticPhrases = new[] {
                "Thật tuyệt vời! ", 
                "Wow! ", 
                "Tuyệt quá! ", 
                "Thật thú vị! ", 
                "Rất hấp dẫn! ",
                "Ồ, ", 
                "Hãy tưởng tượng! "
            };
            
            // Thêm cảm thán vào một đoạn ngẫu nhiên (40% cơ hội)
            if (paragraphs.Length > 1 && _random.Next(10) < 4)
            {
                int randomIndex = _random.Next(paragraphs.Length);
                if (randomIndex > 0 && paragraphs[randomIndex].Length > 40 && 
                    !paragraphs[randomIndex].StartsWith("Wow") && 
                    !paragraphs[randomIndex].StartsWith("Tuyệt") &&
                    !ContainsEmoji(paragraphs[randomIndex].Substring(0, Math.Min(15, paragraphs[randomIndex].Length))))
                {
                    string phrase = enthusiasticPhrases[_random.Next(enthusiasticPhrases.Length)];
                    paragraphs[randomIndex] = phrase + paragraphs[randomIndex];
                }
            }
            
            // 4. Thêm dấu chấm cảm ở một số nơi
            for (int i = 0; i < paragraphs.Length; i++)
            {
                if (paragraphs[i].Length > 40 && paragraphs[i].EndsWith("."))
                {
                    // 30% khả năng thay dấu chấm bằng dấu chấm cảm
                    if (_random.Next(100) < 30)
                    {
                        paragraphs[i] = paragraphs[i].Substring(0, paragraphs[i].Length - 1) + "!";
                    }
                }
            }
            
            return string.Join("\n\n", paragraphs);
        }
        
        // Kiểm tra xem chuỗi có chứa emoji không
        private static readonly HashSet<string> _commonEmojis = new HashSet<string>
        {
            "😊", "👍", "🎓", "📚", "💻", "🌟", "💡", "📝", "🔍", "⭐",
            "🚀", "✅", "💰", "🤝", "🛠️", "🙏", "💖", "❤️", "✨", "👋",
            "📖", "🧠", "✏️", "📌", "🎯", "👨‍🏫", "👩‍🏫", "🏆", "👑",
            "🎉", "💯", "🌠", "💸", "💳", "💵", "🧾", "🤑", "💹",
            "🛟", "📱", "📞", "📧", "🔧", "🔨", "⚙️", "❓", "❗", "⚠️", "🌹", "😅"
        };
            
        private bool ContainsEmoji(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
                
            // Kiểm tra các emoji phổ biến từ HashSet (hiệu quả hơn mảng)
            foreach (var emoji in _commonEmojis)
            {
                if (text.Contains(emoji))
                    return true;
            }
            
            // Kiểm tra theo dải ký tự emoji Unicode
            return text.Any(c => char.IsSurrogate(c));
        }
    }
}
