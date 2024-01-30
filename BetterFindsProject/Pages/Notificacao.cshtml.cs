using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFindsProject.Pages
{
    public class NotificacaoModel : PageModel
    {
        private readonly string _connectionString;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public NotificacaoModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Booleano que indica se cliente é vencedor do leilão
        public bool IsWinnerNotification { get; set; } = false;

        /* Esta função vai buscar os Id dos leilões que já terminaram de modo a obter o seu vencedor */
        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    string queryGetEndedAuctions = "SELECT AuctionId FROM Auction WHERE EndTime < GETDATE()";
                    using (var endedAuctionsCommand = new SqlCommand(queryGetEndedAuctions, con))
                    {
                        using (var reader = await endedAuctionsCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int auctionId = (int)reader["AuctionId"];

                                if (await IsWinner(auctionId))
                                {
                                    IsWinnerNotification = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return Page();
        }

        private async Task<bool> IsWinner(int auctionId)
        {
            // Vai buscar os Id´s do vencedor do leilão e do cliente logado
            int? highestBidderClientId = await GetHighestBidderClientId(auctionId);
            int? loggedInClientId = await GetLoggedInClientId();

            // Verifica se o vencedor do leilão é o cliente que está logado
            return highestBidderClientId.HasValue && loggedInClientId.HasValue && highestBidderClientId.Value == loggedInClientId.Value;
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

        // Consulta para obter o ClientId do cliente logado
        private async Task<int?> GetLoggedInClientId()
        {
            if (User.Identity.IsAuthenticated)
            {
                string userEmail = User.Identity.Name;
                string queryGetClientId = "SELECT ClientId FROM Client WHERE Email = @UserEmail";

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    return await con.ExecuteScalarAsync<int?>(queryGetClientId, new { UserEmail = userEmail });
                }
            }

            return null;
        }
    }
}




