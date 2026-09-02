using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Users;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

public sealed class CreateUserCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork,
    IIdentityAccountService identityAccountService)
    : IMediatRCommandHandler<CreateUserCommand, Result<CreateUserResult>>
{
    public async Task<Result<CreateUserResult>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<CreateUserResult>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<CreateUserResult>.Failure(
                "Only an architect may create users.", ResultErrorType.Forbidden);
        }

        if (!identityAccountService.SupportsPasswordAccounts)
        {
            // ADR-0018: this deployment validates against an external
            // Authority (ADR-0003) — ArchitectureToolkit never manages
            // passwords there. Whoever should get access needs to be
            // provisioned on that provider directly; they'll pick up a
            // domain USER row automatically (Contributor by default) the
            // first time they log in, same as everyone else on that IdP.
            return Result<CreateUserResult>.Failure(
                "This deployment uses an external identity provider. New users must be " +
                "provisioned there directly — they'll be added automatically on first login.",
                ResultErrorType.Conflict);
        }

        var duplicateQuery = queryDbContext.Set<User>().Where(u => u.Email == request.Email);
        var duplicate = await queryDbContext.SingleOrDefaultAsync(duplicateQuery, cancellationToken);

        if (duplicate is not null)
        {
            return Result<CreateUserResult>.Failure(
                "A user with this email already exists.", ResultErrorType.Conflict);
        }

        // Invited before the domain USER row is created, not after: if
        // creating the IdentityUser fails, nothing else has happened yet
        // — no orphaned USER row permanently blocking a retry against the
        // duplicate-email check above. See IIdentityAccountService's own
        // doc comment for why this ordering matters.
        var inviteResult = await identityAccountService.InviteAsync(request.Email, cancellationToken);
        if (!inviteResult.IsSuccess)
        {
            return Result<CreateUserResult>.Failure(inviteResult.Error!, inviteResult.ErrorType);
        }

        // Name isn't collected on the New User form (email + role only,
        // per the stated requirement) — derived from the email's local
        // part as a placeholder. There's no profile-editing feature yet
        // for the person to correct this themselves; worth a follow-up
        // once this sees real use, same as ADR-0018's "no resend invite"
        // gap.
        var placeholderName = DerivePlaceholderName(request.Email);

        var newUser = new User(placeholderName, request.Email, request.SystemRole);
        commandDbContext.Insert(newUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var outcome = inviteResult.Value!;
        var userDto = new UserManagementDto(newUser.Id, newUser.Email, newUser.IsActive);

        return Result<CreateUserResult>.Success(new CreateUserResult(userDto, outcome.EmailSent, outcome.InviteLink));
    }

    private static string DerivePlaceholderName(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }
}
