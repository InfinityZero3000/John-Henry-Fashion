using System.ComponentModel.DataAnnotations;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.ViewModels
{
    public class CreateTicketViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(500, ErrorMessage = "Tiêu đề không được quá 500 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Subject { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng mô tả vấn đề của bạn")]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public string Category { get; set; } = "general";
        
        [Display(Name = "Mức độ ưu tiên")]
        public string Priority { get; set; } = "medium";
        
        [Display(Name = "Đơn hàng liên quan")]
        public Guid? RelatedOrderId { get; set; }
        
        [Display(Name = "Sản phẩm liên quan")]
        public Guid? RelatedProductId { get; set; }
        
        public List<IFormFile>? Attachments { get; set; }
    }

    public class TicketDetailViewModel
    {
        public SupportTicket Ticket { get; set; } = null!;
        public List<TicketReply> Replies { get; set; } = new();
        public Order? RelatedOrder { get; set; }
        public Product? RelatedProduct { get; set; }
        public ApplicationUser? AssignedAdmin { get; set; }
    }

    public class AddReplyViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi")]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;
        
        public List<IFormFile>? Attachments { get; set; }
    }
}
