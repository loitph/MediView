using MediView.BuildingBlocks.Application;
using MediView.Reporting.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Reporting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services) =>
        services
            .AddApplication(typeof(DependencyInjection).Assembly)
            .AddScoped<ReportWriter>();
}
