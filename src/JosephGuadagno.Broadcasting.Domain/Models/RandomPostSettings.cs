using System;
using System.Collections.Generic;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class RandomPostSettings: IRandomPostSettings
{
    public List<string> ExcludedCategories { get; set; }
    public DateTime CutoffDate { get; set; }
}