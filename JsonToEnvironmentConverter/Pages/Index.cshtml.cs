using System.ComponentModel.DataAnnotations;
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

        [BindProperty] public string Format { get; set; } = "Docker";

        [BindProperty] public bool IncludeEmpty { get; set; } = true;

        [BindProperty] public string Separator { get; set; } = "Colon";

        [BindProperty] public bool Kubernetes { get; set; }

        public string Environment { get; set; }

        public void OnGet()
        {
            Environment = string.Empty;
            Json = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Database=master;Server=(local);Integrated Security=SSPI;""
    },
  ""property"": ""value""
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

                    string format;
                    if (Format == "Yaml")
                    {
                        if (Kubernetes)
                        {
                            format = "- name: \"{0}\"\n" +
                                     "  value: \"{1}\"";                        
                        }
                        else
                        {
                            format = "\"{0}\": \"{1}\"";
                        }
                    }
                    else
                    {
                        format = "{0}={1}";
                    }
                    foreach ((string key, string value) in configurationRoot.AsEnumerable()
                        .Where(pair => IncludeEmpty || !string.IsNullOrEmpty(pair.Value))
                        .OrderBy(pair => pair.Key))
                    {
                        string key2 = key;
                        if (Separator == "Underscore") 
                        {
                            key2 = key2.Replace(":", "__");
                        }
                        sb.AppendFormat(format, key2, value);
                        sb.AppendLine();
                    }

                    Environment = sb.ToString();

                }
                catch (System.Text.Json.JsonException e)
                {
                    ModelState.AddModelError("Json", e.Message);
                }
            }

            return Page();
        }
    }
}
