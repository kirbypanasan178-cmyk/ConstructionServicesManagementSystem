using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IClientService
    {
        Task<Client?> GetClientByIdAsync(int id);

        Task<List<Client>> GetAllClientsAsync(); // for dropdowns/select boxes
        Task<List<Client>> GetAllClientsAsync(int pageNumber, int pageSize, string? search = null); // for paged lists

        Task<Client> CreateClientAsync(Client client);

        Task<bool> UpdateClientAsync(Client client);
    }
}