using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IGenericService<SaveViewModel, ViewModel, Model>
        where SaveViewModel : class
        where ViewModel : class
        where Model : class
    {
        Task<ViewModel> AddAsync(SaveViewModel vm);
        Task UpdateAsync(SaveViewModel vm, int id);
        Task DeleteAsync(int id);
        Task<SaveViewModel> GetByIdSaveViewModelAsync(int id);
        Task<ViewModel> GetByIdAsync(int id);
        Task<List<ViewModel>> GetAllViewModelAsync();
    }
}
