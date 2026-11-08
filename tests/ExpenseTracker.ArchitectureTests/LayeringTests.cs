using System.Reflection;
using NetArchTest.Rules;

namespace ExpenseTracker.ArchitectureTests;

/// <summary>
/// Enforces the dependency rule: dependencies point inwards (Api → Infrastructure → Application → Domain).
/// A failing test here means a layer has started to depend on something it shouldn't.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    private const string ApplicationNs = "ExpenseTracker.Application";
    private const string InfrastructureNs = "ExpenseTracker.Infrastructure";
    private const string ApiNs = "ExpenseTracker.Api";

    [Fact]
    public void Domain_depends_on_nothing_but_the_BCL()
    {
        AssertNoDependencies(Domain, ApplicationNs, InfrastructureNs, ApiNs,
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Extensions");
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_presentation()
    {
        AssertNoDependencies(Application, InfrastructureNs, ApiNs,
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Data.Sqlite");
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_presentation()
    {
        // ASP.NET Core Identity's store (Microsoft.AspNetCore.Identity*) is a persistence library with no
        // HTTP dependency, so it is allowed; anything from the web stack is not.
        AssertNoDependencies(Infrastructure, ApiNs,
            "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.Routing",
            "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Authentication", "Microsoft.AspNetCore.Authorization",
            "Microsoft.AspNetCore.SignalR", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server");
    }

    [Fact]
    public void Identity_services_are_hidden_behind_interfaces()
    {
        var result = Types.InAssembly(Infrastructure)
            .That().ResideInNamespace("ExpenseTracker.Infrastructure.Identity")
            .And().HaveNameEndingWith("Service").Or().HaveNameEndingWith("Provider")
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Use_case_implementations_are_hidden_behind_interfaces()
    {
        var result = Types.InAssembly(Application)
            .That().HaveNameEndingWith("Service").And().AreClasses()
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Persistence_adapters_are_internal()
    {
        var result = Types.InAssembly(Infrastructure)
            .That().HaveNameEndingWith("Repository").Or().HaveNameEndingWith("Queries")
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Domain_entities_have_no_public_setters()
    {
        var entities = Domain.GetTypes().Where(t => t.IsSubclassOf(typeof(Domain.Common.Entity)));
        var publicSetters = entities
            .SelectMany(t => t.GetProperties())
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .ToList();

        publicSetters.ShouldBeEmpty();
    }

    private static void AssertNoDependencies(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
