using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using XeroNetStandardApp.IO;

namespace XeroNetStandardApp.ViewComponents
{
    public class NavBarViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;
        public NavBarViewComponent(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class TenantDetails
        {
            public string TenantName { get; set; }
            public Guid TenantId { get; set; }
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            // For this to work, you need a line like this at the top of your appsettings.json file:
            // "IncludeBetaSystems": true,
            ViewBag.IncludeBetaSystems = _configuration.GetValue<bool>("IncludeBetaSystems");

            var tokenIO = LocalStorageTokenIO.Instance;

            var xeroToken = tokenIO.GetToken();

            var tenantId = tokenIO.GetTenantId();
            if (tenantId != null && xeroToken?.AccessToken != null)
            {
                var tenantIdGuid = Guid.Parse(tenantId);

                ViewBag.OrgPickerCurrentTenantId = tenantIdGuid;
                ViewBag.OrgPickerTenantList = xeroToken?.Tenants.Select(
                    t => new TenantDetails { TenantName = t.TenantName, TenantId = t.TenantId })
                    .ToList();
            }

            return Task.FromResult<IViewComponentResult>(View(tokenIO.TokenExists()));
        }

    }

}

