using System.ComponentModel.DataAnnotations;

namespace BaleManagerSystem.Models.ViewModels
{
    public class MenuItemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "شناسه آیتم الزامی است.")]
        [RegularExpression("^[A-Za-z0-9_]+$",
            ErrorMessage = "شناسه فقط می‌تواند شامل حروف انگلیسی، عدد و _ باشد.")]
        [StringLength(50)]
        public string ItemKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام نمایشی الزامی است.")]
        [StringLength(100)]
        public string NamePersian { get; set; } = string.Empty;

        public bool SupportsShots { get; set; }

        [Required(ErrorMessage = "واحد الزامی است.")]
        [StringLength(20)]
        public string Unit { get; set; } = "شات";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Price inputs (in Toman). Price_2 is only used when SupportsShots is true.
        public int Price_1 { get; set; }

        public int Price_2 { get; set; }
    }
}
