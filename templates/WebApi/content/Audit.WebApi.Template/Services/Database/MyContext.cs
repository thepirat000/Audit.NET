﻿using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Audit.WebApi.Template.Services.Database
{
    public class MyContext : AuditDbContext
    {
        public MyContext(DbContextOptions<MyContext> options) : base(options)
        {
        }
        public DbSet<ValueEntity> Values { get; set; }
    }
}