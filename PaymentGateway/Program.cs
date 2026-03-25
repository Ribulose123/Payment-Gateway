using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Application.Policies;
using PaymentGate.Application.Services;
using PaymentGateway.BackgroundServices;
using PaymentGateway.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<LimitResetBackgroundService>();
builder.Services.AddHostedService<ScheduledTransferBackgroundService>();
builder.Services.Configure<FxApiSettings>(builder.Configuration.GetSection("FxApi"));
builder.Services.AddScoped<ITransferInterface, TransferServices>();
builder.Services.AddScoped<IFraudPolicy, BasicFraudPolicy>();
builder.Services.AddScoped<IFxFraudPolicy, BasicFxFraudPolicy>();
builder.Services.AddScoped<IFeePolicy, TieredFeePolicy>();
builder.Services.AddScoped<ILimitPolicy, UserLimitPolicy>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IFxTransfer, FxTransfereServices>();
builder.Services.AddScoped<IScheduleTransfer, ScheduleTransferServices>();

// Register IFxService with an HttpClient so OpenErFxService can be activated
builder.Services.AddHttpClient<IFxService, OpenErFxService>();

builder.Services.AddScoped<IWalletExchangeService, WalletExchangeService>();

//Db contection
builder.Services.AddDbContext<PaymentGatewayDbCOntext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// To serialize enum as string in json response
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
