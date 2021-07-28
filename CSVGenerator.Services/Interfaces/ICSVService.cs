using CSVGenerator.Services.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CSVGenerator.Services.Interfaces
{
    public interface ICSVService
    {
        Task<(string path, string fileName)> GenerateEnumCsvAsync(string projPath, RequestDto request);
    }
}
