using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models
{
    public sealed class Return
    {
        public long Id { get; set; }
        
        [MaxLength(1000)]
        public string Notes { get; set; }
        public double Amount { get; set; }
        public string OrderId { get; set; }
        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Order Order { get; set; }
        public ApplicationUser User { get; set; }
    }
}
