using AutoMapper;
using ABP.Core.Domain.Interfaces.IGenerics;
using ABP.Core.Application.Interfaces.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Services
{
    public class GenericService<SaveViewModel, ViewModel, Model> : IGenericService<SaveViewModel, ViewModel, Model>
        where SaveViewModel : class
        where ViewModel : class
        where Model : class
    {
        private readonly IGenericRepository<Model> _repository;
        private readonly IMapper _mapper;

        public GenericService(IGenericRepository<Model> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<ViewModel> AddAsync(SaveViewModel vm)
        {
            Model entity = _mapper.Map<Model>(vm);
            await _repository.AddAsync(entity);
            ViewModel entityVm = _mapper.Map<ViewModel>(entity);
            return entityVm;
        }

        public virtual async Task UpdateAsync(SaveViewModel vm, int id)
        {
            Model entity = _mapper.Map<Model>(vm);
            await _repository.UpdateAsync(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
                await _repository.DeleteAsync(entity);
        }

        public virtual async Task<SaveViewModel> GetByIdSaveViewModelAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            SaveViewModel vm = _mapper.Map<SaveViewModel>(entity);
            return vm;
        }

        public virtual async Task<ViewModel> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            ViewModel vm = _mapper.Map<ViewModel>(entity);
            return vm;
        }

        public virtual async Task<List<ViewModel>> GetAllViewModelAsync()
        {
            var entityList = await _repository.GetAllAsync();
            List<ViewModel> list = _mapper.Map<List<ViewModel>>(entityList);
            return list;
        }
    }
}
