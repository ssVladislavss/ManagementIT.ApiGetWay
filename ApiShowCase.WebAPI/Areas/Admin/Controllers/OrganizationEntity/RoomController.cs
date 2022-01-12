using ApiShowCase.WebAPI.AddressConstatnts;
using ApplicationContracts.ViewModels.Application.ApplicationToItModels;
using ApplicationContracts.ViewModels.Application.ExistDependency;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.RoomViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class RoomController : Controller
    {
        private readonly IRequestClient<RoomViewModel> _getAll;
        private readonly IRequestClient<RoomByIdRequest> _getById;
        private readonly IRequestClient<CreateRoomViewModel> _create;
        private readonly IRequestClient<UpdateRoomViewModel> _update;
        private readonly IRequestClient<DeleteRoomRequest> _delete;
        private readonly IRequestClient<GetCreateRoomRequest> _getCreate;
        private readonly IRequestClient<GetUpdateRoomRequest> _getUpdate;
        private readonly IRequestClient<ExistDependenceEntityRequest> _existDependency;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public RoomController(IRequestClient<RoomViewModel> getAll,
            IRequestClient<RoomByIdRequest> getById,
            IRequestClient<CreateRoomViewModel> create,
            IRequestClient<UpdateRoomViewModel> update,
            IRequestClient<DeleteRoomRequest> delete,
            IRequestClient<GetCreateRoomRequest> getCreate,
            IRequestClient<GetUpdateRoomRequest> getUpdate,
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
        public async Task<ActionResult<NotificationViewModel<IEnumerable<RoomViewModel>>>> list(bool updateCache)
        {
            if (updateCache) _cache.Remove("listRoom");
            IEnumerable<RoomViewModel> roomCache = null;
            if (!_cache.TryGetValue("listRoom", out roomCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllRoomResponse>(new RoomViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Room + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listRoom", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<RoomViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Room + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<RoomViewModel>>(roomCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<RoomViewModel>>> Details(int id)
        {
            if(id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _getById.GetResponse<RoomByIdResponse>(new RoomByIdRequest { RoomId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<RoomViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel<CreateRoomViewModel>>> Create()
        {
            try
            {
                var result = await _getCreate.GetResponse<GetCreateRoomResponse>(new GetCreateRoomRequest());
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/getCreate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<CreateRoomViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/getCreate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateRoomViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel<UpdateRoomViewModel>>> Update(int id)
        {
            if(id == 0)
            {
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            }
            try
            {
                var result = await _getUpdate.GetResponse<GetUpdateRoomResponse>(new GetUpdateRoomRequest { RoomId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/getUpdate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<UpdateRoomViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/getUpdate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateRoomViewModel request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            }
            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message);
                }

                var updateApp = new UpdateRoomNameRequest()
                { RoomId = request.Id, RoomName = request.Name };
                await _publishEndpoint.Publish(updateApp);
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            if (id == 0) return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var exist = await _existDependency.GetResponse<ExistDepandencyEntityResponse>(new ExistDependenceEntityRequest { RoomId = id });
                if (!exist.Message.Exists)
                {
                    var message = new CreateLog(AddressConst.Room + "/delete",
                        $"Ошибка при удалении || Модель: < {typeof(RoomViewModel)} > || ID: < {id} > || Комната привязана к заявке",
                        NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.DeletionEntityError }));
                }

                var result = await _delete.GetResponse<NotificationViewModel>(new DeleteRoomRequest { RoomId = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Room + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Room + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }
    }
}
