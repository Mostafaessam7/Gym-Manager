using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Expenses;

public interface IExpenseRepository : IRepository<Expense, Guid>;
