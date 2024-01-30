using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace BetterFindsProject.Pages
{
    // Indica-nos que apenas utilizadores autenticados podem aceder a esta página
    [Authorize]
    public class PerfilModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PerfilModel> _logger;

        public PerfilModel(IConfiguration configuration, ILogger<PerfilModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Lida com os dados expostos nesta página
        public class ClienteModel
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string UserName { get; set; }
        }

        public class AuctionItem
        {
            public int AuctionId { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }
            public int AuctionTime { get; set; }
            public string Description { get; set; }
            public DateTime EndTime { get; set; }
            public int MinimumBid { get; set; }
            public int ClientId { get; set; }
        }

        public ClienteModel Cliente { get; set; }
        public List<AuctionItem> Auctions { get; set; }

        // Verifica se o cliente está autenticado
        public void OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                // O utilizador não está autenticado, redireciona para a página de login
                RedirectToPage("/Login");
                return; 
            }

            // A partir dos cookies recupera o email
            string userEmail = User.Identity.Name;

            // String conection que permite a conexão com a base de dados
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Consulta do cliente com o Email do login
                string queryCliente = "SELECT * FROM Client WHERE Email = @Email";
                using (SqlCommand cmdCliente = new SqlCommand(queryCliente, con))
                {
                    cmdCliente.Parameters.AddWithValue("@Email", userEmail);
                    SqlDataReader readerCliente = cmdCliente.ExecuteReader();

                    if (readerCliente.Read())
                    {
                        // Obtém informações do cliente autenticado
                        Cliente = new ClienteModel
                        {
                            FullName = readerCliente.GetString(readerCliente.GetOrdinal("FullName")),
                            UserName = readerCliente.GetString(readerCliente.GetOrdinal("UserName")),
                            Email = readerCliente.GetString(readerCliente.GetOrdinal("Email"))
                        };
                    }

                    readerCliente.Close();
                }

                // Vai buscar os leilões criados pelo cliente logado e exibe no seu perfil essas informações
                string queryAuctions = "SELECT * FROM Auction WHERE ClientId = (SELECT ClientId FROM Client WHERE Email = @Email) ORDER BY AuctionTime ASC";
                using (SqlCommand cmdAuctions = new SqlCommand(queryAuctions, con))
                {
                    cmdAuctions.Parameters.AddWithValue("@Email", userEmail);
                    SqlDataReader readerAuctions = cmdAuctions.ExecuteReader();

                    Auctions = new List<AuctionItem>();
                    while (readerAuctions.Read())
                    {
                        // Obtém informações dos seus leilões
                        Auctions.Add(new AuctionItem
                        {
                            AuctionId = readerAuctions.GetInt32(readerAuctions.GetOrdinal("AuctionId")),
                            Name = readerAuctions.GetString(readerAuctions.GetOrdinal("Name")),
                            Price = readerAuctions.GetInt32(readerAuctions.GetOrdinal("Price")),
                            AuctionTime = readerAuctions.GetInt32(readerAuctions.GetOrdinal("AuctionTime")),
                            Description = readerAuctions.GetString(readerAuctions.GetOrdinal("Description")),
                            MinimumBid = readerAuctions.GetInt32(readerAuctions.GetOrdinal("MinimumBid")),
                            ClientId = readerAuctions.GetInt32(readerAuctions.GetOrdinal("ClientId"))
                        });
                    }

                    readerAuctions.Close();
                }
            }
        }
    }
}







