using System;
using System.Collections.Generic;
using System.Linq;
using FogonDesk.Application.Common;
using FogonDesk.Application.Contracts;
using FogonDesk.Application.Models;
using FogonDesk.Domain.Common;

namespace FogonDesk.Application.Services
{
    public sealed class UserAdministrationService : IUserAdministrationService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IClock clock;

        public UserAdministrationService(IUserRepository userRepository, IPasswordHasher passwordHasher, IClock clock)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.clock = clock;
        }

        public IList<UserManagementView> GetUsers()
        {
            return this.userRepository.LoadUsers();
        }

        public OperationResult CreateUser(CreateUserRequest request)
        {
            if (request == null)
            {
                return OperationResult.Fail("La captura del usuario es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return OperationResult.Fail("Debes capturar el usuario.");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return OperationResult.Fail("Debes capturar el nombre visible.");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
            {
                return OperationResult.Fail("La contraseña debe tener al menos 6 caracteres.");
            }

            var roleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? SystemRoles.Cashier : request.RoleCode.Trim().ToLowerInvariant();
            if (!SystemRoles.All.Contains(roleCode))
            {
                return OperationResult.Fail("El rol seleccionado no es válido.");
            }

            var existingUser = this.userRepository.FindByUsername(request.Username.Trim().ToLowerInvariant());
            if (existingUser != null)
            {
                return OperationResult.Fail("Ya existe un usuario con ese nombre.");
            }

            var passwordHash = this.passwordHasher.HashPassword(request.Password.Trim());
            this.userRepository.CreateUser(
                new UserAccountSeed
                {
                    Username = request.Username.Trim().ToLowerInvariant(),
                    DisplayName = request.DisplayName.Trim(),
                    PasswordHashBase64 = passwordHash.HashBase64,
                    PasswordSaltBase64 = passwordHash.SaltBase64,
                    RoleCode = roleCode
                },
                request.IsActive,
                this.clock.UtcNow);

            return OperationResult.Ok("Usuario agregado correctamente.");
        }

        public OperationResult UpdateUser(UpdateUserRequest request)
        {
            if (request == null || request.UserId <= 0)
            {
                return OperationResult.Fail("Debes seleccionar un usuario válido.");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return OperationResult.Fail("Debes capturar el usuario.");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return OperationResult.Fail("Debes capturar el nombre visible.");
            }

            var roleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? SystemRoles.Cashier : request.RoleCode.Trim().ToLowerInvariant();
            if (!SystemRoles.All.Contains(roleCode))
            {
                return OperationResult.Fail("El rol seleccionado no es válido.");
            }

            var existingUser = this.userRepository.FindById(request.UserId);
            if (existingUser == null)
            {
                return OperationResult.Fail("El usuario seleccionado ya no existe.");
            }

            var userByName = this.userRepository.FindByUsername(request.Username.Trim().ToLowerInvariant());
            if (userByName != null && userByName.UserId != request.UserId)
            {
                return OperationResult.Fail("Ya existe un usuario con ese nombre.");
            }

            string passwordHashBase64 = null;
            string passwordSaltBase64 = null;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password.Trim().Length < 6)
                {
                    return OperationResult.Fail("La nueva contraseña debe tener al menos 6 caracteres.");
                }

                var passwordHash = this.passwordHasher.HashPassword(request.Password.Trim());
                passwordHashBase64 = passwordHash.HashBase64;
                passwordSaltBase64 = passwordHash.SaltBase64;
            }

            this.userRepository.UpdateUser(
                request,
                passwordHashBase64,
                passwordSaltBase64,
                this.clock.UtcNow);

            return OperationResult.Ok("Usuario actualizado correctamente.");
        }

        public OperationResult DeleteUser(int userId)
        {
            if (userId <= 0)
            {
                return OperationResult.Fail("Debes seleccionar un usuario válido.");
            }

            var existingUser = this.userRepository.FindById(userId);
            if (existingUser == null)
            {
                return OperationResult.Fail("El usuario seleccionado ya no existe.");
            }

            this.userRepository.DeleteUser(userId, this.clock.UtcNow);
            return OperationResult.Ok("Usuario eliminado correctamente.");
        }
    }
}