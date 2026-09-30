// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SIARAWEB.Models; // Asegúrate de que este namespace sea el de tu proyecto

namespace SIARAWEB.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Número de Teléfono")]
            public string PhoneNumber { get; set; }

            // --- CAMPOS PERSONALIZADOS DE SIARA ---
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [Display(Name = "Nombre Completo")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "El RFC es obligatorio")]
            [StringLength(13, ErrorMessage = "El RFC no puede exceder 13 caracteres")]
            [Display(Name = "RFC")]
            public string RFC { get; set; }

            [Required(ErrorMessage = "La CURP es obligatoria")]
            [StringLength(18, ErrorMessage = "La CURP no puede exceder 18 caracteres")]
            [Display(Name = "CURP")]
            public string CURP { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = user.FullName, // Carga el dato de la BD
                RFC = user.RFC,           // Carga el dato de la BD
                CURP = user.CURP          // Carga el dato de la BD
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Error inesperado al intentar configurar el número de teléfono.";
                    return RedirectToPage();
                }
            }

            // --- GUARDAR CAMPOS PERSONALIZADOS EN LA BD ---
            user.FullName = Input.FullName;
            user.RFC = Input.RFC?.ToUpper();  // Guardar en mayúsculas
            user.CURP = Input.CURP?.ToUpper(); // Guardar en mayúsculas

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                StatusMessage = "Error inesperado al intentar actualizar el perfil.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Tu perfil ha sido actualizado con éxito.";
            return RedirectToPage();
        }
    }
}