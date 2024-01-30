using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;

namespace BetterFindsProject.Pages
{
    // Indica-nos que apenas utilizadores autenticados podem aceder a esta página
    [Authorize]
    public class LeiloesModel : PageModel
    {
        private readonly string _connectionString;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public LeiloesModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados enviados pelo formulário HTLM ([BindProperty]serve para fazer essa ligação automáticamente) 
        public class AuctionItem
        {
            public int AuctionId { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }
            public int AuctionTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Description { get; set; }
            public int MinimumBid { get; set; }
            public int ClientId { get; set; }
            public bool IsAuctionEnded { get; set; }
            public bool IsWinner { get; set; }
            public bool IsCreator { get; set; }

        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public int Licitacao { get; set; }
        }

        public List<AuctionItem> Auctions { get; set; } = new List<AuctionItem>();

        public async Task OnGetAsync()
        {
            // Verifica se o utilizador está autenticado
            if (User.Identity.IsAuthenticated)
            {
                // Vai buscar os leilões à base de dados
                Auctions = await GetAuctionsFromDatabaseAsync();

                // Itera sobre os leilões 
                foreach (var auction in Auctions)
                {
                    // Verifica se os leilões terminararem comprarando o tempo atual com o tempo de fim do leilão
                    auction.IsAuctionEnded = DateTime.Now > auction.EndTime;

                    // Verifica se o cliente logado é o criador do leilão
                    auction.IsCreator = await IsCreator(auction.AuctionId);

                    if (auction.IsAuctionEnded)
                    {
                        // Verifica se o cliente logado é o vencedor do leilão se o leilão terminou
                        auction.IsWinner = await IsWinner(auction.AuctionId);
                    }
                }
            }
            else
            {
                // O utilizador não está autenticado e redireciona para a página de login
                RedirectToPage("/Login");
            }
        }

        // Esta função vai buscar todos os leilões à base de dados
        private async Task<List<AuctionItem>> GetAuctionsFromDatabaseAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var auctionItems = await connection.QueryAsync<AuctionItem>("SELECT * FROM Auction");
                return auctionItems.ToList();
            }
        }

        private async Task<bool> IsWinner(int auctionId)
        {
            // Vai buscar os Id´s do vencedor do leilão e do cliente logado
            int? highestBidderClientId = await GetHighestBidderClientId(auctionId);
            int? loggedInClientId = await GetLoggedInClientId();

            // Verifica se o vencedor do leilão é o cliente que está logado
            return highestBidderClientId.HasValue && highestBidderClientId.Value == loggedInClientId;
        }

        private async Task<bool> IsCreator(int auctionId)
        {
            // Vai buscar os Id´s do criador do leilão e do cliente logado
            int? auctionCreatorClientId = await GetAuctionCreatorClientId(auctionId);
            int? loggedInClientId = await GetLoggedInClientId();

            // Verifica se o criador do leilão é o cliente que está logado
            return auctionCreatorClientId.HasValue && auctionCreatorClientId.Value == loggedInClientId;
        }

        // Consulta para obter o ClientId do maior licitante do leilão 
        private async Task<int?> GetHighestBidderClientId(int auctionId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT TOP 1 ClientId FROM Bid WHERE AuctionId = @AuctionId ORDER BY Value DESC";

                return await connection.ExecuteScalarAsync<int?>(query, new { AuctionId = auctionId });
            }
        }

        // Consulta para obter o ClientId do criador do leilão
        private async Task<int?> GetAuctionCreatorClientId(int auctionId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT ClientId FROM Auction WHERE AuctionId = @AuctionId";

                return await connection.ExecuteScalarAsync<int?>(query, new { AuctionId = auctionId });
            }
        }

        // Consulta para obter o ClientId do cliente logado
        private async Task<int?> GetLoggedInClientId()
        {
            if (User.Identity.IsAuthenticated)
            {
                string userEmail = User.Identity.Name;
                string queryGetClientId = "SELECT ClientId FROM Client WHERE Email = @UserEmail";

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    // Abre uma conexão assíncrina com a base de dados
                    await con.OpenAsync();

                    // Retorna o ID do cliente obtido pela consulta SQL. 
                    return await con.ExecuteScalarAsync<int?>(queryGetClientId, new { UserEmail = userEmail });
                }
            }

            return null;
        }

        /* Processa os dados do formulário enviado pelo usutilizador, valida os dados, e atualiza a licitação do leilão 
         * na base de dados, se todas as validações forem realizadas com sucesso */
        public async Task<IActionResult> OnPostAsync(int auctionId)
        {
            try
            {
                // Verifica se o usuário está autenticado
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Login");
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    var auctionInfo = await con.QueryFirstOrDefaultAsync<AuctionItem>("SELECT * FROM Auction WHERE AuctionId = @AuctionId", new { AuctionId = auctionId });

                    // Não permite fazer licitação inferior à MinimumBid
                    if (Input.Licitacao < auctionInfo.MinimumBid)
                    {
                        ModelState.AddModelError(string.Empty, "A sua licitação é inferior à licitação mínima estabelecida pelo vendedor.");
                        return RedirectToPage("/Leiloes");
                    }

                    // Não permite fazer licitação inferior ao preço atual
                    if (Input.Licitacao < auctionInfo.Price)
                    {
                        ModelState.AddModelError(string.Empty, "No momento existe uma licitação maior");
                        return RedirectToPage("/Leiloes");
                    }

                    // Atualiza o preço na tabela de leilões
                    string queryUpdateAuction = "UPDATE Auction SET Licitacao = @Licitacao, Price = @Licitacao WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmdUpdateAuction = new SqlCommand(queryUpdateAuction, con))
                    {
                        cmdUpdateAuction.Parameters.AddWithValue("@Licitacao", Input.Licitacao);
                        cmdUpdateAuction.Parameters.AddWithValue("@AuctionId", auctionId);

                        int rowsAffected = await cmdUpdateAuction.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return RedirectToPage("/Leiloes");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Failed to update auction. No rows affected.");
                            return Page();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                ModelState.AddModelError(string.Empty, $"SQL Error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }
            return Page();
        }

    }
}