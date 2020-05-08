using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JsonToEnvironmentConverter.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        [Required]
        [BindProperty] public string Json { get; set; }

        public string Environment
        {
            get; set;
        }

        public void OnGet()
        {
            Environment = string.Empty;
            Json = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Database=master;Server=(local);Integrated Security=SSPI;""
    }
}";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!string.IsNullOrEmpty(Json))
            {
                var builder = new ConfigurationBuilder();
                var stream = new MemoryStream(Json.Length);
                var sw = new StreamWriter(stream);
                await sw.WriteAsync(Json);
                await sw.FlushAsync();
                stream.Position = 0;

                builder.AddJsonStream(stream);

                try
                {
                    var configurationRoot = builder.Build();

                    var sb = new StringBuilder();
                    foreach ((string key, string value) in configurationRoot.AsEnumerable().OrderBy(pair => pair.Key))
                    {
                        sb.AppendLine($"{key} = {value}");
                    }

                    Environment = sb.ToString();

                    ViewData["Environment"] = Environment;

                    Debug.WriteLine(Environment);
                }
                catch (System.Text.Json.JsonException e)
                {
                    ModelState.AddModelError("Json", e.Message);
                }
            }

            return Page();
            // return RedirectToPage();
        }
    }
}
