using GymManager.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(GymManagerDbContext dbContext) : IExpenseRepository
{
    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Expense aggregate) => dbContext.Expenses.Add(aggregate);

    public void Update(Expense aggregate) => dbContext.Expenses.Update(aggregate);

    public void Remove(Expense aggregate) => dbContext.Expenses.Remove(aggregate);
}
