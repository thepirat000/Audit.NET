var builder = WebApplication.CreateBuilder(args);

// Add services to the container and configure the audit global filter
builder.Services.AddControllers(mvc => 
{
    mvc.AuditSetupMvcFilter(); 
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Configure a custom scope factory and data provider
builder.Services.AddAuditScopeFactory();
builder.Services.AddAuditDataProvider();

// TODO: Configure your services
#if ServiceInterception
builder.Services.AddScopedAuditedService<IValuesService, ValuesService>();
#else
builder.Services.AddScoped<IValuesService, ValuesService>();
#endif

#if EnableEntityFramework
// TODO: Configure your context connection
builder.Services.AddDbContextFactory<MyContext>(_ => _.UseInMemoryDatabase("default"), ServiceLifetime.Scoped);
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

await app.RunAsync();