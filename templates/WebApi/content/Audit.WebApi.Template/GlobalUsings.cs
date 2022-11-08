// Global using directives
#if EnableEntityFramework
global using Microsoft.EntityFrameworkCore;
global using Audit.EntityFramework;
global using Audit.WebApi.Template.Services.Database;
#endif
#if ServiceInterception
global using Audit.DynamicProxy;
#endif
global using Audit.Core;
global using Audit.WebApi;
global using Audit.WebApi.Template;
global using Audit.WebApi.Template.Services;