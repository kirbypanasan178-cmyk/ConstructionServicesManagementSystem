using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Services
{
    public class ToolService : IToolService
    {
        private readonly AppDbContext _context;

        public ToolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tool>> GetAllToolsAsync()
        {
            return await _context.Tools
                .Include(t => t.Service)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tool?> GetToolByIdAsync(int id)
        {
            return await _context.Tools
                .Include(t => t.Service)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tool> CreateToolAsync(Tool tool)
        {
            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();

            return tool;
        }

        public async Task<bool> UpdateToolAsync(Tool tool)
        {
            var existingTool = await _context.Tools
                .FirstOrDefaultAsync(t => t.Id == tool.Id);

            if (existingTool == null)
                return false;

            existingTool.Name = tool.Name;
            existingTool.ServiceId = tool.ServiceId;
            existingTool.Quantity = tool.Quantity;
            existingTool.Description = tool.Description;
            existingTool.IsAvailable = tool.IsAvailable;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteToolAsync(int id)
        {
            var tool = await _context.Tools
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tool == null)
                return false;

            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}