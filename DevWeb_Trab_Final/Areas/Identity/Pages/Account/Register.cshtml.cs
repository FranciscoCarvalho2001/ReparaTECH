﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

using DevWeb_Trab_Final.Data;
using DevWeb_Trab_Final.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;

namespace DevWeb_Trab_Final.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<DevWeb_Trab_Final_User> _signInManager;
        private readonly UserManager<DevWeb_Trab_Final_User> _userManager;
        private readonly IUserStore<DevWeb_Trab_Final_User> _userStore;
        private readonly IUserEmailStore<DevWeb_Trab_Final_User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        private readonly DevWeb_Trab_FinalContext _context;

        public RegisterModel(
            UserManager<DevWeb_Trab_Final_User> userManager,
            IUserStore<DevWeb_Trab_Final_User> userStore,
            SignInManager<DevWeb_Trab_Final_User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            DevWeb_Trab_FinalContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            
            [EmailAddress(ErrorMessage = "O {0} não está escrito corretamente")]
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório")]
            [RegularExpression("[a-z._0-9]+@+[a-z]+.com")]
            [StringLength(40)]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório")]
            [StringLength(100, ErrorMessage = "A {0} precisa de ter no mínimo entre {2} e no máximo {1} caractéres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar Password")]
            [Compare("Password", ErrorMessage = "A Nova Password e a Confirmação Password não são iguais.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Referencia para a Classe Funcionarios
            /// </summary>
            public Funcionarios Funcionario { get; set; }

            /// <summary>
            /// Referencia para a Classe Clientes
            /// </summary>
            public Clientes Cliente { get; set; }
            /// <summary>
            /// Flag para se saber se o utilizador é Administrador
            /// </summary>
            public bool flagAdmin { get; set; }
        }

        /// <summary>
        /// método para reagir aos pedidos em GET
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// método que é acionado quando se enviam dados em modo POST
        /// é o que adiciona dados á DB
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            //      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // se os dados forem corretos
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // Regista o tempo em que foi criado o Utilizador
                user.DataRegisto = DateTime.Now;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // cheack para saber se é Administrador
                if (Input.flagAdmin == true) {
                    // iguala o nome do Funcionario ao Nome de Utilizador
                    user.NomeUtilizador = Input.Funcionario.Nome;
                    // obtem o número de Telemovel do Funcionario
                    user.PhoneNumber = Input.Funcionario.Telemovel;
                } else {
                    // iguala o nome do Cliente ao Nome de Utilizador
                    user.NomeUtilizador = Input.Cliente.Nome;
                    // obtem o número de Telemovel do Cliente
                    user.PhoneNumber = Input.Cliente.Telemovel;
                }

                // efetiva criação do USER
                var result = await _userManager.CreateAsync(user, Input.Password);

                // se há sucesso na criação do USER
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // cheack para saber se é Administrador
                    if (Input.flagAdmin == true) {

                        // adicionar ao Role "Funcionario"
                        await _userManager.AddToRoleAsync(user, "Funcionario");

                        // *******************************************
                        // adicionar os dados do Funcionario á DB
                        // *******************************************

                        //atualizar os dados do objeto FUNCIONARIO
                        Input.Funcionario.Email = Input.Email;
                        Input.Funcionario.UserId = user.Id;

                        try {
                            //adiconar os dados á DB
                            _context.Add(Input.Funcionario);
                            await _context.SaveChangesAsync();
                        } catch (Exception) {
                            //remover os dados á DB
                            _context.Remove(Input.Funcionario);
                            await _context.SaveChangesAsync();
                        }
                    } else {

                        // adicionar ao Role "Cliente"
                        await _userManager.AddToRoleAsync(user, "Cliente");

                        // *******************************************
                        // adicionar os dados do Cliente á DB
                        // *******************************************

                        //atualizar os dados do objeto Cliente
                        Input.Cliente.Email = Input.Email;
                        Input.Cliente.UserId = user.Id;

                        try {
                            //adiconar os dados á DB
                            _context.Add(Input.Cliente);
                            await _context.SaveChangesAsync();
                        } catch (Exception) {
                            //remover os dados á DB
                            _context.Remove(Input.Cliente);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // *******************************************

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private DevWeb_Trab_Final_User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<DevWeb_Trab_Final_User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(DevWeb_Trab_Final_User)}'. " +
                    $"Ensure that '{nameof(DevWeb_Trab_Final_User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<DevWeb_Trab_Final_User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<DevWeb_Trab_Final_User>)_userStore;
        }
    }
}
