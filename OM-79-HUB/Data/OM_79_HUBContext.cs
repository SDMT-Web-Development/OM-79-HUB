using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;

namespace OM_79_HUB.Data
{
    public partial class OM_79_HUBContext : DbContext
    {
        public OM_79_HUBContext (DbContextOptions<OM_79_HUBContext> options)
            : base(options)
        {
        }
        public bool HasChanges()
        {
            return ChangeTracker.HasChanges();
        }
        public virtual DbSet<CENTRAL79HUB> CENTRAL79HUB { get; set; }
        public virtual DbSet<SignatureData> SignatureData { get; set; }
    }
}
