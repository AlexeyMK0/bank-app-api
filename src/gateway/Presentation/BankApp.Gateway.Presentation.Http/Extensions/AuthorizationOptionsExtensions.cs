using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using Microsoft.AspNetCore.Authorization;

namespace BankApp.Gateway.Presentation.Http.Extensions;

public static class AuthorizationOptionsExtensions
{
    public static void AddFeaturePolicies(this AuthorizationOptions auth)
    {
        auth.AddPolicy(
            AppFeatures.ReadAccount,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadAccount));

        auth.AddPolicy(
            AppFeatures.AccountDeposit,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.AccountDeposit));

        auth.AddPolicy(
            AppFeatures.AccountWithdraw,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.AccountWithdraw));

        auth.AddPolicy(
            AppFeatures.ReadAccountBalance,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadAccountBalance));

        auth.AddPolicy(
            AppFeatures.CreateAccount,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CreateAccount));

        auth.AddPolicy(
            AppFeatures.CancelInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CancelInvoice));

        auth.AddPolicy(
            AppFeatures.PayInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.PayInvoice));

        auth.AddPolicy(
            AppFeatures.ReadInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadInvoice));

        auth.AddPolicy(
            AppFeatures.CreateInvoice,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.CreateInvoice));

        auth.AddPolicy(
            AppFeatures.ApproveInvoice,
            policy => policy
                .RequireAuthenticatedUser());

        auth.AddPolicy(
            AppFeatures.DeclineInvoice,
            policy => policy
                .RequireAuthenticatedUser());

        auth.AddPolicy(
            AppFeatures.AssignUserToInvoice,
            policy => policy
                .RequireAuthenticatedUser());

        auth.AddPolicy(
            AppFeatures.ReadOperation,
            policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permissions", AppFeatures.ReadOperation));
    }
}