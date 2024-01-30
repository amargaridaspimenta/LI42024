using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BetterFindsProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly string _connectionString;

        // Boolean para verificar se o email existe na base de dados
        public Boolean checkEmailLogin { get; set; } = false;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public LoginModel(ILogger<LoginModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados enviados pelo formulário HTLM ([BindProperty]serve para fazer essa ligação automáticamente) 
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required] // Obrigatório
            [EmailAddress]
            public string Email { get; set; }

            [Required] // Obrigatório
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        // Método chamado quando o formulário de login é submetido
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Estabelece a conexão com a base de dados
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // Consulta para verificar se o usuário com o email e senha fornecidos existe na base de dados
                    string queryCheckUser = "SELECT ClientId FROM Client WHERE Email = @Email AND Password = @Password";

                    using (SqlCommand cmdCheckUser = new SqlCommand(queryCheckUser, con))
                    {
                        cmdCheckUser.Parameters.Add("@Email", SqlDbType.VarChar).Value = Input.Email;
                        cmdCheckUser.Parameters.Add("@Password", SqlDbType.VarChar).Value = Input.Password;

                        var userId = await cmdCheckUser.ExecuteScalarAsync();

                        if (userId != null)
                        {
                            // Verificação bem-sucedida na base de dados
                            // Autenticar o utilizador e criar claims para os cookies
                            List<Claim> claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, Input.Email),
                                new Claim(ClaimTypes.Name, Input.Email)
                            };

                            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                            // Autentica o utilizador e cria o cookie de autenticação
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                            // Redireciona para a página do perfil registado
                            return RedirectToPage("/Perfil", new { userId = userId });
                        }
                        else
                        {
                            // Verificar se o e-mail existe na base de dados
                            string queryCheckEmail = "SELECT COUNT(*) FROM Client WHERE Email = @Email";

                            using (SqlCommand cmdCheckEmail = new SqlCommand(queryCheckEmail, con))
                            {
                                cmdCheckEmail.Parameters.Add("@Email", SqlDbType.VarChar).Value = Input.Email;

                                int emailCount = (int)await cmdCheckEmail.ExecuteScalarAsync();

                                if (emailCount > 0)
                                {
                                    // E-mail existe, mas as credenciais são inválidas
                                    checkEmailLogin = true;
                                    ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                                    return Page();
                                }
                                else
                                {
                                    // E-mail não existe, redireciona para o registo
                                    return RedirectToPage("/Registo");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao verificar usuário: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Erro ao verificar as credenciais.");
            }

            // Se algo der errado, ou se as credenciais são inválidas, retorna para a página de login
            return Page();
        }
    }
}







