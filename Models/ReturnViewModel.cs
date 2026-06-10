namespace MagdyPOS.Models
{
    public class ReturnViewModel
    {
        public string Notes { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public Order Order { get; set; }
        public ApplicationUser User { get; set; }
    }
}
