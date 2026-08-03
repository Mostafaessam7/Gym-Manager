using GymManager.SharedKernel.Results;

namespace GymManager.Application.Abstractions;

/// <summary>
/// Enforces multi-branch data isolation: a caller whose JWT carries a specific <c>branch_id</c> claim (i.e.
/// staff tied to one branch — Manager, Front Desk, Trainer accounts) may only read or write that branch's
/// data. A caller with no branch claim (Owner/HQ-level accounts, created without a <c>BranchId</c>) is not
/// branch-restricted and can act across every branch, matching how <see cref="ICurrentUserService.BranchId"/>
/// is already populated today.
/// </summary>
public interface IBranchAccessGuard
{
    /// <summary>Fails with <c>Branch.AccessDenied</c> if the caller is scoped to a different branch than
    /// <paramref name="branchId"/>. Use before creating or mutating a specific branch's data.</summary>
    Result EnsureCanAccess(Guid branchId);

    /// <summary>Resolves the effective branch filter for a list query: a branch-scoped caller's own branch
    /// always wins over whatever (if anything) was requested, so they can never widen a query beyond their
    /// own branch; an unscoped (HQ-level) caller gets whatever filter — including none — they asked for.</summary>
    Guid? ResolveFilter(Guid? requestedBranchId);
}
