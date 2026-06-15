using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;
using UserService.Application.Services;

namespace UserService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserAppService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
