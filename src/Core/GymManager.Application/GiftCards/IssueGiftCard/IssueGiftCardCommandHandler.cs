using System.Security.Cryptography;
using GymManager.Application.GiftCards.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.GiftCards;
using GymManager.Domain.GiftCards.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.GiftCards.IssueGiftCard;

public sealed class IssueGiftCardCommandHandler(IGiftCardRepository giftCardRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<IssueGiftCardCommand, Result<GiftCardResponse>>
{
    public async Task<Result<GiftCardResponse>> Handle(IssueGiftCardCommand command, CancellationToken cancellationToken)
    {
        var balanceResult = Money.Create(command.InitialBalance);
        if (balanceResult.IsFailure)
            return Result.Failure<GiftCardResponse>(balanceResult.Error);

        var code = string.IsNullOrWhiteSpace(command.Code) ? GenerateCode() : command.Code.Trim().ToUpperInvariant();

        if (await giftCardRepository.CodeExistsAsync(code, cancellationToken))
            return Result.Failure<GiftCardResponse>(GiftCardErrors.CodeAlreadyInUse);

        var giftCard = GiftCard.Issue(code, balanceResult.Value, command.IssuedToMemberId, command.ExpiresOnUtc);

        giftCardRepository.Add(giftCard);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(giftCard.ToResponse());
    }

    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I to avoid ambiguity when read aloud
        var bytes = RandomNumberGenerator.GetBytes(10);
        var chars = bytes.Select(b => alphabet[b % alphabet.Length]).ToArray();
        return $"GC-{new string(chars)}";
    }
}
