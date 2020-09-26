using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces
{
    public interface ISourceReader
    {
        public List<SourceData> Get(DateTime sinceWhen);
        public Task<List<SourceData>> GetAsync(DateTime sinceWhen);
    }
}