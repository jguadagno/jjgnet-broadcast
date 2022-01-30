using System;
using System.Collections.Generic;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IRandomPostSettings
{
    public List<string> ExcludedCategories { get; set; }
    public DateTime CutoffDate { get; set; }
}