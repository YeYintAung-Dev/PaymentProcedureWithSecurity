using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PaymentProcedureWithSecurity;
public class Account { public int AccountId; public string AccountName; public decimal Balance; }
public class PaymentContext : DbContext
{
    public DbSet<Account> Tbl_Accounts { get; set; }
    public PaymentContext(DbContextOptions options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Account>().HasNoKey().ToView(null);
    }
}