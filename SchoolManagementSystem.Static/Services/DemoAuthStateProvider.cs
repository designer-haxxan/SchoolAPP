using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace SchoolManagementSystem.Services;

/// <summary>
/// Pick-a-role sign-in for the static demo. The chosen role is remembered in localStorage
/// so a page refresh stays signed in. There are no passwords: this is a demo, not security.
/// </summary>
public class DemoAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
    private const string StorageKey = "sms-demo-role";
    private static readonly string[] Roles = ["Admin", "Accountant", "Teacher"];
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public static IReadOnlyList<string> AvailableRoles => Roles;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? role = null;
        try
        {
            role = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch (Exception)
        {
            // Storage can be blocked (private mode); stay signed out.
        }

        return role is not null && Roles.Contains(role)
            ? new AuthenticationState(Build(role))
            : new AuthenticationState(Anonymous);
    }

    public async Task SignInAsync(string role)
    {
        if (!Roles.Contains(role)) throw new ArgumentException($"Unknown demo role '{role}'.", nameof(role));

        try { await js.InvokeVoidAsync("localStorage.setItem", StorageKey, role); }
        catch (Exception) { /* keep working without persistence */ }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Build(role))));
    }

    public async Task SignOutAsync()
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", StorageKey); }
        catch (Exception) { }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal Build(string role) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, $"{role} (demo)"), new Claim(ClaimTypes.Role, role)],
        authenticationType: "demo"));
}
