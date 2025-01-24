using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.Models;

public class ODataQueryOptions
{
    public required AllowedQueryOptions? AllowedQueryOptions { get; set; }

    internal AllowedQueryOptions IgnoreQueryOptions { get; private set; }

    public void SetIgnoreQueryOptions(DefaultQueryConfigurations queryConfigurations
        , EntityEndpoint entityEndpoint)
    {
        if (entityEndpoint.AllowedQueryOptions is not null)
        {
            IgnoreQueryOptions = ~entityEndpoint.AllowedQueryOptions.Value;
            return;
        }

        if (AllowedQueryOptions is not null)
        {
            IgnoreQueryOptions = ~AllowedQueryOptions.Value;
            return;
        }

        IgnoreQueryOptions = Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.None;

        // Start with "Allow all" bitmask
        var allowedQueryOptions = Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.All;

        // Disable specific query options based on global configurations
        if (!queryConfigurations.EnableCount)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.Count;

        if (!queryConfigurations.EnableExpand)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.Expand;

        if (!queryConfigurations.EnableFilter)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.Filter;

        if (!queryConfigurations.EnableOrderBy)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.OrderBy;

        if (!queryConfigurations.EnableSelect)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.Select;

        if (!queryConfigurations.EnableSkipToken)
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.SkipToken;

        if (queryConfigurations.MaxTop is not null)
            // Assuming MaxTop being set means Top is allowed, else it's not
            allowedQueryOptions &= ~Microsoft.AspNetCore.OData.Query.AllowedQueryOptions.Top;

        IgnoreQueryOptions = ~allowedQueryOptions;
    }
}
