using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace GymManager.ArchitectureTests;

/// <summary>
/// Guards the one bug class in this codebase that has already shipped twice.
///
/// Since Phase 15, <c>Member</c> carries a global branch-isolation query filter. That makes the
/// following shape silently unsafe:
///
/// <code>
///     var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, ct);
///     if (member is not null)
///     {
///         var access = branchAccessGuard.EnsureCanAccess(member.BranchId);
///         ...
///     }
/// </code>
///
/// The lookup exists only to resolve the owning member's branch so the caller can be authorized
/// against it. But the global filter hides members from other branches, so for a cross-branch
/// caller the member comes back <c>null</c> — "does not exist" and "exists, but is filtered out"
/// become indistinguishable — and the <c>is not null</c> check quietly skips the guard instead of
/// denying access. The caller then proceeds to read or mutate another branch's data by id. Phase 19
/// found this across 17 handlers; Phase 9 had found the same class of hole in 16 others.
///
/// The fix is <c>IgnoreQueryFilters()</c> on the authorization-only lookup (or
/// <c>IMemberRepository.GetBranchIdForAuthorizationAsync</c>, which does the same thing behind the
/// repository). Nothing structural prevented the pattern from coming back, which is what this test
/// is for.
///
/// Note what is deliberately NOT flagged: a filtered lookup whose <c>null</c> branch <em>returns</em>
/// (e.g. <c>if (member is null) return Failure(NotFound);</c>) is safe. Access is still denied —
/// the filter itself did the denying, turning a 403 into a 404. Only the shape where <c>null</c>
/// causes the guard to be <em>skipped</em> is a hole.
///
/// This is a source-text check rather than a NetArchTest rule because the distinction lives in
/// method-body call syntax, which is not visible through reflection over compiled types.
/// </summary>
public sealed partial class BranchIsolationConventionTests
{
    [Fact]
    public void Authorization_only_member_lookups_must_bypass_the_branch_query_filter()
    {
        var applicationRoot = ApplicationSourceRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);

            foreach (Match lookup in MemberLookup().Matches(source))
            {
                // Already bypassing the filter — this is the fixed shape.
                if (lookup.Value.Contains("IgnoreQueryFilters", StringComparison.Ordinal))
                {
                    continue;
                }

                // Only the "null skips the guard" shape is dangerous. Look at what immediately
                // follows the lookup: if the very next thing is a null-check that guards an
                // EnsureCanAccess call, a filtered-out member silently bypasses authorization.
                var tail = source[(lookup.Index + lookup.Length)..];
                var window = tail[..Math.Min(tail.Length, 400)];

                if (SkipsGuardWhenNull().IsMatch(window))
                {
                    var line = source[..lookup.Index].Count(c => c == '\n') + 1;
                    offenders.Add($"{Path.GetRelativePath(applicationRoot, file)}:{line}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These member lookups resolve a branch for authorization but run through the global "
            + "branch-isolation filter, so a cross-branch member returns null and the "
            + "`is not null` check skips EnsureCanAccess entirely — granting the access it was "
            + "meant to deny. Add .IgnoreQueryFilters() to the lookup (or use "
            + "IMemberRepository.GetBranchIdForAuthorizationAsync):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void The_detector_recognises_the_shape_it_is_meant_to_catch()
    {
        // A convention test that silently stops matching is worse than no test at all, so pin the
        // detector against both shapes directly rather than trusting it because the suite is green.
        const string vulnerable = """
            var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
            if (member is not null)
            {
                var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            }
            """;

        const string fixedUp = """
            var member = await readDb.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
            if (member is not null)
            {
                var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            }
            """;

        // Safe: a filtered lookup that *returns* on null still denies access (403 becomes 404).
        const string safeEarlyReturn = """
            var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == query.MemberId, cancellationToken);
            if (member is null)
                return Result.Failure<MemberResponse>(MemberErrors.NotFound);

            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            """;

        Assert.True(IsOffending(vulnerable), "detector no longer catches the pattern that shipped twice");
        Assert.False(IsOffending(fixedUp), "detector flags the corrected IgnoreQueryFilters shape");
        Assert.False(IsOffending(safeEarlyReturn), "detector flags a safe early-return lookup");
    }

    private static bool IsOffending(string source)
    {
        foreach (Match lookup in MemberLookup().Matches(source))
        {
            if (lookup.Value.Contains("IgnoreQueryFilters", StringComparison.Ordinal))
            {
                continue;
            }

            var tail = source[(lookup.Index + lookup.Length)..];
            if (SkipsGuardWhenNull().IsMatch(tail[..Math.Min(tail.Length, 400)]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks up from this file's own compile-time location to the Application project's source, so
    /// the scan reads real source rather than anything copied next to the test binary.
    /// </summary>
    private static string ApplicationSourceRoot([CallerFilePath] string thisFile = "")
    {
        var directory = Path.GetDirectoryName(thisFile)!;

        while (directory is not null)
        {
            var candidate = Path.Combine(directory, "src", "Core", "GymManager.Application");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new DirectoryNotFoundException(
            "Could not locate src/Core/GymManager.Application from " + thisFile);
    }

    // A Members query on the read DB, consumed through its statement-terminating semicolon so that
    // what follows the match is genuinely the next statement. Any IgnoreQueryFilters() sitting
    // between the DbSet and the terminal call falls inside the match, which is how the safe and
    // unsafe shapes are told apart.
    [GeneratedRegex(@"\.Members\s*(?:\.\s*\w+\s*\([^;]*?\)\s*)*?\.\s*(?:FirstOrDefaultAsync|SingleOrDefaultAsync)\s*\([^;]*;",
        RegexOptions.Singleline)]
    private static partial Regex MemberLookup();

    // The next statement being "if (x is not null)" (or != null) with EnsureCanAccess inside it —
    // i.e. a null result means authorization never runs. Anchored at the start of the window so an
    // unrelated guard further down the method isn't mistaken for this one.
    [GeneratedRegex(@"\A\s*if\s*\(\s*\w+\s*(?:is\s+not\s+null|!=\s*null)\s*\)[^}]*?EnsureCanAccess",
        RegexOptions.Singleline)]
    private static partial Regex SkipsGuardWhenNull();
}
