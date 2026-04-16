using Microsoft.EntityFrameworkCore;
using SkyBooker.Payment.API.Data;
using SkyBooker.Payment.API.Entities;

namespace SkyBooker.Payment.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context) => _context = context;

    public async Task<Payments?> FindByBookingIdAsync(string bookingId) =>
        await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);

    public async Task<Payments?> FindByPaymentIdAsync(string paymentId) =>
        await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);

    public async Task<Payments?> FindByTransactionIdAsync(string transactionId) =>
        await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);

    public async Task<IList<Payments>> FindByUserIdAsync(int userId) =>
        await _context.Payments.Where(p => p.UserId == userId).ToListAsync();

    public async Task<IList<Payments>> FindByStatusAsync(string status) =>
        await _context.Payments.Where(p => p.Status == status).ToListAsync();

    public async Task<decimal> SumAmountByUserIdAsync(int userId) =>
        await _context.Payments.Where(p => p.UserId == userId && p.Status == PaymentStatus.Paid)
                                .SumAsync(p => p.Amount);

    public async Task<IList<Payments>> FindByPaidAtBetweenAsync(DateTime from, DateTime to) =>
        await _context.Payments.Where(p => p.PaidAt >= from && p.PaidAt <= to).ToListAsync();

    public async Task<Payments> SaveAsync(Payments payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payments> UpdateAsync(Payments payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}