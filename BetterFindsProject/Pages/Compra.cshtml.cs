using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace BetterFindsProject.Pages
{
    // Indica-nos que apenas utilizadores autenticados podem aceder a esta página
    [Authorize]
    public class CompraModel : PageModel
    {
        private readonly string _connectionString;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public CompraModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados enviados pelo formulário HTLM ([BindProperty] serve para fazer essa ligação automáticamente) 
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public int ClientId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string City { get; set; }
            public string District { get; set; }
            public string Code { get; set; }
            public string CardName { get; set; }
            public string CardNumber { get; set; }
            public string MonthExp { get; set; }
            public int YearExp { get; set; }
            public int CvvNumber { get; set; }
        }

        // Verifica se o utilizador está autenticado
        public IActionResult OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Page();
            }
            return RedirectToPage("/Login");
        }

        /* Processa os dados do formulário enviado pelo usutilizador, valida os dados, e insere o pagamento
         * na base de dados, se todas as validações forem realizadas com sucesso */
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // Obter o email a partir dos cookies de autenticação
                    string email = HttpContext.User.Identity.Name;

                    // Consulta SQL para obter o ClientId com base no email
                    string querySelectClientId = "SELECT ClientId FROM Client WHERE Email = @Email";

                    int clientId;

                    using (SqlCommand cmdSelectClientId = new SqlCommand(querySelectClientId, con))
                    {
                        cmdSelectClientId.Parameters.AddWithValue("@Email", email);

                        // Utilizamos o ExecuteScalarAsync para obter um valor único
                        var result = await cmdSelectClientId.ExecuteScalarAsync();

                        if (result != null)
                        {
                            // Se o resultado for encontrado é convertido para um int
                            clientId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // O utilizador não existe na base de dados
                            ModelState.AddModelError(string.Empty, "Utilizador não existe.");
                            return Page();
                        }
                    }

                    // Insere os dados de pagamento na tabela Payment
                    string queryInsertPayment = "INSERT INTO Payment (ClientId, FullName, Email, City, District, Code, CardName, CardNumber, MonthExp, YearExp, CvvNumber) " +
                        "VALUES (@ClientId, @FullName, @Email, @City, @District, @Code, @CardName, @CardNumber, @MonthExp, @YearExp, @CvvNumber)";

                    using (SqlCommand cmdInsertPayment = new SqlCommand(queryInsertPayment, con))
                    {
                        cmdInsertPayment.Parameters.AddWithValue("@ClientId", clientId);
                        cmdInsertPayment.Parameters.AddWithValue("@FullName", Input.FullName);
                        cmdInsertPayment.Parameters.AddWithValue("@Email", email);
                        cmdInsertPayment.Parameters.AddWithValue("@City", Input.City);
                        cmdInsertPayment.Parameters.AddWithValue("@District", Input.District);
                        cmdInsertPayment.Parameters.AddWithValue("@Code", Input.Code);
                        cmdInsertPayment.Parameters.AddWithValue("@CardName", Input.CardName);
                        cmdInsertPayment.Parameters.AddWithValue("@CardNumber", Input.CardNumber);
                        cmdInsertPayment.Parameters.AddWithValue("@MonthExp", Input.MonthExp);
                        cmdInsertPayment.Parameters.AddWithValue("@YearExp", Input.YearExp);
                        cmdInsertPayment.Parameters.AddWithValue("@CvvNumber", Input.CvvNumber);

                        int rowsAffected = await cmdInsertPayment.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return RedirectToPage("/Index"); // Redireciona para a página principal após o pagamento ser efetuado com sucesso
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Falha ao inserir dados de pagamento.");
                            return Page();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                ModelState.AddModelError(string.Empty, $"Erro SQL: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Ocorreu um erro: {ex.Message}");
            }

            return Page();
        }
    }
}


