using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace BetterFindsProject.Pages
{
    public class SearchToolModel : PageModel
    {
        private readonly string _connectionString;

        // Usamos este construtor para obter a string conection que permite a conexão com a base de dados
        public SearchToolModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Lida com os dados expostos nesta página
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
        }

        public List<AuctionItem> Auctions { get; set; } = new List<AuctionItem>();

        public async Task<IActionResult> OnGetAsync(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // Vai buscar à base de dados.
                Auctions = await GetAuctionsFromDatabaseAsync(name);

                // Itera sobre os leilões e verifica se já acabaram
                foreach (var auction in Auctions)
                {
                    auction.IsAuctionEnded = DateTime.Now > auction.EndTime;
                }

                return Page();
            }
            else
            {
                return Page();
            }
        }

        // Vai buscar os leilões à base de dados apartir do nome e converte a informação numa lista
        private async Task<List<AuctionItem>> GetAuctionsFromDatabaseAsync(string name)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM Auction WHERE Name LIKE @name";
                var auctionItems = await connection.QueryAsync<AuctionItem>(sql, new { name = $"%{name}%" });

                Console.WriteLine($"SQL Query: {sql}, Parameters: {new { name = $"%{name}%" }}");

                return auctionItems.ToList();
            }
        }
    }
}
