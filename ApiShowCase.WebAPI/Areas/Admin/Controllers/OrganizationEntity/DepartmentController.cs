using ApiShowCase.WebAPI.AddressConstatnts;
using ApplicationContracts.ViewModels.Application.ApplicationToItModels;
using ApplicationContracts.ViewModels.Application.ExistDependency;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.DepartmentViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class DepartmentController : Controller
    {
        private readonly IRequestClient<DepartmentViewModel> _getAll;
        private readonly IRequestClient<DepartmentByIdRequest> _getById;
        private readonly IRequestClient<CreateDepartmentViewModel> _create;
        private readonly IRequestClient<UpdateDepartmentViewModel> _update;
        private readonly IRequestClient<DeleteDepartmentRequest> _delete;
        private readonly IRequestClient<GetCreateDepartmentRequest> _getCreate;
        private readonly IRequestClient<GetUpdateDepartmentRequest> _getUpdate;
        private readonly IRequestClient<ExistDependenceEntityRequest> _existDependency;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public DepartmentController(IRequestClient<DepartmentViewModel> getAll,
            IRequestClient<DepartmentByIdRequest> getById,
            IRequestClient<CreateDepartmentViewModel> create,
            IRequestClient<UpdateDepartmentViewModel> update,
            IRequestClient<DeleteDepartmentRequest> delete,
            IRequestClient<GetCreateDepartmentRequest> getCreate,
            IRequestClient<GetUpdateDepartmentRequest> getUpdate,
            IPublishEndpoint publishEndpoint,
            IRequestClient<ExistDependenceEntityRequest> existDependency,
            IMemoryCache cache)
        {
            _getAll = getAll ?? throw new ArgumentNullException(nameof(getAll));
            _getById = getById ?? throw new ArgumentNullException(nameof(getById));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _delete = delete ?? throw new ArgumentNullException(nameof(delete));
            _getCreate = getCreate ?? throw new ArgumentNullException(nameof(getCreate));
            _getUpdate = getUpdate ?? throw new ArgumentNullException(nameof(getUpdate));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _existDependency = existDependency ?? throw new ArgumentNullException(nameof(existDependency));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }


        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<DepartmentViewModel>>>> list(bool updateCache)
        {
            if (updateCache) _cache.Remove("listDepartment");
            IEnumerable<DepartmentViewModel> departmentCache = null;
            if (!_cache.TryGetValue("listDepartment", out departmentCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllDepartmentResponse>(new DepartmentViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Department + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listDepartment", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<DepartmentViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Department + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<DepartmentViewModel>>(departmentCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<DepartmentViewModel>>> Details(int id)
        {
            if(id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _getById.GetResponse<DepartmentByIdResponse>(new DepartmentByIdRequest { DepartmentId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<DepartmentViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel<CreateDepartmentViewModel>>> Create()
        {
            try
            {
                var result = await _getCreate.GetResponse<GetCreateDepartmentResponse>(new GetCreateDepartmentRequest());
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/getCreate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<CreateDepartmentViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/getCreate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateDepartmentViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel<UpdateDepartmentViewModel>>> Update(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _getUpdate.GetResponse<GetUpdateDepartmentResponse>(new GetUpdateDepartmentRequest { DepartmentId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/getUpdate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<UpdateDepartmentViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/getUpdate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateDepartmentViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message);
                }

                var updateApp = new UpdateDepartmentNameRequest()
                { DepartmentId = request.Id, DepartmentName = request.Name };
                await _publishEndpoint.Publish(updateApp);
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var exist = await _existDependency.GetResponse<ExistDepandencyEntityResponse>(new ExistDependenceEntityRequest { DepartmentId = id });
                if (!exist.Message.Exists)
                {
                    var message = new CreateLog(AddressConst.Department + "/delete", 
                        $"Ошибка при удалении || Модель: < {typeof(DepartmentViewModel)} > || ID: < {id} > || Отделение привязано к заявке",
                        NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.DeletionEntityError }));
                }
                var result = await _delete.GetResponse<NotificationViewModel>(new DeleteDepartmentRequest { DepartmentId = id });
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Department + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Department + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }
    }
}
