using SkyBooker.Payment.API.Entities;

namespace SkyBooker.Payment.API.Repositories;

public interface IPaymentRepository
{
    Task<Payments?> FindByBookingIdAsync(string bookingId);
    Task<Payments?> FindByPaymentIdAsync(string paymentId);
    Task<Payments?> FindByTransactionIdAsync(string transactionId);
    Task<IList<Payments>> FindByUserIdAsync(int userId);
    Task<IList<Payments>> FindByStatusAsync(string status);
    Task<decimal> SumAmountByUserIdAsync(int userId);
    Task<IList<Payments>> FindByPaidAtBetweenAsync(DateTime from, DateTime to);
    Task<Payments> SaveAsync(Payments payment);
    Task<Payments> UpdateAsync(Payments payment);
}