using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IClientService
    {
        Task<Client?> GetClientByIdAsync(int id);

        Task<List<Client>> GetAllClientsAsync();

        Task<Client> CreateClientAsync(Client client);

        Task<bool> UpdateClientAsync(Client client);
    }
}