using GestionCalidad.Models;
using GestionCalidad.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad
{
    public static class AppAuth
    {
        private static AuthService _authService;

        public static AuthService AuthService
        {
            get
            {
                if (_authService == null)
                {
                    throw new InvalidOperationException("AuthService no ha sido inicializado. Llame a AppAuth.Initialize() primero.");
                }
                return _authService;
            }
            set
            {
                _authService = value;
            }
        }

        public static bool EstaAutenticado => _authService?.EstaAutenticado() ?? false;
        public static bool EsAdministrador => _authService?.EsAdministrador() ?? false;

        public static Usuario UsuarioActual
        {
            get
            {
                if (_authService == null)
                {
                    throw new InvalidOperationException("AuthService no ha sido inicializado.");
                }
                return _authService.UsuarioActual;
            }
        }

        public static string UsuarioId => UsuarioActual?.Id;
        public static string UsuarioNombre => UsuarioActual?.NombreCompleto;

        // Método de inicialización seguro
        public static void Initialize(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        // Método para verificar si está inicializado sin lanzar excepción
        public static bool IsInitialized() => _authService != null;
    }
}