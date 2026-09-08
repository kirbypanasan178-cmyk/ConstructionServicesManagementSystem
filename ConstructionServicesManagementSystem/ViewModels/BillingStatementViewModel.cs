namespace ConstructionServicesManagementSystem.ViewModels
{
    public class BillingStatementViewModel
    {
        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public decimal TotalBilling { get; set; }

        public decimal TotalPaid { get; set; }

        public decimal Balance { get; set; }

        public List<BillingStatementItemViewModel> Items { get; set; }
            = new List<BillingStatementItemViewModel>();
    }

    public class BillingStatementItemViewModel
    {
        public int BillingId { get; set; }

        public DateTime Date { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal Paid { get; set; }

        public decimal Balance { get; set; }
    }
}