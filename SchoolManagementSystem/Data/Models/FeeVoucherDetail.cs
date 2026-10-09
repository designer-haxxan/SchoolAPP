namespace SchoolManagementSystem.Data.Models;

public class FeeVoucherDetail
{
    public int Id { get; set; }
    public int FeeVoucherId { get; set; }
    public FeeVoucher FeeVoucher { get; set; } = null!;
    public int FeeHeadId { get; set; }
    public FeeHead FeeHead { get; set; } = null!;
    public decimal Amount { get; set; }
}
