namespace Lab1.Presentation.Http.Model;

public class PageTokenModelBinder
{
    /*public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ValueProviderResult providedValueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (providedValueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        string? value = providedValueResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            GetAccountOperations.PageToken?
                pageToken = JsonSerializer.Deserialize<GetAccountOperations.PageToken>(value);
        }
        catch (JsonException exception)
        {

        }

    }*/
}