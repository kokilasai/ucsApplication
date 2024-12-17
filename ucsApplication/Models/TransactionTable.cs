namespace ucsApplication.Models
{
    public class TransactionTable
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CheckinDateTime { get; set; }
        public DateTime? CheckoutDateTime { get; set; }
        public string CheckInMethod { get; set; }  // "UserId" or "FingerPrint"

        // Foreign key relationship
        public MasterTable Master { get; set; }
    }
}
