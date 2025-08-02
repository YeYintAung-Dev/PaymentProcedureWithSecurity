using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PaymentProcedureWithSecurity;

var options = new DbContextOptionsBuilder<PaymentContext>()
    .UseSqlServer("Server=.;Database=PaymentDB;User Id=PaymentAppLogin;Password=sasa@123;TrustServerCertificate=True;")
    .Options;

using var ctx = new PaymentContext(options);

Console.Write("From ID: "); int from = int.Parse(Console.ReadLine());
Console.Write("To ID: "); int to = int.Parse(Console.ReadLine());
Console.Write("Amount: "); decimal amt = decimal.Parse(Console.ReadLine());

try
{
    //int affected = await ctx.Database.ExecuteSqlInterpolatedAsync(
    //    $"EXEC usp_ProcessPayment @FromAccountId={from}, @ToAccountId={to}, @Amount={amt}");
    int affected = await ctx.Database.ExecuteSqlRawAsync(
    "EXEC usp_ProcessPayment @FromAccountId, @ToAccountId, @Amount",
    new SqlParameter("@FromAccountId", from),
    new SqlParameter("@ToAccountId", to),
    new SqlParameter("@Amount", amt));

    Console.WriteLine("Payment succeeded. Rows affected: " + affected);
}
catch (Exception ex)
{
    Console.WriteLine("Payment failed: " + ex.Message);
}
