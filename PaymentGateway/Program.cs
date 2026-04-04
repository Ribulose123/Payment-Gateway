using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Application.Policies;
using PaymentGate.Application.Services;
using PaymentGateway.BackgroundServices;
using PaymentGateway.Persistence;
using Paystack;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<LimitResetBackgroundService>();
builder.Services.AddHostedService<ScheduledTransferBackgroundService>();
builder.Services.Configure<FxApiSettings>(builder.Configuration.GetSection("FxApi"));
builder.Services.Configure<PaystackOptions>(builder.Configuration.GetSection("Paystack"));
builder.Services.AddScoped<ITransferInterface, TransferServices>();
builder.Services.AddScoped<IFraudPolicy, BasicFraudPolicy>();
builder.Services.AddScoped<IFxFraudPolicy, BasicFxFraudPolicy>();
builder.Services.AddScoped<IFeePolicy, TieredFeePolicy>();
builder.Services.AddScoped<ILimitPolicy, UserLimitPolicy>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IFxTransfer, FxTransfereServices>();
builder.Services.AddScoped<IScheduleTransfer, ScheduleTransferServices>();

builder.Services.AddHttpClient<IFxService, OpenErFxService>();

// Register concrete PaystackService implementation (PaystackService lives in PaymentGate.Application.Services)
builder.Services.AddHttpClient<IPaystackService, PaystackService>();

builder.Services.AddScoped<IWalletExchangeService, WalletExchangeService>();

//Db contection
builder.Services.AddDbContext<PaymentGatewayDbCOntext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();