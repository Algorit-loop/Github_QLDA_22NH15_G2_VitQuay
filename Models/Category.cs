using System.ComponentModel.DataAnnotations;

namespace AppEL.Models
{
    public class Category
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;
        
        public bool IsFeatured { get; set; } = false;
    }
}
