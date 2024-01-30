using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace BetterFindsProject.Pages
{
    public class RegistoModel : PageModel
    {
        private readonly string _connectionString;

        // Booleanos para verificar se os Username e/ou Email existem já na base de dados
        public Boolean checkUsername {get;set;} = false;
        public Boolean checkEmail { get;set;} = false;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public RegistoModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados enviados pelo formulário HTLM ([BindProperty]serve para fazer essa ligação automáticamente)
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string FullName { get; set; }
            public string Username { get; set; }

            public string Email { get; set; }

            [MinLength(8, ErrorMessage = "A senha tem de ter no mínimo 8 caracteres.")]
            public string Password { get; set; }

            [Compare("Password", ErrorMessage = "As senhas não coincidem. Insira novamente")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            if (ModelState.IsValid)
            {
                // Verificar se a senha tem pelo menos 8 caracteres 
                if (Input.Password.Length < 8)
                {
                    ModelState.AddModelError(string.Empty, "A senha tem de ter no mínimo 8 carcteres.");
                    return Page();
                }

                // Verifica se as passwords inseridas coincidem
                if (Input.Password != Input.ConfirmPassword)
                {
                    ModelState.AddModelError(string.Empty, "As senhas não coincidem.");
                    return Page();
                }

                using (SqlConnection con = new SqlConnection(_connectionString))

                {
                    await con.OpenAsync();

                    // Verifica se o username já existe na base de dados
                    string queryCheckUser = "SELECT COUNT(*) FROM Client WHERE Username = @Username";

                    using (SqlCommand cmdCheckUser = new SqlCommand(queryCheckUser, con))
                    {
                        cmdCheckUser.Parameters.AddWithValue("@Username", Input.Username);

                        int usernameCount = (int)await cmdCheckUser.ExecuteScalarAsync();

                        if (usernameCount > 0)
                        {
                            checkUsername = true;
                            ModelState.AddModelError("Username", "Este username já está em uso.");
                            return Page();
                        }
                    }

                    // Verifica se o email já existe na base de dados  
                    string queryCheckEmail = "SELECT COUNT(*) FROM Client WHERE Email = @Email";

                    using (SqlCommand cmdCheckEmail = new SqlCommand(queryCheckEmail, con))
                    {
                        cmdCheckEmail.Parameters.AddWithValue("@Email", Input.Email);

                        int emailCount = (int)await cmdCheckEmail.ExecuteScalarAsync();

                        if (emailCount > 0)
                        {
                            checkEmail = true;
                            ModelState.AddModelError("Email", "Este email já está em uso. Utilize outro ou faça login.");
                            return Page();
                        }
                    }

                    // Insere novo cliente na base de dados 
                    string queryInsertUser = "INSERT INTO Client (FullName, Username, Email, Password) VALUES (@FullName, @Username, @Email, @Password)";

                    using (SqlCommand cmdInsertUser = new SqlCommand(queryInsertUser, con))
                    {
                        cmdInsertUser.Parameters.AddWithValue("@FullName", Input.FullName);
                        cmdInsertUser.Parameters.AddWithValue("@Username", Input.Username);
                        cmdInsertUser.Parameters.AddWithValue("@Email", Input.Email);
                        cmdInsertUser.Parameters.AddWithValue("@Password", Input.Password);

                        int rowsAffected = await cmdInsertUser.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Utilizador criado com sucesso
                            return RedirectToPage("/Login");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Failed to create user.");
                            return Page();
                        }
                    }
                }
            }

            // Se alguma coisa der errado volta para a página de registo
            return Page();
        }
    }
}





