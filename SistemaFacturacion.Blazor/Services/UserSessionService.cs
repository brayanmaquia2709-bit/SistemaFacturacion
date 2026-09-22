using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SistemaFacturacion.Core.Entities;

namespace SistemaFacturacion.Blazor.Services
{
    public class UserSessionService
    {
        private const string LocalStorageKey = "pos_user_session_v1";

        public Usuario? UsuarioActual { get; private set; }
        public bool IsLoaded { get; private set; } = false;

        public bool IsLoggedIn => UsuarioActual != null;

        public bool IsAdmin => IsLoggedIn && UsuarioActual != null &&
            (string.Equals(UsuarioActual.Rol, "Admin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(UsuarioActual.Rol, "Administrador", StringComparison.OrdinalIgnoreCase));

        public bool IsCajero => IsLoggedIn && UsuarioActual != null &&
            string.Equals(UsuarioActual.Rol, "Cajero", StringComparison.OrdinalIgnoreCase);

        public event Action? OnChange;

        public async Task RestoreUserAsync(IJSRuntime js)
        {
            try
            {
                var json = await js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var usr = JsonSerializer.Deserialize<Usuario>(json);
                    if (usr != null && usr.Activo)
                    {
                        UsuarioActual = usr;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UserSessionService] Error al restaurar sesión desde localStorage: " + ex.Message);
            }
            finally
            {
                IsLoaded = true;
                NotifyStateChanged();
            }
        }

        public async Task SetUserAsync(Usuario usuario, IJSRuntime js)
        {
            UsuarioActual = usuario;
            IsLoaded = true;
            NotifyStateChanged();

            try
            {
                var json = JsonSerializer.Serialize(usuario);
                await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UserSessionService] Error guardando sesión en localStorage: " + ex.Message);
            }
        }

        public async Task LogoutAsync(IJSRuntime js, NavigationManager? navigation = null)
        {
            UsuarioActual = null;
            IsLoaded = true;
            NotifyStateChanged();

            try
            {
                await js.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UserSessionService] Error eliminando sesión de localStorage: " + ex.Message);
            }

            if (navigation != null)
            {
                navigation.NavigateTo("/", forceLoad: false);
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
