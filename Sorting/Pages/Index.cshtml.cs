using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Sorting.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;

        private static Faker<Widget> Builder = new Faker<Widget>()
            .RuleFor(m => m.Id, f => f.IndexFaker)
            .RuleFor(m => m.Name, f => f.Commerce.ProductName())
            .RuleFor(m => m.CreatedAt, f => f.Date.Soon());
        
        public static IQueryable<Widget> Database = Builder.Generate(100).AsQueryable();
        
        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; }
        
        public SortCollection<Widget> Sorting { get; set; } 

        public IndexModel(ILogger<IndexModel> logger)
        {
            this.logger = logger;
        }

        public void OnGet()
        {
            Sorting = new SortCollection<Widget>(Sort);

            Result =
                Database
                    .OrderBy(Sorting.ToString())
                    .ToList();
        }

        public List<Widget> Result { get; set; }
    }

    public class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}