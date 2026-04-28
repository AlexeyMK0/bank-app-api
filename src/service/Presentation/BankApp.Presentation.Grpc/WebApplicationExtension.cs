using BankApp.Presentation.Grpc.Controllers;

namespace BankApp.Presentation.Grpc;

public static class WebApplicationExtension
{
    public static void UsePresentationGrpc(this WebApplication application)
    {
        application.MapGrpcService<AccountController>();
        application.MapGrpcService<InvoiceController>();
        application.MapGrpcService<OperationHistoryController>();
        application.MapGrpcService<SessionController>();
        application.MapGrpcReflectionService();
    }
}