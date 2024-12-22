﻿using CFW.Core.Entities;

namespace CFW.ODataCore.Testings.Models;

[ODataEntitySet("customers")]
public class Customer : IEntity<int>, IODataViewModel<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }
}

[ODataEntitySet("vouchers")]
public class Voucher : IEntity<string>, IODataViewModel<string>
{
    public string Id { get; set; } = string.Empty;

    public string? Code { get; set; }

    public decimal Discount { get; set; }
}