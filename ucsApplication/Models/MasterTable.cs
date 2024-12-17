namespace ucsApplication.Models
{
    public class MasterTable
    {
        public int MasterId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FingerPrintData { get; set; }
        public DateTime LastTransactionDate { get; set; }

        // Navigation property
        public ICollection<TransactionTable> Transactions { get; set; }

    }
}
