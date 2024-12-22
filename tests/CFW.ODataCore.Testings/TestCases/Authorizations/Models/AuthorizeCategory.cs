﻿using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.TestCases.Authorizations.Models;

[ODataRouting("authorize-categories")]
[ODataAuthorize]
public class AuthorizeCategory : IODataViewModel<Guid>, IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
