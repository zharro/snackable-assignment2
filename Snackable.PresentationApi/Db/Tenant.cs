using System;

namespace Snackable.PresentationApi.Db
{
    public class Tenant
    {
        public static Tenant Default = new()
        {
            TenantId = Guid.Empty,
            Name = "DefaultTenant"
        };

        public Guid TenantId { get; set; }
        public string Name { get; set; }
    }
}