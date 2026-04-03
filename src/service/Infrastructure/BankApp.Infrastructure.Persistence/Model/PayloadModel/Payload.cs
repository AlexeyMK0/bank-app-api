using System.Text.Json.Serialization;

namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(CancelInvoicePayload), typeDiscriminator: "CancelInvoice")]
[JsonDerivedType(typeof(CreateInvoicePayload), typeDiscriminator: "CreateInvoice")]
public record Payload;