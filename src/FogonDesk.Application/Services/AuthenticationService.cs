using System;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;

namespace FogonDesk.Application.Services
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IClock clock;
        private readonly IAppLogger logger;

        public AuthenticationService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IClock clock,
            IAppLogger logger)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.clock = clock;
            this.logger = logger;
        }

        public OperationResult<AuthenticatedUserView> Authenticate(AuthenticationRequest request)
        {
            if (request == null)
            {
                return OperationResult<AuthenticatedUserView>.Fail("La solicitud de acceso es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return OperationResult<AuthenticatedUserView>.Fail("Usuario y contraseña son obligatorios.");
            }

            try
            {
                var user = this.userRepository.FindByUsername(request.Username.Trim().ToLowerInvariant());
                if (user == null || !user.IsActive)
                {
                    return OperationResult<AuthenticatedUserView>.Fail("Credenciales inválidas.");
                }

                if (!this.passwordHasher.Verify(request.Password.Trim(), user.PasswordSaltBase64, user.PasswordHashBase64))
                {
                    return OperationResult<AuthenticatedUserView>.Fail("Credenciales inválidas.");
                }

                var signedInUtc = this.clock.UtcNow;
                this.userRepository.UpdateLastLogin(user.UserId, signedInUtc);
                this.logger.Info("Inicio de sesión correcto para " + user.Username + ".");

                return OperationResult<AuthenticatedUserView>.Ok(
                    new AuthenticatedUserView
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        RoleCode = user.RoleCode,
                        SignedInUtc = signedInUtc
                    },
                    "Acceso correcto.");
            }
            catch (Exception exception)
            {
                this.logger.Error("Fallo el proceso de autenticación.", exception);
                return OperationResult<AuthenticatedUserView>.Fail("No fue posible validar las credenciales.");
            }
        }
    }
}
