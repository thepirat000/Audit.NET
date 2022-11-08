
var builder = WebApplication.CreateBuilder(args);

// Add services to the container and configure the audit global filter
builder.Services.AddControllers(mvc => 
{
    mvc.AuditSetupFilter(); 
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP context accessor
builder.Services.AddHttpContextAccessor();

// TODO: Configure your services
#if ServiceInterception
builder.Services.AddAuditedTransient<IValuesService, ValuesService>();
#else
builder.Services.AddTransient<IValuesService, ValuesService>();
#endif

#if EnableEntityFramework
// TODO: Configure your context connection
builder.Services.AddDbContext<MyContext>(_ => _.UseInMemoryDatabase("default"));
#endif

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

// Enable buffering for auditing HTTP request body
app.Use(async (context, next) => {
    context.Request.EnableBuffering();
    await next();
});

// Configure the audit middleware
app.AuditSetupMiddleware();

#if (EnableEntityFramework)
// Configure the Entity framework audit.
app.AuditSetupDbContext();
#endif

// Configure the audit output.
app.AuditSetupOutput();

app.Run();