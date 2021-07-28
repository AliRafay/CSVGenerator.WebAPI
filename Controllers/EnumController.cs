using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using CSVGenerator.Services.DataTransferObjects;
using CSVGenerator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace CSVGenerator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnumController : ControllerBase
    {
        ICSVService csvService;
        public EnumController(ICSVService csvService)
        {
            this.csvService = csvService;
        }

        [HttpPost("{DLLPath}")]
        public async Task<IActionResult> GenerateEnumPdf(string DLLPath,[FromBody] RequestDto request)
        {
            (string zipPath, string zipName) = await csvService.GenerateEnumCsvAsync(DLLPath, request);
            var stream = System.IO.File.OpenRead(zipPath);
            var contentType = new FileExtensionContentTypeProvider().TryGetContentType(zipPath, out var mimeType) ? mimeType : MediaTypeNames.Application.Octet;
            return File(stream, contentType, zipName);
        }
    }
}
