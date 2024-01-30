using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace BetterFindsProject.Pages
{
    // Indica-nos que apenas utilizadores autenticados podem aceder a esta página
    [Authorize]
    public class CreateAuctionModel : PageModel
    {
        private readonly string _connectionString;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public CreateAuctionModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados enviados pelo formulário HTLM ([BindProperty] serve para fazer essa ligação automáticamente) 
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public int AuctionTime { get; set; }
            public string Description { get; set; }
            public int MinimumBid { get; set; }
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

        /* Processa os dados do formulário enviado pelo usutilizador, valida os dados, e insere um novo leilão 
         * na base de dados, se todas as validações forem realizadas com sucesso */
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // A partir dos cookies obtemos o email do cliente que efetuou login
                    string email = HttpContext.User.Identity.Name;

                    // Obtem o tempo atual
                    DateTime currentTime = DateTime.Now;

                    // Obtem o tempo final adicionando ao tempo atual o tempo especificado pelo cliente
                    DateTime endTime = currentTime.AddHours(Input.AuctionTime);

                    // Executar a consulta SQL para obter o ClientId do cliente que está logado
                    string querySelectClientId = "SELECT ClientId FROM Client WHERE Email = @Email";

                    int clientId;

                    // É criado o comando SQL respetivo à consulta do ClientId
                    using (SqlCommand cmdSelectClientId = new SqlCommand(querySelectClientId, con))
                    {
                        // Adiciona o parâmetro @Email à consulta SQL para ir buscar o Cliente pelo endereço de email
                        cmdSelectClientId.Parameters.AddWithValue("@Email", email);

                        /* Este método executa a consulta na base de dados e retorna o ClientId associado ao email */
                        var result = await cmdSelectClientId.ExecuteScalarAsync();

                        if (result != null)
                        {
                            // Se o resultado for encontrado é convertido para um int
                            clientId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // O utilizador não existe na base de dados
                            ModelState.AddModelError(nameof(email), "Utilizador não existe.");
                            return Page();
                        }
                    }

                    if (Input.Description.Length > 300)
                    {
                        ModelState.AddModelError(nameof(Input.Description), "A descrição não pode exceder 300 caracteres.");
                        return Page();
                    }

                    // Insere o leilão na base de dados
                    string queryInsertAuction = "INSERT INTO Auction (Name, Price, AuctionTime, StartTime, EndTime, Description, MinimumBid, ClientId, Licitacao) VALUES (@Name, @Price, @AuctionTime, @StartTime, @EndTime, @Description, @MinimumBid, @ClientId, @Licitacao)";

                    // É criado o comando respetivo à inserção das informações do leilão na base de dados
                    using (SqlCommand cmdInsertAuction = new SqlCommand(queryInsertAuction, con))
                    {
                        /* Os valores dos parâmetros são extraídos das propriedades do objeto Input, que contém 
                         * os dados fornecidos pelo usuário */
                        cmdInsertAuction.Parameters.AddWithValue("@Name", Input.Name);
                        cmdInsertAuction.Parameters.AddWithValue("@Price", Input.Price);
                        cmdInsertAuction.Parameters.AddWithValue("@AuctionTime", Input.AuctionTime);
                        cmdInsertAuction.Parameters.AddWithValue("@StartTime", currentTime);
                        cmdInsertAuction.Parameters.AddWithValue("@EndTime", endTime);
                        cmdInsertAuction.Parameters.AddWithValue("@Description", Input.Description);
                        cmdInsertAuction.Parameters.AddWithValue("@MinimumBid", Input.MinimumBid);
                        cmdInsertAuction.Parameters.AddWithValue("@ClientId", clientId);
                        cmdInsertAuction.Parameters.AddWithValue("@Licitacao", 0);

                        /* Método chamado para executar a consulta SQL de inserção. Este método retorna o número de linhas 
                         * afetadas pela operação de inserção.*/
                        int rowsAffected = await cmdInsertAuction.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Se mais que uma linha for "afetada" (inserida na Base de Dados) é redirecionado para a págian dos leilões
                            return RedirectToPage("/Leiloes");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Falha ao criar o leilão. Nenhuma linha afetada.");
                            return Page();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Lidar com exceção específica de SQL
                ModelState.AddModelError(string.Empty, $"Erro SQL: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                // Lidar com outras exceções
                ModelState.AddModelError(string.Empty, $"Ocorreu um erro: {ex.Message}");
            }

            return Page();
        }
    }
}

