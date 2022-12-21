using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Audit.Integration.AspNetCore.Pages.Test
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Name { get; set; }

        public string Token { get; set; }

        public void OnGet(string name)
        {
            var scope = this.HttpContext.GetCurrentAuditScope();
            if (scope == null)
            {
                throw new Exception("Current Scope is null");
            }
            Name = name;
        }

        public async Task<IActionResult> OnPostAsync([FromBody] Customer customer)
        {
            await Task.Delay(0);
            if (customer.Name == "404")
            {
                return NotFound();
            }
            if (customer.Name == "ThrowException")
            {
                throw new Exception("TEST EXCEPTION");
            }
            return new JsonResult(customer);
        }

        [return: AuditIgnore]
        public async Task<IActionResult> OnPutAsync([FromBody] Customer customer)
        {
            await Task.Delay(0);
            return new JsonResult(customer);
        }
    }

}
