using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models
{
    public class AddNewReturnModel
    {

        [Required]
        public string OrderId { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Notes { get; set; }

        [Required]
        public double Amount { get; set; }
    }
}
