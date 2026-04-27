
using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Application.Policies;
using PaymentGate.Application.Services;
using PaymentGateway.BackgroundServices;
using PaymentGateway.Persistence;
using PaymentGateway.Persistence.Services;
using Paystack;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<LimitResetBackgroundService>();
builder.Services.AddHostedService<ScheduledTransferBackgroundService>();
builder.Services.Configure<FxApiSettings>(builder.Configuration.GetSection("FxApi"));
builder.Services.Configure<PaystackOptions>(builder.Configuration.GetSection("Paystack"));
builder.Services.AddScoped<ITransferInterface,  TransferServices>();
builder.Services.AddScoped<IFraudPolicy, BasicFraudPolicy>();
builder.Services.AddScoped<IFxFraudPolicy, BasicFxFraudPolicy>();
builder.Services.AddScoped<IFeePolicy, TieredFeePolicy>();
builder.Services.AddScoped<ILimitPolicy, UserLimitPolicy>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IFxTransfer, FxTransfereServices>();
builder.Services.AddScoped<IScheduleTransfer, ScheduleTransferServices>();
builder.Services.AddScoped<IWithdrawalServices, WithdrawalService>();

builder.Services.AddHttpClient<IFxService, OpenErFxService>();

// Register concrete PaystackService implementation (PaystackService lives in PaymentGate.Application.Services)
builder.Services.AddHttpClient<IPaystackService, PaystackService>();

builder.Services.AddScoped<IWalletExchangeService, WalletExchangeService>();

//Db contection
builder.Services.AddDbContext<PaymentGatewayDbCOntext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("PaymentGateway.Persistence")));

// To serialize enum as string in json response
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/paystack/webhook"))
    {
        await next(); 
    }
    else
    {
        if (context.Request.Scheme == "http")
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(httpsUrl, permanent: false);
            return;
        }
        await next();
    }
});
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();