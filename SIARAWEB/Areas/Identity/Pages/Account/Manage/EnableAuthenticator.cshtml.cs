// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using SIARAWEB.Models;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;

namespace SIARAWEB.Areas.Identity.Pages.Account.Manage
{
    public class EnableAuthenticatorModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UrlEncoder _urlEncoder;
        private readonly ILogger<EnableAuthenticatorModel> _logger;

        public EnableAuthenticatorModel(
            UserManager<ApplicationUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        // Agrega esta propiedad para exponer la URL de la imagen QR al archivo .cshtml
        public string QrCodeImageUrl { get; set; }

        // Agrega la clave para mostrar en la vista
        public string SharedKey { get; set; }

        // Mensajes temporales
        [TempData]
        public string StatusMessage { get; set; }

        // Modelo para el código de verificación ingresado por el usuario
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "El código debe tener entre {2} y {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Código")]
            public string Code { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Normalizar el código (quitar espacios y guiones)
            var verificationCode = Input.Code?.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Código no válido.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", await _userManager.GetUserIdAsync(user));

            StatusMessage = "La aplicación de autenticación ha sido habilitada correctamente.";
            return RedirectToPage("./TwoFactorAuthentication");
        }

        // Carga la clave compartida formateada y genera la imagen QR en base64 para la vista
        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            // Obtener o crear clave
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);
            var authenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);

            // Generar imagen QR usando QRCoder y convertir a data URI (base64 PNG)
            using (var generator = new QRCodeGenerator())
            {
                var data = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
                var png = new PngByteQRCode(data);
                // Reduced graphic size parameter (antes 20). Ajusta entre 8 y 16 según calidad deseada.
                var bytes = png.GetGraphic(12);
                QrCodeImageUrl = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
            }
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            // Ejemplo de URI compatible con Google Authenticator
            // otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}&digits=6
            var issuer = _urlEncoder.Encode("SIARAWEB");
            var emailEncoded = _urlEncoder.Encode(email);
            return $"otpauth://totp/{issuer}:{emailEncoded}?secret={unformattedKey}&issuer={issuer}&digits=6";
        }
    }
}
